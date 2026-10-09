# TCT_App — เอกสารโครงสร้างและการทำงานของระบบ

> **TCT = Toll Collection Terminal** — แอปพลิเคชันหน้าตู้เก็บเงินค่าผ่านทาง (ช่องเก็บเงิน/Lane)
> เขียนด้วย **C# / GTK# (gtk-sharp 2.4)** รันบน **Mono** (Linux) — เป็น Desktop GUI แบบ Full Screen
>
> เอกสารนี้ตรงกับ **v2.0.3** (2026-10-07) — ประวัติการแก้ไขแต่ละเวอร์ชันดู **`Version.md`**

---

## 1. ภาพรวม (Overview)

แอปนี้คือซอฟต์แวร์ที่ทำงานบนเครื่องคอมพิวเตอร์ประจำช่องเก็บเงินค่าผ่านทาง ทำหน้าที่:

- แสดงผลหน้าจอให้พนักงานเก็บเงิน (ประเภทรถ, ค่าผ่านทาง, วิธีชำระเงิน, สถานะช่อง)
- รับ event จากอุปกรณ์ในช่อง (sensor ตรวจจับรถ, AVC จำแนกประเภทรถอัตโนมัติ, ไม้กั้น ALB/OTL, ไฟสัญญาณ)
- ประมวลผลการเก็บเงินหลายรูปแบบ: เงินสด, คูปอง, บัตรเครดิต EMV/EDC, QR Code, EasyPass/M-Pass (ETC)
- บันทึก transaction ลงฐานข้อมูล SQLite และส่งต่อไปยังศูนย์ (Data Center / RTO / HQ)
- เชื่อมต่อกล้อง NVR เพื่อถ่ายภาพรถ, พิมพ์ใบเสร็จผ่านเครื่องพิมพ์ ESC/POS
- รองรับโหมดช่อง MTC (เก็บเงินสด), ETC (อัตโนมัติ), Dual (ผสม)

### Stack / Dependency หลัก
| ส่วน | เทคโนโลยี |
|------|-----------|
| UI | GTK# 2.4 (gtk/gdk/glib/pango/atk-sharp), Stetic designer (`gtk-gui/`) |
| Runtime | Mono (.NET Framework 4.6.1 style) |
| ฐานข้อมูล | SQLite ผ่าน `Mono.Data.Sqlite` (ไฟล์ `TCT_DB.db`) |
| Message Queue | RabbitMQ.Client 6.8.1 (3 broker: MTS, MOC/DC, ETC) |
| Socket | SuperSocket (server), WebSocket4Net (client), TCP socket ดิบ |
| Payment | EMV/EDC ผ่าน TCP socket, QRCoder สำหรับ QR |
| Printing | ESCPOS 1.2.2 (windows-874 / Thai encoding) |
| JSON | Newtonsoft.Json 13 |
| อื่นๆ | log4net, Renci.SshNet (SFTP), System.Drawing |

---

## 2. โครงสร้างไฟล์ (Project Layout)

