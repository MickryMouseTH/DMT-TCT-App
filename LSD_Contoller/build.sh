#!/usr/bin/env bash
#
# build.sh — build LSD_Bridge เป็น executable ไฟล์เดียว ด้วย PyInstaller (--onefile)
#
# ใช้งาน:
#   ./build.sh              # build ปกติ
#   ./build.sh --venv       # สร้าง/ใช้ virtualenv .venv แล้ว build ในนั้น
#   ./build.sh --clean      # ลบ build/ dist/ *.spec ก่อน build
#
# ผลลัพธ์:  dist/LSD_Bridge   (Linux/mac binary ไฟล์เดียว รันได้เลย)
#           วาง config/log ไว้ข้าง binary อัตโนมัติ (LogLibrary รองรับ sys.frozen)

set -euo pipefail

# ---- ไปที่โฟลเดอร์ของสคริปต์นี้ ----
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

APP_NAME="LSD_Bridge"
ENTRY="LSD_Bridge.py"

USE_VENV=0
DO_CLEAN=0
for arg in "$@"; do
    case "$arg" in
        --venv)  USE_VENV=1 ;;
        --clean) DO_CLEAN=1 ;;
        *) echo "ไม่รู้จัก option: $arg"; exit 1 ;;
    esac
done

# ---- เลือก python ----
PY="${PYTHON:-python3}"
command -v "$PY" >/dev/null 2>&1 || { echo "ไม่พบ $PY — ติดตั้ง Python 3 ก่อน"; exit 1; }
echo "+ ใช้ Python: $($PY --version 2>&1)"

# ---- virtualenv (ถ้าเลือก) ----
if [ "$USE_VENV" -eq 1 ]; then
    if [ ! -d ".venv" ]; then
        echo "+ สร้าง virtualenv .venv"
        "$PY" -m venv .venv
    fi
    # shellcheck disable=SC1091
    source .venv/bin/activate
    PY="python"
    echo "+ เปิดใช้ virtualenv: $(which python)"
fi

# ---- ล้างของเก่า ----
if [ "$DO_CLEAN" -eq 1 ]; then
    echo "+ ล้าง build/ dist/ __pycache__/ *.spec"
    rm -rf build dist __pycache__ "${APP_NAME}.spec"
fi

# ---- ติดตั้ง dependency + pyinstaller ----
echo "+ ติดตั้ง dependency (requirements.txt + pyinstaller)"
"$PY" -m pip install --upgrade pip >/dev/null
"$PY" -m pip install -r requirements.txt
"$PY" -m pip install pyinstaller

# ---- build ----
echo "+ เริ่ม build --onefile : $APP_NAME"
"$PY" -m PyInstaller \
    --onefile \
    --clean \
    --noconfirm \
    --name "$APP_NAME" \
    --hidden-import avon_protocol \
    --hidden-import LogLibrary \
    --collect-submodules loguru \
    "$ENTRY"

# ---- สรุป ----
OUT="dist/$APP_NAME"
if [ -f "$OUT" ]; then
    chmod +x "$OUT"
    echo ""
    echo "==================================================================="
    echo "  สำเร็จ:  $OUT"
    echo "  ขนาด:    $(du -h "$OUT" | cut -f1)"
    echo ""
    echo "  รัน:     ./$OUT"
    echo "  ครั้งแรกจะสร้าง ${APP_NAME}_config.json ไว้ข้าง binary (แก้ค่าแล้วรันใหม่)"
    echo "==================================================================="
else
    echo "- build ล้มเหลว: ไม่พบ $OUT"
    exit 1
fi
