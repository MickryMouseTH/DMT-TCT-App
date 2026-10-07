#!/usr/bin/env python3
"""
ทดสอบแบบใช้งานจริงต่อเนื่อง (Soak / Shift simulation) — ไม่ restart TCT ตลอดการทดสอบ
จำลองพนักงานเก็บเงิน 1 คน + รถเข้าช่องต่อเนื่องแบบสุ่ม ครบทุก logic:
  เงินสด / คูปอง / QR(KBank) / EMV อนุมัติ-ปฏิเสธ / QR จ่าย-ยังไม่จ่าย / รถยกเว้น / รถออกซ้าย /
  OB อย่างเดียว (H1,H2) / รถลาก / รถขบวน HPMC / รถฝ่าด่าน / แก้ไข / จองคันถัดไป / ขายคูปอง /
  พิมพ์ใบเสร็จ / Force Reset

ตรวจระหว่างรัน:
  - รายการที่ควรเกิด (คาดการณ์ทีละคัน) เทียบกับที่ Sup Adjust และ DC ได้รับจริง
  - TCT ดับ (process restart), ค้าง (กดแป้นแล้วไม่ตอบ), Exception / Error ใน log, หน่วยความจำ (RSS) โต

รัน:  ./sim.sh soak [นาที]        (ค่าเริ่มต้น 10 นาที)
ผล:   TCT_Sim/results/soak_<เวลา>/  report.html + summary.txt + ภาพหน้าจอ + log
"""
import html
import json
import random
import re
import subprocess
import sys
import time
from collections import Counter, defaultdict
from datetime import datetime
from pathlib import Path

import sim_kb_test as T          # ใช้ helper ชุดเดียวกับ sim_kb_test (press / scn / post / state / edc_mode)

SIM_DIR  = Path(__file__).resolve().parent
MINUTES  = float(sys.argv[1]) if len(sys.argv) > 1 else 10.0
SEED     = int(sys.argv[2]) if len(sys.argv) > 2 else int(time.time())
RESULT   = SIM_DIR / "results" / ("soak_" + datetime.now().strftime("%Y%m%d_%H%M%S"))
rng      = random.Random(SEED)
T.RESULT = RESULT


# ------------------------------------------------------------------ helpers
def docker(*args, capture=True):
    r = subprocess.run(["docker", "compose", *args], cwd=SIM_DIR, capture_output=capture, text=True)
    return r.stdout if capture else ""


def tct_starts():
    """จำนวนครั้งที่ TCT_App.exe ถูก start (ถ้าเพิ่ม = โปรแกรมดับแล้วถูก start ใหม่)"""
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


def snap(tag):
    try:
        with __import__("urllib.request").request.urlopen(f"{T.PANEL}/api/shot", timeout=15) as r:
            (RESULT / f"{tag}.png").write_bytes(r.read())
        return f"{tag}.png"
    except Exception:
        return ""


def count_trans(seq):
    sup = dc = 0
    for it in T.state(seq)["logs"]:
        if it["level"] == "trans":
            if it["src"] == "SUPADJ":
                sup += 1
            elif it["src"] == "DC":
                dc += 1
    return sup, dc


def ta_sold(seq):
    return sum(1 for it in T.state(seq)["logs"] if it["src"] == "TA" and "SoldCoupon" in it["text"])


# ------------------------------------------------------------------ scenarios: (ชื่อ, น้ำหนัก, ฟังก์ชัน) ฟังก์ชันคืน จำนวนรายการที่ควรเกิด
SCN = []


def scenario(name, weight):
    def deco(fn):
        SCN.append((name, weight, fn))
        return fn
    return deco


@scenario("class1_cash", 22)
def s_c1():
    T.press("รถประเภท1", "เงินสด")
    if rng.random() < 0.3:
        T.press("พิมพ์ใบเสร็จ")
    pass_car(1)
    return 1


@scenario("class2_cash", 8)
def s_c2():
    T.press("รถประเภท2", "เงินสด")
    pass_car(2, 3)
    return 1


@scenario("coupon", 5)
def s_coupon():
    T.press(rng.choice(["รถประเภท1", "รถประเภท2"]), "คูปอง")
    pass_car(1)
    return 1


@scenario("qr_kbank", 4)
def s_pm6():
    T.press("รถประเภท1", "PM6")
    pass_car(1)
    return 1


@scenario("emv_approve", 5)
def s_emv():
    T.edc_mode("approve")
    T.press("รถประเภท1", "EMV")
    time.sleep(2.5)
    pass_car(1)
    return 1


