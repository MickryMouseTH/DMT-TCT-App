# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## TCT_App — ภาพรวม

แอปนี้คือ **Toll Collection Terminal (TCT)** — ซอฟต์แวร์หน้าตู้เก็บเงินค่าผ่านทาง 1 ช่อง (lane)
เป็น **C# / GTK# 2.4 / Mono** desktop GUI (full screen, รันบน Linux) ไม่มี package.json/Node

repo นี้มีเฉพาะแอปหลัก **`TCT_App/` + `TCT_Lib/`** (C#/Mono, `TCT_App.sln`)
ส่วนที่เคยอยู่ใน repo นี้ ย้ายไปมี repo ของตัวเองแล้ว (ใน workspace `DMT_ALL_MTC/`):
- **LSD_Bridge** (Python) ตัวแปลงคำสั่งป้าย LSD เดิม→controller ใหม่ → `../LSD_Contoller/LSD_Bridge/` (`lsd-bridge.git`)
- **LSD_Simulator** — NovaDemo + ColorRecDemo (Python GUI) ตัวจำลองไว้ทดสอบ Bridge → `../LSD_Simulator/` (`lsd-simulator.git`)
- **TCT_Sim** — จำลองช่อง TCT + ชุดทดสอบ → `../MTC_Sim/TCT_Sim/` (`mtc-sim.git`)

## คำสั่ง Build / Run / Test

### แอปหลัก (C#/Mono)
```bash
nuget restore TCT_App.sln                       # ดึง NuGet packages (อยู่ใน packages/)
msbuild TCT_App.sln /p:Configuration=Debug      # หรือ xbuild (deprecated) บน Mono
mono TCT_App/bin/Debug/TCT_App.exe              # รัน (target .NET Framework v4.7)
```
- `TCT_App.csproj` เป็น project เดียวใน solution และ **include `TCT_Lib.cs` เข้ามา compile ตรง ๆ** (TCT_Lib ไม่ใช่ project แยก)
- **ไม่มี unit test project** — การทดสอบแอปหลักทำแบบ manual บนเครื่อง/ตู้จริง

### LSD_Bridge / LSD_Simulator (Python)
อยู่ใน repo ของตัวเองแล้ว — คำสั่ง build/test ดู `../LSD_Contoller/LSD_Bridge/README.md` และ `../LSD_Simulator/README.md`

### TCT_Sim (เครื่องจำลองช่องทางทั้งระบบ, Docker) — ใช้ทดสอบแอป C# อัตโนมัติ
ย้ายไปอยู่ใน repo `mtc-sim.git` แล้ว → `../MTC_Sim/TCT_Sim/` (หา repo นี้ที่ `../../TCT_App` หรือตั้ง `TCT_REPO=<path>`)
ทดสอบรวมทุกโปรแกรม (TCT + RTO + SupAdjust + LSD) ใช้ `../MTC_Sim` (`./sim.sh up` → http://localhost:8800)
```bash
cd ../MTC_Sim/TCT_Sim
./sim.sh build                   # build TCT_App.sln ด้วย Mono ใน Docker (image tct-mono-build:6.12) — เครื่องนี้ไม่มี mono/msbuild
python3 prepare.py               # copy bin/Debug -> runtime/ + config ชี้ service จำลอง (ต้องรันทุกครั้งหลัง build ใหม่)
./sim.sh start                   # LCSD/RTO/SupAdjust/DC(RabbitMQ)/EDC/TA/กล้อง จำลอง + TCT ใน container, panel http://localhost:8090
./sim.sh kbtest [case ...]       # เคสคีย์บอร์ด (sim_kb_test.py) ผลที่ results/kb_<เวลา>/ (เคสคูปอง = j_sale_coupon_*)
./sim.sh shift [pace]            # ทดสอบตามแผน SHIFT_TEST_PLAN.md (พนักงาน 1 กะ ~25 นาที ครบทุกฟังก์ชัน)
./sim.sh soak [นาที]             # สุ่ม scenario ต่อเนื่อง ไม่ restart
./sim.sh dbpurge                 # ทดสอบลบข้อมูลเก่าใน DB (ลบเฉพาะที่ส่ง DC แล้ว) — ทิ้งข้อมูลทดสอบไว้ใน runtime ต้อง restart ก่อนเทสต์อื่น
```
- เทสต์หลายตัวตรวจข้อความใน log ระดับ DEBUG → `prepare.py` ตั้ง `Config.LogLevel=DEBUG` ให้ runtime แล้ว (ช่องจริงใช้ INFO)
- สั่ง EDC/TA จำลองจากเทสต์: `post(action="edc", mode=approve|decline|timeout|offline, qr=paid|unpaid)`, `post(action="ta", mode=ok|error|slow)`

## สถาปัตยกรรมหลัก — แอป C# (จำไว้)
- **Global static state เป็นศูนย์กลาง:** `TCT_Lib.cs` เก็บเกือบทุกอย่าง (config, สถานะช่อง, staff, payment, สถานะ connection, list คิวรถ) — object ระบบย่อยอ้างผ่าน `TCT_Lib.laneController / webSocketLane / lsdController / LaneDB`
- **หัวใจระบบ = `LaneController/LaneEventControl.cs` (8,355 บรรทัด)** — state machine การเก็บเงิน + จัดการ event + thread พื้นหลัง 5 ตัว
- **UI:** `LoginWnd.cs` เป็นเจ้าของ GTK main loop (`Application.Run`), `MainWindow.cs` คือหน้าจอช่อง, `MainMaintenance.cs` คือโหมดช่าง (staff group 50–59). โค้ด UI generate ใน `gtk-gui/` (ห้ามแก้มือ)
- **DB:** SQLite (`TCT_DB.db`) ผ่าน `Database/LaneDB.cs` — connection เดียวเปิดค้าง, 2 เมธอด `List()/Execute()`, query ต่อ string (ไม่ parameterized), ไม่เปิด WAL
- **การสื่อสาร:** WebSocket client → LCSD (`LaneWebSocket`); SuperSocket server → RTO (`LaneSocketServer`); RabbitMQ 3 broker (MTS=monitoring, MOC=DataCenter, ETC); TCP → EDC (payment); HTTP → NVR (กล้อง) + Web Service (begin/end job)
- **Payment:** EMV/EDC + QR ผ่าน TCP socket (`EMV/EMV_V1/`), มี V0 (เก่า) และ V1 (ใหม่). พิมพ์ใบเสร็จ ESC/POS (`PrintUtils/`, encoding windows-874)
- **LSD** = Lane Signal Device (ป้าย/ไฟแสดงสถานะช่อง) ผ่าน TCP socket async (`LSD/LaneSocketControl_LSD.cs` — ตัวจริง; ไฟล์ `(another copy)` / `(.5)` คือไฟล์ซ้ำ ไม่ใช้)

## flow การเก็บเงิน 1 คัน
รอรถ → AVC จำแนกประเภท → เลือก CLASS → เลือก PAYMENT → ประมวลผลชำระ → บันทึก DB + พิมพ์ใบเสร็จ + ส่ง RabbitMQ → เปิดไม้กั้น ALB/OTL → ถ่ายภาพ NVR → วนใหม่
โหมดพิเศษ: TOWING (รถลาก, 6 state), CONVOY/HPMC (ขบวน), Violation

## สถาปัตยกรรม — LSD Bridge + Simulators (Python, อยู่ใน repo lsd-bridge / lsd-simulator)
สายข้อมูลของระบบป้าย LSD ตัวใหม่ (แทน path C# `LSD/LaneSocketControl_LSD.cs`):
```
[ TCT_App / NovaDemo ] --TCP 16603 (AVON binary)--> [ LSD_Bridge ] --HTTP PUT .../{vsn}/activated--> [ LSD Controller ใหม่ / ColorRecDemo ]
```
- **โปรโตคอล AVON** (`avon_protocol.py`) = frame binary: `Header"AVON"(4) + Seq(1) + Cmd1 opcode(17) + CRC(2) + JSON payload`. Bridge จำแนกคำสั่งจาก `cmd1_hex` (byte 5..22); คำสั่ง Show/Clear มี `identifier` ใน payload → map เป็นชื่อ `.vsn` (CLOSE/MTC/ETC/ETC-MTC) แล้วยิง REST. `LSD_Simulator/avon_protocol.py` เพิ่มฝั่ง **encoder** (`build_show/clear/control`) ที่ `LSD_Contoller/` ไม่มี
- **`LSD_Bridge.py`** = TCP server (เสมือนเป็น LSD ตัวจริง) → ตอบ ACK สั้น + spawn thread ยิง HTTP. log แบ่งตาม level ละเอียด: INFO=เหตุการณ์หลัก, DEBUG=ขั้นตอน/mapping/timing, TRACE=hex dump ทุก frame (ปรับที่ `log_Level` ใน config)
- **NovaDemo** = จำลอง TCT ฝั่งส่ง (ไบต์ที่ส่งตรงกับ TCT จริงทุก byte รวม CRC), **ColorRecDemo** = จำลอง controller ใหม่ฝั่งรับ (HTTP server + โชว์สีป้าย). ต่อตรงกันไม่ได้ (คนละโปรโตคอล) ต้องมี Bridge คั่นเสมอ
- **ไฟล์ร่วมของทั้ง Python 3 โปรแกรม:**
  - `LogLibrary.py` — loguru + config JSON. key ที่ชื่อมีคำว่า `pass` จะถูก **เข้ารหัสด้วย Fernet** อัตโนมัติ (ต้องมี `<Program>.key` ข้างไฟล์; ตั้ง `LOGLIB_KEY` env เพื่อ override) — อย่าลบ `.key`
  - config JSON (`<Program>_config.json`) สร้างอัตโนมัติครั้งแรกที่รัน; ถ้าไฟล์เสียจะ log `CONFIG ERROR` แล้วรันด้วย default โดย**ไม่เขียนทับ**
  - `qt_compat.py` (เฉพาะ LSD_Simulator) — shim ให้ import Qt ได้ทั้ง PySide6/PyQt6/PyQt5/PySide2

## ข้อควรระวังเมื่อแก้โค้ด C# (สำคัญมาก — ดู Bug.md)
ปัญหาเชิงระบบที่ฝังทั่วโค้ด:
1. **ห้ามบล็อก GTK main thread** — โค้ดเดิมมี `Thread.Sleep()` และ sync socket/HTTP บน UI thread เยอะ (รวมถึงใน `Application.Invoke` delegate) → จอค้าง. ใช้ `GLib.Timeout.Add` / async แทน
2. **ห้ามแตะ GTK widget จาก background thread** — ต้องครอบ `Application.Invoke(...)` เสมอ (มีหลายจุดที่ละเมิด เช่น EDC dialog เรียก `Dispose()/Destroy()` ตรงๆ จาก thread)
3. **state ส่วนกลางไม่มี lock** — โดยเฉพาะ `TCT_Lib.xListLoopCar` ถูกหลาย thread อ่าน/`Clear()`/index `[0]` พร้อมกัน → `IndexOutOfRangeException`. ต้องใส่ lock เวลาแตะ
4. **`async void` + `while(true)` ไม่มี cancellation** หลายจุด (keep-alive, monitoring, import staff) → thread ตายเงียบ. มี `Thread.Abort()` ที่อันตราย
5. **resource leak** — `StreamWriter`/socket/RabbitMQ connection ไม่ dispose ในเส้นทาง error
6. **parse input ภายนอกไม่ป้องกัน** — `Substring()` บน EDC response, dynamic JSON access, `dt.Rows[0]`/`DBNull` ไม่เช็คก่อน

## เอกสารในโปรเจกต์
- `TCT_App.md` — เอกสารโครงสร้าง/การทำงานแอปหลัก (ภาษาไทย, สำหรับเจ้าของโปรเจกต์)
- `Bug.md` — รายงานบั๊กที่ทำให้ระบบค้าง/ดับ พร้อมเลขบรรทัด จัดหมวด A–G
- `../LSD_Contoller/LSD_Bridge/README.md`, `../LSD_Simulator/README.md` — คู่มือ Bridge + ตัวจำลอง (repo แยก)
