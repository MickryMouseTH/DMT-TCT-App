"""
NovaDemo.py  (Program: NovaDemo)
================================
ตัวจำลอง "ฝั่ง TCT_App" — ส่งคำสั่งโปรโตคอล AVON (TCP binary) เหมือนที่ระบบเดิมส่งออกมา
ใช้ทดสอบ LSD_Bridge / LSD Controller ใหม่ โดยไม่ต้องมีตู้ TCT จริง

    [ NovaDemo (ไฟล์นี้) ] --TCP AVON--> [ LSD_Bridge ] --HTTP--> [ LSD Controller ใหม่ / ColorRecDemo ]

ความสามารถ:
  - ตั้งค่า IP:Port ปลายทางใน GUI (default 127.0.0.1:16603 = LSD_Bridge)
  - ส่งคำสั่ง Show/Clear (CLOSE / MTC / ETC / ETC-MTC) พร้อม identifier เดิม
  - ส่ง control frame: KeepAlive / SYNC1-3 / LORA_SYNC / ROTATION และปุ่ม Init (SYNC1->2->3)
  - Auto KeepAlive ตามช่วงเวลา (เหมือน TCT หล่อเลี้ยง connection)
  - แสดง log ทั้ง TX (ไบต์ที่ส่ง) และ RX (ACK ที่ตอบกลับ)

GUI = PySide6/PyQt (ผ่าน qt_compat) ; log ผ่าน LogLibrary.py
"""

import os
import json
import queue
import socket
import threading

from qt_compat import QtCore, QtGui, QtWidgets, Signal
from LogLibrary import Load_Config, Loguru_Logging, script_dir
import avon_protocol as avon

# ----------------------- Configuration Values -----------------------
Program_Name = "NovaDemo"
Program_Version = "1.0.0"
# --------------------------------------------------------------------

default_config = {
    # --- logging (ใช้โดย LogLibrary) ---
    "log_Level": "DEBUG",
    "Log_Console": 1,
    "log_Backup": 90,
    "Log_Size": "10 MB",

    # --- ปลายทางที่จะส่งไป (LSD_Bridge / controller) ---
    "Target_Host": "127.0.0.1",
    "Target_Port": 16603,
    "Recv_Timeout": 3,          # วินาที รอ ACK ต่อคำสั่ง
    "Connect_Timeout": 5,       # วินาที ตอน connect

    # --- keepalive อัตโนมัติ ---
    "AutoKeepAlive": 0,         # 1 = เปิดเมื่อ connect
    "KeepAlive_Interval": 5,    # วินาที

    # --- identifier เดิม (map โหมด -> identifier) แก้ได้ถ้าหน้างานใช้ค่าอื่น ---
    "Identifier_Map": dict(avon.DEFAULT_IDENTIFIERS),
}

config = Load_Config(default_config, Program_Name)
logger = Loguru_Logging(config, Program_Name, Program_Version)


def save_config():
    """เขียน config กลับไฟล์ (ไม่มี secret จึง dump ตรง ๆ ได้)"""
    path = os.path.join(script_dir, f"{Program_Name}_config.json")
    try:
        with open(path, "w", encoding="utf-8") as f:
            json.dump(config, f, indent=4)
    except OSError as exc:
        logger.warning("บันทึก config ไม่สำเร็จ: {}", exc)


