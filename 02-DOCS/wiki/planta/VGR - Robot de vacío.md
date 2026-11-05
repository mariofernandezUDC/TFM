---
tags: [planta, estación]
---

# VGR — Robot de vacío (TXT 2)

Robot de 3 ejes con encoder (giro `M1`, vertical `M2`, extensión `M3`) y una **ventosa** (compresor `O7` + válvula `O8`). Mueve las piezas entre las estaciones. Es la estación más compleja junto con el MPO.

## E/S

Tabla en el inventario (fuente única, no se duplica aquí):

![[Inventario de E-S#VGR – Robot de vacío + DPS (TXT 2)]]

## Código
- Python: `VGR.py` (38 KB, la más larga), `VGR_Axes1Ref.py`, `Axes1Ref.py` y `Axes2Ref.py` (referenciado y movimiento de ejes).
- Posiciones de destino en `data/FactoryCalibDefault.json` → `VGR.poslist` (DSI, DSO, HBW, MPO, lector de color…).
- Unity: `ControladorVGR_mqtt.cs`, `GripVGR_proxy.cs`. Topics: `dt/vgr/pos`, `dt/vgr/grip`.

## 🔧 Diseño de Mario
- ❓ Cinemática de 3 ejes: pasos de encoder → ángulo y desplazamientos → posición de la ventosa.
- ❓ ¿Cuándo "agarra" la ventosa? (contacto + vacío). Ver [[Neumática - válvula y compresor]].
