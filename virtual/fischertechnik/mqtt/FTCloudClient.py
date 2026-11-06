"""Cliente de la nube de Fischertechnik, desactivado.

`SLD_MQTT.py` solo lo usa si `get_cloud_active()` es True, y por defecto es
None. Si alguien lo activa, el mensaje no sale a ningún sitio y se avisa una vez.
"""

import logging


class FTCloudClient:
  _instancia = None

  def __init__(self):
    self._avisado = False

  @classmethod
  def getInstance(cls):
    if cls._instancia is None:
      cls._instancia = cls()
    return cls._instancia

  def publish(self, topic, payload, *args, **kwargs):
    if not self._avisado:
      logging.warning("FTCloudClient virtual: la nube está desactivada, no se publica %s", topic)
      self._avisado = True