# ===========================================================================
# Network worker — จัดการ socket ใน thread แยก (ห้ามบล็อก GUI thread)
# ===========================================================================
class NetWorker(QtCore.QObject):
    sig_log = Signal(str, str)          # (tag, message)  tag: TX/RX/INFO/ERR
    sig_state = Signal(bool, str)       # (connected, detail)

    def __init__(self):
        super().__init__()
        self._q = queue.Queue()
        self._sock = None
        self._thread = None
        self._run = False
        self._seq = 1
        self._lock = threading.Lock()

    # ---- seq เดิน 1..254 เหมือนระบบเดิม ----
    def _next_seq(self):
        with self._lock:
            s = self._seq
            self._seq += 1
            if self._seq > 254:
                self._seq = 1
            return s

    @property
    def connected(self):
        return self._run and self._sock is not None

    def start(self, host, port):
        if self._run:
            return
        # ล้าง queue เก่า (กัน sentinel/เฟรมค้างจาก session ก่อนทำให้ session ใหม่หลุดทันที)
        self._q = queue.Queue()
        self._run = True
        self._thread = threading.Thread(
            target=self._loop, args=(host, int(port)), daemon=True)
        self._thread.start()

    def stop(self):
        self._run = False
        # ปลดบล็อก queue.get ด้วย sentinel
        self._q.put((None, None))

    # ---- API ให้ GUI เรียกเพื่อ enqueue คำสั่ง ----
    def send_show(self, mode):
        ident = self._ident(mode)
        if ident:
            self._q.put((avon.build_show(ident, self._next_seq()),
                         f"SHOW {mode} (id={ident})"))

    def send_clear(self, mode):
        ident = self._ident(mode)
        if ident:
            self._q.put((avon.build_clear(ident, self._next_seq()),
                         f"CLEAR {mode} (id={ident})"))

    def send_control(self, cmd_type):
        try:
            frame = avon.build_control(cmd_type, self._next_seq())
        except ValueError as exc:
            self.sig_log.emit("ERR", str(exc))
            return
        self._q.put((frame, cmd_type))

    def _ident(self, mode):
        id_map = config.get("Identifier_Map", {}) or {}
        ident = id_map.get(mode)
        if not ident:
            self.sig_log.emit("ERR", f"ไม่พบ identifier ของโหมด {mode} ใน Identifier_Map")
        return ident

    # ---- thread หลัก ----
    def _loop(self, host, port):
        try:
            self.sig_log.emit("INFO", f"กำลังเชื่อมต่อ {host}:{port} ...")
            self._sock = socket.create_connection(
                (host, port), timeout=float(config.get("Connect_Timeout", 5)))
            self._sock.settimeout(float(config.get("Recv_Timeout", 3)))
            self.sig_state.emit(True, f"{host}:{port}")
            self.sig_log.emit("INFO", f"เชื่อมต่อสำเร็จ {host}:{port}")
        except OSError as exc:
            self.sig_log.emit("ERR", f"เชื่อมต่อไม่สำเร็จ: {exc}")
            self._run = False
            self._sock = None
            self.sig_state.emit(False, str(exc))
            return

        while self._run:
            frame, label = self._q.get()
            if frame is None or not self._run:
                break
            try:
                self._sock.sendall(frame)
                self.sig_log.emit("TX", f"{label}  [{len(frame)}B] {frame.hex(' ')}")
                logger.info("TX {} [{}B]", label, len(frame))
                self._recv_ack(label)
            except OSError as exc:
                self.sig_log.emit("ERR", f"ส่งล้มเหลว: {exc}")
                logger.error("TX fail: {}", exc)
                break

        self._cleanup()

    def _recv_ack(self, label=""):
        try:
            data = self._sock.recv(4096)
        except socket.timeout:
            self.sig_log.emit("RX", "(ไม่มี ACK ภายใน timeout)")
            return
        if not data:
            raise OSError("ปลายทางปิดการเชื่อมต่อ")
        if label == "SYNC2":
            data += self._drain_extra()
        self.sig_log.emit("RX", f"ACK [{len(data)}B] {data.hex(' ')}")
        logger.debug("RX ACK [{}B]", len(data))
        if label == "SYNC2":
            self._update_identifiers(data)

    def _drain_extra(self, quiet=0.3):
        """อ่านต่อจนเงียบ — response SYNC2 ยาว (~1.5KB) อาจมาหลาย TCP segment"""
        extra = b""
        self._sock.settimeout(quiet)
        try:
            while True:
                chunk = self._sock.recv(4096)
                if not chunk:
                    break
                extra += chunk
        except socket.timeout:
            pass
        finally:
            self._sock.settimeout(float(config.get("Recv_Timeout", 3)))
        return extra

    def _update_identifiers(self, data):
        """เลียนแบบ TCT จริง: อ่าน identifier จาก response SYNC2 แล้วอัปเดต Identifier_Map

        identifier ของแต่ละเครื่อง LSD ไม่เหมือนกัน — ค่าใน config เป็นแค่ default
        ถ้าส่ง Show ด้วย identifier ที่เครื่องไม่รู้จัก เครื่องจะเงียบ/ตัด connection
        """
        ident_map = avon.parse_sync2_identifiers(data)
        if not ident_map:
            self.sig_log.emit(
                "INFO", "SYNC2: ไม่พบรายการ identifier ใน response — คงค่าเดิมไว้")
            return
        old = config.get("Identifier_Map", {}) or {}
        changed = {k for k, v in ident_map.items() if old.get(k) != v}
        config.setdefault("Identifier_Map", {}).update(ident_map)
        if changed:
            save_config()
        for name in sorted(ident_map):
            mark = "  ← อัปเดต" if name in changed else ""
            self.sig_log.emit("INFO", f"SYNC2 identifier: {name} = {ident_map[name]}{mark}")
        logger.info("SYNC2 identifiers: {} (changed: {})", ident_map, sorted(changed) or "-")

    def _cleanup(self):
        if self._sock is not None:
            try:
                self._sock.close()
            except OSError:
                pass
        self._sock = None
        self._run = False
        self.sig_state.emit(False, "ปิดการเชื่อมต่อ")
        self.sig_log.emit("INFO", "ปิดการเชื่อมต่อแล้ว")


