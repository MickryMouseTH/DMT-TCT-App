"""
qt_compat.py
============
ชั้นบาง ๆ ให้ NovaDemo / ColorRecDemo ใช้ Qt ได้ทั้ง PySide6, PyQt6, PyQt5, PySide2
โดยไม่ต้องแก้โค้ด (ต่าง binding ใช้ชื่อ Signal/Slot ไม่เหมือนกัน)

ใช้งาน:
    from qt_compat import QtCore, QtGui, QtWidgets, Signal, Slot, QT_BINDING
"""

QtCore = QtGui = QtWidgets = None
Signal = Slot = None
QT_BINDING = None

_errors = []
for _name in ("PySide6", "PyQt6", "PyQt5", "PySide2"):
    try:
        if _name == "PySide6":
            from PySide6 import QtCore, QtGui, QtWidgets
            from PySide6.QtCore import Signal, Slot
        elif _name == "PyQt6":
            from PyQt6 import QtCore, QtGui, QtWidgets
            from PyQt6.QtCore import pyqtSignal as Signal, pyqtSlot as Slot
        elif _name == "PyQt5":
            from PyQt5 import QtCore, QtGui, QtWidgets
            from PyQt5.QtCore import pyqtSignal as Signal, pyqtSlot as Slot
        else:  # PySide2
            from PySide2 import QtCore, QtGui, QtWidgets
            from PySide2.QtCore import Signal, Slot
        QT_BINDING = _name
        break
    except ImportError as exc:  # ลอง binding ตัวถัดไป
        _errors.append("%s: %s" % (_name, exc))

if QT_BINDING is not None:
    # enum ต่าง binding อยู่คนละที่ (Qt6 = scoped enum, Qt5 = flat) — normalize
    try:
        AlignCenter = QtCore.Qt.AlignmentFlag.AlignCenter   # PySide6 / PyQt6
    except AttributeError:
        AlignCenter = QtCore.Qt.AlignCenter                 # PyQt5 / PySide2

if QT_BINDING is None:
    raise ImportError(
        "ไม่พบ Qt binding ใด ๆ — ติดตั้งอย่างใดอย่างหนึ่ง เช่น:\n"
        "    python3 -m pip install PySide6\n"
        "รายละเอียด:\n  " + "\n  ".join(_errors)
    )
