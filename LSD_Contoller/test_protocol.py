"""
test_protocol.py — ทดสอบ avon_protocol ด้วยไบต์จริงจาก log ระบบเดิม (ไม่ต้องมีฮาร์ดแวร์)
รัน:  python3 test_protocol.py
"""
import avon_protocol as avon

# ไบต์จริงที่ดักได้จาก log LSD ของระบบเดิม
SAMPLES = {
    "KEEPALIVE": "41564f4e000000004f50030000000000000000000000d601",
    "LORA_SYNC": "41564f4e010000005152990000000000750000000000e601"
                 "7b22736f75726365223a7b2274797065223a312c22706c6174666f726d223a327d2c227461736b4172726179"
                 "223a5b7b2274797065223a224c4f52415f53594e43222c22616374696f6e223a352c22737461747573223a30"
                 "2c226572726f72436f6465223a302c2264617461223a6e756c6c7d5d7d",
    "SYNC1":     "41564f4e0300000051521600050b00000000000000000002",
    "SYNC2":     "41564f4e0400000051521e0005010000000000000000ff01",
    "SYNC3":     "41564f4e0500000051522600050000000000000000000702",
    "SHOW":      "41564f4e0600000051521e000902000031000000000037027b226964656e74696669"
                 "6572223a223662656438666263383835333464633237353639323536396531353336333365227d",
}

EXPECT_SHOW_IDENT = "6bed8fbc88534dc275692569e153633e"

passed = failed = 0


def check(name, cond, detail=""):
    global passed, failed
    if cond:
        passed += 1
        print("  PASS  %-10s %s" % (name, detail))
    else:
        failed += 1
        print("  FAIL  %-10s %s" % (name, detail))


print("=== 1) ทดสอบ classify + identifier ===")
for name, hexstr in SAMPLES.items():
    f = avon.parse_frame(bytes.fromhex(hexstr))
    check(name, f is not None and f.cmd_type == name,
          "-> type=%s seq=%s ident=%s" % (f.cmd_type, f.seq, f.identifier))
    if name == "SHOW":
        check("SHOW.id", f.identifier == EXPECT_SHOW_IDENT, "-> %s" % f.identifier)

print("\n=== 2) ทดสอบ CRC (เทียบกับ crc ที่ดักได้จริง) ===")
# SYNC1 -> crc ในสาย = 0002
for name in ("SYNC1", "SYNC2", "SYNC3"):
    raw = bytes.fromhex(SAMPLES[name])
    body = raw[:22]          # Header+Seq+Cmd1
    on_wire = raw[22:24]     # crc จริงในสาย
    calc = avon.calc_crc(body)
    check("crc:" + name, calc == on_wire, "wire=%s calc=%s" % (on_wire.hex(), calc.hex()))

print("\n=== 3) ทดสอบ split หลาย frame ต่อกัน ===")
combo = bytes.fromhex(SAMPLES["SYNC1"]) + bytes.fromhex(SAMPLES["SYNC2"]) + bytes.fromhex(SAMPLES["SYNC3"])
frames, leftover = avon.split_frames(combo)
check("split", len(frames) == 3 and leftover == b"", "-> %d frames, leftover=%d" % (len(frames), len(leftover)))

print("\n=== 4) map identifier -> vsn (ตัวอย่าง config) ===")
id_map = {
    "6bed8fbc88534dc275692569e153633e": "CLOSE.vsn",
    "993bd1ac3e9c645360110be0aa4a0f83": "MTC.vsn",
    "f5e7e2d08013ceed5752de941f22c266": "ETC.vsn",
    "80b019ebaea588cbd8135b8753b48922": "ETC-MTC.vsn",
}
f = avon.parse_frame(bytes.fromhex(SAMPLES["SHOW"]))
check("map", id_map.get(f.identifier) == "CLOSE.vsn", "-> %s" % id_map.get(f.identifier))

print("\n================  %d passed, %d failed  ================" % (passed, failed))
raise SystemExit(0 if failed == 0 else 1)
