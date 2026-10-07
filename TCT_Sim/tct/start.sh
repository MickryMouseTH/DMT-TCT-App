#!/bin/bash
# จอเสมือน (Xvfb) + VNC + noVNC (เปิดผ่าน browser) แล้วรัน TCT_App.exe
export DISPLAY=:0
SCREEN=${SCREEN:-1024x768x24}   # 4:3 เหมือนจอจริง

# ลบ lock ค้างจากรอบก่อน (docker compose restart ใช้ filesystem เดิม)
rm -f /tmp/.X0-lock /tmp/.X11-unix/X0
Xvfb :0 -screen 0 "$SCREEN" -nolisten tcp &
for _ in $(seq 1 50); do [ -e /tmp/.X11-unix/X0 ] && break; sleep 0.1; done
openbox &
x11vnc -display :0 -forever -shared -nopw -rfbport 5900 -quiet &
websockify --web /usr/share/novnc 6080 localhost:5900 >/dev/null 2>&1 &
python3 /keyinject.py &        # รับการกดแป้นจากคีย์บอร์ดจำลองบนเว็บ (port 6081)

cd /tct/app
while true; do
    echo "===== $(date '+%F %T') start TCT_App.exe ====="
    mono --debug TCT_App.exe
    echo "===== $(date '+%F %T') TCT_App.exe exited (code $?) — restart in 5s ====="
    sleep 5
done
