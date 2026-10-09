#!/usr/bin/env python3
"""
ทดสอบตามแผน SHIFT_TEST_PLAN.md — จำลองพนักงานเก็บเงิน 1 คนทำงาน 1 กะ (ประมาณ 20–30 นาที) ครบทุกฟังก์ชัน
ทำทีละขั้นตามรหัส A1…G4 ในเอกสาร ด้วยคีย์บอร์ด (xdotool = เส้นทางคีย์บอร์ด USB จริง) ไม่ restart TCT ระหว่างกะ
ตรวจทุกขั้น: รายการที่ Sup Adjust / DC ได้รับ, วิธีชำระ, TA SoldCoupon, EDC, การตอบสนอง, โปรแกรมดับ, Error ใน log

รัน:  ./sim.sh shift [ตัวคูณเวลาพัก]      (2.0 = พักระหว่างขั้นแบบพนักงานจริง ≈ 25 นาที, 1.0 ≈ 17 นาที, 0.3 = เร็ว)
ผล:   TCT_Sim/results/shift_<เวลา>/  summary.txt + report.html + rows.json + tct_log.txt + ภาพทุกขั้น
"""
import html
import json
import re
import subprocess
import sys
import time
import urllib.request
from collections import Counter
from datetime import datetime
from pathlib import Path

import sim_kb_test as T          # helper ชุดเดียวกับ sim_kb_test (press / scn / post / state / edc_mode / Ctx)

SIM_DIR  = Path(__file__).resolve().parent
PACE     = float(sys.argv[1]) if len(sys.argv) > 1 else 2.0
RESULT   = SIM_DIR / "results" / ("shift_" + datetime.now().strftime("%Y%m%d_%H%M%S"))
T.RESULT = RESULT
SIM_IP   = "172.28.0.10"


# ------------------------------------------------------------------ helpers
def docker(*args):
    r = subprocess.run(["docker", "compose", *args], cwd=SIM_DIR, capture_output=True, text=True)
    return r.stdout


def tct_starts():
    return docker("logs", "tct").count("start TCT_App.exe")


def tct_rss_mb():
    out = docker("exec", "-T", "tct", "bash", "-c", "ps -o rss= -C mono | head -1").strip()
    return round(int(out) / 1024, 1) if out.isdigit() else None


def avc(cls, axle=2):
    T.post(action="avc", cls=cls, axle=axle)


def pass_car(cls=1, axle=2, wait=2.5):
    """รถวิ่งผ่าน Loop -> OB แล้ว AVC ส่งค่า (เหมือนช่องจริงที่มี AVC)"""
    T.scn("normal", 3.0)
    avc(cls, axle)
    time.sleep(wait)


def rest(sec):
    time.sleep(sec * PACE)


def ta_sold(c):
    return [it for it in T.state(c.m.seq)["logs"] if it["src"] == "TA" and "SoldCoupon" in it["text"]]


def edc_sales(c):
    return [it["text"] for it in T.state(c.m.seq)["logs"] if it["src"] == "EDC" and "ขอ 20" in it["text"]]


def rto_coupon(c):
    return [it for it in T.state(c.m.seq)["logs"] if it["src"] == "RTO" and "SALES_COUPON" in it["text"]]


def popup_closed(c):
    return any("SalesCoupon : popup closed" in l for l in c.m.lines())


def open_coupon(c, label="coupon_popup"):
    T.press("ขายคูปอง")
    time.sleep(3)
    c.shot(label)


# ------------------------------------------------------------------ ขั้นตอน (รหัสตรงกับ SHIFT_TEST_PLAN.md)
STEPS = []


def step(sid, title, exp=0, cls=None, pay=None, idle=12, keys=True):
    def deco(fn):
        STEPS.append({"id": sid, "title": title, "exp": exp, "cls": cls, "pay": pay, "idle": idle, "keys": keys, "fn": fn})
        return fn
    return deco


# ---- ช่วง A เปิดเครื่องและเข้ากะ
@step("A1", "เปิดโปรแกรม TCT → หน้า Login + เชื่อมต่อครบ", keys=False, idle=4)
def a1(c):
    T.restart_tct()
    c.shot("login")
    st = T.state()["status"]
    c.check(all(st.values()), f"เชื่อมต่อ {st}")
    c.log_has("TCT APP : Version", "โปรแกรมเริ่มทำงาน (บันทึกเวอร์ชัน)")