```
TCT_App/
├── Program.cs                      จุดเริ่มแอป (Main) — ฆ่า process เก่า, สร้างโฟลเดอร์ log, เปิด LoginWnd
├── LoginWnd.cs            (1,698)  หน้าต่าง Login + เจ้าของ GTK main loop + timer หลัก + เริ่ม thread LSD
├── MainWindow.cs         (3,468)  หน้าจอหลักของช่อง — แสดงผล + ปุ่มทุกอย่าง + วาด UI (Pango/DrawingArea)
├── TCT_Lib.cs            (3,972)  คลังตัวแปร static ส่วนกลาง + โหลด config + utility (เวลา, network check)
├── TCT_Obj.cs            (1,590)  สร้าง object/JSON จากข้อมูล DB เพื่อส่งเข้า RabbitMQ
├── TCT_Log.cs              (500)  ระบบเขียน log ลงไฟล์ (แยกประเภท: Event, Trans, EDC, MTS, LSD, Camera)
├── TCT_MTS.cs / TCT_NVR.cs        monitoring สถานะระบบ (MTS) / ดึงภาพจากกล้อง NVR (HTTP)
├── TCT_Version.cs / TCT_MTS_Alarm.cs
├── UiTheme.cs                      (v2.0.2+) ธีมหน้าจอ: สีปุ่ม, การ์ด, ขอบมน, Popup, หัวจอ, แถบสถานะ (ดูหัวข้อ 8)
│
├── LaneController/
│   └── LaneEventControl.cs (8,355) ★ หัวใจของระบบ — state machine การเก็บเงิน + จัดการ event ทั้งหมด + thread พื้นหลัง
│
├── Database/
│   └── LaneDB.cs         (3,802)  ชั้นเข้าถึง SQLite ทั้งหมด (transaction, event, alarm, staff, tariff)
│
├── LaneSocket/                     การสื่อสาร socket
│   ├── LaneWebSocket.cs    (972)   client → Lane Controller (LCSD) ผ่าน WebSocket (JSON-RPC)
│   ├── LaneSocketServer.cs (551)   server รับการเชื่อมต่อจาก RTO (จอ supervisor)
│   └── LaneSocket_SupAdjust.cs     server รับ Supervisor Adjust
│
├── RabbitMQ/
│   ├── MtsRabbitMQ.cs              ส่งข้อมูล monitoring (CPU/RAM/disk/device)
│   ├── MocRabbitMQ.cs              รับ staff/tariff/config จากศูนย์ + ส่ง event/transaction
│   └── EtcRabbitMQ.cs              สื่อสารระบบ ETC (Lane State, Compiled Passage, Manual Action)
│
├── EMV/                            การชำระเงินบัตร/QR (มี V0 และ V1)
│   ├── EMV_V1/EdcPaymentDlg_V1.cs  dialog ชำระบัตร EMV/EDC
│   ├── EMV_V1/EdcPayQrDlg_V1.cs    dialog ชำระ QR Code
│   └── EMV_V1/EmvLib_V1.cs ...     library คุย EDC device
│
├── LSD/                            Lane Signal Device (ควบคุมไฟ/ป้ายแสดงสถานะช่อง)
│   └── LaneSocketControl_LSD.cs    ★ ตัวที่ใช้จริง (ไฟล์ซ้ำถูกลบตั้งแต่ v2.0.0)
│
├── PrintUtils/                     พิมพ์ใบเสร็จ ESC/POS, คูปอง, กราฟิก
├── WebServiceAPI/                  เรียก REST API (Begin/End Job, ส่งภาพกล้อง, คูปองขาย)
├── LaneView/MaintenanceWnd/MainMaintenance.cs (3,030)  หน้าจอ Maintenance (ทดสอบอุปกรณ์)
├── Config/                         log4net config
└── gtk-gui/                        โค้ด UI ที่ Stetic generate (ไม่ควรแก้มือ)

TCT_Sim/                            (ระดับ repo) ระบบจำลองช่องทาง สำหรับรัน/ทดสอบ TCT โดยไม่ต้องมีอุปกรณ์จริง (หัวข้อ 6)
```

---

## 3. การไหลของการทำงาน (Application Flow)

### 3.1 ลำดับการเริ่มต้น (Startup)
1. `Program.Main()` — ฆ่า process `TCT_App` เก่าที่ค้างอยู่ (กัน run ซ้อน)
2. `TCT_Lib.LoadTCT_Configuration()` — อ่านไฟล์ config (JSON) ~200+ ค่า, เปิด connection SQLite
3. `TCT_Lib.LoadKeyMapping()` — โหลดผังปุ่มกด
4. สร้างโฟลเดอร์ log ทั้งหมด (LaneLog, LaneEvent, LaneTrans, LaneAlarm, RabbitMQ, VideoCapture, TfiImage)
5. `UpdateDatabaseStructure()` + `BackupLogFile()`
6. เปิด `LoginWnd` แบบ Fullscreen → `Application.Run()` (เริ่ม GTK event loop)

### 3.2 Login
- พนักงานกรอกรหัส → ตรวจสิทธิ์กับ DB
- ถ้าเป็นกลุ่ม Maintenance (group 50–59) → เปิด `MainMaintenance`
- ถ้าเป็นพนักงานเก็บเงิน → เรียก Web Service `CheckTODBoj` (เช็คกะ) → เปิด `MainWindow`
- เริ่ม timer หลายตัว: นาฬิกา 1 วิ, sync RTO 3 วิ, auto-send DC 5 นาที, TFI 2 นาที ฯลฯ
- เริ่ม background thread ของ LSD

