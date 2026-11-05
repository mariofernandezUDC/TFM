---
tags: [planta, estación]
---

# HBW — Almacén automático (TXT 1)

Almacén de 9 huecos (3×3) con un transelevador de 2 ejes con encoder (horizontal `M2`, vertical `M4`), una horquilla que se extiende y se retrae (`M3`, con finales de carrera) y una cinta de entrada y salida (`M1`).

## E/S

Tabla en el inventario (fuente única, no se duplica aquí):

![[Inventario de E-S#HBW – Almacén (TXT 1)]]

## Código
- Python: `HBW.py`, `HBW_AxesNRef.py` (referenciado de ejes), `HBW_Storage.py` (inventario), `HBW_MQTT.py`.
- Unity: `ControladorHBWposition_mqtt.cs`, `ControladorCintaHBW_mqtt.cs`, `ControladorSpawnPiecesHBW_mqtt.cs`, proxies `BrazoHBW`, `CintaHBW`, `ContenedorHBW`, `SoporteHBW`.
- Topics: `dt/hbw/pos`, `dt/hbw/belt`, `f/i/stock`. Ver [[Contrato MQTT]].

## 🔧 Diseño de Mario
- ❓ Posición de cada hueco en pasos de encoder ↔ coordenadas en la escena.
- ❓ Topes mecánicos de cada eje.
