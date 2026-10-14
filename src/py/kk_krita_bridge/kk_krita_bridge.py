import os
import socket
import threading

from PyQt5 import QtCore
from PyQt5.QtWidgets import QMessageBox

from krita import Krita, Extension

BRIDGE_HOST = "127.0.0.1"
BRIDGE_PORT = 9184


class _Receiver(QtCore.QObject):
    dataReady = QtCore.pyqtSignal(str)

    def __init__(self, parent=None):
        super(_Receiver, self).__init__(parent)
        self._sock = None
        try:
            sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            sock.bind((BRIDGE_HOST, BRIDGE_PORT))
            sock.listen(1)
        except OSError:
            try:
                sock.close()
            except Exception:
                pass
            return
        self._sock = sock
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
                self.dataReady.emit(data)


class KoikatsuBridgeExtension(Extension):

    def __init__(self, parent):
        super(KoikatsuBridgeExtension, self).__init__(parent)
        try:
            receiver = _Receiver()
            receiver.dataReady.connect(self.importCapture)
            # keep a reference alive on the extension instance
            self._receiver = receiver
        except Exception:
            self._receiver = None

    @QtCore.pyqtSlot(str)
    def importCapture(self, payload):
        if not payload.startswith("IMPORT "):
            return
        path = payload[7:].strip()
        if not path or not os.path.exists(path):
            QMessageBox.warning(None, "KK Bridge", "No se encontro la captura:\n%s" % path)
            return
        doc = Krita.instance().activeDocument()
        if doc is None:
            QMessageBox.warning(None, "KK Bridge", "No hay documento activo en Krita.")
            return
        try:
            active = doc.activeNode()
            layer = doc.createNode("KK capture", "paintlayer")
            if layer is None or not layer.loadNodeId(path):
                raise RuntimeError("loadNodeId fallo")
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
            QMessageBox.warning(None, "KK Bridge", "No se pudo importar:\n%s" % ex)
            return
        QMessageBox.information(None, "KK Bridge", "Frame importado como capa nueva.")


def createExtension(application):
    return KoikatsuBridgeExtension(application)