### 3.3 วงจรการเก็บเงิน 1 คัน (Lane Transaction State Machine)
อยู่ใน `LaneEventControl.cs` เป็นหลัก:

```
รอรถ (Idle)
  │  sensor/loop ตรวจจับรถเข้า → AVC จำแนกประเภทรถอัตโนมัติ
  ▼
เลือกประเภทรถ (CLASS)  ← พนักงานกดปุ่ม CLASS_1/2/.. หรือ Auto-AVC
  │
  ▼
เลือกวิธีชำระ (PAYMENT)  ← เงินสด / คูปอง / EMV / QR / ETC
  │
  ▼
ประมวลผลชำระเงิน  ← ถ้า EMV/QR เปิด dialog คุย EDC device, ถ้า ETC คุย RabbitMQ
  │
  ▼
ยืนยัน + บันทึก  → เขียน DB, ออกเลขใบเสร็จ, พิมพ์ใบเสร็จ, ส่งเข้า RabbitMQ (DC/RTO/HQ)
  │
  ▼
เปิดไม้กั้น (ALB/OTL) → รถผ่าน → กลับไปรอรถคันถัดไป
```

ภาพกล้อง (NVR) ถ่ายตาม `VideoNVR.CaptureImageMethod`: 0 = สั่ง NVR บันทึกเอง, 1 = ถ่ายตอนรับชำระ,
2 = ถ่ายตอนรถทับ Loop (หรือ OB ถ้าไม่มี Loop), 3 = หลัง/หลัง/หน้า ตาม Loop/OB — รายละเอียดหัวข้อ 3.5

โหมดพิเศษ:
- **TOWING** — รถลาก (รถคันแรก + คันที่ถูกลาก) มี state แยก 6 ขั้น
- **CONVOY / HPMC** — ขบวนรถผ่านพร้อมกันหลายคัน
- **Violation** — ประเภท/การชำระไม่ตรง หรือ sensor ขาดข้อมูล

### 3.4 Thread พื้นหลังหลัก (ใน LaneEventControl)
| Thread | หน้าที่ | รอบ |
|--------|---------|-----|
| `ThreadCheckNetworkStatus` | เช็ค connectivity LC/RTO/DC/EDC/LSD/Antenna | ~30–60 วิ |
| `ThreadAutoResendTransaction` | ส่ง transaction ที่ค้างซ้ำ | 180 วิ |
| `ThreadChkDeviceStatus` | เช็คสถานะ port อุปกรณ์ | 60 วิ |
| `ThreadTfiTransferFile` | โอนภาพ TFI | 180 วิ |
| `ThreadEndTransaction` | (⚠ ว่างเปล่า — เป็น stub) | 100 ms |

### 3.5 การสื่อสารกับภายนอก
- **LCSD (Lane Controller)** — WebSocket client, รับ KEY/AVC/IODI(input)/IODO(output)/CRS, มี keep-alive PING/PONG
- **RTO (จอ supervisor)** — SuperSocket server, รับคำสั่งควบคุม + ส่งสถานะช่อง
- **Data Center (MOC)** — RabbitMQ, รับ staff/tariff/config, ส่ง event/transaction
- **ETC** — RabbitMQ, Lane State + Compiled Passage + Manual Action
- **MTS** — RabbitMQ, ส่งสถานะระบบ (monitoring)
- **กล้อง / NVR** — HTTP GET ภาพจาก `VideoLaneCapture.Front/Rear/ThirdCameraUrl` → ย่อ 1280x720 JPEG Q85 (v2.0.3, ตั้งค่าได้)
  → บันทึกใน `CaptureImagePath` + ตาราง `ImageCaptureCamera` → หลังเขียนรายการ POST JSON (base64 3 ภาพ) ไป `VideoNVR.WebApiBaseUrl`
  (ต้องตอบ `respCode "0000"`), ส่วน DC/RTO/Sup Adjust ได้แค่ชื่อไฟล์ภาพ
- **EDC** — TCP socket คุยเครื่องรูดบัตร

---

## 4. ตัวแปร/สถานะส่วนกลางสำคัญ (ใน TCT_Lib — static ทั้งหมด)

