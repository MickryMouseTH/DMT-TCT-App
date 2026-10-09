"""
LSD_Bridge.py  (Program: LSD_Bridge)
====================================
ตัวแปลง (Converter/Bridge) ระหว่าง:

    [ ระบบเดิม TCT_App ]  --TCP binary "AVON"-->  [ LSD_Bridge (ไฟล์นี้) ]  --HTTP PUT-->  [ LSD Controller ใหม่ ]

หน้าที่:
  1. เปิด TCP server (ค่า default port 16603) ให้ TCT_App เดิมต่อเข้ามา "เสมือนเป็น LSD ตัวจริง"
  2. รับ frame โปรโตคอลเดิม (KeepAlive / LORA_SYNC / ROTATION / SYNC1-3 / Show / Clear)
  3. เฉพาะคำสั่ง Show/Clear -> อ่าน identifier -> map เป็นชื่อ .vsn -> ยิง HTTP PUT ไป controller ใหม่
     (เช่น  PUT http://169.254.32.1/api/vsns/sources/lan/vsns/MTC.vsn/activated)
  4. ตอบ ACK สั้น ๆ กลับให้ระบบเดิม เพื่อไม่ให้ init/keepalive ของ TCT_App ล้ม

หมายเหตุสำคัญเรื่อง identifier:
  - identifier ของ LSD "แต่ละเครื่องไม่เหมือนกัน" — เครื่องจริงแจกรายการของตัวเอง
    ผ่าน response ของ SYNC2 (JSON array ที่มีคู่ name/identifier)
  - ระบบเดิม (TCT_App) มี identifier hardcode 4 ค่าเป็น fallback และจะอัปเดตเอง
    เมื่อได้ response SYNC2 ยาว (>60 ไบต์) จาก LSD
  - Bridge นี้ตอบ ACK สั้น (<60 ไบต์) ให้ SYNC2 เสมอ => TCT_App จะคง identifier default ไว้
  - Identifier_Map ใน config จึงใส่ไว้ทั้งชุด default ของ TCT_App และชุดของเครื่องจริง
    (หลาย identifier ชี้ vsn เดียวกันได้) — deployment ใหม่เพิ่ม/แก้ได้ที่ config
  - ดู identifier ของเครื่องจริงได้จากปุ่ม SYNC2 ใน NovaDemo (มันจะ log และอัปเดต
    Identifier_Map ของตัวเองอัตโนมัติ)
"""

import time
import socket
import threading
import urllib.request
import urllib.error

from LogLibrary import Load_Config, Loguru_Logging
import avon_protocol as avon

# ----------------------- Configuration Values -----------------------
Program_Name = "LSD_Bridge"
Program_Version = "1.0.0"
# --------------------------------------------------------------------

default_config = {
    # --- logging (ใช้โดย LogLibrary) ---
    "log_Level": "DEBUG",
    "Log_Console": 1,
    "log_Backup": 90,
    "Log_Size": "10 MB",

    # --- TCP server ฝั่งรับจากระบบเดิม (แทนที่ LSD ตัวจริง) ---
    "Listen_Host": "0.0.0.0",       # bind ทุก interface
    "Listen_Port": 16603,           # port เดิมที่ TCT_App ต่อเข้า LSD (TCT_Lib.iLSD_SocketPort)
    "Ack_Enable": 1,                # 1 = ตอบ ACK กลับ (แนะนำเปิด ไม่งั้น init เดิมจะ timeout)

    # --- HTTP client ฝั่งยิงไป LSD controller ใหม่ ---
    "LSD_New_BaseURL": "http://169.254.32.1",
    "LSD_New_ActivatePath": "/api/vsns/sources/lan/vsns/{vsn}/activated",
    "LSD_New_Method": "PUT",
    "LSD_Auth_Header": "Authorization",
    "LSD_Auth_Value": "YW5vbnltb3VzOg==",   # จาก Postman (base64 ของ "anonymous:")
    "HTTP_Timeout": 5,                       # วินาที

    # --- Clear/deactivate ---
    # ระบบใหม่ตาม Postman มีแต่ activate; การเปลี่ยนโหมด = activate ทับได้เลย
    # ปกติจึงไม่ต้อง forward Clear (ตั้ง 0). ถ้าเฟิร์มแวร์ใหม่รองรับ deactivate ให้ตั้ง 1 + ใส่ path
    "Forward_Clear": 0,
    "LSD_New_DeactivatePath": "/api/vsns/sources/lan/vsns/{vsn}/deactivated",

    # --- map: identifier (จาก frame เดิม) -> ชื่อ .vsn (ระบบใหม่) ---
    # identifier แต่ละเครื่อง LSD "ไม่เหมือนกัน" (อ่านได้จาก response SYNC2 ของเครื่องนั้น)
    # จึงใส่ไว้ทั้งชุด default ที่ TCT_App hardcode และชุดของเครื่องจริงหน้างาน
    "Identifier_Map": {
        # ชุด default ที่ TCT_App hardcode (ใช้เมื่อ LSD/Bridge ไม่ตอบ identifier ให้ SYNC2)
        "6bed8fbc88534dc275692569e153633e": "CLOSE.vsn",     # LSD_CLOSE (Lane Close)
        "993bd1ac3e9c645360110be0aa4a0f83": "MTC.vsn",       # LSD_MTC
        "f5e7e2d08013ceed5752de941f22c266": "ETC.vsn",       # LSD_ETC
        "80b019ebaea588cbd8135b8753b48922": "ETC-MTC.vsn",   # LSD_DUAL (ETC-MTC)
        # ชุดของเครื่อง LSD จริง (อ่านจาก SYNC2 ของเครื่อง 172.30.182.98, ก.ย. 2026)
        "655802eab09c106cc2db841b0863ab5e": "CLOSE.vsn",     # CLOSE
        "8a894564a79bfe0fb70f966f361d1c63": "MTC.vsn",       # MTC
        "0f5b50b6addfec394dfd203cf1f478d1": "ETC.vsn",       # ETC
        "8cb258b43a9bc7af75cd9ae352d932df": "ETC-MTC.vsn",   # ETC-MTC
        "614568e4e13668ff955161412111938a": "MTC-N.vsn",     # MTC-N
        "93ee34542e6be0a03be1b98da6b204c1": "ME-N.vsn",      # ME-N
    },
}

