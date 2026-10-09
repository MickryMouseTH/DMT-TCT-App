# Bug.md — รายงานบั๊กที่อาจทำให้ระบบ "ค้าง" หรือ "ดับ"

> ## สถานะการแก้ไขใน Version 2.0.0 (2026-06-02)
> branch: `fix/v2.0.0-stability` — แก้ไขบน working tree แล้ว (ยังไม่ได้ build/test เพราะเครื่องนี้ไม่มี Mono)
>
> | Bug | สถานะ | ไฟล์ที่แก้ |
> |-----|-------|-----------|
> | A1 Thread.Sleep ใน Application.Invoke (ALB) | ✅ แก้แล้ว (ใช้ GLib.Timeout) | LaneEventControl.cs |
> | A2 Thread.Sleep ใน event handler | ⏳ รอ test env (เสี่ยงสูง) | - |
> | A3 HTTP sync ตอน login | ⏳ รอ test env (เสี่ยงสูง) | - |
> | A4 LSD blocking read | ⏳ รอ test env (เสี่ยงสูง) | - |
> | B1/B2 race xListLoopCar + index guard | ✅ แก้จุดอันตราย (lock + LoopCarFirst) | TCT_Lib.cs, LaneEventControl.cs |
> | B3 GTK cross-thread | 🔶 บางส่วน (reconnect ย้ายขึ้น main loop) | LaneWebSocket.cs |
> | B4 bCancel volatile | ✅ แก้แล้ว | EdcPaymentDlg_V1.cs |
> | B5 counter race | ✅ แก้แล้ว (Interlocked) | LaneDB.cs |
> | C1 ThreadEndTransaction stub | ✅ แก้แล้ว | LaneEventControl.cs |
> | C2 async void loop | 🔶 บางส่วน (guard interval) | MocRabbitMQ.cs |
> | C3 Thread.Abort | ✅ แก้ MOC import staff | MocRabbitMQ.cs |
> | D1 StreamWriter leak | ✅ แก้แล้ว (using + lock) | TCT_Log.cs |
> | D2 RabbitMQ conn ไม่ dispose | ✅ แก้แล้ว | EtcRabbitMQ.cs |
> | D3 SqliteCommand leak + try-catch | ✅ แก้แล้ว | LaneDB.cs |
> | E1 Substring EDC ไม่เช็คความยาว | ✅ แก้แล้ว (V0+V1, pay+QR) | EMV/* |
> | F1 WebSocket reconnect backoff | ✅ แก้แล้ว | LaneWebSocket.cs |
> | F2 RabbitMQ ack ก่อนประมวลผล | ✅ แก้แล้ว (ETC+MOC) | EtcRabbitMQ.cs, MocRabbitMQ.cs |
> | F4 NVR timeout infinite | ✅ แก้แล้ว (min 10s) | TCT_NVR.cs |
> | F5 SQLite WAL/busy timeout | ✅ แก้แล้ว | LaneDB.cs |
> | G3 ไฟล์ LSD ซ้ำ | ✅ ลบแล้ว | LSD/ |
> | E2, E3, G1, G2 ฯลฯ | ⏳ ยังไม่แก้ | - |
> | H1 Force Reset ทิ้งรายการรถยกเว้น (OB อย่างเดียว ไม่มี Loop) → ไม่ขึ้น DC/Sup Adj | ✅ แก้แล้ว (2026-10-07, ยังไม่ได้ build/test) | LaneEventControl.cs |
> | H2 OB อย่างเดียวไม่มี Loop → ไม่ตั้ง Auto AVC (รายการค้างคิว) | ✅ แก้แล้ว (2026-10-07, ยังไม่ได้ build/test) | LaneEventControl.cs |
> | H3 Force Reset อาจเรียก ForceEnd ซ้ำ 2 ครั้ง | ✅ แก้แล้ว (2026-10-07, ยังไม่ได้ build/test) | LaneEventControl.cs |
>
> **หมายเหตุสำคัญ:** git repository ของโปรเจกต์นี้เสียหาย (corrupt loose objects + index) — ต้องกู้ git ก่อนจึงจะ commit ได้
> ⏳ = เป็นการแก้เชิงสถาปัตยกรรมที่เสี่ยงสูง ควรมี environment ที่ build/test ได้ก่อนทำ
> ---


> วิเคราะห์จาก source code ของ TCT_App (C#/GTK#/Mono — ระบบเก็บเงินค่าผ่านทาง)
> เน้นเฉพาะบั๊กที่ทำให้ระบบ **ค้าง (hang/freeze)**, **ดับ (crash)**, หรือ **เสื่อมสภาพจนล่ม (resource leak → OOM/handle exhaustion)**
> เลขบรรทัดได้รับการ verify กับโค้ดจริงในจุดสำคัญแล้ว (อาจคลาดเคลื่อน ±2–3 บรรทัดในบางจุด)

## วิธีจัดลำดับความรุนแรง
- 🔴 **Critical** — ทำให้ระบบค้าง/ดับได้โดยตรงในการใช้งานจริง
- 🟠 **High** — ค้าง/ดับภายใต้เงื่อนไข error/network/อุปกรณ์มีปัญหา หรือ leak สะสมจนล่ม
- 🟡 **Medium** — เสี่ยง, ทำให้ข้อมูลเพี้ยน/เสื่อมสภาพช้าๆ

---

## หมวด A — บล็อก UI Thread (จอค้าง / ระบบเหมือนแฮงค์)

GTK เป็น single-thread การบล็อก main thread = ทั้งจอหยุดวาด, รับปุ่มไม่ได้ ดูเหมือนแอปค้าง

### A1. 🔴 `Thread.Sleep()` ภายใน `Application.Invoke` (รันบน GTK main thread)
- **ไฟล์:** `LaneController/LaneEventControl.cs` ~บรรทัด 5375–5381 (และจุดคล้ายกัน 5388, 5459, 5482)
- **โค้ด (verify แล้ว):**
  ```csharp
  private bool Thread_CmdControlOffALB_Open() {
      Application.Invoke(delegate {
          TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Open, OFF);
          Thread.Sleep(100);   // ← บล็อก GTK main thread!
          TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Open, ON);
          ...
      });
  }
  ```
- **ผล:** ทุกครั้งที่ควบคุมไม้กั้น ALB จอจะค้าง 100 ms (สะสมหลายจุดได้นานกว่า) → กดปุ่มไม่ติด, จอกระตุก
- **แก้:** ใช้ `GLib.Timeout.Add` แทน `Thread.Sleep` (ทำงานแบบ non-blocking) — โค้ดบางส่วนทำถูกแล้ว ให้ทำให้สม่ำเสมอ

### A2. 🔴 `Thread.Sleep(1000/500)` + DB write ใน event handler
- **ไฟล์:** `LaneController/LaneEventControl.cs` ~บรรทัด 713–717 (และอีก 30+ จุด เช่น 1063, 1494, 1598, 6997, 7151)
- **โค้ด:**
  ```csharp
  TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.JOB, 1);
  Thread.Sleep(1000);     // ← จอค้าง 1 วินาที
  TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.JOB, 1);
  Thread.Sleep(500);
  ```
- **ผล:** ทุกครั้งที่กดปุ่มสำคัญ (เปิด OTL ฯลฯ) จอค้าง 1–1.5 วิ
- **แก้:** ย้ายงานหน่วงเวลาไป background หรือใช้ async timer

### A3. 🔴 HTTP web service แบบ synchronous บน UI thread ตอน Login
- **ไฟล์:** `WebServiceAPI/WebService_BeginEndJob.cs` บรรทัด ~266 (`request.GetResponse()`), เรียกจาก `LoginWnd.cs` ~707 บน UI thread
- **โค้ด:** `using (WebResponse webResponse = request.GetResponse())` — timeout 3 วิ
- **ผล:** ถ้า TA server ช้า/ล่ม จอ Login ค้าง 3+ วิ ไม่มี feedback → ดูเหมือนแอปตาย
- **แก้:** เปลี่ยนเป็น `HttpClient.GetAsync()` + แสดง spinner

### A4. 🔴 `networkStream.Read()` (blocking) ในเส้นทาง LSD keep-alive
- **ไฟล์:** `LSD/LaneSocketControl_LSD.cs` บรรทัด ~737 (`int iLen = networkStream.Read(...)`)
- **โค้ด (verify แล้ว):** ตั้ง `ReadTimeout = 1000/3000` ก่อน แต่ `Read()` ยังบล็อกได้จนครบ timeout ถ้าอุปกรณ์เงียบ
- **ผล:** ถ้า LSD ไม่ตอบ keep-alive thread บล็อก 1–3 วิ/รอบ, งานที่ขึ้นกับ LSD หยุด
- **แก้:** ใช้ `ReadAsync` + `CancellationToken`

### A5. 🟠 LSD init retry loop หน่วง 3 วิ × 3 ครั้ง = 9 วิ ตอนเริ่มแอป
- **ไฟล์:** `LSD/LaneSocketControl_LSD.cs` ~บรรทัด 272–284 (`while(client==null)` + `await Task.Delay(3000)`)
- **ผล:** ถ้า LSD offline ตอนเปิดแอป → หน่วง 9 วิก่อนพร้อมใช้งาน
- **แก้:** init แบบ background ไม่บล็อกการเปิดหน้าจอ

---

## หมวด B — Threading / Race Condition (crash สุ่ม, ข้อมูลเพี้ยน)

### B1. 🔴 เข้าถึง `TCT_Lib.xListLoopCar` จากหลาย thread โดยไม่มี lock
- **ไฟล์:** `LaneController/LaneEventControl.cs` หลายจุด (3221–3224, 3268–3284, 1411, 2451 ฯลฯ)
- **โค้ด:**
  ```csharp
  int iTotalLoop = TCT_Lib.xListLoopCar == null ? 0 : TCT_Lib.xListLoopCar.Count;
  int iTC_Class  = iTotalLoop == 0 ? ... : TCT_Lib.xListLoopCar[0].iTC_Class;  // ← race
  ```
- **ผล:** ช่วงระหว่างเช็ค `Count` กับเข้าถึง `[0]` thread อื่นอาจ `.Clear()` → `IndexOutOfRangeException` → crash, transaction ที่กำลังทำหาย
- **list ถูกเข้าถึงจาก:** UI thread, AVC event, key event, convoy mode — ไม่มี lock เลย
- **แก้:** ครอบด้วย `lock` ตัวเดียวกันทุกจุด หรือใช้ snapshot/`ConcurrentQueue`

### B2. 🟠 เข้าถึง `xListLoopCar[0]` โดยไม่เช็ค Count ก่อน
- **ไฟล์:** `LaneController/LaneEventControl.cs` บรรทัด ~1411 `if (TCT_Lib.xListLoopCar[0].iTC_Class == 9) ...`
- **ผล:** ถ้า list ว่าง → crash ทันที

### B3. 🟠 GTK widget ถูกเข้าถึง/อัปเดตจาก background thread
- **ไฟล์:** หลายจุด — `LaneEventControl.cs` (network status thread ~6614–6644), `MocRabbitMQ.cs` (callback thread), `EdcPaymentDlg_V1.cs` (483, 929, 954, 982 เรียก `Dispose()/Destroy()` ตรงๆ), `LoginWnd.cs` (~1685 constructor LSD)
- **ผล:** ผิดกฎ GTK single-thread → `Gdk-CRITICAL` / segfault / crash สุ่ม
- **แก้:** ครอบทุกการแตะ widget ด้วย `Application.Invoke(...)`

### B4. 🟠 `bCancel` flag ใช้ร่วมหลาย thread โดยไม่ sync (payment)
- **ไฟล์:** `EMV/EMV_V1/EdcPaymentDlg_V1.cs` บรรทัด 19, ~447, ~1095
- **ผล:** การยกเลิกการชำระอาจส่ง ACK ผิด หรืออ่านค่าเก่า (ไม่ volatile/lock)
- **แก้:** ใช้ `volatile bool` หรือ `lock`

### B5. 🟡 ตัวนับ static `iEventID/iFareTransID` ฯลฯ `++` โดยไม่ lock
- **ไฟล์:** `Database/LaneDB.cs` บรรทัด 22–25 (ประกาศ), เพิ่มที่ 276, 378, 537, 1070 ฯลฯ
- **ผล:** ถ้า 2 thread เขียนพร้อมกัน → ได้ TransactionID ซ้ำ → ข้อมูลชน/ละเมิด PK
- **แก้:** `Interlocked.Increment` หรือ lock

---

## หมวด C — Thread/Loop ที่ไม่มีทางออก & Async ผิดวิธี

### C1. 🔴 `ThreadEndTransaction` — `while(true)` ว่างเปล่า (stub)
- **ไฟล์:** `LaneController/LaneEventControl.cs` บรรทัด ~2991–3007 (verify แล้ว)
- **โค้ด:** `while(true){ try {} catch{...} Thread.Sleep(100); }` — try ว่างเปล่า
- **ผล:** เปลือง CPU/thread, ปิดแอปไม่ clean (thread ไม่ยอมจบ)
- **แก้:** ลบทิ้ง หรือ implement จริง + ใช้ event/CancellationToken

### C2. 🔴 `async void` thread loop ไม่มี cancellation + `.Abort()` ที่ unreachable
- **ไฟล์:** `RabbitMQ/MocRabbitMQ.cs` (~487–516 `ThreadTimerImportStaffAsync`), `TCT_MTS.cs` (~730–854 `ThreadGetEtcStatus`), `LSD/LaneSocketControl_LSD.cs` (~75 `ThreadEventSendKeepAlive`)
- **ปัญหา:** `async void` = exception ที่หลุดออกมาไม่มีใครจับ → thread ตายเงียบ; `while(true)` ไม่มีทางหยุด; บรรทัด `threadX.Abort()` หลัง loop เป็น dead code
- **ผล:** ถ้า thread ตายเงียบ → keep-alive/monitoring/import staff หยุดทำงานโดยไม่มีสัญญาณเตือน
- **แก้:** ใช้ `async Task` + `CancellationToken` + จัดการ exception ครบ

### C3. 🟠 `Thread.Abort()` กลางคัน (อันตราย)
- **ไฟล์:** `RabbitMQ/MocRabbitMQ.cs` (~445 `threadImportStaff.Abort()`), `EMV/EMV_V1/EdcPayQrDlg_V1.cs` (166, 432, 649)
- **ผล:** `ThreadAbortException` แทรกกลาง DB transaction/socket cleanup → DB ค้างสถานะไม่สมบูรณ์, finally ไม่รัน, socket leak
- **แก้:** เลิกใช้ `Abort()` ใช้ flag/CancellationToken ให้ thread จบเอง

### C4. 🟡 `async void` ThreadCheckNetworkStatus — ยิง network check ซ้อนกัน
- **ไฟล์:** `LaneController/LaneEventControl.cs` ~6597 (`async private void` + `while(true)` + `await IsPortReachable`)
- **ผล:** ถ้า network ช้า อาจยิง check ซ้อนหลายอัน → thread pool ตึง

---

## หมวด D — Resource Leak (เสื่อมสภาพ → ค้าง/ดับหลังใช้ไปนานๆ)

### D1. 🟠 `StreamWriter` ไม่ใช้ `using` ใน log methods (file handle leak)
- **ไฟล์:** `TCT_Log.cs` — `WriteLog` (~95–98, verify แล้ว) และอีก 6 เมธอด (148, 176, 203, 231, 437, 463, 489)
- **โค้ด:** `TextWriter tw = new StreamWriter(...); ...; tw.Close();` — ถ้า exception ก่อน `Close()` → handle รั่ว
- **ผล:** เรียกถี่มาก (ทุก transaction) → file handle หมด → เขียน log/ไฟล์ไม่ได้ → ลามไปส่วนอื่น
- **แก้:** ครอบด้วย `using (var tw = new StreamWriter(...)) {...}` (บางเมธอดทำถูกแล้ว เช่น `WriteLaneEventIO`)

### D2. 🔴 RabbitMQ connection/channel ไม่ถูก dispose ตอน error + `Close()` บน null
- **ไฟล์:** `RabbitMQ/EtcRabbitMQ.cs` (~104–105, 120–121 set null โดยไม่ dispose; ~136–137 `CloseEtcRabbitMQ` เรียก `channel.Close()`/`connection.Close()` ทั้งที่อาจ null)
- **ผล:** socket handle ของ RabbitMQ รั่วสะสม → "too many open files"; และ `CloseEtcRabbitMQ()` หลัง connect ล้มเหลว → `NullReferenceException`
- **แก้:** `Dispose()` ก่อน null + null-check ใน Close

### D3. 🟠 SqliteCommand ไม่ dispose ใน `List()/Execute()` + ไม่มี try-catch
- **ไฟล์:** `Database/LaneDB.cs` บรรทัด 74–89 (verify แล้ว)
- **โค้ด:** `var cmd = new SqliteCommand(query, conn); ... dataAdapter.Fill(dt);` — ไม่มี `using`, ไม่มี try-catch
- **ผล:** ถ้า `Fill()` throw ("database is locked") → exception หลุดออกไป crash; command ไม่ถูก dispose
- **แก้:** ครอบ `using` + try-catch + คืน DataTable ว่างเมื่อ error

### D4. 🟠 Socket ไม่ปิดในเส้นทาง error / ไม่มี finally (EMV)
- **ไฟล์:** `EMV/EMV_V1/EdcPaymentDlg_V1.cs` (~240–248, 1001–1065), `EdcPayQrDlg_V1.cs` (~922–938 ไม่ตั้ง timeout เลย)
- **ผล:** socket รั่ว → port หมด → ชำระเงินครั้งถัดไปเชื่อมต่อไม่ได้ / hang ไม่มี timeout
- **แก้:** ใช้ `using`/`finally` ปิด socket เสมอ + ตั้ง Send/Receive timeout ทุก socket

### D5. 🟠 Timer (GLib) ถูก leak — `RemoveAvcTimer` มี logic ผิด
- **ไฟล์:** `LaneController/LaneEventControl.cs` ~2937–2977
- **ผล:** timer ID ไม่ถูกถอดถูกต้อง → GLib timer สะสม → ระบบช้าลง/ครบ limit
- **หมายเหตุ:** `LoginWnd.cs` ~788 ก็ reset timer id ไม่ครบ (ขาด `iTimerIdSendScpTFI`)

### D6. 🟡 List ส่วนกลางโตไม่จำกัด (`xListLoopCar`, `xListQueues`, `szListReceiptNo`)
- **ไฟล์:** `TCT_Lib.cs` บรรทัด ~840–842
- **ผล:** ถ้า logic clear พลาดในเส้นทาง error → โตเรื่อยๆ → OutOfMemory หลังใช้งานหลายชั่วโมง

### D7. 🟡 ไฟล์ log โตไม่จำกัด / ไม่มี rotation
- **ไฟล์:** `TCT_Log.cs` (ทุกเมธอด เขียน append อย่างเดียว)
- **ผล:** หลายสัปดาห์ → disk เต็ม → เขียนไฟล์ fail → ลามทั้งระบบ
- **แก้:** เพิ่ม log rotation / cleanup เก่ากว่า N วัน

---

## หมวด E — การ parse input ภายนอกแบบไม่ป้องกัน (crash จากข้อมูลเพี้ยน)

### E1. 🔴 `Substring()` บน EDC response โดยไม่เช็คความยาว
- **ไฟล์:** `EMV/EMV_V1/EdcPaymentDlg_V1.cs` (~668 `Substring(17,2)`, ~721 `Substring(15,2)`), `EdcPayQrDlg_V1.cs` (381, 444)
- **ผล:** ถ้า EDC ตอบสั้นกว่าที่คาด (เช่น "OK"/"ERROR") → `IndexOutOfRangeException` ใน background thread → crash, socket ค้าง, lane ติด
- **แก้:** เช็ค `szResponse.Length` ก่อน Substring เสมอ

### E2. 🟠 JSON parse / dynamic access ไม่ป้องกัน null (socket/MQ handlers)
- **ไฟล์:** `LaneSocket/LaneWebSocket.cs` (~245–444 `ClassifyLaneMessage`), `RabbitMQ/EtcRabbitMQ.cs` (~150, 209), `LaneSocketServer.cs` (~459–491 WRDO)
- **ผล:** ข้อความผิดรูป/field หาย → exception; ถ้า callback delegate เป็น null → `NullReferenceException` → thread รับข้อมูลตาย, หยุดรับ message
- **แก้:** null-check ทุก field + null-check delegate ก่อนเรียก

### E3. 🟠 อ่านค่า DB เป็น null/DBNull แล้ว parse ตรงๆ
- **ไฟล์:** `Database/LaneDB.cs` (~157–158 `decimal.Parse(...FareClassType1...)`, 183, 212, 738 ฯลฯ) และ `dt.Rows[0]` โดยไม่เช็ค `Rows.Count`
- **ผล:** ถ้าตาราง config/tariff ว่างหรือ column NULL → `FormatException`/`IndexOutOfRangeException` ตอน startup → แอปเปิดไม่ขึ้น
- **แก้:** เช็ค `Rows.Count > 0` + `!= DBNull.Value` ก่อนใช้

---

## หมวด F — Network / Connection (ค้าง/storm เมื่อปลายทางมีปัญหา)

### F1. 🔴 WebSocket reconnect ไม่มี backoff (reconnect storm)
- **ไฟล์:** `LaneSocket/LaneWebSocket.cs` (~132–147 `Update_KeepALive`, 214 `Ws_OnError`, 235 `Ws_OnClosed`) — เรียก `StartLaneSocket()` ทันทีทุกครั้งที่ fail
- **ผล:** ถ้า LCSD ล่ม → reconnect รัวๆ หลายครั้ง/วินาที → กิน CPU, log ท่วม, อาจบล็อก UI ถ้า `Open()` เป็น blocking
- **แก้:** ใส่ exponential backoff (เช่น 1→2→4→8 วิ)

### F2. 🔴 RabbitMQ: `BasicAck` ก่อนประมวลผล + exception ใน consumer ไม่ recover
- **ไฟล์:** `RabbitMQ/EtcRabbitMQ.cs` (~727–746 `HandleBasicDeliver`)
- **โค้ด:** `_channel.BasicAck(deliveryTag,false);` แล้วค่อยเรียก callback — ถ้า callback throw → message หายไปแล้ว (ack ไปแล้ว) และ consumer อาจตาย
- **ผล:** ข้อความ ETC หาย, consumer หยุดทำงานเงียบๆ
- **แก้:** ประมวลผลให้สำเร็จก่อนแล้วค่อย `BasicAck`; ครอบ callback ด้วย try-catch; auto-restart consumer

### F3. 🟠 LaneSocketServer (RTO) restart โดยไม่มี lock/delay
- **ไฟล์:** `LaneSocket/LaneSocketServer.cs` ~153–172 (`Stop()/Dispose()/new/Start()`)
- **ผล:** หลาย thread เข้า error handler พร้อมกัน → restart ชนกัน; port ยังอยู่ TIME_WAIT → start ใหม่ fail → RTO เงียบ
- **แก้:** lock รอบ restart + หน่วงก่อน start

### F4. 🟡 NVR timeout = Infinite เมื่อ config = 0
- **ไฟล์:** `TCT_NVR.cs` (~50, ~211 `if (iVideoNVR_TimeoutSec <= 0) request.Timeout = Timeout.Infinite;`)
- **ผล:** ถ้า config พลาดเป็น 0 และ NVR ไม่ตอบ → thread บล็อกถาวร
- **แก้:** บังคับ timeout ขั้นต่ำเสมอ

### F5. 🟡 SQLite ไม่เปิด WAL / ไม่มี Busy Timeout (connection เดียว)
- **ไฟล์:** `Database/LaneDB.cs` ~36–39 (verify แล้ว: `URI=file:...` ล้วน)
- **ผล:** เขียนพร้อมกันจากหลาย thread → "database is locked" → ค้าง/throw
- **แก้:** เพิ่ม `Journal Mode=WAL; Busy Timeout=5000;` และ serialize การเขียน

---

## หมวด G — ความปลอดภัย / คุณภาพโค้ด (ไม่ทำให้ดับโดยตรง แต่ควรแก้)

### G1. 🟠 SQL Injection — ต่อ string เข้า query ตรงๆ
- **ไฟล์:** `Database/LaneDB.cs` หลายจุด (93–95, 269–273, 534–535 ฯลฯ) — ค่าจาก network/config (PlazaID, method, alarmDetail) ต่อเข้า SQL โดยมีแค่ escape `'`→`''` บางจุด
- **ผล:** ข้อมูลที่เพี้ยน/ประสงค์ร้ายอาจทำให้ query พังหรือลบข้อมูล → ทางอ้อมทำให้ระบบล่ม
- **แก้:** ใช้ parameterized query (`SqliteParameter`) ทุกที่

### G2. 🟡 ไม่ validate checksum/LRC ของ EDC response
- **ไฟล์:** `EMV/EMV_V1/EdcPaymentDlg_V1.cs` ~635–670 — รับ response มา parse เลย
- **ผล:** ข้อมูลชำระเงินที่ corrupt อาจถูกรับว่าถูกต้อง

### G3. 🟡 ไฟล์ source ซ้ำซ้อนในโฟลเดอร์ `LSD/`
- `LaneSocketControl_LSD (another copy).cs`, `LaneSocketControl_LSD (.5).cs` — เสี่ยง build/แก้ผิดไฟล์ ควรลบ (ตัวจริงคือ `LaneSocketControl_LSD.cs`)

---

## หมวด H — Logic การเก็บเงิน / รายการหาย (ไม่ค้าง ไม่ดับ แต่กระทบเงินและรายงาน)

### H1. 🔴 รถยกเว้นผ่าน OB อย่างเดียว (ไม่มี Loop) แล้วกด Force Reset → รายการหาย ไม่ขึ้น DC และ Sup Adjust
- **พบจากหน้างานจริง:** รถยกเว้นผ่าน OB โดยไม่มี Loop พนักงานกด Force Reset แล้วรายการไม่ขึ้นที่ DC และ Sup Adj
- **ไฟล์:** `LaneController/LaneEventControl.cs`
  - ปุ่มรถยกเว้น CLASS_7 ~1153–1166
  - Passage OB ON → `Process_RefreshPresenceLoop()` ~4732–4743
  - `Process_RefreshPresenceLoop()` ~4280 (ล้าง `iCurTC_Class` เมื่อ `bPaid`)
  - ตั้ง Auto AVC ตอน OB OFF ~5166 (ต้องมีทั้ง `bDetectObBack && bDetectLoopBack`)
  - ตัวจัดการ FORCE_RESET ~2604–2800 (จุดที่แก้ ~2665–2727)
- **ลำดับเหตุการณ์:**
  1. กดรถยกเว้น → `iCurTC_Class=7`, `payType=EXEMPT`, `bAfterPayment=true`, เปิด ALB
  2. Passage OB ON (ไม่มี Loop) → `Process_RefreshPresenceLoop()` **ย้ายรถเข้า `xListLoopCar`** แล้วล้าง `iCurTC_Class=0`, `xCurPayment`, `bAfterPayment=false`
  3. Passage OB OFF → ไม่ตั้ง Auto AVC เพราะ `bDetectLoopBack=false` ถ้า AVC จริงไม่ส่งข้อมูลมา ก็ไม่มีอะไรมาสร้างรายการ รถจึงค้างในคิว
  4. กด Force Reset → เงื่อนไข force end ไม่ผ่านทั้ง 2 ข้อ
     ```csharp
     if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.bAfterPayment))       // false: ถูกล้างไปแล้วในข้อ 2
         Perform_ForceEndTransaction();
     if ((bInPassageLoop && bInPassageOB) || (bTrigDownLoop && bInPassageOB))  // false: ไม่มี Loop
         Perform_ForceEndTransaction();
     ...
     TCT_Lib.xListLoopCar.Clear();   // ← ทิ้งรายการรถยกเว้นที่ค้างอยู่
     ```
     เงื่อนไข `|| (TCT_Lib.xListLoopCar.Count > 0)` ที่ครอบเคสนี้เคยมีอยู่ แต่ถูก comment ทิ้งไว้
- **ผล:** ไม่มีการเรียก `WriteLaneTransaction` จึงไม่มีแถวใน `TCT_DB.db` และไม่มีการเรียก `SendLaneTransObjToDC/RTO/SupAdjust` รายการหายถาวร นับรถไม่ครบ ยอด EOJ ไม่ตรงกับ DC
- **ขอบเขต:** ไม่ได้เกิดเฉพาะรถยกเว้น รถที่จ่ายเงินแล้วทุกประเภท (เงินสด คูปอง รถออกซ้าย ฯลฯ) ที่ผ่าน OB อย่างเดียวโดยไม่มี Loop และไม่มี AVC แล้วกด Force Reset จะหายแบบเดียวกัน
- **แก้แล้ว (2026-10-07):** ในกรณีที่ไม่ใช่ Towing ถ้าเงื่อนไขเดิมไม่ผ่าน ระบบจะวนเช็ค `xListLoopCar`
  - ถ้ารถคันแรกมี class > 0 และ payType > 0 จะเรียก `Perform_ForceEndTransaction()` ทีละคัน (จำกัดรอบไม่เกินจำนวนรถในคิวตอนเริ่ม)
  - ขั้นตอนนี้สร้างรายการจาก `xListLoopCar[0]` ติด SimuPassFlag แล้วส่ง RTO, DC และ Sup Adjust ตามปกติ
  - ถ้า force end รถในคิวแล้ว จะข้ามเงื่อนไข Loop ON/OB ON เพื่อไม่ให้เกิดรายการ violation เปล่าเพิ่ม
  - ยังไม่ได้ build เพราะเครื่องนี้ไม่มี Mono และยังไม่ได้ทดสอบที่ตู้
- **ตรวจ log ของเคสเดิม:** ควรเห็น `Press CLASS 7 (EXEMPT)` → `IO - Passage OB : ON/OFF` (ไม่มี `IO - Passage Loop`) → `Press FORCE_RESET` โดย**ไม่มี** `Write DB Trans` ตามมา
- **log หลังแก้:** `+ Force Reset : Found paid car in ListLoopCar (TC Class=7, ...)` ตามด้วย `Write DB Trans : ... LoopCar=7`
- **ขั้นตอนทดสอบ:** กดรถยกเว้น → OB ON/OFF อย่างเดียว ไม่มี Loop และไม่มี AVC → กด Force Reset → ตรวจว่ามีแถวใน `TCT_DB.db` และขึ้นที่ DC และ Sup Adjust ทดสอบซ้ำด้วยรถเงินสดคลาส 1/2 และเคสปกติ (Loop+OB) ว่าไม่เกิดรายการซ้ำ

### H2. 🟠 OB อย่างเดียวไม่มี Loop → ไม่ตั้ง Auto AVC รายการค้างอยู่ในคิว
- **ไฟล์:** `LaneController/LaneEventControl.cs` ~5166–5172 ตั้ง `Perform_ChkEventAVC` เฉพาะเมื่อ `bDetectObBack && bDetectLoopBack`
- **ผล:** ถ้า Loop เสียหรือรถไม่ทับ Loop และ AVC จริงไม่ส่งข้อมูลมา รถที่จ่ายแล้วจะค้างใน `xListLoopCar` จนกว่ารถคันถัดไปมาดันออก หรือพนักงานกด Force Reset (ก่อนแก้ H1 กด Force Reset แล้วรายการหาย) ลำดับรายการและภาพ NVR อาจเลื่อนไปผูกกับรถคันถัดไป
- **แก้แล้ว (2026-10-07):**
  - เพิ่ม `CanAutoAvcObOnly()` ตรวจว่าเป็นเคส "OB อย่างเดียว" ที่ปลอดภัยพอจะตั้ง Auto AVC ต้องผ่านทุกข้อ:
    - เป็นช่อง MTC ไม่อยู่ในโหมด HPMC และไม่ใช่ Towing
    - ไม่มี violation
    - ไม่มี Loop ทั้งตอนนี้และในรอบนี้ (`bDetectLoopBack`, `bFoundLoop`, `bInPassageLoop` เป็นเท็จ) และ OB ออกไปแล้ว
    - ยังไม่ได้เริ่มรถคันถัดไป (`iCurTC_Class == 0`)
    - รถคันแรกใน `xListLoopCar` มี class และจ่ายเงินแล้ว
  - ตอน Exit OB ถ้า `bFoundOB && CanAutoAvcObOnly()` จะตั้ง timer `Perform_ChkEventAVC_ObOnly` ด้วยเวลาเท่ากับ Auto AVC ปกติ (`iTimerSecAutoAVC` วินาที + 700 ms)
  - ตอน timer ทำงานจะตรวจ `CanAutoAvcObOnly()` อีกรอบ ถ้าสถานะเปลี่ยนแล้ว (เช่น พนักงานเริ่มรถคันถัดไป หรือมีรถเข้า Loop) จะข้ามและเขียน log `- Skip Auto AVC (OB only)` ถ้าไม่เปลี่ยนก็เรียก `Perform_ChkEventAVC()` เพื่อเขียนรายการ
  - ถ้า AVC จริงเข้ามาก่อน หรือพนักงานกด Force Reset ก่อน timer จะถูกยกเลิกด้วย `RemoveAvcTimer()` ที่มีอยู่เดิม จึงไม่เกิดรายการซ้ำ
- **ยืนยันด้วย Simulator (`TCT_Sim/`, 2026-10-07):** version เดิม (467348a) → รถยกเว้นผ่าน OB อย่างเดียว ทั้งรอ Auto AVC และกด Force Reset ได้ **0 รายการ** ที่ DC และ Sup Adjust (ตรงกับที่เจอหน้างาน). version ที่แก้ → ได้ 1 รายการ (class 7, EXEMPT, loop=0, ob=1) ครบทั้ง DC / Sup Adjust / RTO ไม่ซ้ำ
- **ข้อจำกัด:** ถ้าพนักงานเริ่มรถคันถัดไปก่อน timer ทำงาน จะยังไม่เขียนรายการให้อัตโนมัติ เพราะ flow OB-only ของ AVC event จะสั่งปิด ALB ต่อหน้ารถคันที่จ่ายแล้ว กรณีนี้ใช้ Force Reset (H1) หรือ AVC ของรถคันถัดไปเป็นทางสำรอง
- **log:** `+ Passage OB only (No Loop) : Add Auto AVC ...` → `+ Auto AVC (OB only, No Loop)` → `Write DB Trans : ... LoopCar=7`

### H3. 🟡 Force Reset อาจเรียก `Perform_ForceEndTransaction()` ซ้ำ 2 ครั้ง
- **ไฟล์:** `LaneController/LaneEventControl.cs` ส่วน FORCE_RESET
- **เงื่อนไข:** `iCurTC_Class > 0 && bAfterPayment` เป็นจริง และในจังหวะเดียวกันมี Loop ON และ OB ON
- **ผล:** ครั้งแรกเขียนรายการรถปัจจุบัน ครั้งที่สองไม่มีข้อมูลรถ จึงอาจเกิดรายการ violation หรือรายการ class 0 เกินมา
- **แก้แล้ว (2026-10-07):** ใช้ flag `bForceEnded` ซึ่งจะเป็นจริงเมื่อ force end รถปัจจุบันหรือรถในคิว (H1) ไปแล้ว ถ้า flag เป็นจริงจะข้ามการ force end จากเงื่อนไข Loop ON/OB ON และเขียน log `+ Force Reset : Found Loop/OB ON but ForceEnd Transaction already done. Skip second ForceEnd.` ถ้ายังไม่ได้ force end อะไรเลย เช่นรถยังไม่จ่ายแต่ค้างบน Loop+OB จะทำงานแบบเดิม
- **ผลจาก Simulator (2026-10-07):** ยังทำให้เงื่อนไขของ H3 เกิดขึ้นจริงไม่ได้ — ถ้าจ่ายเงินตอนรถอยู่บน Loop แล้ว รถจะถูกย้ายเข้า `xListLoopCar` ทันที (`iCurTC_Class` จึงเป็น 0) ทำให้ version เดิมก็ force end แค่ครั้งเดียวผ่านเงื่อนไข Loop/OB. การแก้ H3 จึงเป็นการป้องกันไว้ก่อน ไม่กระทบผล (ได้ 1 รายการเท่าเดิม)
- **ไม่ได้แตะ Towing:** ใน path Towing ถ้า `bEnableWaitAVC=false` ระบบจะไม่สร้างรายการ จึงต้องพึ่ง Loop/OB เป็นทางสำรองเหมือนเดิม

---

## หมวด I — พบจากการทดสอบด้วย Simulator (`TCT_Sim/`) และแก้แล้วใน v2.0.2–2.0.3

| # | ระดับ | อาการ | สาเหตุ | แก้ใน |
|---|---|---|---|---|
| I1 | 🔴 | Popup QR (EDC V1) → **โปรแกรมดับ** (SIGSEGV ใน `gtk_dialog_run`) เช่น ลูกค้ายังไม่สแกน QR | `EdcPayQrDlg_V1` / `EdcPaymentDlg_V1` เรียก `Respond/Dispose/Destroy` จาก socket thread (ผิดกฎข้อ A/B) | 2.0.2 — `RespondOnGtk()` ผ่าน `Application.Invoke`, MainWindow เป็นคน Dispose |
| I2 | 🟠 | จ่าย QR ไม่สำเร็จ — Popup หยุดรอผลก่อนปุ่มตรวจสอบจะเปิด | Accept timeout 3 วิ แต่ปุ่มตรวจสอบเปิดหลัง 5 วิ | 2.0.2 — รอต่อเมื่อ accept timeout |
| I3 | 🟠 | EDC ออฟไลน์ → Popup QR ปิดไม่ได้เลย | รอคำตอบการยกเลิกจาก EDC ไม่มีกำหนด | 2.0.2 — ปิดทันที หรือภายใน 5 วิ |
| I4 | 🟠 | Popup EMV/QR ไม่รับคีย์บอร์ด | `KeyPressEvent` ไม่ได้ผูก | 2.0.2 |
| I5 | 🟡 | กด ENTER แล้วไปกดปุ่มบนจอที่แตะล่าสุดซ้ำ (เช่น FORCE RESET) | ปุ่มรับ focus + คีย์ที่ map แล้วถูกส่งต่อให้ GTK | 2.0.2 — ปุ่มไม่รับ focus, `RetVal = true` |
| I6 | 🟡 | Popup รถขบวนจ่ายเงิน: หลังจ่าย EMV/QR คีย์บอร์ดใช้ไม่ได้ | `eCurAppPage` ไม่คืนค่า | 2.0.2 |
| I7 | 🟡 | `SetLaneTypeMode` handler สะสมทุกครั้งที่เปิด | ถอด event ผิดตัว | 2.0.2 |
| I8 | 🟠 | โหมดช่าง: เข้าเมนูแล้วจอไม่เปลี่ยน | `ModifyBg` ทุกรอบการวาด + Dispose `PangoRenderer.GetDefault()` (ตัวกลางที่ใช้ร่วมกัน) | 2.0.3 — `SetBgIfChanged`, ไม่ Dispose renderer |
| I9 | 🟡 | Popup ขายคูปอง: เลขคูปองล้นปุ่ม ถูกตัด/ทับกัน | ข้อความยาวเกินความกว้าง + `Justify` | 2.0.3 — บรรทัดเดียว ย่อขนาดให้พอดี |
| I10 | 🟡 | Popup ที่ขยายขนาดตอนข้อความเปลี่ยน → เส้นกรอบเก่าค้าง ขอบขาด | GTK วาดใหม่เฉพาะส่วนที่ขยาย | 2.0.3 — `ReallocateRedraws = true` |

ยังค้าง (ยังไม่ได้ยืนยันที่ช่องจริง): ข้อความสถานะ "กรุณาเปิดไม้กั้นหน้า เพื่อเปิดช่องทาง" อาจค้างหลังรับชำระ

---

## หมวด J — ขายคูปอง (`Sales_CouponDlg` / `LaneDB` / `WebService_SalesCoupons`) และ log รั่วรหัสผ่าน — พบจากการตรวจโค้ด 2026-10-09 แก้ใน v2.0.4

ทดสอบยืนยันใน `TCT_Sim` ด้วยเคส `j_sale_coupon_*` 13 เคส + แผน 1 กะ `SHIFT_TEST_PLAN.md`

| # | ระดับ | อาการ | สาเหตุ (ไฟล์:บรรทัด ก่อนแก้) | แก้ใน 2.0.4 |
|---|---|---|---|---|
| J1 | 🔴 | กด **[EMV]/[QR] ซ้ำ** หลังบัตรอนุมัติแล้ว → **ตัดบัตรซ้ำ** ไม่มีบันทึกของครั้งแรก | `Perform_ProcessEMV/QrCode` ไม่เช็ค `bPayCompleted` (`Sales_CouponDlg.cs:958, :1134`) | `CheckCanStartCardPayment()` ปฏิเสธถ้าจ่ายแล้ว |
| J2 | 🔴 | กด **[แก้ไข]** หลังบัตรอนุมัติแล้ว → popup ปิด **ไม่บันทึกการขาย** ไม่ void บัตร | `Action_BtnCancel` (`:1421`) ไม่เช็ค `bPayCompleted`; `Sensitive=false` กันได้แค่เมาส์ | ปฏิเสธ + แจ้ง "ชำระด้วยบัตรแล้ว กรุณากดยืนยัน/พิมพ์" |
| J3 | 🔴 | รถจ่าย EMV แล้วยังไม่ผ่าน → ขายคูปองด้วย EMV → **EmvDetail ของรถกลายเป็นบัตรของคูปอง** | popup EDC set `TCT_Lib.xCurEMV/xCurEDC/xCurQR = new` ใน ctor (`EdcPaymentDlg_V1.cs:96-100`) แต่รายการรถเขียน EmvDetail ตอนรถผ่าน (`LaneDB.cs:2106`) และ SALE_COUPON อนุญาตหลังจ่าย (`kAllowAfterPayType`) | `RunEdcDialog()` save/restore globals รอบ popup |
| J4 | 🟠 | TA timeout/error ตอนเปิด popup → **คูปองหายทั้งหมด** | `DELETE FROM SalesCouponList` ก่อน HTTP (`WebService_SalesCoupons.cs:71`) | ลบหลัง TA ตอบ OK เท่านั้น |
| J5 | 🟠 | insert หัวรายการขายล้มเหลว (`Execute` คืน -1) แต่คูปองถูกมาร์กขายแล้ว + ยิง TA + พิมพ์ใบเสร็จ | `WriteSalesCouponTransaction` ไม่เช็คผล (`LaneDB.cs:1968`) และ dialog ไม่ดูค่าคืน | คืน 0 ทันที; popup ไม่ปิด แจ้งให้กดยืนยันใหม่ |
| J6 | 🟠 | ขายคูปองตอน TA ช้า → **จอค้าง** (probe + HTTP 2 วิ **ต่อใบ** บน GTK thread) และ TA ล้มเหลวแค่ WARN | `CallWebService_SoldCoupon` ใน loop (`LaneDB.cs:1983, :1991`) | `SendSoldCouponsToTA_Async` background thread, retry 3 ครั้ง |
| J7 | 🟠 | **Escape** บนคีย์บอร์ดปิด popup ผ่าน GTK → handler `manageProcessSalesCoupon` ไม่ถูกถอด → เปิดครั้งถัดไปคีย์ถูกส่งให้ dialog ที่ Dispose แล้ว (เสี่ยงดับ) | `OnKeyPressEvent` ไม่ตั้ง `RetVal=true` (`:229`) ต่างจาก MainWindow (I5) | `RetVal=true` + override `OnDeleteEvent` |
| J8 | 🟠 | popup EMV/QR **ไม่ถูก Dispose** เมื่อผลไม่ใช่ Ok → window/socket ค้างสะสมทุกครั้งที่ปฏิเสธ/ยกเลิก | เส้นทาง else ไม่ Dispose (`:1070, :1118, :1230, :1278`) ขณะ MainWindow ทำทั้ง 2 กรณี | Dispose ใน `finally` ของ `RunEdcDialog()` |
| J9 | 🟡 | `UpdateSoldCoupon` ไม่พบ serial → `dt.Rows[0]` โยน exception → `CouponPK=0` | `LaneDB.cs:2022-2024` | คืน 0 โดยไม่ throw |
| J10 | 🟡 | MessageDialog ซ้อน (TA offline / เกิน 1,500) ยังรับคีย์ CONFIRM/RECEIPT → ปิด dialog แม่ขณะ md ยังรัน | `md.Run()` ขณะ `eCurAppPage` ยัง SalesCoupon | `bMsgOpen` ignore คีย์ |
| J11 | 🟡 | Dispose ซ้ำ 2 ฝั่ง (dialog เอง + MainWindow), Cancel path สลับลำดับ Dispose→Destroy | `:1347, :1428` + `MainWindow.cs:3209` | dialog แค่ `Respond()` ผ่าน `CloseDialog()` |
| J12 | 🟡 | คีย์ `PAY_QRCODE` ส่งเข้า popup แต่ไม่ handle; QR ไม่เช็ค TA; ข้อความ QR บอก "EMV"; `>= 1500` แต่ข้อความ "เกิน" | `kSalesCoupon` มี PAY_QRCODE; `:1134-1190` | handle ตามปุ่มที่มองเห็น; ข้อความ "ตั้งแต่ 1,500" |
| J13 | 🟡 | decimal/DateTime ต่อ SQL ไม่ใช้ InvariantCulture (ปี พ.ศ./ลูกน้ำ ถ้า locale th), `decimal.Parse` ราคาไม่กัน DBNull | `LaneDB.cs:1980, :1988`, `WebService_SalesCoupons.cs:149`, `Sales_CouponDlg.cs:175` | InvC + `TryParse` |
| J14 | 🟡 | `ModifyBg` ใน expose ทุกรอบ (รูปแบบเดียวกับ I8) | `:593, :665, :899-911` | `SetBgIfChanged` |
| J16 | 🟡 | **พบจากทดสอบ GUI ด้วยเมาส์**: คลิกคูปองอีกชนิด (เปลี่ยน header) แล้วช่องของชนิดเดิมไม่ถูกวาดใหม่ → หลัง popup EMV ปิด ข้อความเก่า/ใหม่ซ้อนกัน ("[1]M35-0001") | เปลี่ยน `iSelHeaderCouponType` แล้ว `QueueDraw` เฉพาะ header/ช่องที่กด (`:699-702`, `:825`, `:850`) ส่วน expose ภายหลังเป็น partial region | `RedrawAllCoupons()` ทุกครั้งที่ header เปลี่ยน |
| J15 | 🔴 | **log พิมพ์รหัสผ่าน** กล้อง 3 ตัว, RabbitMQ DC/ETC/MTS, SCP ที่ระดับ INFO ทุกครั้งที่เปิดโปรแกรม (ยืนยันในไฟล์ log ช่องจริง) + SQL ดิบใน error path (มี StaffPassword) + URL กล้องที่ฝัง user:pass + Bridge พิมพ์ Authorization ที่ TRACE | `TCT_Lib.cs:1413-1546`, `MtsRabbitMQ.cs:58`, `MocRabbitMQ.cs:137`, `LaneDB.cs:127,163`, `TCT_NVR.cs:31,100`, `LSD_Bridge.py:148` | `LogInfo.MaskSecret()/MaskUrl()`, `SqlForLog` |

ยังไม่แก้ (ต้องยืนยันกับ TA): `solddate` ส่งเวลาท้องถิ่นแต่ต่อท้าย `Z` (`WebService_SalesCoupons.cs:200`) — ถ้า server ตีความเป็น UTC เวลาขายจะคลาด 7 ชม. · ประเภทคูปองตายตัว '35','80' จาก `SalesCouponType` (คูปองชนิดอื่นที่ TA ส่งมาจะไม่แสดง)

---

## ตารางสรุปจัดลำดับการแก้ (แนะนำ)

| ลำดับ | Bug | หมวด | เหตุผล |
|------|-----|------|--------|
| 1 | B1, B2 | race `xListLoopCar` | crash ระหว่างเก็บเงินจริง — กระทบเงิน |
| 2 | A1–A4 | บล็อก UI thread | จอค้างบ่อยในงานปกติ |
| 3 | E1, E2 | parse input | crash จากอุปกรณ์/network ตอบเพี้ยน |
| 4 | F1, F2 | reconnect/MQ | ระบบเงียบ/ข้อมูลหายเมื่อ network สะดุด |
| 5 | C2, C3 | async void/Abort | thread ตายเงียบ, DB ค้างสถานะ |
| 6 | D1–D4 | resource leak | ค้าง/ดับหลังรันยาว |
| 7 | B3 | GTK cross-thread | crash สุ่ม |
| 8 | F5, G1 | DB lock / injection | "database is locked", data integrity |
| 9 | C1, G3 | dead code/ไฟล์ซ้ำ | สุขอนามัยโค้ด |
| 1* | H1, H2 | รายการหาย / ค้างคิว | พบจริงหน้างาน กระทบยอดเงินและรายงาน DC/Sup Adj |

> **ข้อเสนอแนะภาพรวม:** ปัญหาเชิงระบบ 3 อย่างที่ควรแก้เป็น "หลักการ" ไม่ใช่ทีละจุด:
> 1. **ห้ามบล็อก GTK main thread** — แทน `Thread.Sleep`/sync I/O ด้วย `GLib.Timeout` / `async`
> 2. **ห้ามแตะ GTK จาก background thread** — ครอบ `Application.Invoke` เสมอ
> 3. **ใส่ lock ให้ state ส่วนกลางที่ใช้ร่วมหลาย thread** (โดยเฉพาะ list ใน `TCT_Lib`)
