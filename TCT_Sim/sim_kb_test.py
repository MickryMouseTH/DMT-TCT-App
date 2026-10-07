#!/usr/bin/env python3
"""
ทดสอบทุกฟังก์ชันของ TCT ด้วย "คีย์บอร์ด" (ส่งแป้นแบบคีย์บอร์ด USB จริงผ่าน xdotool) — รวม Popup, ขายคูปอง, โหมดช่าง
ต้อง ./sim.sh start ก่อน    รัน: ./sim.sh kbtest  [case ...]
ผล: TCT_Sim/results/kb_<เวลา>/  (summary.txt + ภาพทุกขั้น + log TCT ต่อ case)
"""
import json
import subprocess
import sys
import time
import urllib.request
from datetime import datetime
from pathlib import Path

SIM_DIR = Path(__file__).resolve().parent
PANEL   = "http://localhost:8090"
LOG_DIR = SIM_DIR / "runtime" / "log"
RESULT  = SIM_DIR / "results" / ("kb_" + datetime.now().strftime("%Y%m%d_%H%M%S"))

# ปุ่มบนคีย์บอร์ดจริง -> แป้นที่ส่ง (ตาม TCT_Lib.keyMapping)
K = {"รถประเภท1": "u", "รถประเภท2": "v", "รถลากจูง": "b", "รถยกเว้น": "z", "รถออกซ้าย": "m", "รถขบวน": "f",
     "เงินสด": "c", "คูปอง": "r", "QR": "q", "EMV": "k", "บัตรDMT": "x", "ยืนยัน": "t", "MANUAL": "g",
     "พิมพ์ใบเสร็จ": "e", "FORCE_RESET": "s", "รถฝ่าด่าน": "o", "แก้ไข": "n", "OTL_เปิด": "a", "OTL_ปิด": "w",
     "BOJ/EOJ": "j", "ขายคูปอง": "l", "รถขบวนจ่ายเงิน": "h", "MODE": "d", "CANCEL": "p", "ENTER": "Return",
     "BACKSPACE": "BackSpace", "END": "End", "PM6": "equal", "PM7": "bracketleft", "PM8": "bracketright",
     "PM9": "backslash", "▲": "Up", "▼": "Down", "◀": "Left", "▶": "Right"}


def post(**body):
    req = urllib.request.Request(PANEL + "/api", data=json.dumps(body).encode(),
                                 headers={"Content-Type": "application/json"})
    urllib.request.urlopen(req, timeout=15).read()


def state(since=0):
    with urllib.request.urlopen(f"{PANEL}/api/state?since={since}", timeout=10) as r:
        return json.load(r)


def press(*names, gap=0.7):
    """กดปุ่มตามชื่อบนคีย์บอร์ดจริง หรือ ตัวเลข/ข้อความ (พิมพ์ทีละตัว)"""
    for n in names:
        if n in K:
            post(action="kb", k=K[n], name=n)
            time.sleep(gap)
        else:
            for ch in str(n):
                post(action="kb", k=ch, name=ch)
                time.sleep(0.4)


def scn(name, wait):
    post(action="scenario", name=name)
    time.sleep(wait)


def lane_event_log():
    files = sorted(LOG_DIR.glob("LaneEvent/Event_*/LaneEvent_*.log"))
    return files[-1] if files else None


def lane_log():
    files = sorted(LOG_DIR.glob("LaneLog/Log_*.log"))
    return files[-1] if files else None


class Mark:
    def __init__(self):
        self.seq = state()["seq"]
        self.pos = {}
        for f in (lane_event_log(), lane_log()):
            if f:
                self.pos[f] = f.stat().st_size

    def lines(self):
        out = []
        for f in (lane_event_log(), lane_log()):
            if not f:
                continue
            with open(f, "rb") as fh:
                fh.seek(self.pos.get(f, 0))
                out += fh.read().decode("utf-8", "replace").splitlines()
        return out


