"""
ColorRecDemo.py  (Program: ColorRecDemo)
========================================
ตัวจำลอง "ฝั่ง LSD Controller ใหม่" — เปิด HTTP server รับคำสั่งที่ LSD_Bridge แปลงมาแล้ว
(ที่ TCT_App -> AVON -> LSD_Bridge -> HTTP PUT .../{vsn}/activated) แล้วแสดง "สีป้าย" ที่ถูกสั่ง

    [ NovaDemo/TCT ] --AVON--> [ LSD_Bridge ] --HTTP PUT .../{vsn}/activated--> [ ColorRecDemo (ไฟล์นี้) ]

ความสามารถ:
  - ตั้งค่า Listen Host:Port ใน GUI (default 0.0.0.0:8080)
  - รับ PUT/POST/GET .../vsns/{vsn}/activated | /deactivated แล้วถอดชื่อ {vsn}
  - แสดงป้ายจำลองเป็นกล่องสีใหญ่ตามโหมด (CLOSE/MTC/ETC/ETC-MTC) — map สี/ข้อความแก้ได้ใน config
  - ตอบ HTTP 200 (หรือ status ที่ตั้งค่า) กลับให้ Bridge
  - log ทุก request

GUI = PySide6/PyQt (ผ่าน qt_compat) ; log ผ่าน LogLibrary.py
"""

import os
import json
import threading
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse

from qt_compat import QtCore, QtGui, QtWidgets, Signal, AlignCenter
from LogLibrary import Load_Config, Loguru_Logging, script_dir

# ----------------------- Configuration Values -----------------------
Program_Name = "ColorRecDemo"
Program_Version = "1.0.0"
# --------------------------------------------------------------------

default_config = {
    # --- logging (ใช้โดย LogLibrary) ---
    "log_Level": "DEBUG",
    "Log_Console": 1,
    "log_Backup": 90,
    "Log_Size": "10 MB",

    # --- HTTP server ---
    "Listen_Host": "0.0.0.0",
    "Listen_Port": 8080,          # ตั้งให้ตรงกับ LSD_New_BaseURL ของ Bridge (เช่น http://<ip>:8080)
    "Response_Status": 200,

    # --- map ชื่อ .vsn -> สี/ข้อความบนป้ายจำลอง ---
    "Vsn_Color_Map": {
        "CLOSE.vsn":   {"label": "ปิดช่อง\nCLOSE",  "bg": "#c62828", "fg": "#ffffff"},
        "MTC.vsn":     {"label": "เงินสด\nMTC",     "bg": "#f9a825", "fg": "#000000"},
        "ETC.vsn":     {"label": "ETC",             "bg": "#1565c0", "fg": "#ffffff"},
        "ETC-MTC.vsn": {"label": "ETC + MTC",       "bg": "#2e7d32", "fg": "#ffffff"},
    },
}

config = Load_Config(default_config, Program_Name)
logger = Loguru_Logging(config, Program_Name, Program_Version)


def save_config():
    path = os.path.join(script_dir, f"{Program_Name}_config.json")
    try:
        with open(path, "w", encoding="utf-8") as f:
            json.dump(config, f, indent=4)
    except OSError as exc:
        logger.warning("บันทึก config ไม่สำเร็จ: {}", exc)


# ===========================================================================
# HTTP handler / server
# ===========================================================================
def _parse_target(path):
    """ถอด (vsn, action) จาก path เช่น /api/vsns/sources/lan/vsns/MTC.vsn/activated"""
    clean = urlparse(path).path.strip("/")
    parts = [p for p in clean.split("/") if p]
    if len(parts) >= 2:
        return parts[-2], parts[-1].lower()   # (vsn, action)
    if parts:
        return parts[-1], ""
    return "", ""


