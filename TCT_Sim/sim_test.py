#!/usr/bin/env python3
"""
ทดสอบอัตโนมัติ — Force Reset / รถยกเว้น / OB อย่างเดียว (Bug H1–H3 ใน Bug.md)
ต้อง start simulator ก่อน (./sim.sh start)   รัน: ./sim.sh test   หรือ  python3 sim_test.py [case ...]

ผลลัพธ์: TCT_Sim/results/<เวลา>/  (summary.txt + ภาพหน้าจอ + log TCT ของแต่ละ case)
"""
import json
import subprocess
import sys
import time
import urllib.request
from datetime import datetime
from pathlib import Path

SIM_DIR   = Path(__file__).resolve().parent
PANEL     = "http://localhost:8090"
LOG_DIR   = SIM_DIR / "runtime" / "log"
RESULT    = SIM_DIR / "results" / datetime.now().strftime("%Y%m%d_%H%M%S")
STAFF_CARD = "99001"

# KeyPressedType (TCT_Lib.cs)
K_CLASS1, K_EXEMPT, K_CASH, K_FORCE_RESET = 1, 7, 10, 21
DI_OTL = 7


# ------------------------------------------------------------------ helpers
def api(**body):
    req = urllib.request.Request(PANEL + "/api", data=json.dumps(body).encode(),
                                 headers={"Content-Type": "application/json"})
    urllib.request.urlopen(req, timeout=10).read()


def state(since=0):
    with urllib.request.urlopen(f"{PANEL}/api/state?since={since}", timeout=10) as r:
        return json.load(r)


def key(k, wait=0.6):
    api(action="key", id=k)
    time.sleep(wait)


def scenario(name, wait):
    api(action="scenario", name=name)
    time.sleep(wait)


def shot(name):
    png = RESULT / f"{name}.png"
    with open(png, "wb") as f:
        subprocess.run(["docker", "compose", "exec", "-T", "tct", "bash", "-c",
                        "xwd -root -display :0 -silent | convert xwd:- png:-"],
                       cwd=SIM_DIR, stdout=f, stderr=subprocess.DEVNULL)
    return png.name


def lane_event_log():
    files = sorted(LOG_DIR.glob("LaneEvent/Event_*/LaneEvent_*.log"))
    return files[-1] if files else None


class TctLog:
    """อ่านเฉพาะบรรทัดใหม่ของ LaneEvent log ตั้งแต่ mark()"""
    def mark(self):
        f = lane_event_log()
        self.file, self.pos = f, (f.stat().st_size if f else 0)

    def lines(self):
        f = lane_event_log()
        if not f:
            return []
        pos = self.pos if f == self.file else 0
        with open(f, "rb") as fh:
            fh.seek(pos)
            return fh.read().decode("utf-8", "replace").splitlines()


def wait_ready(timeout=180):
    t0 = time.time()
    while time.time() - t0 < timeout:
        try:
            st = state()["status"]
            if all(st.values()):
                return True
        except Exception:
            pass
        time.sleep(2)
    return False


def trans_since(seq, src):
    out = []
    for it in state(seq)["logs"]:
        if it["src"] == src and it["level"] == "trans":
            p = (it.get("data") or {}).get("params", it.get("data"))
            out.append(p if isinstance(p, dict) else {})
    return out


def brief(t):
    pay = t.get("payInfo") or {}
    sen = t.get("sensorStatus") or {}
    sig = t.get("mtcPassageSignal") or {}
    return (f"TransID={t.get('transactionId')} nTrx={t.get('nTrx')} class={t.get('tcClass')} "
            f"pay={pay.get('payTypeName')} loop={sen.get('loopStatus')} ob={sen.get('obStatus')} "
            f"avc={sen.get('avcStatus')} simuPass={sig.get('simuPass')} remark={t.get('transRemark')}")


# ------------------------------------------------------------------ test cases
CASES = {}


def case(name, title):
    def deco(fn):
        CASES[name] = (title, fn)
        return fn
    return deco


