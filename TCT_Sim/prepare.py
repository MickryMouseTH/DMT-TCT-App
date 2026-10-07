#!/usr/bin/env python3
"""
เตรียม runtime สำหรับจำลอง TCT (ไม่แตะไฟล์จริงใน TCT_App/bin)

- copy TCT_App/bin/Debug  ->  TCT_Sim/runtime/app (exe + dll), runtime/config (TCT.config + TCT_DB.db)
- แก้ TCT.config ใน runtime ให้ชี้ไปที่ service จำลองใน docker compose
- เพิ่มพนักงานทดสอบใน TCT_DB.db ของ runtime

รัน:  python3 TCT_Sim/prepare.py            (ใช้ bin/Debug)
      python3 TCT_Sim/prepare.py Release    (ใช้ bin/Release)
"""
import hashlib
import json
import os
import shutil
import sqlite3
import sys
from pathlib import Path

SIM_DIR     = Path(__file__).resolve().parent
REPO_DIR    = SIM_DIR.parent
CONFIG_NAME = sys.argv[1] if len(sys.argv) > 1 else "Debug"
# TCT_BIN=<โฟลเดอร์ที่มี TCT_App.exe> ใช้ทดสอบ exe ตัวอื่น (เช่น version ก่อนแก้)
SRC_DIR     = Path(os.environ["TCT_BIN"]) if os.environ.get("TCT_BIN") else REPO_DIR / "TCT_App" / "bin" / CONFIG_NAME
RUN_DIR     = SIM_DIR / "runtime"
APP_DIR     = RUN_DIR / "app"
CFG_DIR     = RUN_DIR / "config"

SIM_STAFF_ID       = "99001"
SIM_STAFF_PASSWORD = "123456"
SIM_STAFF_GROUP    = 20          # Toll Collector
SIM_MT_STAFF_ID    = "99050"
SIM_MT_GROUP       = 50          # Maintenance Admin

# IP คงที่ของ docker network (docker-compose.yml) — TCT เทียบ IP ของ Sup Adjust/RTO แบบตรงตัว และ resolve hostname ช้า
SIM_IP   = "172.28.0.10"   # lcsd-sim (LCSD + Sup Adjust + RTO)
RABBIT_IP = "172.28.0.20"  # rabbitmq (DC)

# ค่าที่ override ใน TCT.config  (section -> {key: value})
OVERRIDES = {
    "Lane":   {"LaneId": 1, "LaneName": "SIM01", "LaneConfigType": 0, "LaneType": 0, "LaneDirection": 0},
    "Plaza":  {"PlazaId": "016"},
    "LCSD":   {"SocketIPAddress": SIM_IP, "SocketPort": 58084,
               # ใช้ index ตาม Cmd_DI_Type (เหมือน bin/Debug/TCT.config)
               "DI_0": 0, "DI_1": 1, "DI_2": 4, "DI_3": 3, "DI_4": 2, "DI_5": 5, "DI_6": 6, "DI_7": 7,
               "DI_8": 8, "DI_9": 9, "DI_10": 10, "DI_11": 11, "DI_12": 12, "DI_13": 13, "DI_14": 14, "DI_15": 15},
    "LCIO":   {"SocketIPAddress": SIM_IP, "SocketPort": 18084},
    "RTO":    {"IPAddress": SIM_IP, "SocketPort": 9094},
    "SupAdjust": {"Enable": 1, "IPAddress": SIM_IP, "SocketPort": 9095},
    # EDC จำลองอยู่ใน lcsd-sim (TCT -> 2019, EDC -> TCT:2018) โปรโตคอล V1
    "EMV":    {"Enable": 1, "EdcIPAddress": SIM_IP, "EdcSendPort": 2019, "EdcRecvPort": 2018,
               "EdcTID": "12345678", "EdcVersionType": 1, "EdcCheckNetworkType": 1,
               "EdcCheckKeepAliveIntervalMin": 1, "EdcStartHourClearBatch": 0, "EdcEndHourClearBatch": 0},
    "Printer": {"PrinterPort": "/tmp/printer.out"},
    "LSD":    {"Enable": 0},
    "DSRC_Antenna": {"Enable": 0},
    # TA Web Service จำลองอยู่ใน lcsd-sim port 3000 (คูปอง / LaneAttendance / ภาพ)
    "LaneWebService": {"Enable": 1, "WebServiceBaseUrl": f"http://{SIM_IP}:3000",
                       "ServerIPAddress": SIM_IP, "ServerPort": 3000, "CheckStartJobTOD": 0},
    # กล้องจำลองอยู่ใน lcsd-sim port 3000: GET /cam/{1,2,3}.jpg (1920x1080) , POST /saveImages (รับภาพ + โชว์ที่ Panel)
    # Method 1 = ถ่ายตอนรับชำระ, ThirdCaptureUrl ต้องไม่ว่าง (TCT เช็คค่านี้ก่อนถ่ายภาพที่ 3)
    "VideoNVR": {"Enable": 1, "CaptureImageMethod": 1, "WebApiBaseUrl": f"http://{SIM_IP}:3000/saveImages",
                 "ThirdCaptureUrl": "sim", "Image3TimeLapseMilliSec": 500, "TimeoutSec": 5,
                 "ResizeImage": 1, "ResizeWidth": 1280, "ResizeHeight": 720, "JpegQuality": 85,
                 "AutoKeepDayImageFile": 1},
    "VideoLaneCapture": {"CaptureImagePath": "/tct/log/Capture",
                         "FrontCameraUrl": f"http://{SIM_IP}:3000/cam/1.jpg",
                         "RearCameraUrl":  f"http://{SIM_IP}:3000/cam/2.jpg",
                         "ThirdCameraUrl": f"http://{SIM_IP}:3000/cam/3.jpg"},
    "DC_RabbitMQ": {"Enable": 1, "ServerHostName": RABBIT_IP, "VirtualHost": "/", "Port": 5672,
                    "User": "guest", "Password": "guest",
                    "ServerIPAddress": RABBIT_IP, "ServerPort": 5672},
    "ETC_RabbitMQ": {"Enable": 0},
    "MTS_RabbitMQ": {"Enable": 0},
    "TFI":    {"Enable": 0},
    "Config": {"ManualImportPath": "/tct/Manual-Import", "LogPath": ""},
}