@scenario("emv_decline_then_cash", 2)
def s_emv_dec():
    T.edc_mode("decline")
    T.press("รถประเภท1", "EMV")
    time.sleep(3)
    T.press("แก้ไข")
    time.sleep(2)
    T.press("เงินสด")
    T.edc_mode("approve")
    pass_car(1)
    return 1


@scenario("qr_paid", 4)
def s_qr():
    T.edc_mode("approve", "paid")
    T.press("รถประเภท1", "QR")
    time.sleep(6)
    T.press("ENTER")
    time.sleep(3)
    pass_car(1)
    return 1


@scenario("qr_unpaid_cancel_cash", 2)
def s_qr_unpaid():
    T.edc_mode("approve", "unpaid")
    T.press("รถประเภท1", "QR")
    time.sleep(6)
    T.press("ENTER")                     # ยังไม่จ่าย -> popup ปิด (Reject)
    time.sleep(3)
    T.press("เงินสด")
    T.edc_mode("approve", "paid")
    pass_car(1)
    return 1


@scenario("exempt", 5)
def s_ex():
    T.press("รถยกเว้น")
    pass_car(2, 3)
    return 1


@scenario("left_exit", 3)
def s_left():
    T.press("รถออกซ้าย")
    pass_car(1)
    return 1


@scenario("exempt_ob_only_auto_avc_H2", 3)
def s_h2():
    T.press("รถยกเว้น")
    T.scn("ob_only", 1.3)
    time.sleep(5)                        # Auto AVC (OB only)
    return 1


@scenario("cash_ob_only_force_reset_H1", 3)
def s_h1():
    T.press("รถประเภท1", "เงินสด")
    T.scn("ob_only", 1.3)
    T.press("FORCE_RESET")
    time.sleep(2)
    return 1


@scenario("towing", 2)
def s_tow():
    T.press("รถลากจูง", "รถประเภท2", "เงินสด", "รถประเภท1", "เงินสด")
    pass_car(2, 3, 2)
    pass_car(1, 2, 2.5)
    return 2


@scenario("towing_dmt_plus_cash", 2)
def s_tow_dmt():
    T.press("รถลากจูง", "รถประเภท2", "บัตรDMT")
    time.sleep(2)
    T.post(action="scr", card="1CC70D9C")   # บัตร DMT class 2
    time.sleep(3)
    T.press("รถประเภท1", "เงินสด")
    pass_car(2, 3, 2)
    pass_car(1, 2, 2.5)
    return 2


@scenario("towing_unsplit_force_reset", 2)
def s_tow_unsplit():
    T.press("รถลากจูง", "รถประเภท2", "เงินสด", "รถประเภท1", "เงินสด")
    T.scn("normal", 3.0)                     # ผ่านเป็นคันเดียว ตัดคันไม่ได้
    if rng.random() < 0.5:
        avc(2, 5)                             # AVC เห็นคันเดียว
        time.sleep(3)
    else:
        time.sleep(5)                         # AVC ไม่ส่งค่า
    T.press("FORCE_RESET")                    # พนักงานกด Force Reset
    time.sleep(3)
    return 2


@scenario("hpmc_2_cars", 2)
def s_hpmc():
    T.press("รถขบวน")
    pass_car(1, 2, 1.5)
    pass_car(2, 3, 1.5)
    T.press("รถขบวน")
    time.sleep(1.5)
    return 2


@scenario("violation", 3)
def s_vio():
    T.scn("normal", 3.0)
    time.sleep(5)                        # Auto AVC -> รายการ violation
    T.press("รถฝ่าด่าน")                  # ปิดเสียง siren
    time.sleep(1)
    return 1


@scenario("edit_reclass", 3)
def s_edit():
    T.press("รถประเภท1", "แก้ไข", "รถประเภท2", "เงินสด")
    pass_car(2, 3)
    return 1


@scenario("edit_repay", 2)
def s_edit2():
    T.press("รถประเภท1", "เงินสด", "แก้ไข", "คูปอง")
    pass_car(1)
    return 1


@scenario("next_car_queue", 5)
def s_next():
    T.press("รถประเภท1", "เงินสด", "รถประเภท2")      # คันที่ 2 จองไว้
    pass_car(1, 2, 1.5)
    T.press("เงินสด")
    pass_car(2, 3)
    return 2


