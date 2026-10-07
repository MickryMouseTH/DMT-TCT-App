#!/usr/bin/env python3
"""
LCSD Simulator — จำลอง Lane Controller (LCSD) สำหรับทดสอบ TCT_App

- WebSocket server ที่ TCT ต่อเข้ามา   : ws://<host>:58084/websocket/mtc  (JSON-RPC, subprotocol "JSONRPC")
    TCT -> LCSD : PING, RDIO, RDDI, WRDO, WRDI, WRTFI, WRCRS, RBELL, KEY, WRJOB ...
    LCSD -> TCT : IODI (Loop/OB/ALB...), KEY (ปุ่ม), AVC
- เฝ้าดูข้อมูลที่ TCT ส่งออก:
    Sup Adjust : ws://tct:9095   (LANE_TRANSACTION ...)
    RTO        : ws://tct:9094
    DC         : RabbitMQ queue (อ่านผ่าน management API)
- หน้า Control Panel : http://localhost:8090  (มีจอ TCT ผ่าน noVNC ฝังอยู่ด้วย)
"""
import asyncio
import base64
import json
import os
import time
from collections import deque
from pathlib import Path

import aiohttp
from aiohttp import web

LCSD_PORT       = int(os.environ.get("LCSD_PORT", "58084"))
PANEL_PORT      = int(os.environ.get("PANEL_PORT", "8090"))
TCT_HOST        = os.environ.get("TCT_HOST", "tct")
RTO_PORT        = int(os.environ.get("RTO_PORT", "9094"))
SUPADJ_PORT     = int(os.environ.get("SUPADJ_PORT", "9095"))
RABBIT_API      = os.environ.get("RABBIT_API", "http://rabbitmq:15672")
RABBIT_AUTH     = aiohttp.BasicAuth(os.environ.get("RABBIT_USER", "guest"), os.environ.get("RABBIT_PASS", "guest"))
DC_QUEUES       = [q for q in os.environ.get("DC_QUEUES", "").split(",") if q]
TA_PORT         = int(os.environ.get("TA_PORT", "3000"))
EDC_SEND_PORT   = int(os.environ.get("EDC_SEND_PORT", "2019"))   # TCT -> EDC (request)
EDC_RECV_PORT   = int(os.environ.get("EDC_RECV_PORT", "2018"))   # EDC -> TCT (response, TCT listens)
TCT_CONFIG      = Path(os.environ.get("TCT_CONFIG", "/tct-config/TCT.config"))


def load_dc_config():
    try:
        return json.loads(TCT_CONFIG.read_text(encoding="utf-8-sig")).get("DC_RabbitMQ", {})
    except Exception:
        return {}

# index ตาม Cmd_DI_Type ใน TCT_Lib.cs (และ LCSD.DI_x ใน TCT.config ของ sim)
DI_NAMES = {0: "MLB", 1: "EFS", 2: "PresenceOB", 3: "PassageLoop", 4: "PresenceLoop", 5: "PassageOB",
            6: "ALB", 7: "OTL", 8: "LTL", 9: "EthErr", 10: "Siren", 13: "TreadleError", 14: "TreadleC", 15: "TreadleD"}
DI_IDX   = {v: k for k, v in DI_NAMES.items()}
DO_NAMES = {0: "RegisterBell", 1: "Siren", 2: "ASB", 3: "LTL", 4: "ALB_Open", 5: "ALB_Close", 6: "OTL_RedGreen",
            7: "OTL_MTC_ETC", 8: "OTL_DualMode", 12: "CRS_Reset", 13: "TFI_Reset", 14: "Loop_Reset", 15: "IPC_Reset"}

di_values = [0] * 16
do_values = [0] * 16
tct_clients: set = set()
panel_clients: set = set()
logs: deque = deque(maxlen=5000)
seq_no = [0]
status = {"tct": False, "rto": False, "supadj": False, "dc": False}
counters = {"supadj_trans": 0, "rto_trans": 0, "dc_msgs": 0}


# ------------------------------------------------------------------ log / panel
async def broadcast(obj):
    data = json.dumps(obj, ensure_ascii=False)
    for ws in list(panel_clients):
        try:
            await ws.send_str(data)
        except Exception:
            panel_clients.discard(ws)


async def log(src, text, level="info", data=None):
    item = {"t": time.strftime("%H:%M:%S") + f".{int(time.time()*1000)%1000:03d}", "src": src,
            "text": text, "level": level}
    if data is not None:
        item["data"] = data
    seq_no[0] += 1
    item["seq"] = seq_no[0]
    logs.append(item)
    print(f"[{item['t']}] {src:7s} {text}", flush=True)
    await broadcast({"type": "log", "item": item})


async def push_state():
    await broadcast({"type": "state", "di": di_values, "do": do_values, "status": status, "counters": counters,
                     "edc": edc_public()})


# ------------------------------------------------------------------ to TCT
async def send_tct(method, params=None):
    msg = {"jsonrpc": "2.0", "id": 0, "method": method}
    if params is not None:
        msg["params"] = params
    if not tct_clients:
        await log("SIM", f"! ยังไม่มี TCT ต่อเข้ามา — ส่ง {method} ไม่ได้", "warn")
        return False
    for ws in list(tct_clients):
        await ws.send_str(json.dumps(msg))
    return True


async def set_di(name_or_idx, value, quiet=False):
    idx = DI_IDX[name_or_idx] if isinstance(name_or_idx, str) else int(name_or_idx)
    di_values[idx] = 1 if value else 0
    if not quiet:
        await log("SIM→TCT", f"IODI {DI_NAMES.get(idx, idx)} = {'ON' if value else 'OFF'}", "io")
    await send_tct("IODI", {"values": di_values})
    await push_state()


async def send_key(key_id, name=""):
    await log("SIM→TCT", f"KEY {name or key_id} (id={key_id})", "key")
    vehicle_from_key(name)
    await send_tct("KEY", {"id": int(key_id)})


