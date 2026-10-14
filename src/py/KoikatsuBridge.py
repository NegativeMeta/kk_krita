import Krita
import os, socket, threading, json
from PyQt5 import QtCore

BRIDGE_PORT = 9184
BRIDGE_DIR  = r"E:/Users/johin/Code_Library_Old/Koikatsu BetterRepack RX22/UserData/krita_bridge"

class KoikatsuReceiver(QtCore.QObject):
    def __init__(self, parent):
        super().__init__(parent)
        self.server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self.server.bind(("127.0.0.1", BRIDGE_PORT))
        self.server.listen(1)
        threading.Thread(target=self.accept_loop, daemon=True).start()

    def accept_loop(self):
        while True:
            try:
                client, _ = self.server.accept()
                data = client.recv(1024).decode("utf-8")
                client.close()
                QtCore.QMetaObject.invokeMethod(
                    Krita.instance(), "importKoikatsu", QtCore.Qt.QueuedConnection,
                    QtCore.Q_ARG(str, data)
                )
            except Exception:
                break

class KoikatsuExtension(Krita.Extension):
    def __init__(self, parent):
        super().__init__(parent)
        self.receiver = KoikatsuReceiver(self)

    @QtCore.Slot(str)
    def importKoikatsu(self, payload):
        if not payload.startswith("IMPORT "):
            return
        file = payload[7:].strip()
        app = Krita.instance()
        doc = app.activeDocument()
        if not doc or not os.path.exists(file):
            return

        active = doc.activeNode()
        layer = doc.createNode("KK capture", "paintlayer")
        if layer is None or not layer.loadNodeId(file):
            Krita.message("Could not load capture.", "KK Bridge", Krita.MESSAGE_TYPE_ERROR, False)
            return

        parent = active.parentNode() if active and active.parentNode() else doc.rootNode()
        parent.addChildNode(layer, active)
        doc.setActiveNode(layer)

        status = os.path.join(BRIDGE_DIR, "capture.status")
        try:
            with open(status, "w") as f:
                json.dump({"ready": False, "file": os.path.basename(file)}, f)
        except Exception:
            pass
        Krita.message("Frame imported as new layer.", "KK Bridge", Krita.MESSAGE_TYPE_INFORMATION, False)

def createExtension(application):
    return KoikatsuExtension(application)

Krita.instance().addExtension(createExtension(Krita.instance()))