@case("h2_exempt_ob_only", "H2: รถยกเว้น ผ่าน OB อย่างเดียว (ไม่มี Loop) ไม่กด Force Reset → Auto AVC ต้องสร้างรายการ")
def t_h2(ctx):
    key(K_EXEMPT, 1.5)
    ctx.shot("1_exempt")
    scenario("ob_only", 1.5)
    time.sleep(6)                                       # TimerSecAutoAVC(3s)+0.7 + เผื่อ
    ctx.shot("2_after_auto_avc")
    ctx.expect_trans(1, cls=7, pay="EXEMPT", loop=0, ob=1)
    ctx.expect_log("+ Auto AVC (OB only, No Loop)")


@case("h1_exempt_ob_only_force_reset", "H1: รถยกเว้น ผ่าน OB อย่างเดียว แล้วกด Force Reset ก่อน Auto AVC → ต้องได้ 1 รายการ (ไม่หาย/ไม่ซ้ำ)")
def t_h1(ctx):
    key(K_EXEMPT, 1.5)
    scenario("ob_only", 1.3)
    key(K_FORCE_RESET, 1.0)
    ctx.shot("1_after_force_reset")
    time.sleep(6)                                       # ให้เลยเวลา Auto AVC — timer ต้องถูกยกเลิก
    ctx.expect_trans(1, cls=7, pay="EXEMPT")
    ctx.expect_log("Found paid car in ListLoopCar")


@case("h3_paid_on_loop_ob_force_reset", "H3/H1: รถจอดทับ Loop+OB จ่ายเงินสด แล้วกด Force Reset → ต้องได้ 1 รายการ ไม่ซ้ำ ไม่มี Violation เกิน")
def t_h3(ctx):
    scenario("loop_ob_stuck", 1.5)
    key(K_CLASS1, 0.8)
    key(K_CASH, 1.5)
    ctx.shot("1_paid_on_loop_ob")
    key(K_FORCE_RESET, 1.0)
    time.sleep(4)
    ctx.shot("2_after_force_reset")
    ctx.expect_trans(1, cls=1, pay="CASH")
    ctx.expect_log("Skip second ForceEnd")
    scenario("clear_io", 1.0)                           # รถออก — ไม่นับในผลข้างบน
    time.sleep(5)


@case("normal_pass", "ปกติ: Class 1 เงินสด รถผ่าน Loop → OB → ต้องได้ 1 รายการ (ไม่กระทบ flow เดิม)")
def t_normal(ctx):
    key(K_CLASS1, 0.8)
    key(K_CASH, 1.5)
    scenario("normal", 3.0)
    time.sleep(5)
    ctx.shot("1_after_pass")
    ctx.expect_trans(1, cls=1, pay="CASH", loop=1, ob=1)


@case("guard_next_car_before_timer", "H2 guard: OB อย่างเดียว แล้วพนักงานเริ่มรถคันถัดไปก่อน Auto AVC → ต้องข้าม Auto AVC, Force Reset แล้วได้รถยกเว้น 1 รายการ")
def t_guard(ctx):
    key(K_EXEMPT, 1.5)
    scenario("ob_only", 1.3)
    key(K_CLASS1, 0.5)                                  # รถคันถัดไป (ยังไม่จ่าย)
    time.sleep(5)
    ctx.shot("1_next_car_selected")
    ctx.expect_log("Skip Auto AVC (OB only)")
    ctx.expect_no_do_since_mark("ALB_Close = 1", "ไม่ต้องสั่งปิดไม้กั้นต่อหน้ารถคันถัดไป")
    ctx.expect_trans(0)
    key(K_FORCE_RESET, 1.0)
    time.sleep(4)
    ctx.shot("2_after_force_reset")
    ctx.expect_trans(1, cls=7, pay="EXEMPT")


