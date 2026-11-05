---
tags: [planta, estación]
---

# DPS — Entrada y salida de piezas

Bandeja de entrada (DSI) y de salida (DSO), lector de color y **lector NFC**. No tiene TXT propio: usa E/S del TXT 2 (`I7` barrera de salida, `I8` [[Sensor de color]], contador `C4`) y el lector NFC PN532 (librería `pynfc`, I2C).

## Código
- Python: `DPS.py`, `Nfc.py`. Unity: `ControladorDPS_mqtt.cs`, `PlataformaDSO_proxy.cs`.
- Topics: `dt/dps/dsi`, `dt/dps/dso`, `dt/dps/color`, `dt/dps/nfc`.

## Planta virtual
- Simular `pynfc`: cada pieza virtual lleva un ID de etiqueta. Ver [[Librería fischertechnik virtual]].
