# CLAUDE.md — TCT_App

แอปนี้คือ **Toll Collection Terminal (TCT)** — ซอฟต์แวร์หน้าตู้เก็บเงินค่าผ่านทาง 1 ช่อง (lane)
เป็น **C# / GTK# 2.4 / Mono** desktop GUI (full screen, รันบน Linux) ไม่มี package.json/Node — build ด้วย MSBuild/xbuild จาก `TCT_App.sln`

## สถาปัตยกรรมหลัก (จำไว้)
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

## ข้อควรระวังเมื่อแก้โค้ด (สำคัญมาก — ดู Bug.md)
ปัญหาเชิงระบบที่ฝังทั่วโค้ด:
1. **ห้ามบล็อก GTK main thread** — โค้ดเดิมมี `Thread.Sleep()` และ sync socket/HTTP บน UI thread เยอะ (รวมถึงใน `Application.Invoke` delegate) → จอค้าง. ใช้ `GLib.Timeout.Add` / async แทน
2. **ห้ามแตะ GTK widget จาก background thread** — ต้องครอบ `Application.Invoke(...)` เสมอ (มีหลายจุดที่ละเมิด เช่น EDC dialog เรียก `Dispose()/Destroy()` ตรงๆ จาก thread)
3. **state ส่วนกลางไม่มี lock** — โดยเฉพาะ `TCT_Lib.xListLoopCar` ถูกหลาย thread อ่าน/`Clear()`/index `[0]` พร้อมกัน → `IndexOutOfRangeException`. ต้องใส่ lock เวลาแตะ
4. **`async void` + `while(true)` ไม่มี cancellation** หลายจุด (keep-alive, monitoring, import staff) → thread ตายเงียบ. มี `Thread.Abort()` ที่อันตราย
5. **resource leak** — `StreamWriter`/socket/RabbitMQ connection ไม่ dispose ในเส้นทาง error
6. **parse input ภายนอกไม่ป้องกัน** — `Substring()` บน EDC response, dynamic JSON access, `dt.Rows[0]`/`DBNull` ไม่เช็คก่อน

## เอกสารในโปรเจกต์
- `TCT_App.md` — เอกสารโครงสร้าง/การทำงาน (ภาษาไทย, สำหรับเจ้าของโปรเจกต์)
- `Bug.md` — รายงานบั๊กที่ทำให้ระบบค้าง/ดับ พร้อมเลขบรรทัด จัดหมวด A–G