async def send_avc(cls, axle=2, error=0):
    await log("SIM→TCT", f"AVC class={cls} axle={axle} error={error}", "avc")
    await send_tct("AVC", {"vehicleclass": int(cls), "direction": 0, "axle": int(axle), "wheel": int(axle) * 2,
                           "dualwheel": 0, "height": 0, "error": int(error)})


# ------------------------------------------------------------------ LCSD websocket (TCT connects here)
async def lcsd_ws(request):
    ws = web.WebSocketResponse(protocols=("JSONRPC",), heartbeat=None)
    await ws.prepare(request)
    tct_clients.add(ws)
    status["tct"] = True
    await log("SIM", f"+ TCT ต่อเข้ามาที่ LCSD ({request.remote})", "ok")
    await push_state()
    try:
        async for m in ws:
            if m.type != aiohttp.WSMsgType.TEXT:
                continue
            try:
                req = json.loads(m.data)
            except Exception:
                await log("TCT→SIM", f"? ไม่ใช่ JSON: {m.data[:200]}", "warn")
                continue
            await handle_tct_request(ws, req)
    finally:
        tct_clients.discard(ws)
        status["tct"] = bool(tct_clients)
        await log("SIM", "- TCT หลุดจาก LCSD", "warn")
        await push_state()
    return ws


async def handle_tct_request(ws, req):
    method = (req.get("method") or "").upper()
    params = req.get("params") or {}
    rid = req.get("id", 0)
    result = "OK"

    if method == "PING":
        result = "PONG"
    elif method in ("RDIO", "RDDI"):
        result = {"IODI": {"values": di_values}, "IODO": {"values": do_values}}
        await log("TCT→SIM", f"{method} (ขอสถานะ IO)", "dim")
    elif method == "WRDO":
        i, v = int(params.get("id", -1)), int(params.get("value", 0))
        if 0 <= i < 16:
            do_values[i] = v
        await log("TCT→SIM", f"WRDO {DO_NAMES.get(i, i)} = {v}", "do")
        # จำลองอุปกรณ์จริง: ไม้กั้น / ไฟ LTL สะท้อนกลับเป็น DI
        if i == 4 and v == 1:
            asyncio.create_task(delayed_di("ALB", 1))
        elif i == 5 and v == 1:
            asyncio.create_task(delayed_di("ALB", 0))
        elif i == 3:
            asyncio.create_task(delayed_di("LTL", v))
        elif i == 6:
            asyncio.create_task(delayed_di("OTL", v))
        await push_state()
    elif method == "WRDI":            # ใช้เฉพาะ TCT ที่ build แบบ __SIM__
        i, v = int(params.get("id", -1)), int(params.get("value", 0))
        await log("TCT→SIM", f"WRDI {DI_NAMES.get(i, i)} = {v}", "io")
        asyncio.create_task(set_di(i, v, quiet=True))
    elif method == "KEY":
        await log("TCT→SIM", f"KEY (ปุ่มบนจอ) keyId={params.get('keyId', params.get('id'))}", "dim")
    elif method == "WRTFI":
        await log("TCT→SIM", "TFI " + " | ".join(str(params.get(k, "")) for k in params if params.get(k) not in ("", None)), "dim")
    elif method == "WRCRS":
        await log("TCT→SIM", "CRS " + json.dumps(params, ensure_ascii=False), "dim")
    elif method == "RBELL":
        await log("TCT→SIM", "RBELL (กริ่ง)", "dim")
    else:
        await log("TCT→SIM", f"{method} {json.dumps(params, ensure_ascii=False)[:300]}", "dim")

    await ws.send_str(json.dumps({"jsonrpc": "2.0", "id": rid, "result": result}))


async def delayed_di(name, v, delay=0.3):
    await asyncio.sleep(delay)
    if di_values[DI_IDX[name]] != v:
        await set_di(name, v)


# ------------------------------------------------------------------ observers: Sup Adjust / RTO / DC
def summarize_trans(obj):
    p = obj.get("params") if isinstance(obj, dict) else None
    if isinstance(p, str):
        try:
            p = json.loads(p)
        except Exception:
            pass
    if not isinstance(p, dict):
        return json.dumps(obj, ensure_ascii=False)[:300]
    if "tcClass" in p:
        pay, sen = p.get("payInfo") or {}, p.get("sensorStatus") or {}
        return (f"id={p.get('transactionId')} nTrx={p.get('nTrx')} class={p.get('tcClass')} "
                f"pay={pay.get('payTypeName')} loop={sen.get('loopStatus')} ob={sen.get('obStatus')} "
                f"avc={sen.get('avcStatus')} remark={p.get('transRemark')}")
    return json.dumps(p, ensure_ascii=False)[:400]


async def watch_ws(name, port, key):
    url = f"ws://{TCT_HOST}:{port}/"
    while True:
        try:
            async with aiohttp.ClientSession() as s:
                async with s.ws_connect(url, heartbeat=None, timeout=5) as ws:
                    status[key] = True
                    await log(name, f"+ ต่อ {url} ได้แล้ว", "ok")
                    await push_state()
                    async for m in ws:
                        if m.type != aiohttp.WSMsgType.TEXT:
                            continue
                        try:
                            obj = json.loads(m.data)
                        except Exception:
                            obj = {"raw": m.data}
                        method = obj.get("method", "?") if isinstance(obj, dict) else "?"
                        if method == "LANE_TRANSACTION":
                            counters[f"{key}_trans"] += 1
                            await log(name, f"★ LANE_TRANSACTION {summarize_trans(obj)}", "trans", obj)
                            await push_state()
                        elif key == "supadj" or method not in ("IODI", "LANE_EVENT", "TFI", "CRS", "KEY", "AVC"):
                            await log(name, f"{method} {json.dumps(obj.get('params'), ensure_ascii=False)[:200]}", "dim", obj)
        except Exception as e:
            if status[key]:
                await log(name, f"- หลุด {url}: {e.__class__.__name__}", "warn")
            status[key] = False
            await push_state()
        await asyncio.sleep(3)


