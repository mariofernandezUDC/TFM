---
tags: [concepto, software, clave]
---

# Librería `fischertechnik` virtual

Un paquete Python que **se hace pasar por** la librería oficial del TXT 4.0: mismas clases y mismos métodos, pero lee y escribe en la [[Imagen de E-S]] de la planta virtual en lugar de en el hardware. Es la pieza que hace posible [[D001 Sustituir el hardware, no la lógica]].

## Superficie de la API usada en `lib/` (medida en el código)
| Tipo | Métodos |
|---|---|
| [[Motor con encoder]] | `set_speed`, `set_distance`, `start_sync`, `stop_sync`, `is_running`, `start`, `stop`, `get_speed` |
| Contador | `get_count`, `set_motor` |
| Motor | `set_speed`, `start`, `stop`, `get_speed` |
| [[Final de carrera]] | `is_closed`, `is_open`, `get_state` |
| [[Barrera de luz]] | `is_dark` |
| [[Sensor de color]] | `get_voltage` |
| [[Neumática - válvula y compresor]] | `on`, `off` |
| LED | `set_brightness` |
| LDR | `get_resistance` |
| BME680 | `get_temperature`, `get_humidity`, `get_pressure`, `get_indoor_air_quality_as_number`, `get_accuracy` |
| Fábricas | `fischertechnik.factories.init_*`, `*_factory.create_*`, `fischertechnik.controller.Motor` (CW/CCW) |
| A simular o desactivar | `fischertechnik.mqtt.FTCloudClient` y `MqttClient`, `Constants.CONTROLLER_ID`, `display`, `pynfc`, cámara, sonido |

## Cómo se "cuela"
Se coloca por delante en el `PYTHONPATH`. Así `import fischertechnik.factories` carga la versión virtual y **el código de las estaciones no cambia**.

## Reparto del trabajo
💻 Claude puede escribir la mayor parte (es repetitivo), explicándolo. Mario revisa y decide el protocolo (F0.3).
