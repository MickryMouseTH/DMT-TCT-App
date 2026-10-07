#!/bin/bash
# TCT Simulator — รัน TCT_App บนเครื่องนี้พร้อมอุปกรณ์จำลอง (LCSD / Sup Adjust / RTO / DC)
#
#   ./sim.sh start      build (ถ้ายังไม่มี exe) + เตรียม runtime + เปิดระบบ + เปิด browser
#   ./sim.sh test       รันชุดทดสอบอัตโนมัติ (Force Reset / รถยกเว้น / OB อย่างเดียว)  [ระบุ case ได้]
#   ./sim.sh kbtest     ทดสอบทุกฟังก์ชันด้วยคีย์บอร์ด (รวม Popup / ขายคูปอง / โหมดช่าง)  [ระบุ case ได้]
#   ./sim.sh build      build TCT_App.sln ใหม่ (Debug + Release) ด้วย Mono ใน Docker
#   ./sim.sh stop       ปิดระบบ
#   ./sim.sh restart    ปิด-เปิดใหม่ (เตรียม runtime ใหม่จาก bin/Debug)
#   ./sim.sh status     ดูสถานะ container + การเชื่อมต่อ
#   ./sim.sh logs [svc] ดู log (tct | lcsd-sim | rabbitmq)
#   ./sim.sh shot       จับภาพหน้าจอ TCT -> shots/
#   ./sim.sh open       เปิด Control Panel ใน browser
#
# หน้าเว็บ:  Control Panel + จอ TCT  http://localhost:8090
#            คีย์บอร์ด TCT จำลอง         http://localhost:8090/keyboard
#            จอ TCT อย่างเดียว         http://localhost:6080/vnc.html?autoconnect=true&resize=scale
#            DC (RabbitMQ)             http://localhost:15672   (guest/guest)
set -euo pipefail
cd "$(dirname "$0")"
SIM_DIR=$(pwd)
REPO_DIR=$(cd .. && pwd)
BUILD_IMAGE=tct-mono-build:6.12

ensure_docker() {
    if ! docker info >/dev/null 2>&1; then
        echo "+ เปิด Docker Desktop ..."
        open -a Docker 2>/dev/null || { echo "✘ ไม่พบ Docker — ติดตั้ง Docker Desktop ก่อน"; exit 1; }
        for _ in $(seq 1 60); do docker info >/dev/null 2>&1 && break; sleep 3; done
        docker info >/dev/null 2>&1 || { echo "✘ Docker ยังไม่พร้อม"; exit 1; }
    fi
}

build_image() {
    docker image inspect "$BUILD_IMAGE" >/dev/null 2>&1 && return
    echo "+ สร้าง image สำหรับ build ($BUILD_IMAGE) ..."
    docker build -q -t "$BUILD_IMAGE" -f - . >/dev/null <<'EOF'
FROM mono:6.12
RUN sed -i "s|deb.debian.org|archive.debian.org|g; s|security.debian.org|archive.debian.org|g; /buster-updates/d" /etc/apt/sources.list \
 && apt-get -o Acquire::Check-Valid-Until=false update -qq && apt-get install -y -qq gtk-sharp2 && rm -rf /var/lib/apt/lists/*
EOF
}

do_build() {
    ensure_docker
    build_image
    for c in Release Debug; do
        echo "+ build $c ..."
        docker run --rm -v "$REPO_DIR":/src -w /src "$BUILD_IMAGE" \
            msbuild TCT_App.sln /t:Rebuild /p:Configuration=$c /p:Platform=x86 /v:minimal /nologo \
            | grep -E " error |->" || true
        [ -f "$REPO_DIR/TCT_App/bin/$c/TCT_App.exe" ] || { echo "✘ build $c ไม่ผ่าน"; exit 1; }
    done
}

wait_ready() {
    echo -n "+ รอ TCT เชื่อมต่อ LCSD / Sup Adjust / RTO / DC "
    for _ in $(seq 1 90); do
        if curl -s localhost:8090/api/state 2>/dev/null | python3 -c "import json,sys;sys.exit(0 if all(json.load(sys.stdin)['status'].values()) else 1)" 2>/dev/null; then
            echo " พร้อม"; return 0
        fi
        echo -n "."; sleep 2
    done
    echo " ✘ ยังไม่ครบ — ดู ./sim.sh logs tct"; return 1
}

do_start() {
    ensure_docker
    [ -f "$REPO_DIR/TCT_App/bin/Debug/TCT_App.exe" ] || do_build
    python3 prepare.py
    docker compose up -d --build
    wait_ready || true
    cat <<EOF

  Control Panel + จอ TCT : http://localhost:8090
  จอ TCT อย่างเดียว       : http://localhost:6080/vnc.html?autoconnect=true&resize=scale
  DC (RabbitMQ)          : http://localhost:15672  (guest/guest)

  Login: กด "แตะบัตรพนักงาน" (99001) แล้วสลับ DI "OTL" เป็น ON เพื่อเปิดช่องทาง
  ทดสอบอัตโนมัติ: ./sim.sh test
EOF
    open "http://localhost:8090" 2>/dev/null || true
}

case "${1:-}" in
    start)   do_start ;;
    stop)    docker compose down ;;
    restart) docker compose down; do_start ;;
    build)   do_build ;;
    test)    shift; ensure_docker; python3 sim_test.py "$@" ;;
    kbtest)  shift; ensure_docker; python3 sim_kb_test.py "$@" ;;
    status)  docker compose ps; curl -s localhost:8090/api/state | python3 -c "import json,sys;d=json.load(sys.stdin);print('connections:',d['status']);print('counters   :',d['counters'])" || true ;;
    logs)    shift; docker compose logs -f --tail=100 "${@:-tct}" ;;
    shot)    mkdir -p shots; f="shots/tct_$(date +%H%M%S).png"
             docker compose exec -T tct bash -c 'xwd -root -display :0 -silent | convert xwd:- png:-' > "$f"; echo "+ $f" ;;
    open)    open "http://localhost:8090" ;;
    *)       sed -n '2,17p' "$0"; exit 1 ;;
esac