async def setup_dc_topology(s, dc):
    """สร้าง exchange/queue/binding ของ DC เหมือนฝั่ง DataCenter จริง
    (TCT consume qm.parameters.* โดยไม่ declare เอง — ถ้าไม่มี channel จะถูกปิด แล้วส่ง trans ไม่ได้)"""
    for kind in ("Trans", "Event", "Params"):
        q, ex, rk = dc.get(f"{kind}QueueName"), dc.get(f"{kind}ExchangeName"), dc.get(f"{kind}RoutingKey", "")
        if not q or not ex:
            continue
        await s.put(f"{RABBIT_API}/api/exchanges/%2F/{ex}", json={"type": "direct", "durable": True})
        await s.put(f"{RABBIT_API}/api/queues/%2F/{q}", json={"durable": True})
        await s.post(f"{RABBIT_API}/api/bindings/%2F/e/{ex}/q/{q}", json={"routing_key": rk or ""})
        await log("DC", f"+ เตรียม DC: exchange {ex} -> queue {q}", "dim")


async def watch_dc():
    dc = load_dc_config()
    if not DC_QUEUES and dc.get("TransQueueName"):
        DC_QUEUES.append(dc["TransQueueName"])
    if not DC_QUEUES:
        return
    async with aiohttp.ClientSession(auth=RABBIT_AUTH) as s:
        for _ in range(30):
            try:
                await setup_dc_topology(s, dc)
                break
            except Exception:
                await asyncio.sleep(2)
        while True:
            ok = False
            for q in DC_QUEUES:
                url = f"{RABBIT_API}/api/queues/%2F/{q}/get"
                try:
                    async with s.post(url, json={"count": 50, "ackmode": "ack_requeue_false", "encoding": "auto"},
                                      timeout=5) as r:
                        ok = True
                        if r.status == 404:
                            continue           # TCT ยังไม่ได้ declare queue (ยังไม่ส่งอะไร)
                        if r.status != 200:
                            continue
                        for msg in await r.json():
                            payload = msg.get("payload", "")
                            if msg.get("payload_encoding") == "base64":
                                payload = base64.b64decode(payload).decode("utf-8", "replace")
                            try:
                                obj = json.loads(payload)
                            except Exception:
                                obj = {"raw": payload}
                            # รายการเก็บเงินไป DC = CompiledPassageRecord (ข้างในมี laneTransMessage แบบเดียวกับ Sup Adjust)
                            ltm = (obj.get("params") or {}).get("laneTransMessage") \
                                if isinstance(obj, dict) and obj.get("method") == "CompiledPassageRecord" else None
                            if isinstance(ltm, dict):
                                counters["dc_msgs"] += 1
                                await log("DC", f"★ CompiledPassageRecord {summarize_trans({'params': ltm})}", "trans",
                                          {"method": "CompiledPassageRecord", "params": ltm, "raw": obj})
                            else:
                                m = obj.get("method", "?") if isinstance(obj, dict) else "?"
                                await log("DC", f"{m} ({q})", "dim", obj)
                except Exception:
                    pass
            if status["dc"] != ok:
                status["dc"] = ok
                await log("DC", "+ เชื่อม RabbitMQ (DC) ได้" if ok else "- เชื่อม RabbitMQ (DC) ไม่ได้", "ok" if ok else "warn")
            await push_state()
            await asyncio.sleep(2)


# ------------------------------------------------------------------ scenarios
async def scenario(name):
    steps = {
        # รถผ่านปกติ: Loop ON -> OB ON -> Loop OFF -> OB OFF  (แล้วรอ Auto AVC / ส่ง AVC เอง)
        "normal": [("PresenceLoop", 1), ("PassageLoop", 1), 0.8, ("PassageOB", 1), 0.6, ("PassageLoop", 0),
                   ("PresenceLoop", 0), 0.6, ("PassageOB", 0)],
        # Bug H1/H2: รถผ่าน OB อย่างเดียว ไม่ทับ Loop
        "ob_only": [("PassageOB", 1), 1.0, ("PassageOB", 0)],
        # Loop + OB ค้าง (รถจอดทับ)
        "loop_ob_stuck": [("PassageLoop", 1), 0.5, ("PassageOB", 1)],
        # Loop อย่างเดียว ไม่มี OB
        "loop_only": [("PassageLoop", 1), 1.0, ("PassageLoop", 0)],
        "clear_io": [("PassageOB", 0), ("PassageLoop", 0), ("PresenceLoop", 0), ("PresenceOB", 0)],
    }[name]
    await log("SIM", f"▶ Scenario: {name}", "scn")
    for st in steps:
        if isinstance(st, (int, float)):
            await asyncio.sleep(st)
        else:
            await set_di(st[0], st[1])
    await log("SIM", f"■ Scenario {name} จบ", "scn")


