"""Clase base de los motores, con las constantes de sentido de giro.

`SLD.py` la importa así: `from fischertechnik.controller.Motor import Motor`,
y usa `Motor.CW`. Los valores son textos porque viajan tal cual en el JSON
de `vio/txtN/out` (D006.4: `"dir": "CW"/"CCW"`).
"""


class Motor:
  CW = "CW"     # sentido horario
  CCW = "CCW"   # sentido antihorario