def wait_ready(timeout=180):
    t0 = time.time()
    while time.time() - t0 < timeout:
        try:
            if all(state()["status"].values()):
                return True
        except Exception:
            pass
        time.sleep(2)
    return False


class Ctx:
    def __init__(self, name):
        self.name, self.ok, self.notes, self.n = name, True, [], 0
        self.m = Mark()

    def shot(self, label):
        self.n += 1
        fn = f"{self.name}__{self.n:02d}_{label}.png"
        with urllib.request.urlopen(f"{PANEL}/api/shot", timeout=15) as r:
            (RESULT / fn).write_bytes(r.read())
        self.notes.append(f"    ภาพ: {fn}")

    def check(self, cond, msg):
        self.notes.append(("    ✔ " if cond else "    ✘ ") + msg)
        self.ok &= bool(cond)

    def info(self, msg):
        self.notes.append("    • " + msg)

    def log_has(self, text, msg=None):
        found = any(text in l for l in self.m.lines())
        self.check(found, msg or f"log มี \"{text}\"")
        return found

    def trans(self, src="SUPADJ"):
        out = []
        for it in state(self.m.seq)["logs"]:
            if it["src"] == src and it["level"] == "trans":
                p = (it.get("data") or {}).get("params", it.get("data"))
                out.append(p if isinstance(p, dict) else it.get("data") or {})
        return out

    def expect_trans(self, n, cls=None, pay=None):
        sup, dc = self.trans("SUPADJ"), self.trans("DC")
        self.check(len(sup) == n, f"Sup Adjust ได้ {len(sup)} รายการ (คาด {n})")
        self.check(len(dc) == n, f"DC ได้ {len(dc)} รายการ (คาด {n})")
        for t in sup:
            pi = t.get("payInfo") or {}
            self.info(f"รายการ: class={t.get('tcClass')} pay={pi.get('payTypeName')} fare={t.get('fareAmount')} "
                      f"remark={t.get('transRemark')} towing={t.get('isTowing')}")
        if sup and cls is not None:
            self.check(sup[-1].get("tcClass") == cls if not isinstance(cls, list) else
                       [t.get("tcClass") for t in sup] == cls, f"class = {[t.get('tcClass') for t in sup]} (คาด {cls})")
        if sup and pay is not None:
            got = str((sup[-1].get("payInfo") or {}).get("payTypeName")).upper()
            self.check(pay.upper() in got, f"payType = {got} (คาด {pay})")


