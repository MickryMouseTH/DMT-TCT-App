#!/bin/bash
# ทดสอบรับระบบ TCT (Acceptance) ครบทุก logic ด้วยคำสั่งเดียว
#
#   ./run_acceptance.sh [นาที soak]        (ค่าเริ่มต้น 10 นาที)
#
# ลำดับ:
#   1) Regression บั๊ก H1–H3 (Force Reset / OB อย่างเดียว)        sim_test.py
#   2) ทุกฟังก์ชันด้วยคีย์บอร์ด + Popup + EDC + ขายคูปอง + โหมดช่าง   sim_kb_test.py
#   3) ใช้งานต่อเนื่องเหมือนพนักงานจริง ไม่ restart TCT                 soak_test.py
# ผลรวม: results/acceptance_<เวลา>/summary.txt  (+ ลิงก์ไปผลละเอียดของแต่ละชุด)
set -uo pipefail
cd "$(dirname "$0")"
SOAK_MIN=${1:-10}
OUT="results/acceptance_$(date +%Y%m%d_%H%M%S)"
mkdir -p "$OUT"

./sim.sh status >/dev/null 2>&1 || true
if ! curl -s localhost:8090/api/state >/dev/null 2>&1; then
    echo "+ เปิด simulator ก่อน ..."
    ./sim.sh start
fi

run() {   # ชื่อ, คำสั่ง...
    local name=$1; shift
    echo; echo "=================== $name ==================="
    local t0=$(date +%s)
    "$@" 2>&1 | tee "$OUT/$name.log"
    local rc=${PIPESTATUS[0]}
    local sec=$(( $(date +%s) - t0 ))
    local res=$(grep -h "สรุป: ผ่าน\|^ผ่าน " "$OUT/$name.log" | tail -1)
    local dir=$(grep -ho "ผลอยู่ที่ .*" "$OUT/$name.log" | tail -1)
    echo "$name | $( [ $rc -eq 0 ] && echo PASS || echo FAIL ) | ${res:-?} | ${sec}s | ${dir#ผลอยู่ที่ }" >> "$OUT/summary.txt"
}

echo "TCT Acceptance test — $(date '+%F %T')" > "$OUT/summary.txt"
echo "ชุด | ผล | รายละเอียด | เวลา | โฟลเดอร์ผล" >> "$OUT/summary.txt"
run 1_regression_H1-H3  python3 sim_test.py
run 2_all_logic_keyboard python3 sim_kb_test.py
run 3_soak_${SOAK_MIN}min   python3 soak_test.py "$SOAK_MIN"

echo; echo "=================== สรุป ==================="
cat "$OUT/summary.txt"
grep -q "| FAIL |" "$OUT/summary.txt" && exit 1 || exit 0
