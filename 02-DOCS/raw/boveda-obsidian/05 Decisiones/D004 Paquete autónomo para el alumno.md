---
tags: [decisión]
---

# D004 · Paquete autónomo para el alumno

- **Fecha:** 2026-10-29 · **Estado:** ✅ Aceptada (los detalles técnicos se cierran en F6)

## Decisión
El alumno descarga **un paquete** y funciona sin instalar ni configurar nada:
- Ejecutable de Unity para Windows (VR opcional).
- Broker MQTT embebido (por ejemplo un servidor MQTTnet dentro de Unity) o `mosquitto.exe` portable.
- Python embebido con la lógica original y la [[Librería fischertechnik virtual]], que Unity lanza automáticamente.
- Sin InfluxDB, sin red y sin credenciales.

## Opciones abiertas (F6)
- Broker dentro de Unity o como proceso aparte.
- Python embebido o pedir que el alumno lo tenga instalado.

Relacionado: [[Arquitectura de la planta virtual]].