# ------------------------------------------------------------------ setup
def restart_tct():
    subprocess.run(["docker", "compose", "restart", "tct"], cwd=SIM_DIR,
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    time.sleep(8)
    if not wait_ready():
        sys.exit("✘ เชื่อมต่อไม่ครบ — ดู ./sim.sh logs tct")
    subprocess.run(["docker", "compose", "exec", "-d", "tct", "python3", "/keyinject.py"], cwd=SIM_DIR,
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    time.sleep(5)
    for i in range(16):
        post(action="di", idx=i, value=0)


def kb_login(staff="99001", pw="123456"):
    press("MANUAL")
    press(staff, "ENTER", pw, "ENTER")
    time.sleep(4)


def ready_lane():
    restart_tct()
    kb_login()
    press("OTL_เปิด")
    time.sleep(1.5)


# ------------------------------------------------------------------ cases
CASES = {}


def case(name, title, setup=True):
    def deco(fn):
        CASES[name] = (title, fn, setup)
        return fn
    return deco


@case("a_login_keyboard", "Login ด้วยคีย์บอร์ด: [MANUAL] → รหัส → [ENTER] → รหัสผ่าน → [ENTER]", setup=False)
def t_login(c):
    restart_tct()
    c.shot("login_page")
    press("MANUAL")
    c.shot("manual_panel")
    press("99001", "ENTER", "123456", "ENTER")
    time.sleep(4)
    c.shot("after_login")
    c.log_has("Login Manual Success")
    press("OTL_เปิด")
    time.sleep(1)
    c.log_has("Keyboard (Key) : OTL_OPEN", "[OTL เปิด] ถึง TCT")


@case("b_otl_close_open", "[OTL ปิด] / [OTL เปิด]")
def t_otl(c):
    press("OTL_ปิด")
    c.shot("otl_closed")
    c.log_has("OTL_CLOSE", "[OTL ปิด] ถึง TCT")
    press("OTL_เปิด")
    c.log_has("OTL_OPEN", "[OTL เปิด] ถึง TCT")


@case("c_class1_cash_receipt", "[รถประเภท 1] → [เงินสด] → [พิมพ์ใบเสร็จ] → รถผ่าน")
def t_c1(c):
    press("รถประเภท1")
    c.shot("class1")
    press("เงินสด")
    c.shot("paid")
    press("พิมพ์ใบเสร็จ")
    c.log_has("PRINT RECEIPT", "[พิมพ์ใบเสร็จ] ทำงาน")
    scn("normal", 3)
    time.sleep(5)
    c.expect_trans(1, cls=1, pay="Cash")


@case("c_class2_coupon", "[รถประเภท 2] → [คูปอง] → รถผ่าน")
def t_c2(c):
    press("รถประเภท2", "คูปอง")
    c.shot("paid")
    scn("normal", 3)
    time.sleep(5)
    c.expect_trans(1, cls=2, pay="Coupon")


@case("c_pm6_qr_kbank", "[รถประเภท 1] → [QR (KBank)] (Payment 6) → รถผ่าน")
def t_pm6(c):
    press("รถประเภท1", "PM6")
    c.shot("paid_pm6")
    scn("normal", 3)
    time.sleep(5)
    c.expect_trans(1, cls=1)


@case("c_pm7_9_no_label", "[Payment 7/8/9] (config ไม่มีชื่อปุ่ม) → ต้องไม่รับเงิน")
def t_pm79(c):
    press("รถประเภท1", "PM7", "PM8", "PM9")
    c.shot("after_pm7_9")
    time.sleep(1)
    c.check(not any("ALB Open" in l for l in c.m.lines()), "ไม่มีการเปิดไม้กั้น")
    press("FORCE_RESET")


@case("c_next_car", "คันที่ 1 [รถประเภท 1][เงินสด] → จองคันที่ 2 [รถประเภท 2] → คันที่ 1 ผ่าน → [เงินสด] → คันที่ 2 ผ่าน")
def t_next(c):
    press("รถประเภท1", "เงินสด", "รถประเภท2")
    c.shot("next_car_selected")
    scn("normal", 3)
    time.sleep(5)
    press("เงินสด")
    scn("normal", 3)
    time.sleep(5)
    c.shot("after_two")
    c.expect_trans(2, cls=[1, 2])


@case("d_exempt", "[รถยกเว้น] → รถผ่าน")
def t_ex(c):
    press("รถยกเว้น")
    c.shot("exempt")
    scn("normal", 3)
    time.sleep(5)
    c.expect_trans(1, cls=7, pay="EXEMPT")


@case("d_left_exit", "[รถออกซ้าย] → รถผ่าน")
def t_left(c):
    press("รถออกซ้าย")
    c.shot("left_exit")
    scn("normal", 3)
    time.sleep(5)
    c.expect_trans(1, cls=8)


@case("d_exempt_ob_only_force_reset", "[บั๊ก H1] [รถยกเว้น] → OB อย่างเดียว → [FORCE RESET]")
def t_h1(c):
    press("รถยกเว้น")
    scn("ob_only", 1.3)
    press("FORCE_RESET")
    time.sleep(6)
    c.shot("after_force_reset")
    c.expect_trans(1, cls=7, pay="EXEMPT")


@case("e_force_reset_idle", "[FORCE RESET] ตอนไม่มีรถ → ไม่มีรายการ")
def t_fr(c):
    press("FORCE_RESET")
    time.sleep(3)
    c.log_has("Press FORCE_RESET")
    c.expect_trans(0)


@case("f_violation", "รถฝ่าด่าน (ไม่กดอะไร รถผ่าน) → [รถฝ่าด่าน] ปิด Siren")
def t_vio(c):
    scn("normal", 3)
    time.sleep(1)
    c.shot("violation")
    c.log_has("Found Violation", "เกิด Violation")
    press("รถฝ่าด่าน")
    time.sleep(5)
    c.shot("after_stop")
    c.expect_trans(1)


@case("g_towing", "[รถลากจูง] → [รถประเภท 2][เงินสด] → [รถประเภท 1][เงินสด] → รถ 2 คันผ่าน")
def t_tow(c):
    press("รถลากจูง")
    c.shot("towing")
    press("รถประเภท2", "เงินสด")
    c.shot("first_paid")
    press("รถประเภท1", "เงินสด")
    c.shot("second_paid")
    press("พิมพ์ใบเสร็จ")
    time.sleep(2)
    # Towing ไม่มี Auto AVC (ต้องใช้ AVC จริง) -> ส่ง AVC หลังรถแต่ละคันผ่าน
    scn("normal", 3)
    post(action="avc", cls=2, axle=3)
    time.sleep(3)
    scn("normal", 3)
    post(action="avc", cls=1, axle=2)
    time.sleep(4)
    c.shot("after_pass")
    c.expect_trans(2, cls=[2, 1])


@case("h_hpmc", "[รถขบวน] เริ่ม HPMC → รถผ่าน 2 คัน → [รถขบวน] จบ")
def t_hpmc(c):
    press("รถขบวน")
    c.shot("hpmc_on")
    # HPMC ไม่มี Auto AVC (ต้องใช้ AVC จริง) -> ส่ง AVC หลังรถแต่ละคันผ่าน
    for cls in (1, 2):
        scn("normal", 3)
        post(action="avc", cls=cls, axle=2)
        time.sleep(3)
    press("รถขบวน")
    time.sleep(2)
    c.shot("hpmc_off")
    c.log_has("CLASS 9", "[รถขบวน] ถึง TCT")
    c.expect_trans(2)


@case("h_convoy_dialog", "[รถขบวนจ่ายเงิน] (Popup) → ใส่ Class1=2 [ENTER] Class2=1 [ENTER] → [เงินสด] → [ยืนยัน]")
def t_convoy(c):
    press("รถขบวนจ่ายเงิน")
    time.sleep(2)
    c.shot("convoy_popup")
    press("2", "ENTER", "1", "ENTER")
    c.shot("convoy_filled")
    press("เงินสด")
    c.shot("convoy_cash")
    press("ยืนยัน")
    time.sleep(3)
    c.shot("convoy_after_confirm")
    c.log_has("Keyboard (Key) : PRINT_CONVOY", "[รถขบวนจ่ายเงิน] เปิด Popup")
    ok = any("Convoy" in l and ("2" in l) for l in c.m.lines())
    c.info("ดูภาพ convoy_after_confirm ว่าจอแสดง 'รถขบวน : 2 + 1' หรือไม่")
    press("CANCEL")
    time.sleep(1)


@case("i_edit_reclass", "[รถประเภท 1] → [แก้ไข] → [รถประเภท 2] → [เงินสด] → รถผ่าน")
def t_edit1(c):
    press("รถประเภท1", "แก้ไข")
    c.shot("after_edit")
    press("รถประเภท2", "เงินสด")
    scn("normal", 3)
    time.sleep(5)
    c.expect_trans(1, cls=2, pay="Cash")


@case("i_edit_repay", "[รถประเภท 1] → [เงินสด] → [แก้ไข] → [คูปอง] → รถผ่าน")
def t_edit2(c):
    press("รถประเภท1", "เงินสด", "แก้ไข")
    c.shot("after_edit_paid")
    press("คูปอง")
    scn("normal", 3)
    time.sleep(5)
    c.expect_trans(1, cls=1, pay="Coupon")


@case("j_sale_coupon", "[ขายคูปอง] (Popup) → [1] เลือกคูปอง → [เงินสด] → [ยืนยัน]")
def t_coupon(c):
    press("ขายคูปอง")
    time.sleep(3)
    c.shot("coupon_popup")
    press("1")
    c.shot("coupon_selected")
    press("เงินสด", "ยืนยัน")
    time.sleep(3)
    c.shot("after_sold")
    sold = [it for it in state(c.m.seq)["logs"] if it["src"] == "TA" and "SoldCoupon" in it["text"]]
    c.check(len(sold) == 1, f"TA ได้ SoldCoupon {len(sold)} รายการ (คาด 1)")
    for it in sold:
        c.info(it["text"])


@case("j_sale_coupon_cancel", "[ขายคูปอง] → [แก้ไข] (ยกเลิก) → ต้องไม่ขาย")
def t_coupon_cancel(c):
    press("ขายคูปอง")
    time.sleep(3)
    press("แก้ไข")
    time.sleep(2)
    c.shot("after_cancel")
    sold = [it for it in state(c.m.seq)["logs"] if it["src"] == "TA" and "SoldCoupon" in it["text"]]
    c.check(len(sold) == 0, "ไม่มีการขาย")
    rto = [it for it in state(c.m.seq)["logs"] if it["src"] == "RTO" and "SALES_COUPON" in it["text"]]
    c.check(len(rto) >= 2, f"RTO ได้ SALES_COUPON เปิด/ปิด {len(rto)} ครั้ง (คาด 2)")


@case("k_emv_popup", "[รถประเภท 1] → [EMV] (Popup) → [แก้ไข] ยกเลิก")
def t_emv(c):
    edc_mode("timeout")                                # ให้ EDC ไม่ตอบ เพื่อทดสอบปุ่มยกเลิก
    press("รถประเภท1", "EMV")
    time.sleep(3)
    c.shot("emv_popup")
    press("แก้ไข")
    time.sleep(3)
    c.shot("after_cancel")
    c.log_has("Response Cancel from EMV", "[แก้ไข] ยกเลิก Popup EMV ได้")
    edc_mode("approve")
    press("FORCE_RESET")


@case("k_qr_popup", "[รถประเภท 1] → [QR CODE] (Popup) → [แก้ไข] ยกเลิก")
def t_qr(c):
    press("รถประเภท1", "QR")
    time.sleep(3)
    c.shot("qr_popup")
    press("CANCEL")
    time.sleep(3)
    c.shot("after_cancel")
    c.check(any(": ESC" in l and "Keyboard" in l for l in c.m.lines()), "[CANCEL] ถึง Popup QR")
    c.log_has("Response Cancel from EMV QR Code", "Popup QR ปิดแล้ว (Cancel)")
    press("FORCE_RESET")


def edc_mode(mode=None, qr=None):
    post(action="edc", mode=mode, qr=qr)
    time.sleep(0.5)


def edc_logs(c):
    return [it["text"] for it in state(c.m.seq)["logs"] if it["src"] == "EDC"]


@case("o_edc_card_approve", "EDC: [รถประเภท 1] → [EMV] → EDC อนุมัติ → รถผ่าน")
def t_edc_ok(c):
    edc_mode("approve")
    press("รถประเภท1", "EMV")
    time.sleep(5)
    c.shot("after_approve")
    c.log_has("Response OK from EMV", "TCT รับผลอนุมัติจาก EDC")
    scn("normal", 3)
    time.sleep(5)
    c.expect_trans(1, cls=1, pay="EMV")
    for t in edc_logs(c):
        c.info(t)


@case("o_edc_card_decline", "EDC: [EMV] → EDC ปฏิเสธบัตร → [แก้ไข] ปิด → [เงินสด]")
def t_edc_decline(c):
    edc_mode("decline")
    press("รถประเภท1", "EMV")
    time.sleep(4)
    c.shot("declined")
    press("แก้ไข")
    time.sleep(3)
    press("เงินสด")
    scn("normal", 3)
    time.sleep(5)
    c.expect_trans(1, cls=1, pay="Cash")
    edc_mode("approve")


@case("o_edc_card_timeout", "EDC: [EMV] → EDC ไม่ตอบ → [CANCEL] ปิด Popup ได้ ไม่ค้าง")
def t_edc_timeout(c):
    edc_mode("timeout")
    press("รถประเภท1", "EMV")
    time.sleep(6)
    c.shot("waiting")
    press("CANCEL")
    time.sleep(3)
    c.shot("after_cancel")
    c.log_has("Response Cancel from EMV", "ยกเลิกได้")
    edc_mode("approve")
    press("FORCE_RESET")


@case("o_edc_qr_paid", "EDC: [รถประเภท 1] → [QR CODE] → ลูกค้าสแกน → [ENTER] ตรวจสอบ → รถผ่าน")
def t_edc_qr(c):
    edc_mode("approve", "paid")
    press("รถประเภท1", "QR")
    time.sleep(6)                                      # ปุ่มตรวจสอบเปิดหลัง 5 วินาที
    c.shot("qr_popup")
    press("ENTER")
    time.sleep(4)
    c.shot("after_inquiry")
    c.log_has("Response OK from QR Code", "QR ชำระสำเร็จ")
    scn("normal", 3)
    time.sleep(5)
    c.expect_trans(1, cls=1)


@case("o_edc_qr_unpaid_cancel", "EDC: [QR CODE] → ยังไม่จ่าย → [ENTER] → แจ้งยังไม่สแกน → [QR CODE] → [CANCEL]")
def t_edc_qr2(c):
    edc_mode("approve", "unpaid")
    press("รถประเภท1", "QR")
    time.sleep(6)
    press("ENTER")
    time.sleep(4)
    c.shot("not_paid")
    c.log_has("Response Reject", "แจ้งลูกค้ายังไม่สแกน")
    press("QR")
    time.sleep(6)
    press("CANCEL")
    time.sleep(4)
    c.shot("after_cancel")
    c.log_has("Response Cancel from EMV QR Code", "ยกเลิก QR ได้")
    edc_mode("approve", "paid")
    press("FORCE_RESET")


@case("o_edc_offline_qr_cancel", "[บั๊ก 2.0.2] EDC ออฟไลน์: [QR CODE] → [CANCEL] ต้องปิด Popup ได้ (เดิมค้าง)")
def t_edc_off(c):
    edc_mode("offline")
    press("รถประเภท1", "QR")
    time.sleep(6)
    c.shot("offline_popup")
    press("CANCEL")
    time.sleep(7)                                      # fallback ปิดเองภายใน 5 วินาทีถ้า EDC ไม่ตอบ
    c.shot("after_cancel")
    c.log_has("Cancel QR Code", "Popup QR ปิดได้แม้ EDC ออฟไลน์")
    c.log_has("Response Cancel from EMV QR Code", "TCT กลับหน้าหลัก (Cancel)")
    edc_mode("approve")
    press("FORCE_RESET")


@case("k_dmt_popup", "[รถประเภท 1] → [บัตร DMT] (Popup) → [แก้ไข] ยกเลิก")
def t_dmt(c):
    press("รถประเภท1", "บัตรDMT")
    time.sleep(3)
    c.shot("dmt_popup")
    press("CANCEL")
    time.sleep(2)
    c.shot("after_cancel")
    c.log_has("Cancel DMT Payment Dialog", "[CANCEL] ยกเลิก Popup บัตร DMT ได้")
    press("FORCE_RESET")


@case("k_enter_no_repress", "แตะปุ่ม [รถประเภท 1] บนจอ (touch) แล้วกด [ENTER] บนคีย์บอร์ด -> ต้องไม่กดปุ่มซ้ำ")
def t_enter(c):
    post(action="click", x=920, y=697)        # ปุ่ม "รถประเภท 1" บนจอ 1024x768
    time.sleep(1.5)
    press("ENTER", "ENTER")
    time.sleep(1.5)
    c.shot("after_enter")
    n = sum("Press CLASS 1" in l for l in c.m.lines())
    c.check(n == 1, f"กด CLASS 1 ไป {n} ครั้ง (คาด 1 = ENTER ไม่ไปกดปุ่มบนจอซ้ำ)")
    press("FORCE_RESET")


@case("l_backspace_end_keys", "[BACKSPACE] [END] บนหน้าหลัก -> TCT รับรู้แป้น แต่ไม่มีผลกับรายการ")
def t_unmapped(c):
    press("BACKSPACE", "END")
    time.sleep(1)
    c.log_has("Keyboard (Key) : BACKSPACE", "[BACKSPACE] ถึง TCT")
    c.expect_trans(0)


@case("m_eoj_mode", "[OTL ปิด] → [BOJ/EOJ] → หน้า Login → [MODE] → [1] MTC → [CANCEL]")
def t_eoj(c):
    press("OTL_ปิด")
    post(action="di", idx=7, value=0)
    time.sleep(1)
    press("BOJ/EOJ")
    time.sleep(6)
    c.shot("after_eoj")
    c.log_has("END_SHIFT", "[BOJ/EOJ] ส่ง END_SHIFT")
    s1 = [t for t in edc_logs(c) if "S1" in t or "Clear Batch" in t]
    c.info("EDC Clear Batch ตอน EOJ: " + (" | ".join(s1) if s1 else "ไม่มี (อาจติดเงื่อนไขช่วงเวลา/ช่วงห่าง)"))
    press("MODE")
    time.sleep(2)
    c.shot("mode_popup")
    press("1")
    time.sleep(2)
    c.shot("after_mode_1")
    c.log_has("MODE", "[MODE] ถึง TCT")


@case("n_maintenance", "โหมดช่าง: Login 99050 ด้วยคีย์บอร์ด → เมนู → ทดสอบอุปกรณ์ → ออก", setup=False)
def t_mt(c):
    restart_tct()
    kb_login("99050")
    c.shot("maint_menu")
    c.log_has("Login Manual Success", "Login ช่างสำเร็จ")
    press("2")
    time.sleep(1.5)
    c.shot("menu_2")
    press("1")
    time.sleep(1.5)
    c.shot("menu_2_1")
    press("1")
    time.sleep(2)
    c.shot("submenu_1")
    press("CANCEL")
    time.sleep(1.5)
    c.shot("after_cancel")
    press("◀", "◀")
    time.sleep(1)
    press("1")
    time.sleep(2)
    c.shot("menu_1_simulate")
    press("รถประเภท1", "เงินสด")
    time.sleep(1)
    c.shot("simulate_paid")
    c.info("ดูภาพเมนูแต่ละขั้น (ข้อความเมนูอยู่ในภาพ)")


def main():
    names = sys.argv[1:] or list(CASES)
    RESULT.mkdir(parents=True, exist_ok=True)
    if not wait_ready(30):
        sys.exit("✘ simulator ยังไม่พร้อม — ./sim.sh start ก่อน")
    results = []
    for name in names:
        title, fn, setup = CASES[name]
        print(f"\n=== {name}\n    {title}", flush=True)
        if setup:
            ready_lane()
        c = Ctx(name)
        try:
            fn(c)
        except Exception as e:
            c.check(False, f"exception: {e!r}")
        (RESULT / f"{name}__tct.log").write_text("\n".join(c.m.lines()), encoding="utf-8")
        print("\n".join(c.notes))
        print("    ==> " + ("PASS ✅" if c.ok else "FAIL ❌"), flush=True)
        results.append((name, title, c))
    lines = [f"TCT keyboard test — {datetime.now():%Y-%m-%d %H:%M:%S}", ""]
    for name, title, c in results:
        lines += [f"[{'PASS' if c.ok else 'FAIL'}] {name}", f"    {title}", *c.notes, ""]
    passed = sum(c.ok for *_, c in results)
    lines.append(f"สรุป: ผ่าน {passed}/{len(results)}")
    (RESULT / "summary.txt").write_text("\n".join(lines), encoding="utf-8")
    print(f"\nสรุป: ผ่าน {passed}/{len(results)}   ผลอยู่ที่ {RESULT}")


if __name__ == "__main__":
    main()