@scenario("force_reset_idle", 2)
def s_fr():
    T.press("FORCE_RESET")
    time.sleep(1.5)
    return 0


@scenario("sale_coupon", 2)
def s_sale():
    T.press("ขายคูปอง")
    time.sleep(3)
    T.press("1", "เงินสด", "ยืนยัน")
    time.sleep(3)
    return 0                             # ไม่ใช่รายการรถ (ตรวจ TA แยก)


# ------------------------------------------------------------------ main
def main():
    RESULT.mkdir(parents=True, exist_ok=True)
    if not T.wait_ready(60):
        sys.exit("✘ simulator ยังไม่พร้อม — ./sim.sh start ก่อน")

    print(f"+ Soak test {MINUTES:g} นาที (seed={SEED}) — ไม่ restart TCT", flush=True)
    T.restart_tct()                      # เริ่มกะครั้งเดียว: TCT ใหม่ + login + เปิดช่อง
    T.kb_login()
    T.press("OTL_เปิด")
    time.sleep(2)
    T.edc_mode("approve", "paid")

    starts0   = tct_starts()
    mark      = T.Mark()
    seq0      = mark.seq
    rss       = [(0, tct_rss_mb())]
    rows      = []                       # ผลทีละคัน
    expected  = 0
    problems  = []
    t0        = time.time()
    shots     = [snap("00_start")]
    n         = 0
    names, weights, fns = zip(*[(a, w, f) for a, w, f in SCN])
    first_round = list(range(len(SCN)))
    rng.shuffle(first_round)

    while time.time() - t0 < MINUTES * 60:
        n += 1
        # รอบแรกให้ครบทุก logic อย่างน้อย 1 ครั้ง (สุ่มลำดับ) จากนั้นสุ่มตามสัดส่วนรถจริง
        i     = first_round.pop() if first_round else rng.choices(range(len(SCN)), weights=weights)[0]
        name  = names[i]
        seq_b = T.state()["seq"]
        sup_b, dc_b = count_trans(seq0)
        act_b = sum(1 for l in mark.lines() if "Keyboard (Key)" in l or "IO - " in l)
        ts    = time.time()
        try:
            exp = fns[i]()
        except Exception as e:
            exp = 0
            problems.append(f"#{n} {name}: exception ใน script ทดสอบ {e!r}")
        time.sleep(1.0)
        expected += exp
        sup_a, dc_a = count_trans(seq0)
        got_sup, got_dc = sup_a - sup_b, dc_a - dc_b

        # ตอบสนอง: ต้องมี log การกดแป้น/IO ใหม่ระหว่างทำ scenario
        act_a = sum(1 for l in mark.lines() if "Keyboard (Key)" in l or "IO - " in l)
        alive = act_a > act_b
        crashed = tct_starts() != starts0
        ok = (got_sup == exp) and (got_dc == exp) and alive and not crashed
        rows.append({"n": n, "t": round(ts - t0), "name": name, "exp": exp, "sup": got_sup, "dc": got_dc,
                     "alive": alive, "ok": ok})
        flag = "✔" if ok else "✘"
        print(f"  {flag} #{n:03d} {int(ts - t0):4d}s {name:32s} คาด {exp}  SupAdj {got_sup}  DC {got_dc}"
              + ("" if alive else "  [ไม่ตอบสนอง!]"), flush=True)
        if not ok:
            problems.append(f"#{n} {name}: คาด {exp} รายการ ได้ SupAdj {got_sup} / DC {got_dc}"
                            + ("" if alive else " — TCT ไม่ตอบสนอง") + (" — TCT ดับ/restart" if crashed else ""))
            shots.append(snap(f"{n:03d}_FAIL_{name}"))
            T.press("FORCE_RESET")       # คืนสถานะช่องแล้วทำต่อ (เหมือนพนักงานจริง)
            time.sleep(2)
            if crashed:
                break
        if n % 10 == 0:
            rss.append((round(time.time() - t0), tct_rss_mb()))
            shots.append(snap(f"{n:03d}_progress"))
        time.sleep(rng.uniform(0.5, 2.5))   # เว้นระยะระหว่างรถ

    # ------------------------------------------------------------ สรุป
    time.sleep(6)
    rss.append((round(time.time() - t0), tct_rss_mb()))
    shots.append(snap("99_end"))
    sup_all, dc_all = count_trans(seq0)
    sold = ta_sold(seq0)
    crashes = tct_starts() - starts0
    lines = mark.lines()
    errors = [l for l in lines if re.search(r"Exception|- Error|Error ", l) and "Error Zip File" not in l]
    err_kinds = Counter(re.sub(r"^\S+ \S+ : ", "", e)[:90] for e in errors)
    by_name = defaultdict(lambda: [0, 0])
    for r in rows:
        by_name[r["name"]][0] += 1
        by_name[r["name"]][1] += 0 if r["ok"] else 1
    dur = round((time.time() - t0) / 60, 1)
    passed = sum(r["ok"] for r in rows)

    summary = [
        f"TCT Soak test — {datetime.now():%Y-%m-%d %H:%M:%S}  (seed {SEED})",
        f"ระยะเวลา {dur} นาที, ไม่ restart TCT, รถ/งานทั้งหมด {len(rows)} รายการ",
        f"ผ่าน {passed}/{len(rows)}",
        f"รายการที่ควรเกิด {expected} | Sup Adjust ได้ {sup_all} | DC ได้ {dc_all} | ขายคูปอง (TA) {sold}",
        f"TCT ดับ/restart: {crashes} ครั้ง",
        f"หน่วยความจำ mono (MB): " + ", ".join(f"{t}s={m}" for t, m in rss),
        f"Error/Exception ใน log: {len(errors)} บรรทัด",
        "",
        "แยกตาม logic (ครั้ง / ไม่ผ่าน):",
        *[f"  {k:34s} {v[0]:3d} / {v[1]}" for k, v in sorted(by_name.items())],
        "",
        "ปัญหาที่พบ:" if problems else "ปัญหาที่พบ: ไม่มี",
        *[f"  - {p}" for p in problems],
        "",
        "ชนิด Error ใน log (จำนวน):" if err_kinds else "",
        *[f"  {c:4d} × {k}" for k, c in err_kinds.most_common(20)],
    ]
    (RESULT / "summary.txt").write_text("\n".join(summary), encoding="utf-8")
    (RESULT / "tct_log.txt").write_text("\n".join(lines), encoding="utf-8")
    (RESULT / "rows.json").write_text(json.dumps(rows, ensure_ascii=False, indent=1), encoding="utf-8")
    write_html(summary, rows, rss, shots, err_kinds)
    print("\n" + "\n".join(summary))
    print(f"\nผลอยู่ที่ {RESULT}")
    sys.exit(0 if (passed == len(rows) and crashes == 0 and sup_all == expected == dc_all) else 1)


