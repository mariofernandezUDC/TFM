---
tags: [software, mqtt, referencia]
---

# Contrato MQTT existente

Lo que hoy une la planta (o el simulador) con Unity. **No se debe romper.** Ver [[MQTT]].

| Topic | Campos | Sentido |
|---|---|---|
| `dt/vgr/pos` | rotation, vertical, extend | planta → Unity |
| `dt/vgr/grip` | active | planta → Unity |
| `dt/hbw/pos` | horizontal, vertical, extend | planta → Unity |
| `dt/hbw/belt` | belt_speed, isTriggeredIn, isTriggeredOut, rot_direction | planta → Unity |
| `dt/dps/dsi`, `dt/dps/dso` | dsi_sensor / dso_sensor | planta → Unity |
| `dt/dps/color`, `dt/dps/nfc` | color · id, nfc | planta → Unity |
| `dt/mpo/oven` | oven_sensor, close_door, open_door, lights, move2Ref5/6 | planta → Unity |
| `dt/mpo/arm` | move2Ref3/4, lowering, vacuum | planta → Unity |
| `dt/mpo/turntable` | eject, move2Ref7–10, rotation, saw | planta → Unity |
| `dt/mpo/belt` | active, exit_sensor | planta → Unity |
| `dt/sld/belt` | speed, entry_sensor, cylinder_sensor | planta → Unity |
| `dt/sld/cylinder` | cyl_color, active, is_white/red/blue | planta → Unity |
| `dt/ssc/leds`, `dt/ssc/camera` | led_online, leds_semaphore · pan, tilt | planta → Unity |
| `dt/factory` | connected (heartbeat) | planta → Unity |
| `dt/ping` ↔ `dt/pong` | latencia | ambos |
| `f/i/stock`, `i/bme680`, `i/ldr`, `i/cam` | stock, ambiente, luz, cámara | planta → Unity |
| `f/o/order`, `o/ptu`, `c/*` | pedido, cámara, peticiones | **Unity → planta** |

Todos los mensajes llevan `ts` (marca de tiempo UTC). Los publica `Digital_Twin.py` ([[Lógica de control en Python]]).

## Nuevo (planta virtual)
Protocolo `vio/*` para la [[Imagen de E-S]]: se diseña en [[F0 - Prueba de concepto SLD]], paso F0.3.