config = Load_Config(default_config, Program_Name)
logger = Loguru_Logging(config, Program_Name, Program_Version)


# ===========================================================================
# helper สำหรับ log ละเอียด (ระดับลึกจะโผล่เฉพาะเมื่อ log_Level = DEBUG / TRACE)
#   ลำดับ level:  TRACE < DEBUG < INFO < SUCCESS < WARNING < ERROR
#   - INFO  = เหตุการณ์หลัก (connect, คำสั่ง Show/Clear, ผล HTTP)
#   - DEBUG = รายละเอียดขั้นตอน (recv/parse/ack, mapping, timing)
#   - TRACE = ไบต์ดิบ (hex dump) ของทุก frame/ack/payload
# ===========================================================================
def _hx(data, limit=256):
    """คืน hex string อ่านง่ายของ bytes (ตัดที่ limit ไบต์) — ใช้กับ logger.trace"""
    if not data:
        return "(empty)"
    b = bytes(data[:limit])
    suffix = "" if len(data) <= limit else " ...(+%d bytes)" % (len(data) - limit)
    return b.hex(" ") + suffix


def _log_startup():
    """สรุป config ที่ใช้จริงตอนเริ่ม (DEBUG) เพื่อไล่ปัญหา deployment ได้"""
    logger.debug("Config: Listen={}:{} Ack_Enable={} Forward_Clear={}",
                 config.get("Listen_Host"), config.get("Listen_Port"),
                 config.get("Ack_Enable"), config.get("Forward_Clear"))
    logger.debug("Config: New_BaseURL={} Method={} HTTP_Timeout={}s",
                 config.get("LSD_New_BaseURL"), config.get("LSD_New_Method"),
                 config.get("HTTP_Timeout"))
    logger.debug("Config: ActivatePath={}", config.get("LSD_New_ActivatePath"))
    logger.debug("Config: DeactivatePath={}", config.get("LSD_New_DeactivatePath"))
    id_map = config.get("Identifier_Map", {}) or {}
    logger.debug("Config: Identifier_Map มี {} รายการ", len(id_map))
    for ident, vsn in id_map.items():
        logger.trace("  map {} -> {}", ident, vsn)
    logger.debug("Log level ปัจจุบัน = {}", str(config.get("log_Level", "DEBUG")).upper())