def write_html(summary, rows, rss, shots, err_kinds):
    tr = "".join(
        f"<tr class='{'ok' if r['ok'] else 'bad'}'><td>{r['n']}</td><td>{r['t']}s</td><td>{html.escape(r['name'])}</td>"
        f"<td>{r['exp']}</td><td>{r['sup']}</td><td>{r['dc']}</td><td>{'✔' if r['alive'] else '✘'}</td>"
        f"<td>{'ผ่าน' if r['ok'] else 'ไม่ผ่าน'}</td></tr>" for r in rows)
    imgs = "".join(f"<figure><img src='{s}'><figcaption>{s}</figcaption></figure>" for s in shots if s)
    page = f"""<!doctype html><html lang="th"><head><meta charset="utf-8"><title>TCT Soak Test</title>
<style>body{{font:14px system-ui,sans-serif;margin:20px;background:#14171c;color:#e6e9ee}}
pre{{background:#1d222a;padding:12px;border-radius:8px;white-space:pre-wrap}}
table{{border-collapse:collapse;width:100%}}td,th{{border-bottom:1px solid #2c333d;padding:4px 8px;text-align:left}}
tr.bad td{{color:#ff8a8a}} tr.ok td:last-child{{color:#3fb950}}
.g{{display:flex;flex-wrap:wrap;gap:10px}} figure{{margin:0;width:330px}} img{{width:100%;border-radius:6px}}
figcaption{{font-size:12px;color:#8b95a3}}</style></head><body>
<h1>TCT Soak Test</h1><pre>{html.escape(chr(10).join(summary))}</pre>
<h2>ทีละคัน</h2><table><tr><th>#</th><th>เวลา</th><th>logic</th><th>คาด</th><th>SupAdj</th><th>DC</th><th>ตอบสนอง</th><th>ผล</th></tr>{tr}</table>
<h2>ภาพหน้าจอ</h2><div class="g">{imgs}</div></body></html>"""
    (RESULT / "report.html").write_text(page, encoding="utf-8")


if __name__ == "__main__":
    main()