# ------------------------------------------------------------------ HTTP API / panel
async def api(request):
    body = await request.json()
    act = body.get("action")
    if act == "di":
        await set_di(int(body["idx"]), int(body["value"]))
    elif act == "key":
        await send_key(int(body["id"]), body.get("name", ""))
    elif act == "keys":                       # ส่งหลายปุ่มต่อกัน (เช่นพิมพ์ตัวเลข)
        for k in body["ids"]:
            await send_key(int(k))
            await asyncio.sleep(0.35)         # มากกว่า KeyStrokeTimeMilliSec (300ms)
    elif act == "avc":
        await send_avc(body.get("cls", 1), body.get("axle", 2), body.get("error", 0))
    elif act == "scenario":
        asyncio.create_task(scenario(body["name"]))
    elif act == "kb":                         # คีย์บอร์ดจำลอง -> xdotool ในเครื่อง TCT (เส้นทาง USB จริง)
        k, name = body.get("k"), body.get("name", "")
        vehicle_from_key(name)
        if not k:
            await log("KB", f"[{name}] — ไม่มีใน keyMapping ของ TCT (ไม่ส่ง)", "warn")
        else:
            try:
                async with aiohttp.ClientSession() as s:
                    async with s.get(f"http://{TCT_HOST}:6081/key", params={"k": k}, timeout=5) as r:
                        ok = r.status == 200
                await log("KB", f"[{name}] → {k}" + ("" if ok else " (ส่งไม่สำเร็จ)"), "key" if ok else "warn")
            except Exception as e:
                await log("KB", f"[{name}] ส่งไม่ได้: {e.__class__.__name__}", "warn")
    elif act == "click":                      # แตะจอ TCT
        async with aiohttp.ClientSession() as s:
            async with s.get(f"http://{TCT_HOST}:6081/click", params={"x": int(body["x"]), "y": int(body["y"])}, timeout=5):
                pass
        await log("TOUCH", f"แตะจอที่ ({body['x']},{body['y']})", "key")
    elif act == "edc":                        # ตั้งโหมด EDC จำลอง
        if body.get("mode"):
            edc["mode"] = body["mode"]
        if body.get("qr"):
            edc["qr"] = body["qr"]
        if "auto" in body and body["auto"] is not None:
            edc["auto"] = bool(body["auto"])
        await log("EDC", f"⚙ โหมด EDC = {edc['mode']}, QR = {edc['qr']}, auto = {edc['auto']}", "scn")
        await push_state()
    elif act == "edc_card":                   # หน้า /edc: ลูกค้าแตะบัตร / ปฏิเสธ / ไม่ตอบ
        pc = edc["pending_card"]
        if pc:
            await edc_card_result(body.get("result", "approve"), pc["peer"], pc["f"], pc["amt"])
    elif act == "edc_qr_pay":                 # หน้า /edc: ลูกค้าสแกน QR จ่ายแล้ว
        edc["qr"] = "paid"
        await edc_show(edc["screen"], "ลูกค้าสแกนจ่ายแล้ว — กด [ENTER] ที่ TCT", None)
        await log("EDC", "ลูกค้าสแกน QR จ่ายแล้ว (รอ TCT ตรวจสอบ)", "edc")
    elif act == "scr":                        # แตะบัตรพนักงาน (Smart Card Reader)
        await log("SIM→TCT", f"SCR บัตร {body['card']}", "key")
        await send_tct("SCR", {"id": str(body["card"])})
    elif act == "raw":
        await send_tct(body["method"], body.get("params"))
    elif act == "clear":
        logs.clear()
        for k in counters:
            counters[k] = 0
        await broadcast({"type": "clear"})
        await push_state()
    return web.json_response({"ok": True})


# ------------------------------------------------------------------ TA Web Service จำลอง (คูปอง / LaneAttendance / ภาพ)
TA_COUPONS = [{"CouponPK": (9000 if t == "35" else 9500) + i, "CouponType": t, "SerialNo": f"SIM{t}-{i:04d}", "Price": float(t)}
              for t in ("35", "80") for i in range(1, 11)]
ta_sold = []


def ta_ok(data=None):
    return web.json_response({"errors": {"hasError": False, "errMsg": ""}, "data": data if data is not None else []})


async def ta_handler(request):
    path = request.path
    try:
        body = await request.json()
    except Exception:
        body = {}
    if path.endswith("/coupons/search"):
        sold = {c["serialno"] for c in ta_sold}
        data = [c for c in TA_COUPONS if c["SerialNo"] not in sold]
        await log("TA", f"GetCoupons → ส่งคูปองคงเหลือ {len(data)} ใบ", "dim", body)
        return ta_ok(data)
    if path.endswith("/coupons/sold"):
        ta_sold.append(body)
        counters["coupon_sold"] = counters.get("coupon_sold", 0) + 1
        await log("TA", f"★ SoldCoupon serial={body.get('serialno')} type={body.get('coupontype')} "
                        f"price={body.get('price')} pay={body.get('paytypename')}", "trans", body)
        await push_state()
        return ta_ok()
    if path.endswith("/CheckTODBoj"):
        await log("TA", "CheckTODBoj → อนุญาต", "dim", body)
        return ta_ok([{"staffShiftId": 1}])
    if path.endswith("/LaneAttendance/save"):
        await log("TA", f"LaneAttendance {json.dumps(body, ensure_ascii=False)[:160]}", "dim", body)
        return ta_ok()
    await log("TA", f"{request.method} {path} ({len(json.dumps(body))} bytes)", "dim")
    return ta_ok()


# ------------------------------------------------------------------ กล้องจำลอง (IP camera) + Web API รับภาพ
# TCT (CaptureImageMethod 1) GET http://<sim>:3000/cam/{1,2,3}.jpg ตอนรับชำระ -> ย่อ/บันทึก JPEG
# หลังเขียนรายการผ่านทาง TCT POST /saveImages (JSON + base64) -> เก็บไว้ใน CAP_DIR แล้วโชว์ที่หน้า Panel
CAM_W, CAM_H    = int(os.environ.get("CAM_W", "1920")), int(os.environ.get("CAM_H", "1080"))
CAP_DIR         = Path(os.environ.get("CAP_DIR", "/tmp/captures"))
CAM_NAMES       = {1: "CAM1 FRONT", 2: "CAM2 REAR", 3: "CAM3 OVERVIEW"}
THAI_CHARS      = "กขคงจฉชซฌญฎฐฒณดตถทธนบปผพฟภมยรลวศษสหฬอฮ"
FONT_FILES      = ["/usr/share/fonts/truetype/tlwg/Loma-Bold.ttf", "/usr/share/fonts/truetype/tlwg/Garuda-Bold.ttf",
                   "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"]
cam_vehicle     = {"plate": "1กข 2345", "province": "กรุงเทพมหานคร", "kind": 1, "color": (200, 40, 40), "seq": 0}
captures        = deque(maxlen=12)        # รายการที่ TCT อัปโหลดล่าสุด


def new_vehicle(kind=None):
    import random
    cam_vehicle["seq"] += 1
    cam_vehicle["kind"] = kind or cam_vehicle["kind"]
    cam_vehicle["plate"] = f"{random.randint(1, 9)}{random.choice(THAI_CHARS)}{random.choice(THAI_CHARS)} {random.randint(1000, 9999)}"
    cam_vehicle["province"] = random.choice(["กรุงเทพมหานคร", "นนทบุรี", "ปทุมธานี", "ชลบุรี", "เชียงใหม่"])
    cam_vehicle["color"] = random.choice([(200, 40, 40), (30, 70, 160), (230, 230, 230), (40, 40, 45), (150, 150, 155)])