# ===========================================================================
# HTTP: ยิงคำสั่งไป LSD Controller ใหม่
# ===========================================================================
def _http_call(vsn, activate=True):
    """เรียก REST ระบบใหม่ (PUT .../{vsn}/activated). รันใน thread แยกกันไม่ให้บล็อก ACK"""
    action = "ACTIVATE" if activate else "DEACTIVATE"
    logger.debug("[HTTP] เริ่ม {} vsn={} (thread={})", action, vsn, threading.current_thread().name)

    base = str(config.get("LSD_New_BaseURL", "")).rstrip("/")
    if activate:
        path = str(config.get("LSD_New_ActivatePath", "/api/vsns/sources/lan/vsns/{vsn}/activated"))
    else:
        path = str(config.get("LSD_New_DeactivatePath", "/api/vsns/sources/lan/vsns/{vsn}/deactivated"))
    url = base + path.format(vsn=vsn)
    method = str(config.get("LSD_New_Method", "PUT")).upper()
    timeout = float(config.get("HTTP_Timeout", 5))
    logger.debug("[HTTP] {} {}  (timeout={}s)", method, url, timeout)

    t0 = time.perf_counter()
    try:
        req = urllib.request.Request(url, method=method)
        auth_header = str(config.get("LSD_Auth_Header", "")).strip()
        auth_value = str(config.get("LSD_Auth_Value", ""))
        if auth_header:
            req.add_header(auth_header, auth_value)
            logger.trace("[HTTP] add header {}: ***** (len {})", auth_header, len(auth_value))

        logger.info("-> HTTP {} {} {}", method, action, url)
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            body = resp.read(512)
            elapsed = (time.perf_counter() - t0) * 1000
            logger.info("<- HTTP {} {} (status={}) ใช้เวลา {:.1f} ms", action, vsn, resp.status, elapsed)
            logger.debug("[HTTP] response headers: {}", dict(resp.getheaders()))
            logger.trace("[HTTP] response body={!r}", body)
            return True
    except urllib.error.HTTPError as e:
        elapsed = (time.perf_counter() - t0) * 1000
        logger.error("- HTTP {} {} failed: HTTP {} {} ({:.1f} ms)", action, url, e.code, e.reason, elapsed)
        try:
            logger.debug("[HTTP] error body={!r}", e.read(512))
        except Exception:  # noqa
            pass
    except urllib.error.URLError as e:
        elapsed = (time.perf_counter() - t0) * 1000
        logger.error("- HTTP {} {} failed: {} ({:.1f} ms)", action, url, e.reason, elapsed)
    except Exception as e:  # noqa
        logger.exception("- HTTP {} {} error: {}", action, url, e)
    return False


def _forward_display(frame):
    """แปลงคำสั่ง Show/Clear ของโปรโตคอลเดิม -> เรียก HTTP ระบบใหม่"""
    ident = frame.identifier
    id_map = config.get("Identifier_Map", {}) or {}
    logger.debug("[MAP] {} seq={} identifier={}", frame.cmd_type, frame.seq, ident)
    logger.trace("[MAP] payload={!r}", frame.payload)

    if not ident:
        logger.warning("! {} ไม่มี identifier ใน payload -> ข้าม (payload={!r})",
                       frame.cmd_type, frame.payload[:120])
        return

    vsn = id_map.get(ident) or id_map.get(str(ident).lower()) or id_map.get(str(ident).upper())
    if not vsn:
        logger.warning("! identifier ไม่รู้จัก: {} (cmd={}) -> ข้าม. เพิ่มลง Identifier_Map ใน config ได้",
                       ident, frame.cmd_type)
        return
    logger.debug("[MAP] identifier {} -> vsn={}", ident, vsn)

    if frame.cmd_type == "SHOW":
        logger.info("+ SHOW  identifier={} -> vsn={}", ident, vsn)
        th = threading.Thread(target=_http_call, args=(vsn, True), daemon=True,
                              name="http-activate-%s" % vsn)
        th.start()
        logger.debug("[MAP] spawn HTTP thread '{}' (activate)", th.name)
    elif frame.cmd_type == "CLEAR":
        if int(config.get("Forward_Clear", 0)) == 1:
            logger.info("+ CLEAR identifier={} -> vsn={} (deactivate)", ident, vsn)
            th = threading.Thread(target=_http_call, args=(vsn, False), daemon=True,
                                  name="http-deactivate-%s" % vsn)
            th.start()
            logger.debug("[MAP] spawn HTTP thread '{}' (deactivate)", th.name)
        else:
            logger.info("+ CLEAR identifier={} -> vsn={} (Forward_Clear=0, log อย่างเดียว)", ident, vsn)


# ===========================================================================
# TCP server: รับ frame จากระบบเดิม
# ===========================================================================
def _make_ack(frame):
    """สร้าง ACK สั้น ๆ กลับให้ระบบเดิม

    ระบบเดิม (SendCmdLSD) จะ Read() รอ response เสมอ:
      - ถ้า response ว่าง/timeout -> init/keepalive จะล้ม
      - ถ้า response == "00" (1 ไบต์ 0x00) เดิมมองว่าล้มสำหรับ Show/Clear
      - ถ้า iLen > 60 เดิมจะพยายาม parse identifier (เราไม่อยากให้ทับ default)
    => ตอบ echo Header+Seq (5 ไบต์, non-zero, < 60) เป็น ACK ที่ปลอดภัยที่สุด
    """
    if frame is not None and frame.raw and len(frame.raw) >= 5:
        ack = frame.raw[:5]
        logger.trace("[ACK] echo header+seq -> {}", _hx(ack))
        return ack
    ack = avon.HEADER + b"\x00"
    logger.trace("[ACK] fallback -> {}", _hx(ack))
    return ack