@step("A2", "[MANUAL] → 99001 [ENTER] → 123456 [ENTER] → เข้าหน้าหลัก", idle=6)
def a2(c):
    T.press("MANUAL")
    T.press("99001", "ENTER", "123456", "ENTER")
    time.sleep(4)
    c.shot("main")
    c.log_has("Login Manual Success")
    c.check(not any("123456" in l for l in c.m.lines()), "รหัสผ่านไม่ปรากฏใน log")
    c.log_has("BeginOfJob", "ส่ง Begin-of-Job (เกิดตอนเข้าหน้าหลัก)")
    c.log_has("Success CallWebService_GetCoupons", "โหลดคูปองจาก TA (เกิดตอนเข้าหน้าหลัก)")


@step("A3", "[OTL เปิด] → เปิดช่อง", idle=6)
def a3(c):
    T.press("OTL_เปิด")
    time.sleep(4)
    c.log_has("OTL_OPEN")
    T.edc_mode("approve", "paid")


@step("A4", "ตรวจสถานะอุปกรณ์ (EDC / DC)", keys=False, idle=4)
def a4(c):
    st = T.state()["status"]
    c.check(all(st.values()), f"สถานะเชื่อมต่อ {st}")
    c.shot("status")


# ---- ช่วง B รถปกติช่วงเช้า
@step("B1", "[รถประเภท 1] → [เงินสด] → รถผ่าน", exp=1, cls=1, pay="Cash")
def b1(c):
    T.press("รถประเภท1", "เงินสด")
    pass_car(1)


@step("B2", "[รถประเภท 1] → [เงินสด] → [พิมพ์ใบเสร็จ] → รถผ่าน", exp=1, cls=1, pay="Cash")
def b2(c):
    T.press("รถประเภท1", "เงินสด", "พิมพ์ใบเสร็จ")
    c.log_has("PRINT RECEIPT", "[พิมพ์ใบเสร็จ] ทำงาน")
    pass_car(1)


@step("B3", "[รถประเภท 2] → [เงินสด] → รถผ่าน (3 เพลา)", exp=1, cls=2, pay="Cash")
def b3(c):
    T.press("รถประเภท2", "เงินสด")
    pass_car(2, 3)


@step("B4", "[รถประเภท 1] → [คูปอง] → รถผ่าน", exp=1, cls=1, pay="Coupon")
def b4(c):
    T.press("รถประเภท1", "คูปอง")
    pass_car(1)


@step("B5", "[รถประเภท 2] → [คูปอง] → รถผ่าน", exp=1, cls=2, pay="Coupon")
def b5(c):
    T.press("รถประเภท2", "คูปอง")
    pass_car(2, 3)


@step("B6", "จองคันถัดไป: C1 เงินสด + [รถประเภท 2] → ผ่าน → [เงินสด] → ผ่าน", exp=2, cls=[1, 2])
def b6(c):
    T.press("รถประเภท1", "เงินสด", "รถประเภท2")
    pass_car(1, 2, 1.5)
    T.press("เงินสด")
    pass_car(2, 3)


@step("B7", "[รถประเภท 1] → [แก้ไข] → [รถประเภท 2] → [เงินสด] → รถผ่าน", exp=1, cls=2, pay="Cash")
def b7(c):
    T.press("รถประเภท1", "แก้ไข", "รถประเภท2", "เงินสด")
    pass_car(2, 3)


@step("B8", "[รถประเภท 1] → [เงินสด] → [แก้ไข] → [คูปอง] → รถผ่าน", exp=1, cls=1, pay="Coupon")
def b8(c):
    T.press("รถประเภท1", "เงินสด", "แก้ไข", "คูปอง")
    pass_car(1)


@step("B9", "รถประเภท 1 เงินสด 3 คันติดกัน", exp=3, idle=10)
def b9(c):
    for _ in range(3):
        T.press("รถประเภท1", "เงินสด")
        pass_car(1, 2, 1.5)