def vehicle_from_key(name):
    """ปุ่มประเภทรถ / ยกเว้น / ลากจูง = รถคันใหม่มาหน้ากล้อง (ทะเบียน/สีใหม่)"""
    n = str(name)
    if any(w in n for w in ("ประเภท", "Class", "ยกเว้น", "Exempt", "ลากจูง", "Towing")):
        new_vehicle(2 if ("2" in n or "ลากจูง" in n or "Towing" in n) else 1)


def _font(size):
    from PIL import ImageFont
    for f in FONT_FILES:
        if Path(f).exists():
            return ImageFont.truetype(f, size)
    return ImageFont.load_default()


def render_camera(idx):
    """ภาพเหมือนกล้องหน้าช่อง 1920x1080: ถนน ช่องทาง รถ ป้ายทะเบียน + แถบเวลา (ขนาดเท่ากล้องจริง เพื่อทดสอบการย่อภาพ)"""
    from io import BytesIO
    from PIL import Image, ImageDraw
    W, H = CAM_W, CAM_H
    im = Image.new("RGB", (W, H), (88, 92, 98))
    d = ImageDraw.Draw(im)
    for y in range(0, int(H * 0.38)):                                   # ฟ้า / ตัวอาคารด่าน
        c = 150 + int(70 * y / (H * 0.38))
        d.line((0, y, W, y), fill=(c - 40, c - 20, c))
    d.rectangle((0, int(H * 0.2), W, int(H * 0.38)), fill=(120, 110, 100))
    d.rectangle((0, int(H * 0.16), W, int(H * 0.21)), fill=(30, 90, 160))
    d.text((40, int(H * 0.185)), "ด่านเก็บเงิน DM1-T  ช่องทาง SIM01", font=_font(int(H * 0.024)), fill=(255, 255, 255), anchor="lm")
    for x in (0.18, 0.82):                                              # เส้นแบ่งช่องทาง
        d.polygon([(W * x - 8, H * 0.38), (W * x + 8, H * 0.38), (W * (x * 1.6 - 0.3) + 30, H),
                   (W * (x * 1.6 - 0.3) - 30, H)], fill=(235, 235, 220))
    kind, col = cam_vehicle["kind"], cam_vehicle["color"]
    dark = tuple(max(0, v - 60) for v in col)
    cx, base = W // 2, int(H * 0.86)
    bw = int(W * (0.46 if kind == 2 else 0.38))
    bh = int(H * (0.50 if kind == 2 else 0.30))
    if idx == 3:                                                        # มุมกว้าง: รถเล็กลง เยื้องขวา
        bw, bh, cx, base = int(bw * 0.55), int(bh * 0.55), int(W * 0.62), int(H * 0.78)
    x0, x1, y1 = cx - bw // 2, cx + bw // 2, base
    y0 = y1 - bh
    d.ellipse((x0 - 30, y1 - 25, x1 + 30, y1 + 35), fill=(50, 52, 55))  # เงา
    if kind == 2:
        d.rectangle((x0, y0, x1, y1 - int(bh * 0.12)), fill=col, outline=dark, width=6)          # ตัวรถบรรทุก
        d.rectangle((x0 + bw * 0.1, y0 + bh * 0.08, x1 - bw * 0.1, y0 + bh * 0.42), fill=(150, 190, 215), outline=dark, width=5)
    else:
        d.rounded_rectangle((x0, y0 + bh * 0.38, x1, y1 - bh * 0.1), 40, fill=col, outline=dark, width=6)
        d.polygon([(x0 + bw * 0.16, y0 + bh * 0.40), (x0 + bw * 0.27, y0), (x1 - bw * 0.27, y0), (x1 - bw * 0.16, y0 + bh * 0.40)],
                  fill=dark)
        d.polygon([(x0 + bw * 0.21, y0 + bh * 0.37), (x0 + bw * 0.30, y0 + bh * 0.07), (x1 - bw * 0.30, y0 + bh * 0.07),
                   (x1 - bw * 0.21, y0 + bh * 0.37)], fill=(140, 180, 205) if idx != 2 else (60, 70, 80))
    lamp = (255, 245, 200) if idx in (1, 3) else (230, 30, 30)          # หน้า = ไฟหน้า, หลัง = ไฟท้าย
    ly = y1 - bh * 0.30
    d.ellipse((x0 + bw * 0.04, ly - bh * 0.07, x0 + bw * 0.17, ly + bh * 0.05), fill=lamp, outline=dark, width=4)
    d.ellipse((x1 - bw * 0.17, ly - bh * 0.07, x1 - bw * 0.04, ly + bh * 0.05), fill=lamp, outline=dark, width=4)
    for wx in (x0 + bw * 0.06, x1 - bw * 0.20):                          # ล้อ
        d.rectangle((wx, y1 - bh * 0.12, wx + bw * 0.14, y1 + 10), fill=(20, 20, 22))
    pw, ph = int(bw * 0.34), int(bw * 0.34 * 0.42)                        # ป้ายทะเบียน
    px, py = cx - pw // 2, int(y1 - bh * 0.27)
    d.rounded_rectangle((px, py, px + pw, py + ph), 10, fill=(250, 250, 250), outline=(10, 10, 10), width=max(3, pw // 90))
    f1, f2 = _font(int(ph * 0.48)), _font(int(ph * 0.22))
    d.text((cx, py + ph * 0.36), cam_vehicle["plate"], font=f1, fill=(10, 10, 10), anchor="mm")
    d.text((cx, py + ph * 0.80), cam_vehicle["province"], font=f2, fill=(10, 10, 10), anchor="mm")
    stamp = time.strftime("%Y-%m-%d %H:%M:%S") + f".{int(time.time() * 1000) % 1000:03d}"     # แถบข้อมูลกล้อง
    fs = _font(int(H * 0.03))
    d.rectangle((0, 0, W, int(H * 0.06)), fill=(0, 0, 0))
    d.text((24, int(H * 0.012)), f"{CAM_NAMES[idx]}   {stamp}   #{cam_vehicle['seq']}", font=fs, fill=(255, 255, 0))
    d.text((W - 24, int(H * 0.012)), f"SIM {W}x{H}", font=fs, fill=(180, 180, 180), anchor="ra")
    buf = BytesIO()
    im.save(buf, "JPEG", quality=92)
    return buf.getvalue()


async def cam_handler(request):
    idx = int(request.match_info["idx"])
    if idx not in CAM_NAMES:
        raise web.HTTPNotFound()
    data = await asyncio.get_running_loop().run_in_executor(None, render_camera, idx)
    counters["cam_shots"] = counters.get("cam_shots", 0) + 1
    await log("CAM", f"📷 {CAM_NAMES[idx]} ถ่ายภาพ {CAM_W}x{CAM_H} ({len(data) // 1024} KB) ทะเบียน {cam_vehicle['plate']}", "dim")
    return web.Response(body=data, content_type="image/jpeg")


async def save_images_handler(request):
    """Web API รับภาพของรายการผ่านทาง (เหมือน server จริง: ตอบ respCode 0000)"""
    from io import BytesIO
    from PIL import Image
    try:
        body = await request.json()
    except Exception:
        return web.json_response({"respCode": "9001", "respMesg": "invalid json"})
    tr, imgs = body.get("transaction") or {}, []
    CAP_DIR.mkdir(parents=True, exist_ok=True)
    for c in body.get("captureImages") or []:
        b64 = c.get("imageData") or ""
        if not b64:
            imgs.append({"cameraIndex": c.get("cameraIndex"), "fileName": c.get("fileName") or "", "missing": True})
            continue
        raw = base64.b64decode(b64)
        name = f"{body.get('requestId', 'IMG')}_{c.get('cameraIndex')}.jpg"
        (CAP_DIR / name).write_bytes(raw)
        try:
            w, h = Image.open(BytesIO(raw)).size
        except Exception:
            w = h = 0
        imgs.append({"cameraIndex": c.get("cameraIndex"), "fileName": c.get("fileName"), "url": f"/cap/{name}",
                     "w": w, "h": h, "kb": round(len(raw) / 1024, 1)})
    item = {"requestId": body.get("requestId"), "jobNo": tr.get("jobNo"), "nTrx": tr.get("nTrx"),
            "time": tr.get("transactionDateTime") or body.get("requestDateTime"), "plate": cam_vehicle["plate"],
            "auth": request.headers.get("Authorization", "")[:6], "images": imgs}
    captures.appendleft(item)
    counters["cap_upload"] = counters.get("cap_upload", 0) + 1
    got = [i for i in imgs if not i.get("missing")]
    desc = ", ".join(f"#{i['cameraIndex']} {i['w']}x{i['h']} {i['kb']}KB" for i in got) or "ไม่มีภาพ"
    await log("NVR", f"★ รับภาพรายการ Job {item['jobNo']} nTrx {item['nTrx']}: {len(got)}/{len(imgs)} ภาพ ({desc})", "trans", item)
    await broadcast({"type": "captures", "items": list(captures)})
    await push_state()
    return web.json_response({"respCode": "0000", "respMesg": "Success"})


async def cap_file(request):
    f = CAP_DIR / Path(request.match_info["name"]).name
    if not f.exists():
        raise web.HTTPNotFound()
    return web.FileResponse(f)


# ------------------------------------------------------------------ EDC จำลอง (โปรโตคอล EDC V1 ของ TCT)
# frame = STX | LEN(2B BCD) | DATA | ETX | LRC(XOR STX..ETX)
# DATA  = "0000000000" + "1" + req/resp + code(2) + resp(2) + "0" + 0x1C + fields
# field = ID(2) | LEN(2B BCD) | value | 0x1C
# request: TCT ต่อ EDC:2019 ส่ง 1 frame -> EDC ตอบ ACK 1 byte แล้วปิด
# response: EDC ต่อกลับไป TCT:2018 ส่ง 1 frame -> TCT ตอบ ACK 1 byte
EDC_ACK = {"20": 0x06, "QR": 0x06, "IQ": 0x06, "CP": 0x05, "S1": 0x08, "H1": 0x09}
edc = {"mode": "approve",       # approve | decline | timeout | busy | offline
       "qr": "paid",            # paid | unpaid
       "invoice": 100, "pending_qr": None, "tid": "12345678",
       "auto": True,            # True = ตอบเอง | False = รอคนกดบนหน้า /edc
       "pending_card": None,    # รายการบัตรที่รอคนกดบนหน้า /edc
       "screen": "พร้อมใช้งาน", "screen2": "", "last": ""}


async def edc_show(line1, line2="", last=None):
    edc["screen"], edc["screen2"] = line1, line2
    if last is not None:
        edc["last"] = last
    await push_state()


def edc_public():
    q = edc["pending_qr"]
    return {"mode": edc["mode"], "qr": edc["qr"], "auto": edc["auto"],
            "screen": edc["screen"], "screen2": edc["screen2"], "last": edc["last"],
            "card_wait": bool(edc["pending_card"]),
            "qr_wait": bool(q), "qr_text": (f"SIMQR|{q['A1']}|{q['amt']}" if q else "")}


def bcd2(n):
    s = f"{n:04d}"
    return bytes([int(s[0:2], 16), int(s[2:4], 16)])


def edc_frame(code, resp, fields):
    data = b"0000000000" + b"1" + b"1" + code.encode() + resp.encode() + b"0" + b"\x1c"
    for fid, val in fields:
        v = str(val).encode("ascii", "replace")
        data += fid.encode() + bcd2(len(v)) + v + b"\x1c"
    frame = b"\x02" + bcd2(len(data)) + data + b"\x03"
    lrc = 0
    for b in frame:
        lrc ^= b
    return frame + bytes([lrc])


def edc_parse(frame):
    """คืน (code, fields dict) จาก request ของ TCT"""
    if len(frame) < 20 or frame[0] != 0x02:
        return None, {}
    code = frame[15:17].decode("ascii", "replace")
    body = frame[21:]
    fields, i = {}, 0
    while i + 4 <= len(body) and body[i] != 0x03:
        fid = body[i:i + 2].decode("ascii", "replace")
        ln = int(f"{body[i + 2]:02x}{body[i + 3]:02x}")
        fields[fid] = body[i + 4:i + 4 + ln].decode("ascii", "replace")
        i += 4 + ln + 1
    return code, fields


async def edc_send_back(host, frame, label, delay=0.4):
    await asyncio.sleep(delay)
    try:
        r, w = await asyncio.wait_for(asyncio.open_connection(host, EDC_RECV_PORT), 3)
        w.write(frame)
        await w.drain()
        try:
            ack = await asyncio.wait_for(r.read(1), 3)
            ack_s = f"0x{ack[0]:02X}" if ack else "ไม่มี"
        except asyncio.TimeoutError:
            ack_s = "timeout"
        w.close()
        await log("EDC", f"→ TCT:{EDC_RECV_PORT} {label} (TCT ACK {ack_s})", "edc")
    except Exception as e:
        await log("EDC", f"- ส่งผลกลับ TCT:{EDC_RECV_PORT} ไม่ได้ ({e.__class__.__name__}) — {label}", "warn")


def edc_now():
    t = time.localtime()
    return time.strftime("%y%m%d", t), time.strftime("%H%M%S", t)


async def edc_handle(reader, writer):
    peer = writer.get_extra_info("peername")[0]
    try:
        frame = await asyncio.wait_for(reader.read(2048), 3)
    except asyncio.TimeoutError:
        frame = b""
    if not frame:                                    # TCT ตรวจ port (probe) — เปิดแล้วปิด
        writer.close()
        return
    if edc["mode"] == "offline":
        writer.close()
        await log("EDC", "✕ โหมด offline — ปิดการเชื่อมต่อโดยไม่ตอบ", "warn")
        return
    code, f = edc_parse(frame)
    ack = 0x04 if edc["mode"] == "busy" else EDC_ACK.get(code, 0x06)
    writer.write(bytes([ack]))
    await writer.drain()
    writer.close()
    amt = f.get("40", "")
    baht = f"{int(amt) / 100:.2f}" if amt.isdigit() else amt
    await log("EDC", f"← TCT ขอ {code} amount={baht} ref1={f.get('A1', '')} → ACK 0x{ack:02X}", "edc", f)
    if edc["mode"] == "busy":
        return
    d, t = edc_now()

    if code == "20":                                 # ชำระด้วยบัตร
        if not edc["auto"]:                          # รอคน "แตะบัตร" บนหน้า /edc
            edc["pending_card"] = {"peer": peer, "f": f, "amt": amt}
            await edc_show(f"ยอด {baht} บาท", "กรุณาแตะ / เสียบบัตร")
            await log("EDC", f"รอลูกค้าแตะบัตรบนหน้า EDC ({baht} บาท)", "edc")
            return
        await edc_card_result(edc["mode"], peer, f, amt)

    elif code == "QR":                               # เริ่มจ่าย QR — EDC แสดง QR เอง ยังไม่ส่งผล
        edc["pending_qr"] = {"amt": amt, "A1": f.get("A1", ""), "A2": f.get("A2", ""), "peer": peer}
        if not edc["auto"]:
            edc["qr"] = "unpaid"
        await edc_show(f"QR {baht} บาท", "สแกนเพื่อชำระ")
        await log("EDC", f"แสดง QR {baht} บาท บนเครื่อง EDC (รอลูกค้าสแกน) — กด [ENTER] ที่ TCT เพื่อตรวจสอบ", "edc")

    elif code == "IQ":                               # ตรวจสอบการจ่าย QR
        q = edc["pending_qr"] or {"amt": "", "A1": "", "A2": ""}
        if edc["mode"] == "timeout":
            await log("EDC", "… โหมด timeout — ไม่ตอบผลตรวจสอบ QR", "warn")
            return
        if edc["qr"] == "unpaid" or edc["mode"] == "decline":
            fr = edc_frame("IQ", "12", [("02", "NOT PAID")])
            await edc_show(edc["screen"], "ยังไม่ได้รับชำระ", "QR ยังไม่ชำระ")
            asyncio.create_task(edc_send_back(peer, fr, "QR ยังไม่ชำระ (12)"))
            return
        edc["invoice"] += 1
        fr = edc_frame("IQ", "00", [
            ("02", "QR APPROVED"), ("01", f"{edc['invoice'] % 1000000:06d}"), ("03", d), ("04", t),
            ("16", edc["tid"]), ("40", q["amt"]), ("65", f"{edc['invoice'] % 1000000:06d}"),
            ("A1", q["A1"]), ("A2", q["A2"]), ("A3", f"SIMQR{edc['invoice']:06d}")])
        edc["pending_qr"] = None
        await edc_show("ชำระสำเร็จ (QR)", f"{int(q['amt'] or 0) / 100:.2f} บาท", "QR สำเร็จ")
        asyncio.create_task(edc_send_back(peer, fr, "QR ชำระแล้ว"))

    elif code == "CP":                               # ยกเลิก — popup QR รอข้อความกลับก่อนปิด
        edc["pending_card"] = None
        await edc_show("ยกเลิกรายการ", "", "ยกเลิก")
        if edc["pending_qr"]:
            edc["pending_qr"] = None
            asyncio.create_task(edc_send_back(peer, edc_frame("CP", "00", [("02", "CANCELLED")]), "ยืนยันยกเลิก QR"))

    elif code == "S1":                               # Clear Batch / Summary ตอน EOJ
        asyncio.create_task(edc_send_back(peer, edc_frame("S1", "00", [("02", "SETTLEMENT OK")]),
                                          "Clear Batch สำเร็จ", 0.3))
    # H1 (handshake) : ACK อย่างเดียวพอ


async def edc_card_result(result, peer, f, amt):
    """ส่งผลการชำระด้วยบัตรกลับ TCT : approve | decline | timeout"""
    baht = f"{int(amt) / 100:.2f}" if str(amt).isdigit() else amt
    edc["pending_card"] = None
    if result == "timeout":
        await edc_show("ไม่มีการตอบกลับ", "", "timeout")
        await log("EDC", "… ไม่ตอบผลกลับ (timeout)", "warn")
        return
    d, t = edc_now()
    if result == "decline":
        fr = edc_frame("20", "51", [("02", "INSUFFICIENT FUNDS"), ("A1", f.get("A1", "")), ("A2", f.get("A2", ""))])
        await edc_show("ปฏิเสธรายการ", "51 ยอดเงินไม่พอ", "ปฏิเสธ")
        asyncio.create_task(edc_send_back(peer, fr, "ปฏิเสธบัตร (51)"))
        return
    edc["invoice"] += 1
    fr = edc_frame("20", "00", [
        ("02", "APPROVED"), ("01", f"{edc['invoice'] % 1000000:06d}"), ("03", d), ("04", t),
        ("16", edc["tid"]), ("30", "4111XXXXXXXX1111"), ("31", "XXXX"), ("40", amt),
        ("A1", f.get("A1", "")), ("A2", f.get("A2", "")), ("A3", f"SIMREF{edc['invoice']:06d}"),
        ("D0", "SIM TOLLWAY"), ("D1", "000000000123"), ("D2", "VISA")])
    await edc_show("อนุมัติ", f"{baht} บาท  APPR {edc['invoice'] % 1000000:06d}", "อนุมัติ")
    asyncio.create_task(edc_send_back(peer, fr, f"อนุมัติบัตร {baht} บาท"))


async def api_state(request):
    """สำหรับ script ทดสอบอัตโนมัติ: GET /api/state?since=<seq>"""
    since = int(request.query.get("since", "0"))
    items = [it for it in logs if it.get("seq", 0) > since]
    return web.json_response({"status": status, "counters": counters, "di": di_values, "do": do_values,
                              "edc": edc_public(), "captures": list(captures),
                              "seq": seq_no[0], "logs": items})


async def api_shot(request):
    async with aiohttp.ClientSession() as s:
        async with s.get(f"http://{TCT_HOST}:6081/shot", timeout=10) as r:
            return web.Response(body=await r.read(), content_type="image/png")


async def rto_check_listener(reader, writer):
    writer.close()                            # TCT แค่เช็คว่า port RTO-1 (9093) เปิดอยู่


async def panel_ws(request):
    ws = web.WebSocketResponse(heartbeat=20)
    await ws.prepare(request)
    panel_clients.add(ws)
    await ws.send_str(json.dumps({"type": "init", "logs": list(logs), "di_names": DI_NAMES, "do_names": DO_NAMES}))
    await ws.send_str(json.dumps({"type": "state", "di": di_values, "do": do_values, "status": status, "counters": counters}))
    await ws.send_str(json.dumps({"type": "captures", "items": list(captures)}))
    try:
        async for _ in ws:
            pass
    finally:
        panel_clients.discard(ws)
    return ws


async def index(request):
    return web.FileResponse(Path(__file__).parent / "panel.html")


async def edc_page(request):
    return web.FileResponse(Path(__file__).parent / "edc.html")


async def keyboard_page(request):
    return web.FileResponse(Path(__file__).parent / "keyboard.html")


async def start_bg(app):
    app["bg"] = [asyncio.create_task(watch_ws("SUPADJ", SUPADJ_PORT, "supadj")),
                 asyncio.create_task(watch_ws("RTO", RTO_PORT, "rto")),
                 asyncio.create_task(watch_dc())]


def main():
    lcsd = web.Application()
    lcsd.router.add_get("/websocket/{name}", lcsd_ws)
    lcsd.router.add_get("/{tail:.*}", lcsd_ws)

    panel = web.Application()
    panel.router.add_get("/", index)
    panel.router.add_get("/ws", panel_ws)
    panel.router.add_get("/keyboard", keyboard_page)
    panel.router.add_get("/edc", edc_page)
    panel.router.add_post("/api", api)
    panel.router.add_get("/api/state", api_state)
    panel.router.add_get("/api/shot", api_shot)
    panel.router.add_get("/cap/{name}", cap_file)
    panel.on_startup.append(start_bg)

    async def run():
        r1 = web.AppRunner(lcsd)
        r2 = web.AppRunner(panel)
        await r1.setup()
        await r2.setup()
        await web.TCPSite(r1, "0.0.0.0", LCSD_PORT).start()
        await web.TCPSite(r2, "0.0.0.0", PANEL_PORT).start()
        await asyncio.start_server(rto_check_listener, "0.0.0.0", RTO_PORT - 1)
        await asyncio.start_server(edc_handle, "0.0.0.0", EDC_SEND_PORT)
        print(f"+ EDC sim  tcp://0.0.0.0:{EDC_SEND_PORT} (ตอบกลับ TCT:{EDC_RECV_PORT})", flush=True)
        ta = web.Application(client_max_size=32 * 1024 * 1024)       # /saveImages ส่ง base64 3 ภาพ
        ta.router.add_get("/cam/{idx}.jpg", cam_handler)              # ต้องมาก่อน catch-all
        ta.router.add_post("/saveImages", save_images_handler)
        ta.router.add_route("*", "/{tail:.*}", ta_handler)
        r3 = web.AppRunner(ta)
        await r3.setup()
        await web.TCPSite(r3, "0.0.0.0", TA_PORT).start()
        print(f"+ TA web service sim  http://0.0.0.0:{TA_PORT}", flush=True)
        print(f"+ LCSD sim  ws://0.0.0.0:{LCSD_PORT}/websocket/mtc", flush=True)
        print(f"+ Panel     http://0.0.0.0:{PANEL_PORT}", flush=True)
        await asyncio.Event().wait()

    asyncio.run(run())


if __name__ == "__main__":
    main()
