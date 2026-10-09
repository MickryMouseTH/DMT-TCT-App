#!/usr/bin/env bash
#
# build.sh — build NovaDemo และ ColorRecDemo เป็น executable (PyInstaller --onefile, GUI)
#            สำหรับ Linux / macOS
#
# ใช้งาน:
#   ./build.sh              # build ทั้ง 2 โปรแกรม
#   ./build.sh --venv       # สร้าง/ใช้ virtualenv .venv แล้ว build ในนั้น
#   ./build.sh --clean      # ลบ build/ dist/ *.spec ก่อน build
#   ./build.sh NovaDemo     # build เฉพาะตัวที่ระบุ (NovaDemo หรือ ColorRecDemo)
#
# ผลลัพธ์: dist/NovaDemo , dist/ColorRecDemo  (วาง config/log ข้าง binary อัตโนมัติ)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

USE_VENV=0
DO_CLEAN=0
ONLY=""
for arg in "$@"; do
    case "$arg" in
        --venv)  USE_VENV=1 ;;
        --clean) DO_CLEAN=1 ;;
        NovaDemo|ColorRecDemo) ONLY="$arg" ;;
        *) echo "ไม่รู้จัก option: $arg"; exit 1 ;;
    esac
done

# ---- เลือก python ----
PY="${PYTHON:-python3}"
command -v "$PY" >/dev/null 2>&1 || { echo "ไม่พบ $PY — ติดตั้ง Python 3 ก่อน"; exit 1; }
echo "+ ใช้ Python: $($PY --version 2>&1)"

# ---- virtualenv (ถ้าเลือก) ----
if [ "$USE_VENV" -eq 1 ]; then
    [ -d ".venv" ] || { echo "+ สร้าง virtualenv .venv"; "$PY" -m venv .venv; }
    # shellcheck disable=SC1091
    source .venv/bin/activate
    PY="python"
    echo "+ เปิดใช้ virtualenv: $(which python)"
fi

# ---- ล้างของเก่า ----
if [ "$DO_CLEAN" -eq 1 ]; then
    echo "+ ล้าง build/ dist/ __pycache__/ *.spec"
    rm -rf build dist __pycache__ ./*.spec
fi

# ---- ติดตั้ง dependency + pyinstaller ----
echo "+ ติดตั้ง dependency (requirements.txt + pyinstaller)"
"$PY" -m pip install --upgrade pip >/dev/null
"$PY" -m pip install -r requirements.txt
"$PY" -m pip install pyinstaller

build_one() {
    local app="$1"
    local entry="$2"
    local extra="${3:-}"
    echo "+ เริ่ม build --onefile : $app"
    # shellcheck disable=SC2086
    "$PY" -m PyInstaller \
        --onefile \
        --windowed \
        --clean \
        --noconfirm \
        --name "$app" \
        --hidden-import qt_compat \
        --hidden-import LogLibrary \
        --collect-submodules loguru \
        $extra \
        "$entry"
    local out="dist/$app"
    if [ -f "$out" ]; then
        chmod +x "$out"
        echo "  สำเร็จ: $out ($(du -h "$out" | cut -f1))"
    else
        echo "  - build ล้มเหลว: ไม่พบ $out"; exit 1
    fi
}

if [ -z "$ONLY" ] || [ "$ONLY" = "NovaDemo" ]; then
    build_one "NovaDemo"     "NovaDemo.py"     "--hidden-import avon_protocol"
fi
if [ -z "$ONLY" ] || [ "$ONLY" = "ColorRecDemo" ]; then
    build_one "ColorRecDemo" "ColorRecDemo.py"
fi

echo ""
echo "==================================================================="
echo "  เสร็จแล้ว — binary อยู่ในโฟลเดอร์ dist/"
echo "  รัน:  ./dist/NovaDemo   และ   ./dist/ColorRecDemo"
echo "  ครั้งแรกจะสร้าง <ชื่อ>_config.json ไว้ข้าง binary (แก้ค่าแล้วรันใหม่)"
echo "==================================================================="