# ---- ช่วง C ชำระอิเล็กทรอนิกส์
@step("C1", "[รถประเภท 1] → [EMV] → EDC อนุมัติ → รถผ่าน", exp=1, cls=1, pay="EMV")
def c1(c):
    T.edc_mode("approve")
    T.press("รถประเภท1", "EMV")
    time.sleep(4)
    c.log_has("Response OK from EMV", "TCT รับผลอนุมัติจาก EDC")
    pass_car(1)


@step("C2", "[EMV] → EDC ปฏิเสธ → [แก้ไข] → [เงินสด] → รถผ่าน", exp=1, cls=1, pay="Cash")
def c2(c):
    T.edc_mode("decline")
    T.press("รถประเภท1", "EMV")
    time.sleep(4)
    c.shot("declined")
    T.press("แก้ไข")
    time.sleep(2)
    T.press("เงินสด")
    T.edc_mode("approve")
    pass_car(1)


@step("C3", "[EMV] → EDC ไม่ตอบ → [CANCEL] → [เงินสด] → รถผ่าน", exp=1, cls=1, pay="Cash")
def c3(c):
    T.edc_mode("timeout")
    T.press("รถประเภท1", "EMV")
    time.sleep(5)
    T.press("CANCEL")
    time.sleep(3)
    c.log_has("Response Cancel from EMV", "ยกเลิก popup EMV ได้")
    T.press("เงินสด")
    T.edc_mode("approve")
    pass_car(1)


@step("C4", "[QR CODE] → ลูกค้าสแกน → [ENTER] → รถผ่าน", exp=1, cls=1, pay="QR")
def c4(c):
    T.edc_mode("approve", "paid")
    T.press("รถประเภท1", "QR")
    time.sleep(6)
    T.press("ENTER")
    time.sleep(4)
    c.log_has("Response OK from QR Code", "QR ชำระสำเร็จ")
    pass_car(1)


@step("C5", "[QR CODE] → ยังไม่สแกน → [ENTER] → ยังไม่ชำระ → [เงินสด] → รถผ่าน", exp=1, cls=1, pay="Cash")
def c5(c):
    T.edc_mode("approve", "unpaid")
    T.press("รถประเภท1", "QR")
    time.sleep(6)
    T.press("ENTER")
    time.sleep(4)
    c.log_has("Response Reject", "แจ้งลูกค้ายังไม่สแกน")
    T.press("เงินสด")
    T.edc_mode("approve", "paid")
    pass_car(1)


@step("C6", "[รถประเภท 1] → [PM6] (QR KBank) → รถผ่าน", exp=1, cls=1)
def c6(c):
    T.press("รถประเภท1", "PM6")
    pass_car(1)


@step("C7", "[รถประเภท 2] → [บัตร DMT] → แตะบัตร → รถผ่าน", exp=1, cls=2, pay="DMT")
def c7(c):
    T.press("รถประเภท2", "บัตรDMT")
    time.sleep(2)
    T.post(action="scr", card="1CC70D9C")
    time.sleep(3)
    pass_car(2, 3)


# ---- ช่วง D ขายคูปอง
@step("D1", "[ขายคูปอง] → [1] → [เงินสด] → [ยืนยัน]", idle=8)
def d1(c):
    open_coupon(c)
    T.press("1", "เงินสด", "ยืนยัน")
    time.sleep(3)
    sold = ta_sold(c)
    c.check(len(sold) == 1 and (sold[0].get("data") or {}).get("paytypeid") == 0, f"TA ได้ SoldCoupon {len(sold)} ใบ (คาด 1 CASH)")
    c.check(len(rto_coupon(c)) >= 2, f"RTO ได้ SALES_COUPON เปิด/ปิด {len(rto_coupon(c))} ครั้ง")
    c.check(popup_closed(c), "popup ปิดหลังยืนยัน")


@step("D2", "[ขายคูปอง] → [1][2] (35) → [▶] → [1] (80) → [พิมพ์ใบเสร็จ]", idle=8)
def d2(c):
    open_coupon(c)
    T.press("1", "2", "▶", "1")
    c.shot("selected_3")
    T.press("พิมพ์ใบเสร็จ")
    time.sleep(3)
    sold = ta_sold(c)
    total = sum(float((it.get("data") or {}).get("price") or 0) for it in sold)
    c.check(len(sold) == 3 and total == 150, f"TA ได้ SoldCoupon {len(sold)} ใบ รวม {total:.0f} บาท (คาด 3 ใบ 150 บาท)")
    c.log_has("PrintSalesCoupon : sent", "พิมพ์ใบเสร็จคูปอง")