class _Handler(BaseHTTPRequestHandler):
    protocol_version = "HTTP/1.1"

    def _handle(self, method):
        vsn, action = _parse_target(self.path)
        length = int(self.headers.get("Content-Length", 0) or 0)
        if length:
            try:
                self.rfile.read(length)   # อ่าน body ทิ้ง (ไม่ให้ค้าง keep-alive)
            except OSError:
                pass

        emitter = self.server.emitter
        peer = self.client_address[0]
        emitter.sig_log.emit(
            "REQ", f"{method} {self.path}  จาก {peer}  -> vsn={vsn or '?'} action={action or '?'}")
        logger.info("{} {} from {} -> vsn={} action={}", method, self.path, peer, vsn, action)

        if action == "activated" or (action == "" and vsn):
            emitter.sig_display.emit(vsn, True)
        elif action == "deactivated":
            emitter.sig_display.emit(vsn, False)

        status = int(config.get("Response_Status", 200))
        body = json.dumps({"result": "ok", "vsn": vsn, "action": action}).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        try:
            self.wfile.write(body)
        except OSError:
            pass

    def do_GET(self):
        self._handle("GET")

    def do_PUT(self):
        self._handle("PUT")

    def do_POST(self):
        self._handle("POST")

    def log_message(self, fmt, *args):   # ปิด log ดิบของ BaseHTTPRequestHandler
        pass


class _Server(ThreadingHTTPServer):
    daemon_threads = True
    allow_reuse_address = True

    def __init__(self, addr, emitter):
        super().__init__(addr, _Handler)
        self.emitter = emitter


class ServerWorker(QtCore.QObject):
    sig_log = Signal(str, str)          # (tag, message)
    sig_display = Signal(str, bool)     # (vsn, activate)
    sig_state = Signal(bool, str)       # (running, detail)

    def __init__(self):
        super().__init__()
        self._srv = None
        self._thread = None

    @property
    def running(self):
        return self._srv is not None

    def start(self, host, port):
        if self._srv is not None:
            return
        try:
            self._srv = _Server((host, int(port)), self)
        except OSError as exc:
            self.sig_log.emit("ERR", f"เปิด server ไม่สำเร็จ ({host}:{port}): {exc}")
            self.sig_state.emit(False, str(exc))
            self._srv = None
            return
        self._thread = threading.Thread(target=self._srv.serve_forever, daemon=True)
        self._thread.start()
        self.sig_state.emit(True, f"{host}:{port}")
        self.sig_log.emit("INFO", f"HTTP server ทำงานที่ {host}:{port}")
        logger.info("HTTP server started on {}:{}", host, port)

    def stop(self):
        if self._srv is None:
            return
        try:
            self._srv.shutdown()
            self._srv.server_close()
        except OSError:
            pass
        self._srv = None
        self.sig_state.emit(False, "หยุดแล้ว")
        self.sig_log.emit("INFO", "หยุด HTTP server แล้ว")
        logger.info("HTTP server stopped")


# ===========================================================================
# GUI
# ===========================================================================
TAG_COLOR = {"REQ": "#1565c0", "INFO": "#616161", "ERR": "#c62828"}


