---
tags: [planta, estación]
---

# SSC — Cámara, semáforo y sensores ambientales (TXT 0, maestro)

Cámara USB con pan/tilt (2 motores con encoder y finales de carrera), semáforo de estado (LEDs `O6`–`O8`), LED de conexión `O5`, LDR (`I3`) y sensor ambiental BME680 (I2C).

## E/S

Tabla en el inventario (fuente única, no se duplica aquí):

![[Inventario de E-S#SSC – Cámara y semáforo (TXT 0, maestro)]]

## Código
- Python: `SSC_PTU.py`, `SSC_PTU_Axes1Ref.py`, `SSC_Lights.py`, `SSC_Publisher.py`, `SSC_Subscriber.py`.
- Unity: `ControladorCamaraSSC_mqtt.cs`, `ControladorLEDsSSC_mqtt.cs`, `UI_PTUController.cs`.

## Planta virtual
Baja prioridad. Valores sintéticos para el BME680 y la LDR, y la cámara como *render texture* (opcional).