@step("D3", "[ขายคูปอง] → [1] → [EMV] อนุมัติ → [EMV] ซ้ำ → [แก้ไข] → [ยืนยัน] (บั๊ก 2.0.4)", idle=8)
def d3(c):
    T.edc_mode("approve")
    open_coupon(c)
    T.press("1", "EMV")
    time.sleep(5)
    T.press("EMV")
    time.sleep(2)
    T.press("แก้ไข")
    time.sleep(2)
    c.shot("after_retry_cancel")
    c.log_has("EMV ignored, already paid", "กด [EMV] ซ้ำถูกปฏิเสธ")
    c.log_has("cancel ignored, already paid", "[แก้ไข] หลังอนุมัติถูกปฏิเสธ")
    c.check(len(edc_sales(c)) == 1, f"EDC ได้คำขอชำระ {len(edc_sales(c))} ครั้ง (คาด 1)")
    T.press("ยืนยัน")
    time.sleep(3)
    sold = ta_sold(c)
    d = (sold[0].get("data") or {}) if sold else {}
    c.check(len(sold) == 1 and d.get("paytypeid") == 1 and bool(d.get("edccardno")),
            f"TA ได้ SoldCoupon {len(sold)} ใบ pay={d.get('paytypename')} card={d.get('edccardno')} (คาด 1 EMV + บัตร)")


@step("D4", "[ขายคูปอง] → [1] → [EMV] ปฏิเสธ → กลับเงินสด → [ยืนยัน]", idle=8)
def d4(c):
    T.edc_mode("decline")
    open_coupon(c)
    T.press("1", "EMV")
    time.sleep(4)
    if not any("EdcPaymentDlg_V1 closed" in l for l in c.m.lines()):
        T.press("แก้ไข")
        time.sleep(3)
    c.log_has("payment not approved -> back to pay Cash", "กลับเป็นเงินสด")
    T.edc_mode("approve")
    T.press("ยืนยัน")
    time.sleep(3)
    sold = ta_sold(c)
    c.check(len(sold) == 1 and (sold[0].get("data") or {}).get("paytypeid") == 0, f"TA ได้ SoldCoupon {len(sold)} ใบ (คาด 1 CASH)")


@step("D5", "รถจ่าย EMV (ยังไม่ผ่าน) → ขายคูปองด้วย EMV → รถผ่าน → ข้อมูลบัตรของรถไม่ปน (บั๊ก 2.0.4)", exp=1, cls=1, pay="EMV", idle=8)
def d5(c):
    T.edc_mode("approve")
    T.press("รถประเภท1", "EMV")
    time.sleep(4)
    open_coupon(c)
    T.press("1", "EMV")
    time.sleep(5)
    T.press("ยืนยัน")
    time.sleep(3)
    sold = ta_sold(c)
    c.check(len(sold) == 1, f"TA ได้ SoldCoupon {len(sold)} ใบ (คาด 1)")
    pass_car(1)
    time.sleep(2)
    dc = c.trans("DC")
    if dc and sold:
        edc = dc[-1].get("edcInfo") or {}
        cp = sold[0].get("data") or {}
        c.info(f"รถ: approve={edc.get('approveCode')} amount={edc.get('transAmount')} ref3={edc.get('transRef')} | คูปอง: ref3={cp.get('edcref3')}")
        c.check(bool(edc.get("approveCode")) and edc.get("transRef") != cp.get("edcref3") and str(edc.get("transAmount")) != "3500",
                "ข้อมูลบัตรในรายการรถเป็นของรถ ไม่ใช่ของคูปอง")


