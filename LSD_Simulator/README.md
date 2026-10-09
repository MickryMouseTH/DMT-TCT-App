# LSD_Simulator — ตัวจำลอง 2 ฝั่งของ LSD (NovaDemo + ColorRecDemo)

ชุดโปรแกรมทดสอบ **LSD_Bridge** / ป้าย LSD ตัวใหม่ โดยไม่ต้องมีตู้ TCT จริงหรือ controller จริง
ทั้งคู่เป็น GUI (PySide6/PyQt) และใช้ `LogLibrary.py` เป็นตัว log/config เดียวกับระบบหลัก

```
[ NovaDemo ] --TCP AVON--> [ LSD_Bridge ] --HTTP PUT .../{vsn}/activated--> [ ColorRecDemo ]
  (จำลอง TCT ส่งคำสั่ง)      (โปรแกรมหลัก)         (จำลอง LSD Controller ใหม่ + แสดงสีป้าย)
```

## โปรแกรมในโฟลเดอร์
| ไฟล์ | หน้าที่ |
|------|--------|
| `NovaDemo.py` | **ฝั่งส่ง** — จำลอง TCT_App ส่ง AVON frame (Show/Clear/SYNC/KeepAlive) ตั้งค่า IP:Port ปลายทางใน GUI |
| `ColorRecDemo.py` | **ฝั่งรับ** — เปิด HTTP server รับคำสั่งที่ Bridge แปลงมาแล้ว แล้วโชว์สีป้ายตามโหมด |
| `avon_protocol.py` | encode/decode โปรโตคอล AVON + CRC (ใช้โดย NovaDemo) |
| `qt_compat.py` | ชั้นบางให้ใช้ Qt ได้ทั้ง PySide6/PyQt6/PyQt5/PySide2 |
| `LogLibrary.py` | log/config (loguru) — ชุดเดียวกับระบบหลัก |
| `build.sh` / `build.bat` | build เป็น executable (Linux/macOS / Windows) |

## ติดตั้ง & รัน (ตอน dev)
```bash
cd LSD_Simulator
python3 -m pip install -r requirements.txt
python3 ColorRecDemo.py    # เปิดฝั่งรับก่อน (default 0.0.0.0:8080)
python3 NovaDemo.py        # แล้วเปิดฝั่งส่ง
```
รันครั้งแรกแต่ละตัวจะสร้าง `<ชื่อ>_config.json` ให้ — แก้ค่าแล้วรันใหม่ (หรือแก้ใน GUI ก็บันทึกให้)

## build เป็น executable
```bash
./build.sh            # Linux/macOS -> dist/NovaDemo , dist/ColorRecDemo
./build.sh --clean    # ล้างก่อน build
```
```bat
build.bat             :: Windows -> dist\NovaDemo.exe , dist\ColorRecDemo.exe
```

## การต่อสาย 3 โปรแกรมเข้าด้วยกัน
1. **ColorRecDemo** — กด Start (เช่น port `8080`)
2. **LSD_Bridge** (`../LSD_Contoller`) — ตั้ง `LSD_New_BaseURL` = `http://<ip ColorRecDemo>:8080` แล้วรัน
3. **NovaDemo** — ตั้ง Target = `<ip Bridge>:16603` กด Connect แล้วกด Show โหมดต่าง ๆ
   - จะเห็น NovaDemo(TX) → Bridge(HTTP) → ColorRecDemo เปลี่ยนสีป้ายตามโหมด

> ทดสอบเร็ว (ไม่ผ่าน Bridge): ตั้ง Target ของ NovaDemo ไปที่ ColorRecDemo ตรง ๆ ไม่ได้ เพราะคนละโปรโตคอล
> (NovaDemo = TCP AVON, ColorRecDemo = HTTP) — ต้องมี Bridge คั่นเสมอ

## หมายเหตุ port 80
Bridge ตั้งต้น `LSD_New_BaseURL=http://169.254.32.1` (port 80). ถ้าจะให้ ColorRecDemo ฟัง port 80
ต้องรันด้วยสิทธิ์ root/admin — แนะนำใช้ `8080` แล้วใส่ port ใน `LSD_New_BaseURL` ของ Bridge แทน