class ColorRecWindow(QtWidgets.QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle(f"{Program_Name} v{Program_Version} — LSD Controller Simulator")
        self.resize(720, 640)

        self.worker = ServerWorker()
        self.worker.sig_log.connect(self.on_log)
        self.worker.sig_display.connect(self.on_display)
        self.worker.sig_state.connect(self.on_state)

        self._build_ui()
        self._apply_config_to_ui()

    def _build_ui(self):
        central = QtWidgets.QWidget()
        self.setCentralWidget(central)
        root = QtWidgets.QVBoxLayout(central)

        # --- config ---
        cfg_box = QtWidgets.QGroupBox("HTTP Server")
        cfg = QtWidgets.QGridLayout(cfg_box)
        cfg.addWidget(QtWidgets.QLabel("Listen Host:"), 0, 0)
        self.ed_host = QtWidgets.QLineEdit()
        cfg.addWidget(self.ed_host, 0, 1)
        cfg.addWidget(QtWidgets.QLabel("Port:"), 0, 2)
        self.ed_port = QtWidgets.QSpinBox()
        self.ed_port.setRange(1, 65535)
        cfg.addWidget(self.ed_port, 0, 3)
        self.btn_srv = QtWidgets.QPushButton("Start")
        self.btn_srv.setCheckable(True)
        self.btn_srv.clicked.connect(self.on_toggle)
        cfg.addWidget(self.btn_srv, 0, 4)
        self.lbl_state = QtWidgets.QLabel("● หยุด")
        self.lbl_state.setStyleSheet("color:#c62828; font-weight:bold;")
        cfg.addWidget(self.lbl_state, 1, 0, 1, 5)
        root.addWidget(cfg_box)

        # --- ป้ายจำลอง (สี) ---
        self.sign = QtWidgets.QLabel("—")
        self.sign.setAlignment(AlignCenter)
        self.sign.setMinimumHeight(220)
        self._set_sign_style("#000000", "#555555", "รอรับคำสั่ง")
        root.addWidget(self.sign, 1)
        self.lbl_cur = QtWidgets.QLabel("ป้ายปัจจุบัน: (ยังไม่มี)")
        self.lbl_cur.setAlignment(AlignCenter)
        root.addWidget(self.lbl_cur)

        # --- log ---
        log_box = QtWidgets.QGroupBox("Log")
        lv = QtWidgets.QVBoxLayout(log_box)
        self.txt = QtWidgets.QPlainTextEdit()
        self.txt.setReadOnly(True)
        self.txt.setMaximumBlockCount(5000)
        self.txt.setStyleSheet("font-family: Menlo, Consolas, monospace; font-size: 12px;")
        lv.addWidget(self.txt)
        row = QtWidgets.QHBoxLayout()
        row.addStretch(1)
        btn_clr = QtWidgets.QPushButton("ล้าง log")
        btn_clr.clicked.connect(self.txt.clear)
        row.addWidget(btn_clr)
        lv.addLayout(row)
        root.addWidget(log_box, 1)

    def _apply_config_to_ui(self):
        self.ed_host.setText(str(config.get("Listen_Host", "0.0.0.0")))
        self.ed_port.setValue(int(config.get("Listen_Port", 8080)))

    def _set_sign_style(self, bg, fg, text):
        self.sign.setStyleSheet(
            f"background-color:{bg}; color:{fg}; border-radius:12px;"
            f"font-size:44px; font-weight:bold;")
        self.sign.setText(text)

    # ---------------- actions ----------------
    def on_toggle(self, checked):
        if checked:
            config["Listen_Host"] = self.ed_host.text().strip()
            config["Listen_Port"] = int(self.ed_port.value())
            save_config()
            self.worker.start(config["Listen_Host"], config["Listen_Port"])
        else:
            self.worker.stop()

    # ---------------- slots ----------------
    def on_display(self, vsn, activate):
        cmap = config.get("Vsn_Color_Map", {}) or {}
        if not activate:
            self._set_sign_style("#000000", "#555555", "OFF")
            self.lbl_cur.setText(f"ป้ายปัจจุบัน: OFF (deactivate {vsn})")
            return
        info = cmap.get(vsn) or cmap.get(str(vsn).upper()) or cmap.get(str(vsn).lower())
        if info:
            self._set_sign_style(info.get("bg", "#37474f"),
                                 info.get("fg", "#ffffff"),
                                 info.get("label", vsn))
        else:
            self._set_sign_style("#37474f", "#ffffff", vsn or "?")
        self.lbl_cur.setText(f"ป้ายปัจจุบัน: {vsn}")

    def on_log(self, tag, message):
        color = TAG_COLOR.get(tag, "#000000")
        self.txt.appendHtml(f'<span style="color:{color}"><b>[{tag}]</b> {_esc(message)}</span>')

    def on_state(self, running, detail):
        if running:
            self.lbl_state.setText(f"● ทำงาน {detail}")
            self.lbl_state.setStyleSheet("color:#2e7d32; font-weight:bold;")
            self.btn_srv.setChecked(True)
            self.btn_srv.setText("Stop")
        else:
            self.lbl_state.setText(f"● หยุด ({detail})")
            self.lbl_state.setStyleSheet("color:#c62828; font-weight:bold;")
            self.btn_srv.setChecked(False)
            self.btn_srv.setText("Start")

    def closeEvent(self, event):
        self.worker.stop()
        super().closeEvent(event)


def _esc(s):
    return str(s).replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def main():
    app = QtWidgets.QApplication.instance() or QtWidgets.QApplication([])
    win = ColorRecWindow()
    win.show()
    logger.info("ColorRecDemo GUI started")
    run = getattr(app, "exec", None) or app.exec_
    raise SystemExit(run())


if __name__ == "__main__":
    main()