@step("D6", "[ขายคูปอง] → [1][1] → [ยืนยัน] (ไม่มีคูปอง) → Escape → [แก้ไข]", idle=8)
def d6(c):
    open_coupon(c)
    T.press("1", "1", "ยืนยัน")
    time.sleep(2)
    c.log_has("no coupon selected", "แจ้ง 'ไม่พบการเลือกคูปอง'")
    T.post(action="kb", k="Escape", name="Escape")
    time.sleep(2)
    c.check(not popup_closed(c), "Escape ไม่ปิด popup")
    T.press("แก้ไข")
    time.sleep(2)
    c.check(popup_closed(c), "[แก้ไข] ปิด popup")
    c.check(len(ta_sold(c)) == 0, "ไม่มีการขาย")


# ---- ช่วง E กรณีพิเศษ
@step("E1", "[รถยกเว้น] → รถผ่าน", exp=1, pay="EXEMPT")
def e1(c):
    T.press("รถยกเว้น")
    pass_car(2, 3)


@step("E2", "[รถออกซ้าย] → รถผ่าน", exp=1, pay="LEFT")
def e2(c):
    T.press("รถออกซ้าย")
    pass_car(1)


@step("E3", "[รถยกเว้น] → ผ่าน OB อย่างเดียว → Auto AVC (H2)", exp=1)
def e3(c):
    T.press("รถยกเว้น")
    T.scn("ob_only", 1.3)
    time.sleep(5)


@step("E4", "[รถประเภท 1] [เงินสด] → ผ่าน OB อย่างเดียว → [FORCE RESET] (H1)", exp=1, pay="Cash")
def e4(c):
    T.press("รถประเภท1", "เงินสด")
    T.scn("ob_only", 1.3)
    T.press("FORCE_RESET")
    time.sleep(2)


@step("E5", "[รถลากจูง] C2 เงินสด + C1 เงินสด → ผ่าน 2 คัน", exp=2, cls=[2, 1])
def e5(c):
    T.press("รถลากจูง", "รถประเภท2", "เงินสด", "รถประเภท1", "เงินสด")
    pass_car(2, 3, 2)
    pass_car(1, 2, 2.5)


@step("E6", "[รถลากจูง] C2 บัตร DMT + C1 เงินสด → ผ่าน 2 คัน", exp=2)
def e6(c):
    T.press("รถลากจูง", "รถประเภท2", "บัตรDMT")
    time.sleep(2)
    T.post(action="scr", card="1CC70D9C")
    time.sleep(3)
    T.press("รถประเภท1", "เงินสด")
    pass_car(2, 3, 2)
    pass_car(1, 2, 2.5)


@step("E7", "[รถลากจูง] จ่าย 2 คัน → ผ่านเป็นคันเดียว → [FORCE RESET]", exp=2)
def e7(c):
    T.press("รถลากจูง", "รถประเภท2", "เงินสด", "รถประเภท1", "เงินสด")
    T.scn("normal", 3.0)
    avc(2, 5)
    time.sleep(3)
    T.press("FORCE_RESET")
    time.sleep(3)


@step("E8", "[รถขบวน] HPMC → ผ่าน 2 คัน → [รถขบวน] ปิดโหมด", exp=2)
def e8(c):
    T.press("รถขบวน")
    pass_car(1, 2, 1.5)
    pass_car(2, 3, 1.5)
    T.press("รถขบวน")
    time.sleep(1.5)


@step("E9", "รถวิ่งผ่านโดยไม่กดอะไร → ไซเรน → [รถฝ่าด่าน]", exp=1)
def e9(c):
    T.scn("normal", 3.0)
    time.sleep(5)
    T.press("รถฝ่าด่าน")
    time.sleep(1)


@step("E10", "[FORCE RESET] ตอนช่องว่าง", exp=0, idle=6)
def e10(c):
    T.press("FORCE_RESET")
    time.sleep(1.5)


@step("E11", "[OTL ปิด] → [OTL เปิด]", exp=0, idle=6)
def e11(c):
    T.press("OTL_ปิด")
    time.sleep(1)
    c.log_has("OTL_CLOSE")
    T.press("OTL_เปิด")
    time.sleep(1)
    c.log_has("OTL_OPEN")