# ===========================================================================
# GUI
# ===========================================================================
TAG_COLOR = {
    "TX":   "#1565c0",   # ฟ้าเข้ม
    "RX":   "#2e7d32",   # เขียว
    "INFO": "#616161",   # เทา
    "ERR":  "#c62828",   # แดง
}
MODES = ["CLOSE", "MTC", "ETC", "ETC-MTC"]


class NovaWindow(QtWidgets.QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle(f"{Program_Name} v{Program_Version} — TCT AVON Sender Simulator")
        self.resize(760, 620)

        self.worker = NetWorker()
        self.worker.sig_log.connect(self.on_log)
        self.worker.sig_state.connect(self.on_state)

        self._build_ui()
        self._apply_config_to_ui()

        # timer สำหรับ auto keepalive (อยู่ใน GUI thread, แค่ enqueue)
        self.ka_timer = QtCore.QTimer(self)
        self.ka_timer.timeout.connect(lambda: self.worker.send_control("KEEPALIVE"))

    # ---------------- build UI ----------------
    def _build_ui(self):
        central = QtWidgets.QWidget()
        self.setCentralWidget(central)
        root = QtWidgets.QVBoxLayout(central)

        # --- config ปลายทาง ---
        cfg_box = QtWidgets.QGroupBox("ปลายทาง (Target)")
        cfg = QtWidgets.QGridLayout(cfg_box)
        cfg.addWidget(QtWidgets.QLabel("Host / IP:"), 0, 0)
        self.ed_host = QtWidgets.QLineEdit()
        cfg.addWidget(self.ed_host, 0, 1)
        cfg.addWidget(QtWidgets.QLabel("Port:"), 0, 2)
        self.ed_port = QtWidgets.QSpinBox()
        self.ed_port.setRange(1, 65535)
        cfg.addWidget(self.ed_port, 0, 3)
        self.btn_conn = QtWidgets.QPushButton("Connect")
        self.btn_conn.setCheckable(True)
        self.btn_conn.clicked.connect(self.on_connect_clicked)
        cfg.addWidget(self.btn_conn, 0, 4)
        self.lbl_state = QtWidgets.QLabel("● ไม่ได้เชื่อมต่อ")
        self.lbl_state.setStyleSheet("color:#c62828; font-weight:bold;")
        cfg.addWidget(self.lbl_state, 1, 0, 1, 3)
        # auto keepalive
        self.chk_ka = QtWidgets.QCheckBox("Auto KeepAlive ทุก")
        cfg.addWidget(self.chk_ka, 1, 3)
        self.spin_ka = QtWidgets.QSpinBox()
        self.spin_ka.setRange(1, 3600)
        self.spin_ka.setSuffix(" วิ")
        cfg.addWidget(self.spin_ka, 1, 4)
        self.chk_ka.toggled.connect(self.on_ka_toggled)
        root.addWidget(cfg_box)

        # --- คำสั่ง Show/Clear ---
        disp_box = QtWidgets.QGroupBox("คำสั่งป้าย (Show / Clear)")
        disp = QtWidgets.QHBoxLayout(disp_box)
        disp.addWidget(QtWidgets.QLabel("โหมด:"))
        self.cmb_mode = QtWidgets.QComboBox()
        self.cmb_mode.addItems(MODES)
        disp.addWidget(self.cmb_mode)
        btn_show = QtWidgets.QPushButton("▶ Show")
        btn_show.clicked.connect(lambda: self._send(lambda: self.worker.send_show(self.cmb_mode.currentText())))
        btn_show.setStyleSheet("font-weight:bold;")
        disp.addWidget(btn_show)
        btn_clear = QtWidgets.QPushButton("✕ Clear")
        btn_clear.clicked.connect(lambda: self._send(lambda: self.worker.send_clear(self.cmb_mode.currentText())))
        disp.addWidget(btn_clear)
        disp.addStretch(1)
        root.addWidget(disp_box)

        # --- control frames ---
        ctl_box = QtWidgets.QGroupBox("Control frames")
        ctl = QtWidgets.QHBoxLayout(ctl_box)
        for name in ("Init (SYNC1-3)", "KEEPALIVE", "SYNC1", "SYNC2", "SYNC3", "LORA_SYNC", "ROTATION"):
            b = QtWidgets.QPushButton(name)
            if name.startswith("Init"):
                b.clicked.connect(lambda _=False: self._send(self._send_init))
            else:
                b.clicked.connect(lambda _=False, n=name: self._send(lambda: self.worker.send_control(n)))
            ctl.addWidget(b)
        root.addWidget(ctl_box)

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
        self.ed_host.setText(str(config.get("Target_Host", "127.0.0.1")))
        self.ed_port.setValue(int(config.get("Target_Port", 16603)))
        self.spin_ka.setValue(int(config.get("KeepAlive_Interval", 5)))
        self.chk_ka.setChecked(bool(int(config.get("AutoKeepAlive", 0))))

    # ---------------- actions ----------------
    def _persist_target(self):
        config["Target_Host"] = self.ed_host.text().strip()
        config["Target_Port"] = int(self.ed_port.value())
        config["KeepAlive_Interval"] = int(self.spin_ka.value())
        config["AutoKeepAlive"] = 1 if self.chk_ka.isChecked() else 0
        save_config()

    def on_connect_clicked(self, checked):
        if checked:
            self._persist_target()
            self.worker.start(config["Target_Host"], config["Target_Port"])
        else:
            self.ka_timer.stop()
            self.worker.stop()

    def _send(self, fn):
        """ helper: ให้แน่ใจว่าเชื่อมต่อก่อน แล้วค่อยส่ง """
        if not self.worker.connected:
            self._persist_target()
            self.btn_conn.setChecked(True)
            self.worker.start(config["Target_Host"], config["Target_Port"])
            # เผื่อ connect ยังไม่เสร็จ ให้ queue คำสั่งไว้เลย (worker จะส่งหลัง connect)
        fn()

    def _send_init(self):
        for n in ("SYNC1", "SYNC2", "SYNC3"):
            self.worker.send_control(n)

    def on_ka_toggled(self, on):
        if on and self.worker.connected:
            self.ka_timer.start(max(1, self.spin_ka.value()) * 1000)
        else:
            self.ka_timer.stop()

    # ---------------- slots จาก worker ----------------
    def on_log(self, tag, message):
        color = TAG_COLOR.get(tag, "#000000")
        self.txt.appendHtml(
            f'<span style="color:{color}"><b>[{tag}]</b> {_esc(message)}</span>')

    def on_state(self, connected, detail):
        if connected:
            self.lbl_state.setText(f"● เชื่อมต่อ {detail}")
            self.lbl_state.setStyleSheet("color:#2e7d32; font-weight:bold;")
            self.btn_conn.setChecked(True)
            self.btn_conn.setText("Disconnect")
            if self.chk_ka.isChecked():
                self.ka_timer.start(max(1, self.spin_ka.value()) * 1000)
        else:
            self.lbl_state.setText(f"● ไม่ได้เชื่อมต่อ ({detail})")
            self.lbl_state.setStyleSheet("color:#c62828; font-weight:bold;")
            self.btn_conn.setChecked(False)
            self.btn_conn.setText("Connect")
            self.ka_timer.stop()

    def closeEvent(self, event):
        self.ka_timer.stop()
        self.worker.stop()
        super().closeEvent(event)


def _esc(s):
    return (str(s).replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;"))


def main():
    app = QtWidgets.QApplication.instance() or QtWidgets.QApplication([])
    win = NovaWindow()
    win.show()
    logger.info("NovaDemo GUI started")
    # รองรับทั้ง exec() (PySide6/PyQt6) และ exec_() (PyQt5/PySide2)
    run = getattr(app, "exec", None) or app.exec_
    raise SystemExit(run())


if __name__ == "__main__":
    main()
