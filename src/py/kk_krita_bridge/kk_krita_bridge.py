"""KK Krita Bridge: importa capturas de CharaStudio como capa nueva.

Todo el cuerpo va dentro de un try para volcar cualquier error de import
al log (Krita no muestra tracebacks de plugins en ningun lado visible).
"""
try:
    with open("E:/Users/johin/Code_Library_Old/Koikatsu BetterRepack RX22/UserData/krita_bridge/kk_bridge.log", "a", encoding="utf-8") as _f:
        _f.write("import-top\n")
except Exception:
    pass
import os
import socket
import threading
import time
import traceback

BRIDGE_HOST = "127.0.0.1"
BRIDGE_PORT = 9184
BRIDGE_DIR = "E:/Users/johin/Code_Library_Old/Koikatsu BetterRepack RX22/UserData/krita_bridge"
LOG_PATH = BRIDGE_DIR + "/kk_bridge.log"


def _log(msg):
    try:
        with open(LOG_PATH, "a", encoding="utf-8") as f:
            f.write(msg + "\n")
    except Exception:
        pass


try:
    # Krita 5 usa PyQt5, Krita 6.0 trae PyQt6 (otros builds: PySide6)
    try:
        from PyQt6 import QtCore
        from PyQt6.QtGui import QImage
        from PyQt6.QtCore import QByteArray
        from PyQt6.QtWidgets import QMessageBox
        _Signal = QtCore.pyqtSignal
        _Slot = QtCore.pyqtSlot
    except ImportError:
        try:
            from PySide6 import QtCore
            from PySide6.QtGui import QImage
            from PySide6.QtCore import QByteArray
            from PySide6.QtWidgets import QMessageBox
            _Signal = QtCore.Signal
            _Slot = QtCore.Slot
        except ImportError:
            from PyQt5 import QtCore
            from PyQt5.QtGui import QImage
            from PyQt5.QtCore import QByteArray
            from PyQt5.QtWidgets import QMessageBox
            _Signal = QtCore.pyqtSignal
            _Slot = QtCore.pyqtSlot

    from krita import Krita, Extension

    class _Receiver(QtCore.QObject):
        dataReady = _Signal(str)

        def __init__(self, parent=None):
            super(_Receiver, self).__init__(parent)
            self._sock = None
            try:
                sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
                sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
                sock.bind((BRIDGE_HOST, BRIDGE_PORT))
                sock.listen(1)
            except OSError as ex:
                _log("bind fallo: %s" % ex)
                try:
                    sock.close()
                except Exception:
                    pass
                return
            self._sock = sock
            _log("escuchando en %s:%d" % (BRIDGE_HOST, BRIDGE_PORT))
            thread = threading.Thread(target=self._accept_loop)
            thread.daemon = True
            thread.start()

        def _accept_loop(self):
            while True:
                try:
                    client, _ = self._sock.accept()
                except OSError:
                    break
                try:
                    data = client.recv(4096).decode("utf-8", "replace")
                except Exception:
                    data = ""
                try:
                    client.close()
                except Exception:
                    pass
                if data:
                    _log("recibido: %s" % data[:200])
                    self.dataReady.emit(data)

    class KoikatsuBridgeExtension(Extension):

        def __init__(self, parent):
            try:
                super(KoikatsuBridgeExtension, self).__init__(parent)
            except Exception as ex:
                _log("super-init fallo: %s" % ex)
                raise
            _log("plugin cargado")
            self._pending_live = None
            self._busy_live = False
            self._receiver = None

        def setup(self):
            _log("setup")
            try:
                receiver = _Receiver()
                receiver.dataReady.connect(self.importCapture)
                # keep a reference alive on the extension instance
                self._receiver = receiver
            except Exception as ex:
                _log("receiver fallo: %s" % ex)
                self._receiver = None

        def createActions(self, window):
            pass

        @_Slot(str)
        def importCapture(self, payload):
            payload = payload.lstrip("﻿ \t\r\n")
            if payload.startswith("LIVE "):
                importLive(self, payload[5:].strip())
                return
            if not payload.startswith("IMPORT "):
                return
            path = payload[7:].strip()
            # CaptureScreenshot escribe al final del frame: esperar hasta 3s
            for _ in range(12):
                if path and os.path.exists(path):
                    break
                time.sleep(0.25)
            if not path or not os.path.exists(path):
                _log("falta archivo: %s" % path)
                QMessageBox.warning(None, "KK Bridge", "No se encontro la captura:\n%s" % path)
                return
            doc = Krita.instance().activeDocument()
            if doc is None:
                _log("sin documento activo")
                QMessageBox.warning(None, "KK Bridge", "No hay documento activo en Krita.")
                return
            try:
                active = doc.activeNode()
                qimg = QImage()
                if not qimg.load(path):
                    raise RuntimeError("QImage no pudo leer el archivo")
                if qimg.format() != QImage.Format.Format_ARGB32:
                    qimg = qimg.convertToFormat(QImage.Format.Format_ARGB32)
                w, h = qimg.width(), qimg.height()
                ptr = qimg.constBits()
                if ptr is None:
                    raise RuntimeError("QImage sin datos")
                data = QByteArray(ptr.asstring(qimg.sizeInBytes()))
                layer = doc.createNode("KK capture", "paintlayer")
                if layer is None:
                    raise RuntimeError("createNode fallo")
                layer.setPixelData(data, 0, 0, w, h)
                if active is not None and active.parentNode() is not None:
                    active.parentNode().addChildNode(layer, active)
                else:
                    doc.rootNode().addChildNode(layer, None)
                doc.setActiveNode(layer)
                try:
                    doc.refreshProjection()
                except Exception:
                    pass
            except Exception as ex:
                _log("import fallo: %s" % ex)
                QMessageBox.warning(None, "KK Bridge", "No se pudo importar:\n%s" % ex)
                return
            _log("import ok: %s" % path)

    def _load_pixels(path):
        try:
            qimg = QImage()
            if not qimg.load(path):
                return None
            if qimg.format() != QImage.Format.Format_ARGB32:
                qimg = qimg.convertToFormat(QImage.Format.Format_ARGB32)
            w, h = qimg.width(), qimg.height()
            ptr = qimg.constBits()
            if ptr is None:
                return None
            return (QByteArray(ptr.asstring(qimg.sizeInBytes())), w, h)
        except Exception as ex:
            _log("pixels fallo: %s" % ex)
            return None

    def _find_layer(node, name):
        try:
            if node.name() == name:
                return node
            for child in node.childNodes():
                found = _find_layer(child, name)
                if found is not None:
                    return found
        except Exception:
            pass
        return None

    def importLive(self, path):
        self._pending_live = path
        if self._busy_live:
            return
        self._busy_live = True
        try:
            while self._pending_live is not None:
                p = self._pending_live
                self._pending_live = None
                _do_live(self, p)
        finally:
            self._busy_live = False

    def _do_live(self, path):
        for _ in range(12):
            if path and os.path.exists(path):
                break
            time.sleep(0.25)
        if not path or not os.path.exists(path):
            _log("live: falta archivo")
            return
        doc = Krita.instance().activeDocument()
        if doc is None:
            return
        px = _load_pixels(path)
        if px is None:
            _log("live: no se pudo leer")
            return
        data, w, h = px
        try:
            layer = _find_layer(doc.rootNode(), "KK live")
            if layer is None:
                layer = doc.createNode("KK live", "paintlayer")
                if layer is None:
                    return
                doc.rootNode().addChildNode(layer, None)
                doc.setActiveNode(layer)
            layer.setPixelData(data, 0, 0, w, h)
            try:
                doc.refreshProjection()
            except Exception:
                pass
        except Exception as ex:
            _log("live fallo: %s" % ex)
            return
        _log("live ok")

    _log("registrando extension")
    Krita.instance().addExtension(KoikatsuBridgeExtension(Krita.instance()))

except Exception:
    try:
        with open(LOG_PATH, "a", encoding="utf-8") as f:
            f.write(traceback.format_exc() + "\n")
    except Exception:
        pass
    raise