# ---- ช่วง F ช่วงรถหนาแน่น
@step("F1", "รถ 6 คันติดกัน: C1 เงินสด, C1 เงินสด, C2 เงินสด, C1 คูปอง, C1 PM6, C2 คูปอง", exp=6, idle=3)
def f1(c):
    for keys, cls, axle in [(("รถประเภท1", "เงินสด"), 1, 2), (("รถประเภท1", "เงินสด"), 1, 2), (("รถประเภท2", "เงินสด"), 2, 3),
                            (("รถประเภท1", "คูปอง"), 1, 2), (("รถประเภท1", "PM6"), 1, 2), (("รถประเภท2", "คูปอง"), 2, 3)]:
        T.press(*keys)
        pass_car(cls, axle, 1.5)


@step("F2", "จองคันถัดไป 2 รอบ (C1+C2, C2+C1)", exp=4, cls=[1, 2, 2, 1], idle=3)
def f2(c):
    T.press("รถประเภท1", "เงินสด", "รถประเภท2")
    pass_car(1, 2, 1.5)
    T.press("เงินสด")
    pass_car(2, 3, 2)
    T.press("รถประเภท2", "เงินสด", "รถประเภท1")
    pass_car(2, 3, 1.5)
    T.press("คูปอง")
    pass_car(1, 2, 2)


@step("F3", "รถ 6 คันติดกัน: C1 เงินสด ×2, C1 EMV, C2 เงินสด, C1 เงินสด, C1 คูปอง", exp=6, idle=3)
def f3(c):
    T.edc_mode("approve")
    T.press("รถประเภท1", "เงินสด")
    pass_car(1, 2, 1.5)
    T.press("รถประเภท1", "เงินสด")
    pass_car(1, 2, 1.5)
    T.press("รถประเภท1", "EMV")
    time.sleep(4)
    pass_car(1, 2, 1.5)
    T.press("รถประเภท2", "เงินสด")
    pass_car(2, 3, 1.5)
    T.press("รถประเภท1", "เงินสด")
    pass_car(1, 2, 1.5)
    T.press("รถประเภท1", "คูปอง")
    pass_car(1, 2, 1.5)
    emv = [t for t in c.trans("SUPADJ") if "EMV" in str((t.get("payInfo") or {}).get("payTypeName")).upper()]
    c.check(len(emv) == 1, f"รายการ EMV {len(emv)} รายการ (คาด 1)")


# ---- ช่วง G ปิดกะและโหมดช่าง
@step("G1", "[รถประเภท 1] [เงินสด] [พิมพ์ใบเสร็จ] ×2 เร็ว ๆ → รถผ่าน (พิมพ์ครั้งเดียว)", exp=1, pay="Cash")
def g1(c):
    T.press("รถประเภท1", "เงินสด", "พิมพ์ใบเสร็จ", "พิมพ์ใบเสร็จ")
    time.sleep(1)
    n = sum("Press PRINT RECEIPT" in l for l in c.m.lines())
    c.check(n == 1, f"พิมพ์ใบเสร็จ {n} ครั้ง (คาด 1 — ครั้งที่ 2 ถูกกันภายใน 2 วิ)")
    pass_car(1)


@step("G2", "[OTL ปิด] → สวิตช์ OTL ลง → [BOJ/EOJ] → End-of-Job → หน้า Login", idle=6)
def g2(c):
    T.press("OTL_ปิด")
    T.post(action="di", idx=7, value=0)
    time.sleep(1)
    T.press("BOJ/EOJ")
    time.sleep(6)
    c.shot("after_eoj")
    c.log_has("END_SHIFT", "ส่ง END_SHIFT")
    c.log_has("EndOfJob", "ส่ง End-of-Job")


@step("G3", "[MODE] → [1] → [CANCEL]", idle=4)
def g3(c):
    T.press("MODE")
    time.sleep(2)
    c.shot("mode_popup")
    T.press("1")
    time.sleep(2)
    T.press("CANCEL")
    time.sleep(1)
    c.log_has("MODE", "[MODE] ถึง TCT")


@step("G4", "Login ช่าง 99050 → เมนู [2] [1] [1] → [CANCEL] → [◀][◀]", idle=2)
def g4(c):
    T.kb_login("99050")
    c.shot("maint_menu")
    c.log_has("Login Manual Success", "Login ช่างสำเร็จ")
    T.press("2")
    time.sleep(1.5)
    T.press("1")
    time.sleep(1.5)
    T.press("1")
    time.sleep(2)
    c.shot("maint_submenu")
    T.press("CANCEL")
    time.sleep(1.5)
    T.press("◀", "◀")
    time.sleep(1)
    c.shot("maint_back")