# ------------------------------------------------------------------ runner
class Ctx:
    def __init__(self, name):
        self.name, self.ok, self.notes = name, True, []
        self.seq = state()["seq"]
        self.log = TctLog()
        self.log.mark()

    def shot(self, label):
        self.notes.append(f"    ภาพ: {shot(f'{self.name}__{label}')}")

    def check(self, cond, msg):
        self.notes.append(("    ✔ " if cond else "    ✘ ") + msg)
        self.ok &= bool(cond)

    def expect_trans(self, n, cls=None, pay=None, loop=None, ob=None):
        sup = trans_since(self.seq, "SUPADJ")
        dc = trans_since(self.seq, "DC")
        self.check(len(sup) == n, f"Sup Adjust ได้ {len(sup)} รายการ (คาด {n})")
        self.check(len(dc) == n, f"DC ได้ {len(dc)} รายการ (คาด {n})")
        for t in sup:
            self.notes.append("      - " + brief(t))
        if n and sup:
            t = sup[0]
            if cls is not None:
                self.check(t.get("tcClass") == cls, f"tcClass = {t.get('tcClass')} (คาด {cls})")
            if pay is not None:
                self.check(str((t.get("payInfo") or {}).get("payTypeName")).upper() == pay.upper(), f"payType = {(t.get('payInfo') or {}).get('payTypeName')} (คาด {pay})")
            if loop is not None:
                self.check((t.get("sensorStatus") or {}).get("loopStatus") == loop, f"loopStatus = {(t.get('sensorStatus') or {}).get('loopStatus')} (คาด {loop})")
            if ob is not None:
                self.check((t.get("sensorStatus") or {}).get("obStatus") == ob, f"obStatus = {(t.get('sensorStatus') or {}).get('obStatus')} (คาด {ob})")

    def expect_log(self, text):
        self.check(any(text in l for l in self.log.lines()), f"TCT log มี \"{text}\"")

    def expect_no_do_since_mark(self, text, why):
        found = [it for it in state(self.seq)["logs"] if it["src"] == "TCT→SIM" and text in it["text"]]
        self.check(not found, f"ไม่มี WRDO {text} — {why}")


def restart_tct_and_login():
    print("+ restart TCT (สถานะสะอาด) ...", flush=True)
    subprocess.run(["docker", "compose", "restart", "tct"], cwd=SIM_DIR, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    time.sleep(8)
    if not wait_ready():
        sys.exit("✘ TCT/LCSD/SupAdj/RTO/DC เชื่อมไม่ครบใน 180 วินาที — ดู ./sim.sh logs")
    time.sleep(4)
    for i in range(16):
        api(action="di", idx=i, value=0)
    api(action="scr", card=STAFF_CARD)                  # แตะบัตรพนักงาน
    time.sleep(4)
    api(action="di", idx=DI_OTL, value=1)               # เปิดช่องทาง
    time.sleep(3)


def main():
    names = sys.argv[1:] or list(CASES)
    RESULT.mkdir(parents=True, exist_ok=True)
    if not wait_ready(30):
        sys.exit("✘ simulator ยังไม่พร้อม — รัน ./sim.sh start ก่อน")

    results = []
    for name in names:
        title, fn = CASES[name]
        print(f"\n=== {name}\n    {title}", flush=True)
        restart_tct_and_login()
        ctx = Ctx(name)
        try:
            fn(ctx)
        except Exception as e:
            ctx.check(False, f"exception: {e!r}")
        (RESULT / f"{name}__tct_lane_event.log").write_text("\n".join(ctx.log.lines()), encoding="utf-8")
        print("\n".join(ctx.notes))
        print("    ==> " + ("PASS ✅" if ctx.ok else "FAIL ❌"), flush=True)
        results.append((name, title, ctx))

    lines = [f"TCT Simulator test — {datetime.now():%Y-%m-%d %H:%M:%S}", ""]
    for name, title, ctx in results:
        lines += [f"[{'PASS' if ctx.ok else 'FAIL'}] {name}", f"    {title}", *ctx.notes, ""]
    passed = sum(c.ok for _, _, c in results)
    lines.append(f"สรุป: ผ่าน {passed}/{len(results)}")
    (RESULT / "summary.txt").write_text("\n".join(lines), encoding="utf-8")
    print(f"\nสรุป: ผ่าน {passed}/{len(results)}   ผลอยู่ที่ {RESULT}")
    sys.exit(0 if passed == len(results) else 1)


if __name__ == "__main__":
    main()