| ตัวแปร | ความหมาย |
|--------|----------|
| `iLaneID`, `szLaneName`, `iLaneConfigType` | ข้อมูลช่อง (0=MTC, 1=ETC, 2=Dual) |
| `eCurLaneType`, `eCurLaneMode` | ประเภท/โหมดช่องปัจจุบัน |
| `szStaffID`, `iStaffShiftID`, `dtStaffStartShift` | ข้อมูลพนักงาน/กะ |
| `nTrx`, `iJobNo`, `iCurTC_Class`, `fCurFareAmount` | ตัวนับ transaction / ประเภท / ค่าผ่านทาง |
| `xCurPayment`, `xCurEDC`, `xCurEMV`, `xCurQR` | ข้อมูลการชำระเงินปัจจุบัน |
| `xListLoopCar`, `xListQueues`, `szListReceiptNo` | คิวรถ/รายการที่ตรวจจับได้ (มี lock + safe accessor ตั้งแต่ v2.0.0 — โค้ดใหม่ต้องใช้ lock เสมอ) |
| `bDC_NetworkStatus`, `bEMV_Connected`, `bLSD_Connected` ... | สถานะการเชื่อมต่ออุปกรณ์ต่างๆ |
| `laneController`, `webSocketLane`, `lsdController`, `LaneDB` | อ้างอิง object ระบบย่อย |

> **ข้อสังเกตเชิงสถาปัตยกรรม:** ระบบใช้ "global static state" เป็นศูนย์กลางอย่างหนัก (TCT_Lib เก็บเกือบทุกอย่าง) และมีหลาย thread เข้าถึงตัวแปร/list เดียวกัน — v2.0.0 ใส่ lock ให้คิวรถหลักแล้ว แต่ตัวแปรอื่นส่วนใหญ่ยังไม่มี (ดู `Bug.md`)

---

## 5. ฐานข้อมูล (SQLite — `TCT_DB.db`)

ตารางหลัก: `LaneTransaction`, `LaneEvent` / `LaneEventRTO` / `LaneEventHQ`, `LaneAlarm`, `StaffShift` / `SupervisorShift`, `TransactionStatistic`, `EmvDetail`, `SalesCouponTransaction`, `DmtCardTrans`, `Tariff`, `Staff`, `Plaza`, `TransactionSummary`

- ใช้ **connection เดียว** เปิดค้างตลอดอายุแอป (`LaneDBLib` constructor)
- 2 เมธอดหลัก: `List(query)` คืน `DataTable`, `Execute(query)` สำหรับ INSERT/UPDATE/DELETE (timeout 10 วิ)
- Query สร้างด้วยการต่อ string / `string.Format` (⚠ เสี่ยง SQL injection — ดู Bug.md)
- เปิด **WAL mode + Busy Timeout** ตั้งแต่ v2.0.0 → มีไฟล์ `TCT_DB.db-wal`, `TCT_DB.db-shm` อยู่ข้างไฟล์ DB (ห้ามลบ, สำรองต้องคัดลอกไปด้วย)
- ตารางภาพกล้อง: `ImageCaptureCamera` (ชื่อไฟล์ภาพต่อรายการ, สถานะส่งภาพ `SendStatusImage`)

---

## 6. การ Build / Run

- Solution: `TCT_App.sln` — build ด้วย MSBuild/Mono (`msbuild` หรือ `xbuild`)
- ใช้ NuGet packages ในโฟลเดอร์ `packages/` (`packages.config`)
- มี `supersocket.sh` / `supersocket.cmd` เป็น helper script
- รันบน Linux ผ่าน Mono, เปิดเต็มจอ (`#define __FULL_SCREEN__` ใน Program.cs)
- โครงโฟลเดอร์ที่เครื่องช่อง: `<root>/app/TCT_App.exe`, `<root>/config/TCT.config` (JSON), `<root>/log/`
  — config อ่านจาก `../config/TCT.config` เทียบกับโฟลเดอร์ exe (`ConfigInfo.GetConfigPath()` ใน `TCT_Log.cs`)
- **Simulator (`TCT_Sim/`)** — Docker: จอ TCT (Xvfb + noVNC), Lane Controller, คีย์บอร์ด Cherry บนเว็บ, EDC (บัตร/QR),
  กล้อง 3 ตัว + Web API รับภาพ, DC (RabbitMQ), Sup Adjust, RTO, TA Web Service
  ```bash
  cd TCT_Sim
  ./sim.sh build        # build ด้วย Mono 6.12 ใน Docker (Release + Debug)
  ./sim.sh start        # เปิดระบบจำลอง → http://localhost:8090
  ./sim.sh kbtest       # ทดสอบอัตโนมัติด้วยคีย์บอร์ด (35 กรณี)
  ./sim.sh soak         # ทดสอบต่อเนื่อง 10 นาที
  python3 gui_tour.py   # ถ่ายภาพทุกหน้าจอ / Popup
  ```