# ------------------------------------------------------------------ main
def main():
    RESULT.mkdir(parents=True, exist_ok=True)
    if not T.wait_ready(60):
        sys.exit("✘ simulator ยังไม่พร้อม — ./sim.sh start ก่อน")
    exp_total = sum(s["exp"] for s in STEPS)
    print(f"+ Shift test ตาม SHIFT_TEST_PLAN.md : {len(STEPS)} ขั้น, คาดรายการรถ {exp_total}, pace x{PACE:g}", flush=True)

    T.post(action="edc", mode="approve", qr="paid")
    T.post(action="ta", mode="ok")
    rows, problems = [], []
    t0 = time.time()
    starts0 = None
    rss = []
    seq0 = T.state()["seq"]
    mark_all = None

    for s in STEPS:
        c = T.Ctx(s["id"])
        ts = time.time()
        print(f"\n[{int(ts - t0) // 60:02d}:{int(ts - t0) % 60:02d}] === {s['id']}  {s['title']}", flush=True)
        try:
            s["fn"](c)
        except Exception as e:
            c.check(False, f"exception ใน script: {e!r}")
        time.sleep(1.0)
        if s["id"] == "A1":
            starts0 = tct_starts()
            mark_all = T.Mark()
            rss.append((0, tct_rss_mb()))
        if s["exp"] or s["cls"] is not None or s["pay"] is not None:
            c.expect_trans(s["exp"], cls=s["cls"], pay=s["pay"])
        alive = True
        if s["keys"]:
            alive = any(("Keyboard (Key)" in l) or ("IO - " in l) for l in c.m.lines())
            c.check(alive, "TCT ตอบสนองต่อคีย์/IO ในขั้นนี้")
        crashed = starts0 is not None and tct_starts() != starts0
        if crashed:
            c.check(False, "TCT ดับ / ถูก start ใหม่")
        ok = c.ok
        rows.append({"id": s["id"], "t": int(ts - t0), "title": s["title"], "exp": s["exp"], "ok": ok,
                     "notes": c.notes, "sec": round(time.time() - ts, 1)})
        print("\n".join(c.notes))
        print(f"    ==> {'PASS ✅' if ok else 'FAIL ❌'}  ({rows[-1]['sec']} s)", flush=True)
        if not ok:
            problems.append(f"{s['id']} {s['title']}")
            c.shot("FAIL")
            if crashed:
                break
            if s["id"] not in ("G2", "G3", "G4"):
                T.press("FORCE_RESET")
                time.sleep(2)
        if s["id"] in ("B9", "C7", "D6", "E11", "F3"):
            rss.append((int(time.time() - t0), tct_rss_mb()))
        rest(s["idle"])

    # ------------------------------------------------------------ สรุป
    time.sleep(3)
    rss.append((int(time.time() - t0), tct_rss_mb()))
    sup = dc = 0
    for it in T.state(seq0)["logs"]:
        if it["level"] == "trans":
            sup += it["src"] == "SUPADJ"
            dc += it["src"] == "DC"
    sold = sum(1 for it in T.state(seq0)["logs"] if it["src"] == "TA" and "SoldCoupon" in it["text"])
    crashes = (tct_starts() - starts0) if starts0 is not None else -1
    lines = mark_all.lines() if mark_all else []
    expected_err = ("INSUFFICIENT", "NOT PAID", "Reject", "timeout", "Timeout", "Cancel", "TestNetworkConnection")
    errors = [l for l in lines if re.search(r"Exception|- Error", l) and not any(k in l for k in expected_err)]
    err_kinds = Counter(re.sub(r"^\S+ \S+ : (\[\w+ ?\] )?", "", e)[:100] for e in errors)
    passed = sum(r["ok"] for r in rows)
    dur = round((time.time() - t0) / 60, 1)
    mem_ok = (rss[0][1] is not None and rss[-1][1] is not None and rss[-1][1] - rss[0][1] < 50)
    summary = [
        f"TCT Shift test (SHIFT_TEST_PLAN.md) — {datetime.now():%Y-%m-%d %H:%M:%S}",
        f"ระยะเวลา {dur} นาที (pace x{PACE:g}), {len(rows)} ขั้น, ผ่าน {passed}/{len(rows)}",
        f"รายการรถที่คาด {exp_total} | Sup Adjust ได้ {sup} | DC ได้ {dc}",
        f"ขายคูปอง TA SoldCoupon {sold} ใบ (คาด 7)",
        f"TCT ดับ/restart: {crashes} ครั้ง",
        f"หน่วยความจำ mono (MB): " + ", ".join(f"{t}s={m}" for t, m in rss) + ("" if mem_ok else "   [โตเกิน 50 MB]"),
        f"Error/Exception ใหม่ใน log (ไม่นับที่คาดไว้): {len(errors)} บรรทัด",
        "",
        "ผลรายขั้น:",
        *[f"  [{'PASS' if r['ok'] else 'FAIL'}] {r['id']:4s} {r['t'] // 60:02d}:{r['t'] % 60:02d}  {r['title']}" for r in rows],
        "",
        "ขั้นที่ไม่ผ่าน:" if problems else "ขั้นที่ไม่ผ่าน: ไม่มี",
        *[f"  - {p}" for p in problems],
        "",
        "ชนิด Error ใน log (จำนวน):" if err_kinds else "",
        *[f"  {n:4d} × {k}" for k, n in err_kinds.most_common(20)],
    ]
    (RESULT / "summary.txt").write_text("\n".join(summary), encoding="utf-8")
    (RESULT / "tct_log.txt").write_text("\n".join(lines), encoding="utf-8")
    (RESULT / "rows.json").write_text(json.dumps(rows, ensure_ascii=False, indent=1), encoding="utf-8")
    write_html(summary, rows)
    print("\n" + "\n".join(summary))
    print(f"\nผลอยู่ที่ {RESULT}")
    sys.exit(0 if (passed == len(rows) and crashes == 0 and sup == exp_total == dc and sold == 7) else 1)