def copy_runtime():
    # โครงเหมือนเครื่องจริง (build แบบ __FULL_SCREEN__): <root>/app/TCT_App.exe , <root>/config/TCT.config , <root>/log
    if not (SRC_DIR / "TCT_App.exe").exists():
        sys.exit(f"ไม่พบ {SRC_DIR/'TCT_App.exe'} — build ก่อน")
    # ล้างเฉพาะข้างใน runtime/ (ไม่ลบตัวโฟลเดอร์) — container ที่ mount ไว้จะยังเห็นไฟล์ใหม่
    RUN_DIR.mkdir(exist_ok=True)
    for child in RUN_DIR.iterdir():
        shutil.rmtree(child) if child.is_dir() else child.unlink()
    for d in ("app", "config", "log", "Manual-Import"):
        (RUN_DIR / d).mkdir(parents=True)
    for f in SRC_DIR.iterdir():
        if f.is_file() and ".bak" not in f.name and "copy" not in f.name \
                and f.suffix not in (".config", ".db"):
            shutil.copy2(f, APP_DIR / f.name)
    # config/DB: ใช้ของ bin/<cfg> ถ้ามี ไม่งั้นใช้ของ Debug
    for name in ("TCT.config", "TCT_DB.db"):
        src = SRC_DIR / name if (SRC_DIR / name).exists() else REPO_DIR / "TCT_App" / "bin" / "Debug" / name
        shutil.copy2(src, CFG_DIR / name)
    for f in RUN_DIR.rglob("*"):
        f.chmod(0o777 if f.is_dir() else 0o666)


def patch_config():
    p = CFG_DIR / "TCT.config"
    cfg = json.loads(p.read_text(encoding="utf-8-sig"))
    for sec, kv in OVERRIDES.items():
        cfg.setdefault(sec, {}).update(kv)
    p.write_text(json.dumps(cfg, indent=2, ensure_ascii=False), encoding="utf-8")


def add_staff():
    con = sqlite3.connect(CFG_DIR / "TCT_DB.db")
    md5 = hashlib.md5(SIM_STAFF_PASSWORD.encode()).hexdigest()
    now = "2026-01-01 00:00:00"
    for sid, grp, name in ((SIM_STAFF_ID, SIM_STAFF_GROUP, "SIM Toll Collector"),
                           (SIM_MT_STAFF_ID, SIM_MT_GROUP, "SIM Maintenance")):
        con.execute("DELETE FROM Staff WHERE StaffID=?", (sid,))
        con.execute("INSERT INTO Staff (StaffID, StaffGroupID, StaffCode, StaffName, StaffPassword, CardSerialNo, "
                    "StaffTimeStamp, UpdateDateTime, Activate, Deleted) VALUES (?,?,?,?,?,?,?,?,1,0)",
                    (sid, grp, sid, name, md5, sid, now, now))
    con.commit()
    con.close()


if __name__ == "__main__":
    copy_runtime()
    patch_config()
    add_staff()
    print(f"+ runtime พร้อม: {RUN_DIR}  (จาก {SRC_DIR})")
    print(f"+ Login พนักงานเก็บเงิน : {SIM_STAFF_ID} / {SIM_STAFF_PASSWORD}")
    print(f"+ Login ช่าง (Maintenance): {SIM_MT_STAFF_ID} / {SIM_STAFF_PASSWORD}")