---

## 7. จุดที่ควรระวังเป็นพิเศษ (สรุปสั้น)

1. **โค้ดบล็อก UI thread** — ยังมี socket/HTTP แบบ synchronous บน GTK main thread บางจุด → จอค้าง (v2.0.0 แก้ `Thread.Sleep` แล้ว)
2. **thread synchronization** — คิวรถมี lock แล้ว (v2.0.0) แต่ตัวแปรส่วนกลางอื่นยังไม่มี → แตะจากหลาย thread ต้องระวัง
3. **GTK ถูกเรียกจาก background thread** — ผิดกฎ GTK (ต้องผ่าน `Application.Invoke`) → crash สุ่ม
   (v2.0.2 แก้ Popup EMV/QR ที่ปิดตัวเองจาก socket thread แล้ว — เคยทำโปรแกรมดับ)
4. **Resource leak** — socket/StreamWriter/RabbitMQ connection ในเส้นทาง error (v2.0.0 แก้ส่วนใหญ่แล้ว)
5. **`ModifyBg` ทุกรอบ / Dispose `PangoRenderer.GetDefault()`** — ทำให้หน้าจอไม่วาดใหม่ (เจอในโหมดช่าง แก้แล้ว v2.0.3) ห้ามทำซ้ำ

> รายละเอียด bug ทั้งหมดพร้อมเลขบรรทัด อยู่ในไฟล์ **`Bug.md`**

---

## 8. ธีมหน้าจอ (`UiTheme.cs`, v2.0.2–2.0.3)

ปรับเฉพาะสี/ตัวอักษร/เส้น — **ตำแหน่งปุ่มทุกปุ่มเหมือนเดิม** (ไม่ได้แก้ `gtk-gui/`)

| ฟังก์ชัน | ใช้ทำอะไร |
|---|---|
| `ApplyGlobalStyle()` | เรียกครั้งเดียวใน `Program.cs` หลัง `Application.Init()` — ปุ่มไม่มีกรอบ focus |
| `StyleKey(btn, สี, font)` | ปุ่มสีตามคีย์ Cherry + ขอบมน + ตัวอักษรตัดกับพื้นอัตโนมัติ (ปุ่มไม่รับ focus) |
| `StylePopup(window)` | ไม่มีแถบหัวหน้าต่าง, มุมมน, กรอบเส้นสีอ่อน, ปุ่มตกลง=เขียว / ยกเลิก=แดง / พิมพ์=ม่วง อัตโนมัติจากข้อความปุ่ม |
| `FlattenFrames(window)` | เปลี่ยนกรอบ GTK (เส้นคู่) เป็นการ์ดพื้นสีอ่อน ไม่มีเส้น มุมมน |
| `StyleHeader(...)` | หัวจอ 3 กลุ่ม (ด่าน/ช่อง \| พนักงาน \| กะ) ข้อความ/ความกว้าง/ฟอนต์เดียวกันทุกหน้า |
| `StyleStatusBar(statusbar)` | แถบสถานะล่าง: เส้นคั่น 1 เส้นระหว่างทุกช่อง |
| `SetButtonText(btn, text)` | เปลี่ยนข้อความปุ่มที่มีไอคอน (ไม่ทำไอคอนหาย) |

กฎเมื่อเพิ่มหน้าจอ / Popup ใหม่:
- Popup ใหม่ → เรียก `UiTheme.StylePopup(this)` หลัง `Build()` (ได้กรอบ มุมมน สีปุ่มตกลง/ยกเลิก อัตโนมัติ)
- ปุ่มที่ตรงกับคีย์บนคีย์บอร์ด Cherry → `UiTheme.StyleKey(ปุ่ม, สีของคีย์นั้น)` ให้สีบนจอ = สีบนคีย์บอร์ด
- ข้อความยาวในปุ่ม/DrawingArea → ตรวจว่าไม่ถูกตัดที่จอ 1024x768 (`python3 TCT_Sim/gui_tour.py`)
