"""
avon_protocol.py
================
ตัว decode/encode โปรโตคอล "AVON" ที่ระบบเดิม (TCT_App / LaneSocketControl_LSD.cs)
ใช้สั่งงาน LSD Controller ตัวเก่าผ่าน TCP socket (binary)

โครงสร้าง Frame (ทุกคำสั่ง):
    [Header "AVON"] [Seq] [Cmd1/opcode] [CRC] [JSON payload]
       4 bytes      1 B     17 bytes    2 B    variable (ascii)
      41 56 4f 4e    XX    00 00 00 ..  crc    7b 22 ...

- Header = ASCII "AVON" (0x41564F4E)
- Seq    = running byte 1..254
- Cmd1   = opcode 17 ไบต์ (ต่างกันตามชนิดคำสั่ง)
- CRC    = คำนวณจาก (Header+Seq+Cmd1)  -> ดู calc_crc()
- Payload= JSON string ที่เข้ารหัสเป็น bytes (มีเฉพาะบางคำสั่ง เช่น Show/Clear/LORA/ROTATION)

ไฟล์นี้ใช้เพื่อ "แปลงคำสั่งเดิม" ให้ Bridge เข้าใจ แล้วนำไปยิง REST ระบบใหม่ต่อ
"""

import json

HEADER = bytes.fromhex("41564f4e")  # "AVON"
MIN_FRAME_LEN = 24                  # Header(4)+Seq(1)+Cmd1(17)+CRC(2)

# ---------------------------------------------------------------------------
# ตาราง opcode (cmd1 17 ไบต์) -> ชื่อคำสั่ง  (อ้างจาก LaneSocketControl_LSD.cs)
# ---------------------------------------------------------------------------
CMD1_LORA_SYNC = "0000005152990000000000750000000000"
CMD1_ROTATION  = "0000005152990000000000740000000000"
CMD1_SYNC1     = "00000051521600050b0000000000000000"
CMD1_SYNC2     = "00000051521e0005010000000000000000"
CMD1_SYNC3     = "0000005152260005000000000000000000"
CMD1_SHOW      = "00000051521e0009020000310000000000"   # Show ป้าย (activate)
CMD1_CLEAR     = "00000051521e000b020000310000000000"   # Clear ป้าย (deactivate)

_KNOWN_CMD1 = {
    CMD1_LORA_SYNC: "LORA_SYNC",
    CMD1_ROTATION:  "ROTATION",
    CMD1_SYNC1:     "SYNC1",
    CMD1_SYNC2:     "SYNC2",
    CMD1_SYNC3:     "SYNC3",
    CMD1_SHOW:      "SHOW",
    CMD1_CLEAR:     "CLEAR",
}

# KeepAlive เป็น frame ตายตัว (ไม่ผ่าน GenCmdLSD) — ตรวจด้วยการเทียบทั้ง frame
KEEPALIVE_FRAME = bytes.fromhex("41564f4e000000004f50030000000000000000000000d601")


def calc_crc(frame_without_crc):
    """คำนวณ CRC ตามอัลกอริทึมเดิม (GetCRC ใน LaneSocketControl_LSD.cs)

    lrc      = ผลรวมของทุกไบต์ใน (Header+Seq+Cmd1)
    lowByte  = ไบต์ต่ำสุดของ lrc
    return   = [lowByte, (0x02 ถ้า lowByte < 0x7F ไม่งั้น 0x01)]
    ใช้ตรวจความถูกต้อง / ใช้ตอนต้องประกอบ frame ตอบกลับ (ถ้าจำเป็น)
    """
    lrc = sum(frame_without_crc) & 0xFFFFFFFF
    low = lrc & 0xFF
    second = 0x02 if low < 0x7F else 0x01
    return bytes([low, second])


class AvonFrame(object):
    """ผลลัพธ์การ parse 1 frame"""

    def __init__(self, raw):
        self.raw = raw
        self.seq = None
        self.cmd1_hex = None
        self.crc = None
        self.payload = b""
        self.cmd_type = "UNKNOWN"   # LORA_SYNC / ROTATION / SYNC1-3 / SHOW / CLEAR / KEEPALIVE / UNKNOWN
        self.json = None            # dict ถ้า payload เป็น JSON
        self.identifier = None      # ค่า identifier (สำหรับ SHOW/CLEAR)

    def is_display_cmd(self):
        return self.cmd_type in ("SHOW", "CLEAR")

    def __str__(self):
        extra = ""
        if self.identifier:
            extra = " identifier=%s" % self.identifier
        return "AvonFrame(type=%s seq=%s len=%d%s)" % (
            self.cmd_type, self.seq, len(self.raw), extra)


def parse_frame(data):
    """แปลง bytes 1 frame -> AvonFrame (คืน None ถ้าไม่มี header เลย)"""
    idx = data.find(HEADER)
    if idx < 0:
        return None
    data = data[idx:]

    frame = AvonFrame(data)

    # KeepAlive: frame ตายตัว
    if data[:len(KEEPALIVE_FRAME)] == KEEPALIVE_FRAME:
        frame.cmd_type = "KEEPALIVE"
        frame.seq = data[4]
        return frame

    if len(data) < MIN_FRAME_LEN:
        return frame  # UNKNOWN (สั้นเกินไป)

    frame.seq = data[4]
    frame.cmd1_hex = data[5:22].hex()
    frame.crc = data[22:24]
    frame.payload = data[24:]
    frame.cmd_type = _KNOWN_CMD1.get(frame.cmd1_hex, "UNKNOWN")

    # ถอด JSON payload (สำหรับ SHOW/CLEAR จะได้ identifier)
    if frame.payload:
        try:
            text = frame.payload.decode("ascii", errors="ignore").strip("\x00 \r\n\t")
            if text.startswith("{") or text.startswith("["):
                frame.json = json.loads(text)
                if isinstance(frame.json, dict):
                    frame.identifier = frame.json.get("identifier")
        except (ValueError, UnicodeDecodeError):
            frame.json = None

    return frame


def split_frames(buffer):
    """แยก buffer ที่อาจมีหลาย frame ต่อกัน โดยตัดที่ Header "AVON"

    คืน (frames_bytes_list, leftover_bytes)
    - เนื่องจากระบบเดิมส่งทีละคำสั่งแล้วรอ response ปกติจะได้ frame เดียวต่อครั้ง
    - แต่รองรับกรณีต่อกันมาหลาย frame ด้วย
    """
    idx = buffer.find(HEADER)
    if idx < 0:
        return [], buffer  # ยังไม่เจอ header — เก็บไว้รอ

    # ตัด byte ขยะก่อน header แรกทิ้ง
    buffer = buffer[idx:]

    parts = []
    starts = []
    start = 0
    while True:
        nxt = buffer.find(HEADER, start + len(HEADER))
        starts.append(start)
        if nxt < 0:
            break
        start = nxt

    for i, s in enumerate(starts):
        e = starts[i + 1] if i + 1 < len(starts) else len(buffer)
        parts.append(buffer[s:e])

    # frame สุดท้ายอาจยังมาไม่ครบ (payload ขาด) — ถ้าเป็น SHOW/CLEAR ที่ payload
    # ยังไม่เป็น JSON สมบูรณ์ ให้เก็บเป็น leftover ไว้รอรอบหน้า
    if parts:
        last = parts[-1]
        f = parse_frame(last)
        if f is not None and f.is_display_cmd() and f.json is None and f.payload:
            leftover = parts.pop()   # ยังไม่ครบ เก็บไว้
            return parts, leftover

    return parts, b""
