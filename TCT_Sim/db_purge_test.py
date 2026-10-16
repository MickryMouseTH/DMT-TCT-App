"""ทดสอบการลบข้อมูลเก่าใน DB (TCT_Lib.ClearDatabaseFile, v2.0.5) — รันผ่าน  ./sim.sh dbpurge
   seed  : ใส่แถวทดสอบ (ส่ง DC แล้ว/ยังไม่ส่ง, เก่า 45 วัน/ใหม่ 10 วัน, แถวล่าสุด, ข้อมูลลูก) + ตั้ง KeepDatabaseDay=30
   verify: ตรวจว่าแถวไหนต้องถูกลบ/ต้องเหลือ หลัง TCT เปิดโปรแกรม (ClearDatabaseFile ตอนเปิด)
   usage : python3 db_purge_test.py seed|verify runtime/config"""
import sqlite3, sys, json, datetime as dt, pathlib
mode, cfg_dir = sys.argv[1], pathlib.Path(sys.argv[2])
db = cfg_dir / "TCT_DB.db"
OLD = (dt.date.today() - dt.timedelta(days=45)).isoformat() + " 10:00:00.000"
NEW = (dt.date.today() - dt.timedelta(days=10)).isoformat() + " 10:00:00.000"
# (table, idcol, id, expect_kept, {cols})
CASES = [
 ("LaneTransaction","TransactionID",900001,False,dict(TransactionDateTime=OLD,SendStatusDC=1)),
 ("LaneTransaction","TransactionID",900002,True ,dict(TransactionDateTime=OLD,SendStatusDC=0,SendRetryDC=3)),
 ("LaneTransaction","TransactionID",900003,True ,dict(TransactionDateTime=NEW,SendStatusDC=1)),
 ("LaneTransaction","TransactionID",900004,False,dict(TransactionDateTime=OLD,SendStatusDC=1,EMV_DetailID=900004,DmtCardTransID=900004)),
 ("LaneTransaction","TransactionID",900005,True ,dict(TransactionDateTime=OLD,SendStatusDC=0,EMV_DetailID=900005,DmtCardTransID=900005)),
 ("LaneTransaction","TransactionID",999999,True ,dict(TransactionDateTime=OLD,SendStatusDC=1)),          # newest row = next ID
 ("EmvDetail","EmvDetailID",900004,False,dict(UpdateDate=OLD)),
 ("EmvDetail","EmvDetailID",900005,True ,dict(UpdateDate=OLD)),                                           # trans not sent
 ("EmvDetail","EmvDetailID",900006,False,dict(UpdateDate=OLD)),                                           # orphan
 ("EmvDetail","EmvDetailID",900007,True ,dict(UpdateDate=OLD)),                                           # used by coupon sale
 ("EmvDetail","EmvDetailID",999999,True ,dict(UpdateDate=OLD)),
 ("DMTCardTrans","DmtCardTransID",900004,False,dict(UpdateDateTime=OLD)),
 ("DMTCardTrans","DmtCardTransID",900005,True ,dict(UpdateDateTime=OLD)),
 ("DMTCardTrans","DmtCardTransID",999999,True ,dict(UpdateDateTime=OLD)),
 ("ImageCaptureCamera","ImageCaptureID",900001,False,dict(UpdateDateTime=OLD,LaneTransactionID=900001)),
 ("ImageCaptureCamera","ImageCaptureID",900002,True ,dict(UpdateDateTime=OLD,LaneTransactionID=900002)),
 ("ImageCaptureCamera","ImageCaptureID",999999,True ,dict(UpdateDateTime=OLD,LaneTransactionID=900001)),
 ("LaneEventHQ","EventHqID",900001,False,dict(EventDateTime=OLD,SendHqStatus=1,EventType=1)),
 ("LaneEventHQ","EventHqID",900002,True ,dict(EventDateTime=OLD,SendHqStatus=0,EventType=1)),
 ("LaneEventHQ","EventHqID",900003,False,dict(EventDateTime=OLD,SendHqStatus=1,EventType=5)),
 ("LaneEventHQ","EventHqID",900004,True ,dict(EventDateTime=NEW,SendHqStatus=0,EventType=5)),            # old code deleted type 4-9 always
 ("LaneEventHQ","EventHqID",999999,True ,dict(EventDateTime=OLD,SendHqStatus=1,EventType=1)),
 ("LaneAlarmRTO","AlarmRtoID",900001,False,dict(AlarmDateTime=OLD,AlarmDevice=5,SendStatusDC=1)),
 ("LaneAlarmRTO","AlarmRtoID",900002,True ,dict(AlarmDateTime=OLD,AlarmDevice=5,SendStatusDC=0)),        # MTS device, not sent
 ("LaneAlarmRTO","AlarmRtoID",900003,False,dict(AlarmDateTime=OLD,AlarmDevice=8,SendStatusDC=0)),        # not a DC device
 ("LaneAlarmRTO","AlarmRtoID",999999,True ,dict(AlarmDateTime=OLD,AlarmDevice=8,SendStatusDC=1)),
 ("StaffShift","StaffShiftID",900001,False,dict(StaffStartShiftDateTime=OLD,StaffEndShiftDateTime=OLD,BojSendStatusDC=1,EojSendStatusDC=1)),
 ("StaffShift","StaffShiftID",900002,True ,dict(StaffStartShiftDateTime=OLD,StaffEndShiftDateTime=OLD,BojSendStatusDC=1,EojSendStatusDC=0)),
 ("StaffShift","StaffShiftID",900003,True ,dict(StaffStartShiftDateTime=OLD,BojSendStatusDC=1,EojSendStatusDC=1)),
 ("StaffShift","StaffShiftID",999999,True ,dict(StaffStartShiftDateTime=OLD,StaffEndShiftDateTime=OLD,BojSendStatusDC=1,EojSendStatusDC=1)),
 ("TransactionStatistic","StaffShiftID",900001,False,{}),
 ("TransactionStatistic","StaffShiftID",900002,True ,{}),
 ("SalesCouponTransaction","TransactionID",900001,True,dict(TransactionDateTime=OLD,SendStatusDC=0,EmvDetailID=900007)),
 ("SupervisorShift","SupervisorShiftID",900001,False,dict(StartShiftDateTime=OLD,EndShiftDateTime=OLD)),
 ("SupervisorShift","SupervisorShiftID",999999,True ,dict(StartShiftDateTime=OLD,EndShiftDateTime=OLD)),
 ("LaneEvent","EventID",900001,False,dict(EventDateTime=OLD)),
 ("LaneEvent","EventID",999999,True ,dict(EventDateTime=OLD)),
 ("LaneEventRTO","EventRtoID",900001,False,dict(EventDateTime=OLD,DeviceType=1)),
 ("LaneEventRTO","EventRtoID",999999,True ,dict(EventDateTime=OLD,DeviceType=1)),
]
con = sqlite3.connect(db)
if mode == "seed":
    p = cfg_dir / "TCT.config"
    cfg = json.loads(p.read_text(encoding="utf-8-sig")); cfg["Config"]["KeepDatabaseDay"] = 30
    p.write_text(json.dumps(cfg, indent=2, ensure_ascii=False), encoding="utf-8")
    for t, idc, i, _, cols in CASES:
        c = {idc: i, **cols}
        con.execute(f"INSERT INTO {t} ({','.join(c)}) VALUES ({','.join('?'*len(c))})", list(c.values()))
    con.commit(); print(f"seeded {len(CASES)} rows, OLD={OLD}, NEW={NEW}, KeepDatabaseDay=30")
else:
    bad = 0
    for t, idc, i, keep, _ in CASES:
        n = con.execute(f"SELECT COUNT(*) FROM {t} WHERE {idc}=?", (i,)).fetchone()[0]
        ok = (n == 1) == keep
        bad += not ok
        print(f"{'PASS' if ok else 'FAIL'}  {t:24} {i:7}  expect {'KEEP' if keep else 'DELETE':6} -> {'kept' if n else 'deleted'}")
    print("RESULT:", "ALL PASS" if bad == 0 else f"{bad} FAIL"); sys.exit(1 if bad else 0)
