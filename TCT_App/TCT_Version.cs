using System;

namespace TCT_App
{
    public class TCT_Version
    {
        public TCT_Version()
        {
        }
    }
}

/*======================================================================================

TCT Update Feature (V.0.0.5)
    1. Support keyboard in login page
    2. Change Layout Button follow by real keyboard
    3. Support DMT Card Button (Payment DMT Card)
    4. Support Left Out Button (รถออกซ้าย)
    5. Auto AVC (255) when no response from AVC after BO Off (2 sec.)
    6. Show TFI of HPMC
    7. Support Day time mode / Night time mode of TFI and CRS (Setting time in config)
    8. Running no in receipt.
    9. When press EFS, Becon is ON (No Siren), RTO has alert sound.
    10. Fix bugs.

TCT (V.0.0.6)
    - แก้เรื่องการจ่ายด้วย EMV
    - สามารถส่งค่า JobNo, nTrx ให้กับ RabbitMQ ไป DC ให้ถูกต้องได้แล้ว (JobNo reset=1 ใหม่ถูกๆ 06:00, nTrx reset=1 ใหม่ทุกๆ JobNo)
    - นับจำนวนรถทั้งหมด และส่งค่าให้ RTO แสดง
    - เก็บประวัติการนับรถ ตามประเภท / การจ่ายเงิน / ข้อมูล AVC / รถยกเว้น ในแต่ละ JobNo (EOJ) 
    - Fix Bug and more stable

TCT (V.0.0.7)
    - ส่งข้อมูล BOJ ไปยัง DC / RTO
    - ส่งข้อมูล EOJ ไปยัง DC / RTO
    - ส่งข้อมูล Statistic Data เมื่อ EOJ ไปยัง DC / RTO
    
TCT (V.0.0.9)
    - เปลี่ยนรถออกซ้าย เป็นปุ่มเดียว
    - รองรับรถลากจูง (Towing)
    - รองรับการพิมพ์ใบเสร็จซ้ำ
    - เกิด Violation เมื่อ Loop ON และ OB ON เมื่อไม่มี Class หรือ Payment
    - Fix bug and more stable (Mapping key, Car image lost, Violation missing)

TCT (V.0.0.10)
    - เพิ่มการตรวจสอบสถานะ Network Connection ของ LC / RTO / EDC / DC
    - แก้เรื่องการแสดงผล CRS และ TFI ให้สามารถแสดงรถคันที่ 2 ได้ถูกต้อง
    - ใบเสร็จ เพิ่มชื่อพนักงานขาย
    - รองรับรถขบวนจ่ายเงิน
    - สามารถนับถอยหลังรถขบวนจ่ายเงินได้
    - พิมพ์ใบเสร็จทั้งหมดของรถขบวนจ่ายเงิน
    - รองรับการขายคูปอง
    - เมื่อ EOJ เพิ่มการส่งข้อมูลของ Sales Coupon ไปยัง DC
    - พิมพ์ใบเสร็จของ Sales Coupon
    - พิมพ์ใบเสร็จ การขายคูปองได้
    - คำนวณและแสดง Queue 
    - Fix bug and more stable (Towing, TFI, CRS)
    
TCT (V.0.0.11)
    - เพิ่มการพิมพ์ใบเสร็จของรถลากจูง
    - รถลากจูง สร้าง 2 Transaction ของรถลาก และรถถูกลากได้
    - ส่งข้อมูล Statistic ให้กับ RTO แบบ Real Time
    - แก้ไข UI ของ Sales Coupon ให้ทำงานได้ถูกต้อง
    - Mapping Keyboard ของ Sales Coupon
    - Mapping Keyboard ของ Print Receipt Convoy
    
TCT (V.0.0.12)
    - เพิ่มการทำงานสามารถกดรถคันถัดไปได้ (เมื่อคันแรกยังไม่ออกจาก LOOP)
    - แก้เรื่องการจ่ายเงินด้วย EMV ของรถลากจูง 2 คัน (สร้าง 2 Transaction)
    - แก้เรื่องการเปิด Dialog มากกว่า 1 หน้าต่าง
    - แก้เรื่องให้ขายคูปอง เลือกลูกศรซ้ายขวา (เพิ่มกด . สำหรับเปลี่ยนชนิดคูปอง)
    - ขยายหน้า Login ให้ Full Screen (การ Exit โปรแกรมให้กดปุ่ม Force Reset)
    
TCT (V.0.0.13)
    - สามารถตรวจสอบ RTO Connection ได้แล้ว
    - รองรับ Staff, Tariff, PlazaConfiguration จาก RabbitMQ เข้ามาในระบบได้
    - เพิ่มการรองรับ Message LaneState, CompilePassage จากระบบ ETC
    - ส่ง Transaction ไปยังระบบ SCW
    - รองรับการตรวจสอบการ Login ด้วย User / Password ของจริง (MD5 Password)

TCT (V.0.0.13)
    - Support RTO Connection
    - Support Staff, Tariff, PlazaConfiguration from RabbitMQ
    - Support LaneState, CompilePassage from ETC system.
    - Send Transaction messaage to SCW
    - Support login with user / passsword from real staff data
    
TCT (V.0.0.14)
    - Maintenance Mode
    - Support Read TAG of ETC from RabbitMQ
    - Send Login, MaunalPayment of  ManualAction to RabbitMQ ETC

TCT (V.0.1.15)
    - Support Auto Resend LaneTransaction, LaneEvent Data to RabbitMQ of DC.
    - Send ALB_Mode ManualAction to RabbitMQ ETC
    
TCT (V.0.2.2)
    - Fix bug send ALB open when Dual Mode 
    - Disable Key pad when ETC mode
    - Support LSD feature
    
TCT (V.0.2.20)
    - Fix ETC mode (Normal, HPMC)
    - Support change Lane mode from RTO
    - Auto Login in ETC mode when send command from RTO
    - Separent ETC RabbitMQ Log and LSD Log file.

TCT (V.0.2.32)
    - Support control Lane mode from ETC
    - Support ETC Reverse direction

TCT (V.0.2.35)
    - Fix bug when ETC Mode

TCT (V.0.2.38)
    - Support in case no Passage Loop/OB, only have AVC, then TCT will end transaction and alert IO error
    - Support ETC RabbitMQ new 4 methods (SetLaneMode, ClearQueue, EnableSecondaryReader, ControlOutputDevice)
    - Fix to set payment type when end transaction.
    
TCT (V.0.2.59)
    - SetLaneMode to MAINTENANCE when auto maintenance mode from RTO
    - SetLaneMode to CLOSE when change LaneType to ETC
    - Fix bug set payment type when end transaction.    
    
TCT (V.0.2.70)
    - Fix bug ETC SetLaneMode Method.
    - Add SetLaneModeType button in Login page.     

TCT (V.0.2.84)
    - Support EMV QR Payment
    - Show TFI in Maintenance Mode

TCT (V.0.2.89)
    - Fix bug EMV Payment
    - Fix bug duplicate generate auto create transaction.
    - Change UI screen when vehicle Passage Loop ON. (Original clear UI when recv AVC)
    - Add structure database column Trans_SignalCode and LC_SignalStatus in LaneTransaction Table for Support Transaction Signal Code of MTC

TCT (V.0.2.90)
    - Fix bug EMV QR Code Payment
    - Fix bug Reverse and send MTCPassagePayment
    - Fix bug print receipt convoy
    - Show next car when first car touch passage loop and disappear (Not wait AVC of first car)

TCT (V.0.2.92)
    - Fix bug Violation when found only AVC signal
    - Fix bug when press EDIT many times.

TCT (V.0.3.02)
    - Performance improvements
    - Fix bug ALB of HPMC mode
    - Fix bug print receipts
    - Add ALB not close when toll has 2 cars continue

TCT (V.0.3.03)
    - Fix when vehicle touch loop and no class type or payment, TCT will wait collector input class or payment (No auto ckear screen)
    - Auto create transaction when Towing cannot detect AVC of second vehicle and press force reset. 

TCT (V.0.3.06)
    - Add LaneEvent log of each event
    - Fix bug ETC mode
    - Performance improvement (Log data)

TCT (V.0.3.07)
    - Add more information of ETC Mode
    - Fix bug ETC mode

TCT (V.0.3.08)
    - Fix bug of count summary vehicles in ETC mode

TCT (V.0.3.09)
    - Fix bug create jobNo
    - Fix bug SetLaneMode ETC when RTO send Close command
    - Add enable/disable option of SupAdjust
    - Adjust passage date time of transaction when ETC mode
    - Add log of CompiledPassage in EventLog

TCT (V.0.3.14)
    - Fix bug Towing (Not send ALB Close)
    - Add MTC Passage SignalCode Json in CompiledPassage Message
    - Add Pay with QR Code of Convoy
    - Add Pay with QR Code of Sales Coupon

TCT (V.0.3.15)
    - Change LTL close when vehicle touch passage loop
    - Change CRS, TFI will show information of second vehicle when first vehicle touch passage loop
    - Change convoy information. show each class type of convoy.

TCT (V.0.3.20)
    - Fix bug violation second vehicle of Towing.
    - Fix bug auto reconnect RTO and resend lane transaction
    - Modify menu of Maintenance Mode
    - Add config of DSRC_Antenna

TCT (V.0.3.22)
    - Add feature Maintenance mode
    - Add feature monitoring and checking failure equipment
    - Fix bug convoy mode (Other vehicle into convoy)
    - Fix bug HPMC when finish HPMC

TCT (V.0.3.25)
    - Fix open/close ALB command from RTO in maintenance mode.
    - Fix bug open/close OTL in maintenance mode.
    - Auto create JobNo of Maintenance mode (Separent from normal mode)
    - Send BOJ, EOJ in maintenance mode to SCW
    - Ignore send discrepancy class message to SCW when class type is Exempt and Left Exit 
    - Send LCIO, LCSD real time state to RTO
    - Fix bug of HPMC, when no loop (Only OB), after get AVC, ALB will close.
    - Fix bug ETC history, not show lane transaction.

TCT (V.0.3.26)
    - Auto zip log file
    - Auto end force transaction (Found loop and press Force Reset)
    - Auto resend message to SupAdjust
    - Send BOJ/EOJ message to SupAdjust
    - DUAL mode, when edit payment, then merge ReClass 3 message to 1 Json message

TCT (V.0.3.32)
    - Fix bug Dual Mode when EDIT pay type from TAG to Manual
    - Fix bug Re-class json message
    - Fix HPMC in Maintenance Mode
    - Fix clear TFI when EDIT in Dual Lane Mode
    - Send supervisor to BIS when login
    - Fix send BOJ message to SupAdjust
    - Fix volation, alert sound not stop
    
TCT (V.0.3.36)
    - Fix Json Message to ETC RabbitMQ when Towing Payment in Dual Mode
    - Change state of LSD when Open OTL and Close OTL (Original action when login and end shift)
    - Add show method type of login mode in top panel (Manual Key, Smart Card, RTO Remote, Auto ETC, Maintenance) 

TCT (V.0.3.37)
    - Auto disable Enable ETC Rabbit MQ when set pure MTC Mode
    - Auto clear old coupons data when receive new coupons from web service.

TCT (V.0.3.38)
    - Clear all sales coupons list in database before get new list coupons
    - Move LSD log from LogTrans to LsdLog
    
TCT (V.0.3.40)
    - Fix Towing in Dual mode, Send ALB Normal for Close ALB
    - Fix Auto-Resend SupAdjust
    
TCT (V.0.3.41)
    - Set ClassDiscrepancy Flag = 0 when Class Violation
    - Set both RePayment Flag = 1 when Change payment of Towing
    
TCT (V.0.3.42)
    - Set SimuFlag = 1 when Force End Transaction (Force Reset for end transaction)
    - No save transaction and no send SupAdjust when Lane is in Closed mode
    
TCT (V.0.3.43)
    - Send EMV, QR Code information when sales coupon.
    - Send EMV, QR Code information when convoy mode.
    - Support payment EMV, QR Code information for second vehicle of Towing.
    - Support payment EMV, QR Code information for convoy mode.

TCT (V.0.3.44)
    - Modify remark Signal Code ETC 01 -> Re-Payment
    - No send violation when in ETC mode
    - Remove tag info when payment cash (Not M-Pass, Easy Pass)

TCT (V.0.3.45)
    - Fix bug of violation in MTC mode not show at RTO.
    - Auto close ALB when 1 vehicle pass when send Open ALB from RTO.
    
TCT (V.0.3.47)
    - Add check device status and send to DC
    
TCT (V.0.3.49)
    - Change JobNo=999, nTrx=0 when vehicle pass in Lane Close (Violation)
    - Add Treadle Status Test in Maintenance Mode
    - Add ALB Open/Close in Digital Input Test in Maintenance Mode

TCT (V.0.3.52)
    - Fix bug Auto AVC
    - ETC mode fiilter key Open/Close OTL, End Job, HPMC
    - Auto Send SignalCode = 81 into database for inform Open ALB from RTO to SCW
    - Add check Treadle status in Maintenance mode.

TCT (V.0.3.55)
    - Fix bug show text summary of convoy pyament
    - Fix bug auto create JobNo
    - Send Ref1, Ref2 of EDC to DC
    - Separent EMV and QR variable
    - Auto re-connect RabbitMQ when lost connection
    
TCT (V.0.4.01)
    - Add confiig setting of KeepDatabaseDay (Default=90 days)
    - Add config setting of Provider (Default=10)
    
TCT (V.0.4.02)
    - Add feature capture image when payment and OB on
    - Add config setting of VideoNVR

TCT (V.0.4.03)
    - Fix bug of capture image (Ignore thread abort)

TCT (V.0.4.04)
    - Move capture image 3,4 from OB On to Loop On
    - Add capture image when vehicle is exmpt and left out 

TCT (V.0.4.06)
    - Fix bug Video Capture 

TCT (V.0.4.10)
    - Add capture image of ETC and DUAL mode

TCT (V.0.4.11)
    - Fix bug violation when human pass only OB
    - Fix bug Violation when payment and has violation
    - Fix bug display price of Convoy

== 01/06/2021 Release       TCT (V.1.0.0)

TCT (V.1.0.1)
    - Separate thread for send data to RTO

TCT (V.1.0.2)
    - Fix bug Press 'EDIT' and not close LTL
    - Fix bug classDis when TC_Class=AVC Class
    - Add more log information (LCSD Connection)
    - Fix bug close CRS Night off when EOJ
    
TCT (V.1.0.4)
    - Fix bug has violation when system has OB and AVC 
    - Fix bug OB double time (No AVC)
    - Fix bug Cash after Loop ON (ClassType -> Loop ON -> Cash) 
    - Clear and close CRS when Start shift/End shift
    
TCT (V.1.0.6)
    - Fix bug date time of staff login
    - Fix bug of edit towing payment
    - Fix bug avc class=9
    
TCT (V.1.0.6.2)
    - Add feature of ALB Queue Management
    
TCT (V.1.0.6.3)
    - Fix encoding EDC Qr Message from EDC
    - Add ref1, ref2 string to EDC

TCT (V.1.0.6.4)     02/07/2021
    - Fix bug add queue to 2 1/2 vehicle queues
    - Fix bug save HPMC mode when normal vehicle in loop
    - Fix bug set class dis when violation and TC class is not same avc class

TCT (V.1.0.6.5)     03/07/2021
    - Fix bug capture image when violation and loop on
    
TCT (V.1.0.6.6)     19/07/2021
    - Fix bug capture image when edit Exempt or Left exit (image shift 1 nTrx)
    - Fix bug reverse HPMC and End HPMC, then no clear data in loop event
    - Fix bug Ref1, Ref2 of EDC QR Payment and add TID (Terminal ID) Setting
    - Fix bug Save database and Send to DC of Ref1, Ref2 EDC.
    - Fix bug Login Smart Card Maintenance Staff
    - Add close button in Login page for hide manual login panel
    - Add logic of check start job TOD before BOJ of TCT
    
TCT (V.1.0.6.7)
    - Fix bug not allow EOJ when found vehicle is not end transaction (Vehicle in loop)
    - Fix bug HPMC send ClassDis when End HPMC before receive AVC
    - Add MTS monitor data and send to MTS Rabbit MQ
    - Fix bug try...catch when WebSocket cannot connect to server
    
TCT (V.1.0.7)
    - Add feature check open shift TOD before BOJ toll.
    - Fix bug close CRS when exit maintenance mode.
    - Fix bug re-send lane transaction when re-connect DC RabbitMQ

TCT (V.1.0.7.1)     2021-08-28
    - Fix bug press ForceReset of Login Page then close program
    - Allow BOJ job when TA Server network fail.
    - Close CRS when Lane Mode is close lane
    
TCT (V.1.0.7.2)     2021-09-01
    - Check DC connection before send data to DC
    - Add more log ASB, Siren IO ON/OFF
    - Turn off LTL, ASB, Siren when EOJ
    - Protect duplicate read smart card. (Fix duplicate create job no)
    - Change take camera from WebRequeat to HttpWebRequest
    - Change take camera function from static/share method to dynamic method
    
TCT (V.1.0.7.3)     2021-10-01
    - Protect error from test CRS, TFI, LSD in maintenance mode
    - Capture image camera from direct camera (Method=2)
    - Send image json data to web service
    - Separent thread for LSD task process
    - Fix camera ignore abort.
    - Add config setting of TowingTimeLapseMilliSec for VideoNVR section
    
TCT (V.1.0.7.4)     2021-11-04
    - Improve thread for capture camera
    - Add config timeout of capture camera
    - Add json value of OBU to MTS
    - Add LSD keep alive time setting
    - Add sleep thread for re-send data to RTO
    - Add reconnect RTO when lost connection
    - Fix bug slow process when lost connection
    
TCT (V.1.0.7.5)     2021-11-12
    - Fix bug Press Edit, ALB will off when only payment complete.
    - Fix bug Towing when first car on loop and press towing process.
    - Fix bug LSD connnection module from synchronous to async task process.
    - Fix bug DC Connection delay
    - Fix bug Invalid supervisor in BIS
    - Fix bug capture camera case timeout

TCT (V.1.0.7.6)     2021-11-22
    - Send current lane mode to ETC when start program
    - Fix bug press 'Edit' button many times 
    - Fix bug capture image (case exempt sometime not capture)
    - Separate resend transaction to SupAdjust (Request from RTO)
    - Add feature capture and save image when confirm payment into local storage and send images to File Server by calling Web Service
    - Modify JSON (add 3 fields for 3 images file name) send to RTO and Sup Adjust
    - Fix bug alert violation when BOJ
    - Fix bug send command to LSD
    
TCT (V.1.0.7.7)     2021-11-29
    - Fix bug 'EDIT' process
    - Add press 'EDIT' or 'ESC' in Convoy Process for exit windows
    - Fix bug Reconnect LSD
    - Fix bug Violation when BOJ

TCT (V.1.0.7.8) 
    - Add auto resend capture image to DC 
    - Fix bug violation in maintenance mode when start simulation
    - Auto create image capture folder
    
TCT (V.1.1.0.0) 
    - Add auto move image to date folder when success send data to server
    - Auto change LSD follow by OTL change status.
    - Fix bug ETC mode - Clear screen when finish transaction
    - Fix bug Key Stroke - protect repeat key of HPMC and EDIT
    - Fix bug capture image camera when found violation
    
TCT (V.1.1.1)
    - Fix bug capture image clear image of Edit process
    - Auto delete old capture image files.

TCT (V.1.1.2)       2022-01-04
    - Fix bug protect ALB close from RTO send open ALB in HPMC mode
    - Fix bug login with Samrt Card - Duplicate JobNo
    - Add feature alert to MTS and RTO when OB Fail (OB ON - not OFF when Loop ON/OFF)

TCT (V.1.1.3)       2022-01-18
    - Fix bug LSD (Set KeepAlive=0)
    - Fix bug Key Stroke - protect repeat key of Exempt
    - Send alert MTS when TCT cannot send command to LSD (When KeepAlive=0)
    - Fix bug send Open/Close ALB from RTO
    
TCT (V.1.1.4)       
    - Fix bug CompiledPassage, and LaneState for ETC
    - Fix bug Capture Image Camera - Contraint violation
    - Add feature remote LogOff from RTO
    - Add feature, Auto send  command Close ALB when EOJ
    - Add feature, Check interval timer  (sec.) of Fail Loop No OB
    - Add feature, Set retry times and timer wait retry of LSD re-connect.
    - Add feature, Check SCR connection from LCSD (SYS method bit 9 in json message)
    
TCT (V.1.1.5)
    - Block display message from ETC when in MTC of Dual Mode
    - Send maintenance mode to ETC when in MTC of Dual Mode
    - Dispose request when error send to MTS
    - Clear and backup NVR image file
    - Change TFI message to normal vehicle when cancel QR Payment
    - Send MTC Mode to ETC when Dual-MTC of Maintenance Mode
    - Send Close Lane command to LSD when start Maintenance mode
    
TCT (V.1.1.6)
    - Add feature reading ETC status and send to MTS
    
TCT (V.1.1.7)
    - Adjust text in receipt
    - Add feature Auto zip file of ETC and LSD log file
    
TCT (V.1.1.8)
    - Fix bug print receipt amount=0 when press EDIT
    - Add feature Key Stroke - protect repeat key of PrintReceipt
    - Add feature Separent thread for get ETC status from URL
    
TCT (V.1.1.9)
    - Fix enable/disable inquiry qr payment button in QR Payment
    - Add config EdcQrWaitTimeSec (Default 10 sec)
    - Add get TcpState in MTS
    
TCT (V.1.2.0)
    - Add feature - Capture Image method = 3 (Take image when found loop and OB Event IO)
    - Fix LSD - Reconnect initial connection (Max 3 times, no forever loop)
    - More log information for HPMC Mode and QR Payment

TCT (V.1.2.1)
    - Add feature - Send SumReport (Settlement) to EDC when start job
    - Fix EDC - Error unknown / no ack from EDC
    - Fix MTS - try catch when get ETC staus from Html
    
TCT (V.1.2.2)
    - Fix EDC - Sum Report (Settlement) hang when login
    - Fix EDC - Inquiry QR code hang (Port already to used)
        
TCT (V.1.2.3)
    - Add feature - Check response message and validate payment type (EMV or QR)
    - Add feature - Save Ref1, Ref2  of Violation transaction into DB, when user press QR and no inquiry response message.
    
TCT (V.1.2.4)
    - Add timeout for clear resource of EDC SumReport (Settlement)
    - Add check EDC TransCode of SumReport (Settlement), EMV Payment and QR Payment
    
TCT (V.1.2.5)
    - Fix ETC - When supervisor change, then ETC will auto end shift and auto start new shift with new supervisor
    - Fix ETC - When user change pay type from MPass/EasyPass to Cash and Vehicle touch loop in background process.
    - Fix ETC - When restart TCT, then auto login, system will not skip send LaneMode Close to ETC (Send only LaneMode Normal)
    - Add feature - ETC skip write log lane state setting (Default = Not write lane state log)
    
TCT (V.1.2.6)
    - Fix ETC -  When change supervisor, then send new staff to BIS
    - Fix bug - When login multiple smart card, then staff lost
    - Fix UI - When loop queue is null, then TCT stop.
    
TCT (V.1.2.7)
    - Add feature -  Auto Backup MTS Log Folder (Monthly)
    - Add feature - Send alarm when change lane type 
    - Add feature - Send alarm when Ref#2 of EMV payment is not match
    - Fix bug - Change Lane Type from RTO 
    - Fix EMV - When vehicle#2 pay emv success before vehicle #1 (Same pay emv) out of lane
            (before vehicle#1 create transaction, - emv detail is clear)

TCT (V.1.2.8)
    - Add feature - Send MTS Alarm to DC (Clear Queue,  EDC Ref2 not match)
    - Fix EDC - SumReport (Clear Batch) change TransCode S1 to 50 and process when EOJ
    
TCT (V.1.3.0)
    - Add feature - Capture camera method=3, Image#1=Rear camer when Loop ON, Image#2=Rear camera when OB ON, Image#3=Front camera when OB ON
    - Fix EDC - When cancel EMV and object not set

TCT (V.1.3.1)
    - Add feature - Add Chief Toll Collector can Remote Log-off (StaffGroup=40)
    - Add feature - Create EDC log in LaneLog (Separent from LanLog)
    - Add feature - Add minute interval setting for Clear Batch of EDC
    - Fix bug - Send image file name to SupAdjust when Image Capture Method = 3
    - Fix EDC - Cancel payment and program hang.
    - Fix EDC - Protect call SumReport (ClearBatch) more than 1 time,
    - Fix Bug - Add Key Stroke - protect repeat press key of EndShift and BEOJ
    - Fix Bug - Get Capture Image when capture time out (Add key word Date for search image file name)
    - Fix Bug - Exempt capture image is missing, when 2nd vehicle EDIT and change to Exempt, then index of image wrong

TCT (V.1.3.2)
    - Change Digital Input Signal between Presence Loop and Presence OB
    
TCT (V.1.3.3)
    - Fix EDC - When EDC not send Ack, TCT will not process and wait (Time and Retry is setting)
    - Add feature - Add setting resize process of Video NVR
    - Add feature - Add setting waiting time and retry time of EDC no ack
    - Add feature - Add setting DI (Digital Input) signal
    - Add feature - Print QR Code at Footer Receipt

TCT (V.1.3.4)
    - Fix EDC - Catch when cancel EDC
    - Add feature - Check capture file and delete file before move file to folder.
    - Fix EDC - Fix generate nTrx of  Ref1 when  car not out of OB

TCT (V.1.3.5)
    - Add feature - Add manual config folder and read config (.json file) when EOJ
    - Add feature - Set device status (Loop/OB) from LCSD or real signal data (Web Socket)
    - Add feature - Print QR Code of customer url at footer of receipt.
    - Add feature - Ignore EDIT payment when pay EMV Success

 TCT (V.1.3.6)
    - Fix bug : Check EMV re-mapping to last EMV detai  when current transaction cannot map EMV detail
    - Add feature : Create separent thread for print receipt.
    
 TCT (V.1.3.7)
    - Add feature : Add new 4 payment buttons in UI (PM6-9)
    - Print convoy, Print receipt is not close windows (User can reprint unit press Confirm button)
    - Fix EDC - Fix object not reference and miss match transaction

TCT (V.1.3.8)
    - Fix Towing - Print 2 receipts together when payment of towing
    - Fix Convoy - When found exempt vehicle is between convoy
    
TCT (V.1.4.0)
    - Add feature - Pay with DMT Card will check permission from DB (Json data send from SCW)
    - Add feature - Add config for adjust QR Code Print Size (Default=5)
    - Fix Sales Coupon - Not allow sales coupon when TA Server Network is fail
    
TCT (V.1.4.1)   2022-11-19
    - Fix HPMC - Force open ALB in ETC Mode (Not check current status in Class stage)
    - Fix MTS - Use time stamp of staff from DB 
    - Add MTS - Add time stamp of DMT Card from DB for send to MTS
    - Add DMT Card - Add effective date setting for enable DMT Card feature
    - Add MTS - Add Promotion Version send to MTS
    - Change - Remove footer and QR Code of Sales Coupon Receipt
    
TCT (V.1.4.2)   2023-05-22
    - Add TFI - Recv TFI json and send SCP to TFI server
    - Fix TARIFF - Add effective date time to change new tariff
    - Fix Print Receipt - Fix print xxxx-xxxxxxxxxxxx of first print (Clear variable receipt no list when start BOJ)
    - Fix Print Receipt - Protect duplicate press key 2 second. (In main process and maintenance)
    - Fix Print Receipt - Call direct print (No thread print)
    - Add Zip Log - Add EDC Zip Log File
    - Fix Payment - Fix PM6-PM9 when disable PaymentBtn, Program will not process when press at keyboard.
    - Fix EMV - After found AutoAVC, Program will clear EMV variable data.
    - Fix bug - When TCT hang, then restart TCT will auto generate EOJ file and send to SCW
    - Add SalesCoupon - Change ordering of coupon list (From horizontal ordering to vertical ordering)
    - Add SalesCoupon - Auto get coupon type and coupon price to sales coupon
    - Add feature - Add Key Stroke for protect repeat press key of Force Reset
    - Change Auto EOJ -  Change End Shift DateTime to last transacton time +5 sec. 
                        or if no transaction, End shift will be Start shift + 5 sec.
    - Add EDC - Add new EMV function for new EDC machine
    - Add EMV - Add test EDC of new machine in Maintenance Mode
    - Change - Change color in Sales Coupon Mode.
    - Add feature - Print QR Code BuzzeBee, change url to https://www.tollway.co.th in Maintenance mode
    - Change Sales Coupon - Change list of coupons ordering by SerialNo

TCT (V.1.4.3)   2023-05-31
    - Add feature - Add Key Stroke for protect repeat press key of Class 1, Class 2, Cash, Voucher
    - Add feature - Separate user key at keyboard or touch at screen in log file.
    - Add feature - Add table SalesCouponType in database for separate coupon type.
    
TCT (V.1.4.4)   2023-06-08
    - Add feature - Add more information in EDC Log
    
TCT (V.1.4.5)   2023-06-21
    - Fix bug EDC - When TCT is in EDC Payment, then EDC restart, TCT will always try and exception.
    - Fix bug EDC - After send ack to EDC, then TCT will delay 1.5 sec before disconnect socket.
    - Fix bug MTS - Add try...catch exception of MTS function. (GetResourceInfo, GetDiskInfo, GetUsageDiskPath)
    - Add feature - If found Passage Loop ON and OB ON or Trig Loop (Loop ON & OFF) and OB ON, 
                    then User cannot End of JOB, User must Force Reset.
    - Add feature - When print receipt found Error Interrupt system call, then TCT will auto cut paper and re-print receipt again.
    - Add feature - EDC change test network EDC connection open port to recachable network.
    
TCT (V.1.4.6)   2023-07-24
    - Fix bug New EDC - Dispose/Close Recv Port when EDC is busy of Handskaing Mode
    - Add feature EDC - Add Interval time for check network EDC
    - Add feature import - Add interval time for import staff
    
TCT (V.1.4.7)   2023-09-19
    - Add feature - Protect block edit when pay EMV in Towing Mode
    - Add feature - Auto end transaction Towing when press force reset after payment second vechile (Towing)
    - Add feature - Capture image when RTO send command => open ALB
    - Fix bug Import Staff - Fix fail import staff and cannot move to backup file, Delete old file before create new staff log.
    - Fix bug EDC - Fix EDC hang and close program of Old EDC (EDC Type=0)
    - Add feature - Compress and zip CaptureVideo Log File
    - Add feature - Add check role group id for allow to login program (Toll Collector and Maintenance role)
    - Fix bug LC - Add check pong from another message (Not only pong message)
    
TCT (V.1.4.8)   2023-09-28
    - Fix bug - Fix read config allow role group
    - Fix bug - Fix write config \n\t will ignor character.
    - Fix bug - ETC Mode when End shift will backup zip file.
    - Fix bug - Add more log web socket LCSD
    

TCT (V.1.4.9)   2024-01-24
    - Fix bug - AutoResend BOJ/EOJ, Ignore send current Staff Shift.

    [หมายเหตุ: changelog ระหว่าง 1.5.0 - 1.5.7 ไม่ได้ถูกบันทึกไว้ในไฟล์นี้]

TCT (V.2.0.0)   2026-06-02   *** Stability / Concurrency Hardening Release ***
    -------------------------------------------------------------------------
    รวมการแก้ไขบั๊กที่ทำให้ระบบ "ค้าง" หรือ "ดับ" (อ้างอิงรายงานใน Bug.md)
    -------------------------------------------------------------------------
    [Logging]
    - Fix resource leak - ปิด StreamWriter ด้วย using ทุกเมธอด (กัน file handle รั่ว)
    - Fix thread-safety - เพิ่ม global lock กัน log ชนกัน/บรรทัดเพี้ยนจากหลาย thread
    [Database - LaneDB]
    - เปิด SQLite WAL mode + Busy Timeout (กัน "database is locked")
    - List()/Execute() ครอบ using + try-catch (กัน command leak และ crash จาก DB error)
    - ใช้ Interlocked สำหรับตัวนับ EventID/TransID (กัน ID ซ้ำจาก race)
    [State - TCT_Lib]
    - เพิ่ม lock object + safe accessor สำหรับ xListLoopCar / xListQueues
    - แก้ race condition การเข้าถึง list ส่วนกลางจากหลาย thread (กัน IndexOutOfRange)
    [LaneEventControl]
    - ลบ/แก้ ThreadEndTransaction stub (while(true) ว่างเปล่า)
    - แทน Thread.Sleep บน UI thread ด้วย GLib.Timeout (กันจอค้าง)
    - เพิ่ม guard เช็ค Count ก่อนเข้าถึง index [0]
    [EMV / EDC Payment]
    - เช็คความยาวก่อน Substring บน EDC response (กัน crash จาก response เพี้ยน)
    - ใส่ try/finally ปิด socket + ตั้ง timeout ทุก socket
    - เปลี่ยน bCancel เป็น volatile
    [Networking]
    - WebSocket reconnect แบบ exponential backoff (กัน reconnect storm)
    - RabbitMQ: ครอบ try-catch ใน consumer + dispose connection/channel + null-guard
    - NVR: บังคับ timeout ขั้นต่ำเสมอ (กัน thread บล็อกถาวร)
    [อื่นๆ]
    - Bump version 1.5.7 -> 2.0.0, AssemblyVersion 0.0.2 -> 2.0.0
    - ลบไฟล์ source ซ้ำในโฟลเดอร์ LSD/

TCT (V.2.0.1)   2026-10-07
    - Fix bug (H1) - รถยกเว้น/รถที่จ่ายเงินแล้ว ผ่าน OB อย่างเดียว (ไม่มี Loop) แล้วกด Force Reset
                     รายการหาย ไม่ขึ้น DC และ Sup Adjust -> Force Reset จะ Force End รถที่จ่ายแล้วใน ListLoopCar ก่อนล้างคิว
    - Fix bug (H2) - ผ่าน OB อย่างเดียว (ไม่มี Loop) ไม่มีการตั้ง Auto AVC -> ตั้ง Auto AVC ให้เมื่อมีรถจ่ายแล้วค้างในคิว
                     (ข้ามถ้าพนักงานเริ่มรถคันถัดไปแล้ว เพื่อไม่ปิดไม้กั้นหน้ารถคันถัดไป)
    - Fix bug (H3) - Force Reset ไม่เรียก ForceEnd ซ้ำ 2 ครั้ง (กันรายการ Violation/Class 0 เกิน)
    - Bump version 2.0.0 -> 2.0.1, AssemblyVersion 2.0.0 -> 2.0.1

TCT (V.2.0.2)   2026-10-07
    [Keyboard - ใช้คีย์บอร์ด Cherry ได้ครบขึ้น รวม Popup]
    - Fix bug - Popup EMV / QR Code (EDC V1) ไม่รับคีย์บอร์ด (KeyPressEvent ไม่ได้ผูก) -> [แก้ไข] / [CANCEL] ยกเลิกได้
    - Fix bug - หน้าหลักกด ENTER แล้วไปกดปุ่มบนจอที่ถูกแตะล่าสุดซ้ำ (เช่น FORCE RESET) -> ปุ่มไม่รับ focus, คีย์ที่ map แล้วไม่ส่งต่อให้ GTK
    - Fix bug - Popup รถขบวนจ่ายเงิน: หลังจ่าย EMV/QR แล้วคีย์บอร์ดใช้ไม่ได้ (eCurAppPage ไม่คืนค่า)
    - Fix bug - SetLaneTypeMode ถอด event ผิดตัว (handler ค้างสะสมทุกครั้งที่เปิด)
    - Add - [CANCEL] ยกเลิก Popup EMV / QR / บัตร DMT ได้, [ENTER] ตรวจสอบการจ่าย QR ได้
    - Add - map ปุ่มจริง BackSpace / End / Escape / Tab / Keypad Enter
    [EDC V1 - พบจากการทดสอบกับ EDC จำลอง]
    - Fix crash - Popup QR/EMV ปิดตัวเอง (Respond/Dispose/Destroy) จาก socket thread -> โปรแกรมดับ (SIGSEGV ใน gtk_dialog_run)
                  เช่น ลูกค้ายังไม่สแกน QR (code 12) -> ย้ายไปทำบน GTK thread, MainWindow เป็นคน Dispose
    - Fix bug - Popup QR หยุดรอผลจาก EDC หลัง 3 วินาที (Accept timeout) แต่ปุ่มตรวจสอบเปิดหลัง 5 วินาที -> จ่าย QR ไม่สำเร็จ
    - Fix bug - EDC ออฟไลน์ / ไม่ตอบการยกเลิก -> Popup QR ปิดไม่ได้เลย -> ปิดทันที หรือภายใน 5 วินาที
    - Fix bug - แถบสถานะแสดง "EDC : Disconnect" หลัง Login นานถึง EdcCheckKeepAliveIntervalMin -> แสดงผลตรวจล่าสุดทันที
    [GUI - Header]
    - จัดหัวจอเป็น 3 กลุ่ม (ด่าน/ช่อง | พนักงาน | กะ) ชิดซ้าย, นาฬิกาไม่ถูกตัด, เวลาเริ่มงานไม่ล้นกรอบ, แก้คำ "Maunal" -> "Manual"
    [GUI - ปรับสีและตัวอักษร (ตำแหน่งปุ่มเหมือนเดิม)]
    - สีปุ่มบนจอ = สีปุ่มเดียวกันบนคีย์บอร์ด Cherry, ตัวอักษรหนาอ่านง่าย, ปุ่มที่ใช้ไม่ได้เป็นสีเทา
    - ไม่มีกรอบ focus ประบนปุ่ม, ปุ่ม "รถขบวนจ่ายเงิน" แสดง 2 บรรทัด
    - Popup ทุกตัว (EMV/QR/DMT/ขายคูปอง/รถขบวนจ่ายเงิน/เลือกประเภทช่อง/โหมดช่าง/MessageDialog) ไม่มีแถบหัวหน้าต่าง
    - Bump version 2.0.1 -> 2.0.2


TCT (V.2.0.3)   2026-10-07
    [GUI - ตรวจทุกหน้าจอ: ข้อความทับกัน / เส้น / ความเหมือนกันทุกหน้า]
    - Fix bug - โหมดช่าง (MainMaintenance) เข้าเมนูแล้วจอไม่เปลี่ยน/ไม่วาดใหม่ (ModifyBg ทุกรอบ + Dispose PangoRenderer ตัวกลาง)
    - Fix bug - Popup ขายคูปอง: เลขคูปอง "[1] SIM35-0001" ยาวเกินปุ่ม -> ขึ้นบรรทัดสองถูกตัด และ "[ 1 ]" ถูกยืด -> บรรทัดเดียว ย่อขนาดให้พอดี จัดกลาง
    - หัวจอ Login / หน้าหลัก / โหมดช่าง ใช้มาตรฐานเดียวกัน (ข้อความ ฟอนต์ สี ความกว้างคอลัมน์ นาฬิกา วันที่ พ.ศ.)
    - Popup ทุกตัวมีกรอบเส้นสีอ่อน 3 px มองเห็นขอบชัดบนพื้นจอเข้ม
    - ปุ่มใน Popup EMV / QR / บัตร DMT / ขายคูปอง ใช้สีเดียวกับคีย์ Cherry (ยกเลิก=แดง, ตรวจสอบ QR=เขียวน้ำทะเล, ยืนยัน=เขียว, พิมพ์=ม่วง) ตัวอักษรหนา 14
    - กรอบทุกหน้าเป็นแบบ "การ์ด" ไม่มีเส้น (เดิมกรอบ GTK เป็นเส้นคู่ วางติดกันเห็น 4 เส้น เช่น ข้างนาฬิกา / ใต้หัวจอ)
    - มุมโค้งมน: ปุ่มทุกปุ่ม การ์ด และ Popup (วาดด้วย GDK ไม่ต้องลง theme engine เพิ่มในเครื่องช่อง)
    - Popup ทุกตัว: ปุ่มตกลง/ยืนยัน = เขียว, ยกเลิก = แดง, พิมพ์ = ม่วง, ข้อความ OK/Cancel -> ตกลง/ยกเลิก
    - Fix bug - Popup ขยายขนาดตอนข้อความเปลี่ยน -> เส้นกรอบเก่าค้าง / ขอบขวาขาด (วาดใหม่ทั้งบาน)
    - Popup QR: ปุ่ม "ตรวจสอบการจ่าย [ENTER]" ข้อความไม่ถูกตัด
    - หน้า Login: แป้นตัวเลข / ปุ่ม LOGIN / ยกเลิก เป็นธีมเดียวกัน, ช่องข้อมูลพนักงานที่ยังว่างแสดง "—", ปุ่ม ENTER ที่ถูกตัด -> "ตกลง"
    - Popup รถขบวนจ่ายเงิน: แป้นตัวเลขธีมเดียวกับหน้า Login
    - Popup เลือกชนิดช่อง / โหมดช่าง: ปุ่มเลือกสีฟ้า, Open/Close ALB สีฟ้า (ไม่ปนกับสีตกลง/ยกเลิก)
    - แถบสถานะล่าง: มีเส้นคั่น 1 เส้นระหว่างทุกช่อง (เดิมมีกล่องว่าง "[ ]", เส้นคู่ "||" และบางช่องไม่มีเส้น)
    [ภาพกล้อง (VideoNVR)]
    - ย่อภาพรายการผ่านทางเป็น 1280x720 (เดิม 1024x576) แบบคุณภาพสูง (HighQualityBicubic) รักษาสัดส่วน ไม่ขยายภาพเล็ก
    - บันทึก JPEG คุณภาพ 85 (เดิมค่า default ~75) -> ไฟล์ ~60 KB/ภาพ (เดิม ~35 KB)
    - ตั้งค่าได้: VideoNVR.ResizeWidth / ResizeHeight / JpegQuality, ResizeImage=0 = ไม่ย่อ (ขนาดเต็มจากกล้อง)
    - Bump version 2.0.2 -> 2.0.3


TCT (V.2.0.4)   2026-10-09
    [ขายคูปอง (Sales_CouponDlg) - แก้บั๊กจากการตรวจโค้ด + ทดสอบใน TCT_Sim ครบทุกเส้นทาง]
    - Fix bug - กด [EMV]/[QR] ซ้ำหลังบัตรอนุมัติแล้ว -> ตัดบัตรซ้ำ (Perform_ProcessEMV/QrCode ไม่เช็ค bPayCompleted)
    - Fix bug - กด [แก้ไข] หลังบัตรอนุมัติแล้ว -> popup ปิดโดยไม่บันทึกการขาย (บัตรถูกตัดแล้ว) -> ปฏิเสธ + แจ้งให้กดยืนยัน/พิมพ์
    - Fix bug - EMV/QR ของคูปองไปทับ TCT_Lib.xCurEMV/xCurEDC/xCurQR ของรถคันปัจจุบันที่จ่ายแล้วยังไม่ผ่าน
                  -> EmvDetail ของรถกลายเป็นบัตรของคูปอง -> save/restore รอบ popup EDC (RunEdcDialog)
    - Fix bug - popup EMV/QR ไม่ถูก Dispose เมื่อผลไม่ใช่ Ok (ปฏิเสธ/ยกเลิก) -> window + socket ค้างสะสม
    - Fix bug - Escape บนคีย์บอร์ดปิด popup ผ่าน GTK (delete-event) โดยไม่ถอด handler -> เปิดครั้งถัดไปคีย์ถูกส่งให้ dialog ที่ Dispose แล้ว
                  -> OnKeyPressEvent ตั้ง RetVal=true + override OnDeleteEvent
    - Fix bug - WriteSalesCouponTransaction: insert หัวรายการล้มเหลวแต่ยังมาร์กคูปองว่าขายแล้ว/ยิง TA/พิมพ์ใบเสร็จ
                  -> เช็คผล insert ก่อน, popup ไม่ปิดถ้าบันทึกไม่สำเร็จ (กดยืนยันซ้ำได้)
    - Fix bug - CallWebService_GetCoupons ลบ SalesCouponList ก่อนเรียก TA -> TA timeout/error แล้วคูปองหายทั้งหมด -> ลบหลัง TA ตอบ OK
    - Fix bug - ยิง TA SoldCoupon บน GTK thread ทีละใบ (probe + HTTP 2 วิ) -> จอค้างเมื่อ TA ช้า -> ย้ายไป background thread + retry 3 ครั้ง
    - Fix bug - UpdateSoldCoupon ไม่พบ serial -> IndexOutOfRange; decimal/DateTime ต่อ SQL ไม่ใช้ InvariantCulture; ราคาอ่านด้วย TryParse
    - Fix bug - MessageDialog ซ้อน (TA offline / เกิน 1,500) ยังรับคีย์ CONFIRM/RECEIPT -> ignore คีย์ขณะ message เปิด
    - Fix bug - คีย์ PAY_QRCODE ถูกส่งเข้า popup แต่ไม่ได้ handle; QR ไม่เช็ค TA Server; ข้อความ QR บอก "EMV"; "เกิน 1,500" -> "ตั้งแต่ 1,500"
    - Fix bug - popup Destroy/Dispose ตัวเอง + MainWindow Dispose ซ้ำ -> popup แค่ Respond(), MainWindow เป็นคน Dispose
    - ModifyBg ใน expose ทุกรอบ -> SetBgIfChanged (แบบเดียวกับ I8)
    - Fix bug (พบจากทดสอบ GUI ด้วยเมาส์) - คลิกคูปองอีกชนิด (header เปลี่ยน) ช่องชนิดเดิมไม่ถูกวาดใหม่ -> ข้อความซ้อนกันหลัง popup EMV ปิด
                  -> RedrawAllCoupons() ทุกครั้งที่ header เปลี่ยน
    [Log - ไม่พิมพ์รหัสผ่าน/credential]
    - TCT_Lib config dump: กล้อง 3 ตัว, DC/ETC/MTS RabbitMQ, TFI SCP password -> แสดง "***** (len N)" (LogInfo.MaskSecret)
    - MtsRabbitMQ / MocRabbitMQ ตอน connect ไม่พิมพ์ password; LaneDB error path ใช้ SqlForLog (ซ่อน StaffPassword/ResponseCardNo)
    - URL กล้อง/NVR ที่ฝัง user:pass@ -> ซ่อน (LogInfo.MaskUrl); LSD_Bridge ไม่พิมพ์ค่า Authorization header ที่ระดับ TRACE
    [TCT_Sim]
    - เพิ่มเคสคีย์บอร์ด j_sale_coupon_* 11 เคส (พิมพ์/หลายใบ/ไม่เลือก/EMV/EMV ซ้ำ/ยกเลิกหลังอนุมัติ/ปฏิเสธ/รถรอผ่าน/Escape/TA error/TA ช้า)
    - เพิ่มโหมด TA จำลอง (ok/error/slow), แผนทดสอบ 1 กะ SHIFT_TEST_PLAN.md + shift_test.py (./sim.sh shift)
    - Bump version 2.0.3 -> 2.0.4, AssemblyVersion 2.0.3 -> 2.0.4


TCT (V.2.0.5)   2026-10-15
    [ลบข้อมูลเก่าในฐานข้อมูล (TCT_Lib.ClearDatabaseFile) - ลบเฉพาะข้อมูลที่ส่ง DC แล้ว]
    - ข้อมูลที่ต้องส่ง DC ลบได้เมื่อ "ส่ง DC สำเร็จแล้ว" และเก่ากว่า Config.KeepDatabaseDay วัน (ค่าเริ่มต้น 90 -> 30)
        LaneTransaction      : SendStatusDC=1
        LaneEventHQ          : SendHqStatus=1   (ส่ง DC ผ่าน MOC RabbitMQ)
        LaneAlarmRTO         : SendStatusDC=1 ถ้าเป็นอุปกรณ์ใน MTS_RabbitMQ.AlarmDeviceList (อุปกรณ์อื่นไม่ได้ส่ง DC -> ลบตามวันที่)
        StaffShift           : BojSendStatusDC=1 และ EojSendStatusDC=1 และปิดกะแล้ว (นับวันจาก StaffEndShiftDateTime) ไม่ใช่กะปัจจุบัน
    - ยังไม่ส่ง / ส่งไม่สำเร็จ (status=0) -> ไม่ลบ ไม่ว่าจะเก่าแค่ไหน
    - ข้อมูลลูกลบตามรายการแม่: EmvDetail / DMTCardTrans ลบเมื่อไม่มี LaneTransaction (และ SalesCouponTransaction) อ้างถึงแล้ว,
      ImageCaptureCamera ลบเมื่อ LaneTransaction ถูกลบแล้ว, TransactionStatistic ลบเมื่อ StaffShift ถูกลบแล้ว
    - SalesCouponTransaction / SalesCouponDetail ไม่ลบแล้ว (ไม่ได้ส่ง DC - SendStatusDC ไม่เคยถูกตั้งเป็น 1)
    - ข้อมูลในเครื่องที่ไม่ได้ส่ง DC (LaneEvent, LaneEventRTO, SupervisorShift, TransactionSummary, SalesCouponList) ลบตามวันที่เหมือนเดิม
    - Fix bug - เดิมลบทุกแถวที่เก่ากว่า 90 วันโดยไม่ดูสถานะส่ง -> รายการที่ DC ยังไม่ได้รับถูกลบทิ้ง
    - Fix bug - เดิม "DELETE FROM LaneEventHQ WHERE EventType IN (4..9)" ลบทุกครั้งที่เปิดโปรแกรม ไม่ดูวันที่/สถานะส่ง DC -> เอาออก
    - Fix bug - ID ถัดไป = MAX(ID)+1 แต่เดิมลบได้จนหมดตาราง -> ID เริ่ม 1 ใหม่ ซ้ำกับที่ DC มีแล้ว
                  -> เก็บแถวล่าสุดของทุกตารางไว้เสมอ (AND ID < MAX(ID)) + ไม่ลบกะ/หัวหน้ากะปัจจุบัน
    - Fix bug - TransactionStatistic ลบ StaffShiftID < MIN+1 -> สถิติของกะเก่าสุดที่ยังอยู่ถูกลบไปด้วย
    - ลบเป็นชุดละ 2,000 แถว (DELETE ... WHERE rowid IN (SELECT ... LIMIT)) -> ไม่ถือ DB lock นาน
    - เดิมลบเฉพาะตอนเปิดโปรแกรม -> เพิ่มลบวันละครั้งใน ThreadAutoResendTrans (หลัง resend, ไม่ VACUUM, พัก 200 ms ระหว่างชุด)
    - กันรันซ้อน (Interlocked), log จำนวนแถวที่ลบต่อตาราง (INFO), SQL (DEBUG)
    - Bump version 2.0.4 -> 2.0.5, AssemblyVersion 2.0.4 -> 2.0.5


=============================================================================================*/