def write_html(summary, rows):
    tr = "".join(
        f"<tr class='{'ok' if r['ok'] else 'bad'}'><td>{r['id']}</td><td>{r['t'] // 60:02d}:{r['t'] % 60:02d}</td>"
        f"<td>{html.escape(r['title'])}</td><td>{r['exp']}</td><td>{'ผ่าน' if r['ok'] else 'ไม่ผ่าน'}</td>"
        f"<td><pre>{html.escape(chr(10).join(r['notes']))}</pre></td></tr>" for r in rows)
    imgs = "".join(f"<figure><img src='{p.name}'><figcaption>{p.name}</figcaption></figure>"
                   for p in sorted(RESULT.glob("*.png")))
    page = f"""<!doctype html><html lang="th"><head><meta charset="utf-8"><title>TCT Shift Test</title>
<style>body{{font:14px system-ui,sans-serif;margin:20px;background:#14171c;color:#e6e9ee}}
pre{{background:#1d222a;padding:8px;border-radius:8px;white-space:pre-wrap;margin:0}}
table{{border-collapse:collapse;width:100%}}td,th{{border-bottom:1px solid #2c333d;padding:4px 8px;text-align:left;vertical-align:top}}
tr.bad td{{background:#3a1d1d}}tr.ok td:nth-child(5){{color:#7ee08a}}
figure{{display:inline-block;margin:8px}}img{{width:320px;border:1px solid #333}}figcaption{{font-size:12px;color:#9aa}}</style></head>
<body><h1>TCT Shift Test — ตาม SHIFT_TEST_PLAN.md</h1><pre>{html.escape(chr(10).join(summary))}</pre>
<h2>รายขั้น</h2><table><tr><th>รหัส</th><th>เวลา</th><th>ขั้นตอน</th><th>คาด</th><th>ผล</th><th>รายละเอียด</th></tr>{tr}</table>
<h2>ภาพหน้าจอ</h2>{imgs}</body></html>"""
    (RESULT / "report.html").write_text(page, encoding="utf-8")


if __name__ == "__main__":
    main()
