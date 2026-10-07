#!/usr/bin/env python3
"""
เดินทุกหน้าจอ / Popup ของ TCT แล้วเก็บภาพ (ใช้ตรวจความสวยงาม / ข้อความทับกัน / เส้น)
รัน: python3 gui_tour.py   -> results/gui_<เวลา>/  (ภาพทีละหน้า + sheet_*.png รวมภาพ)
"""
import subprocess
import time
from datetime import datetime
from pathlib import Path

import sim_kb_test as T

SIM_DIR  = Path(__file__).resolve().parent
OUT      = SIM_DIR / "results" / ("gui_" + datetime.now().strftime("%Y%m%d_%H%M%S"))
T.RESULT = OUT
shots    = []


def shot(name):
    import urllib.request
    with urllib.request.urlopen(f"{T.PANEL}/api/shot", timeout=15) as r:
        (OUT / f"{name}.png").write_bytes(r.read())
    shots.append(f"{name}.png")
    print("  +", name, flush=True)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    T.restart_tct()
    time.sleep(2)
    shot("01_login")
    T.press("MANUAL")
    shot("02_login_manual")
    T.press("99001", "ENTER", "123456", "ENTER")
    time.sleep(4)
    shot("03_main_lane_closed")
    T.press("OTL_เปิด")
    time.sleep(1.5)
    shot("04_main_lane_open")
    T.press("รถประเภท1")
    shot("05_class1_selected")
    T.press("เงินสด")
    time.sleep(1)
    shot("06_class1_paid_cash")
    T.press("รถประเภท2")                      # จองคันถัดไป
    shot("07_next_car_queued")
    T.press("FORCE_RESET")
    time.sleep(2)
    T.press("รถยกเว้น")
    shot("08_exempt")
    T.press("FORCE_RESET")
    time.sleep(2)
    T.scn("normal", 3)
    time.sleep(4)
    shot("09_violation")
    T.press("รถฝ่าด่าน")
    time.sleep(2)
    T.press("รถลากจูง", "รถประเภท2", "เงินสด")
    shot("10_towing_first_paid")
    T.press("FORCE_RESET")
    time.sleep(3)
    T.edc_mode("timeout")
    T.press("รถประเภท1", "EMV")
    time.sleep(3)
    shot("11_popup_emv")
    T.press("แก้ไข")
    time.sleep(3)
    T.press("FORCE_RESET")
    T.edc_mode("approve", "paid")
    T.press("รถประเภท1", "QR")
    time.sleep(6)
    shot("12_popup_qr")
    T.press("CANCEL")
    time.sleep(5)
    T.press("FORCE_RESET")
    T.press("รถประเภท1", "บัตรDMT")
    time.sleep(2)
    shot("13_popup_dmt")
    T.press("CANCEL")
    time.sleep(2)
    T.press("FORCE_RESET")
    T.press("ขายคูปอง")
    time.sleep(3)
    T.press("1")
    shot("14_popup_sale_coupon")
    T.press("แก้ไข")
    time.sleep(2)
    T.press("รถขบวนจ่ายเงิน")
    time.sleep(2)
    T.press("2", "ENTER", "1")
    shot("15_popup_convoy")
    T.press("CANCEL")
    time.sleep(2)
    T.press("รถขบวน")
    time.sleep(2)
    shot("16_hpmc")
    T.press("รถขบวน")
    time.sleep(2)
    T.press("OTL_ปิด")
    T.post(action="di", idx=7, value=0)
    time.sleep(1)
    T.press("BOJ/EOJ")
    time.sleep(5)
    T.press("MODE")
    time.sleep(2)
    shot("17_popup_lane_type")
    T.press("CANCEL")
    time.sleep(2)
    T.kb_login("99050")
    shot("18_maint_menu")
    T.press("2")
    time.sleep(1.5)
    shot("19_maint_equipment")
    T.press("▼", "1", "▶")
    time.sleep(1.5)
    T.press("1")
    time.sleep(2.5)
    shot("20_maint_digital_input")
    T.press("CANCEL")
    time.sleep(2)

    # รวมภาพเป็น sheet ละ 8 หน้า (ใช้ ImageMagick ในเครื่อง TCT)
    for f in shots:
        subprocess.run(["docker", "compose", "cp", str(OUT / f), f"tct:/tmp/{f}"], cwd=SIM_DIR,
                       capture_output=True)
    for i in range(0, len(shots), 8):
        part = " ".join(shots[i:i + 8])
        with open(OUT / f"sheet_{i // 8 + 1}.png", "wb") as fh:
            subprocess.run(["docker", "compose", "exec", "-T", "tct", "bash", "-c",
                            f"cd /tmp && montage {part} -tile 4x2 -geometry 512x384+4+4 -background '#111' "
                            f"-title '' png:-"], cwd=SIM_DIR, stdout=fh, stderr=subprocess.DEVNULL)
    print("ผลอยู่ที่", OUT)


if __name__ == "__main__":
    main()