def _handle_client(conn, addr):
    peer = "%s:%d" % addr
    logger.info("+ Client connected: {}", peer)
    idle = 300
    conn.settimeout(idle)
    logger.debug("[CONN] {} settimeout={}s thread={}", peer, idle, threading.current_thread().name)
    buffer = b""
    ack_enable = int(config.get("Ack_Enable", 1)) == 1
    logger.debug("[CONN] {} Ack_Enable={}", peer, ack_enable)
    rx_count = 0
    frame_count = 0
    try:
        while True:
            try:
                data = conn.recv(8192)
            except socket.timeout:
                logger.debug("~ idle timeout ({}s), close {}", idle, peer)
                break
            if not data:
                logger.debug("[CONN] {} recv 0 byte (peer ปิด connection)", peer)
                break
            rx_count += 1
            logger.debug("[RX] {} chunk#{} รับ {} byte (buffer เดิม {} byte)",
                         peer, rx_count, len(data), len(buffer))
            logger.trace("[RX] {} bytes = {}", peer, _hx(data))
            buffer += data

            frames, buffer = avon.split_frames(buffer)
            logger.debug("[RX] {} แยกได้ {} frame, leftover {} byte", peer, len(frames), len(buffer))
            if buffer:
                logger.trace("[RX] {} leftover = {}", peer, _hx(buffer))

            for fbytes in frames:
                frame = avon.parse_frame(fbytes)
                if frame is None:
                    logger.warning("! {} parse_frame คืน None (ไม่มี header?) raw={}", peer, _hx(fbytes, 64))
                    continue
                frame_count += 1
                logger.trace("[FRAME] {} #{} raw = {}", peer, frame_count, _hx(fbytes))

                if frame.cmd_type == "KEEPALIVE":
                    logger.debug("  recv KEEPALIVE seq={}", frame.seq)
                elif frame.is_display_cmd():
                    logger.info("  recv {} seq={} identifier={}",
                                frame.cmd_type, frame.seq, frame.identifier)
                    _forward_display(frame)
                elif frame.cmd_type == "UNKNOWN":
                    logger.warning("  recv UNKNOWN seq={} cmd1={} (ack only) raw={}",
                                   frame.seq, frame.cmd1_hex, _hx(fbytes, 64))
                else:
                    logger.debug("  recv {} seq={} (control, ack only)",
                                 frame.cmd_type, frame.seq)

                if ack_enable:
                    try:
                        ack = _make_ack(frame)
                        conn.sendall(ack)
                        logger.debug("[TX] {} ส่ง ACK {} byte ให้ {} seq={}",
                                     peer, len(ack), frame.cmd_type, frame.seq)
                    except OSError as e:
                        logger.warning("! send ACK fail {}: {}", peer, e)
                        raise
                else:
                    logger.trace("[TX] {} ข้ามการส่ง ACK (Ack_Enable=0)", peer)
    except (ConnectionResetError, BrokenPipeError):
        logger.info("~ Client reset: {}", peer)
    except Exception as e:  # noqa
        logger.exception("- handler error {}: {}", peer, e)
    finally:
        try:
            conn.close()
        except OSError:
            pass
        logger.info("- Client disconnected: {} (รับรวม {} chunk, {} frame)", peer, rx_count, frame_count)


def serve_forever():
    host = str(config.get("Listen_Host", "0.0.0.0"))
    port = int(config.get("Listen_Port", 16603))

    _log_startup()

    srv = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    srv.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    logger.debug("[SRV] กำลัง bind {}:{} ...", host, port)
    srv.bind((host, port))
    srv.listen(5)
    logger.debug("[SRV] bind + listen(5) สำเร็จ")

    logger.info("=" * 80)
    logger.info("LSD_Bridge listening on {}:{}  ->  new controller {}",
                host, port, config.get("LSD_New_BaseURL"))
    logger.info("=" * 80)

    conn_no = 0
    try:
        while True:
            conn, addr = srv.accept()
            conn_no += 1
            logger.debug("[SRV] accept #{} จาก {}:{}", conn_no, addr[0], addr[1])
            t = threading.Thread(target=_handle_client, args=(conn, addr), daemon=True,
                                 name="client-%d-%s" % (conn_no, addr[0]))
            t.start()
            logger.trace("[SRV] spawn thread '{}' (active threads={})",
                         t.name, threading.active_count())
    except KeyboardInterrupt:
        logger.info("KeyboardInterrupt -> shutting down (รับมาทั้งหมด {} connection)", conn_no)
    finally:
        srv.close()
        logger.debug("[SRV] ปิด listening socket แล้ว")


if __name__ == "__main__":
    serve_forever()
