---
tags: [software, unity]
---

# Gemelo en Unity

Unity 6000.3.6f1, URP, VR opcional (HTC VIVE + SteamVR), cliente MQTT `M2Mqtt.Net.dll`.

## Modos
| Modo | Fuente de datos |
|---|---|
| En vivo | MQTT de la planta real ([[Contrato MQTT]]) |
| Histórico | InfluxDB (`InfluxDBClient.cs`) |
| Simulación | Grabaciones JSON → [[Modo simulación actual]] |

## Scripts (`Assets/Scripts/`)
- **Clientes:** `MQTTClient.cs` (suscripción a `dt/*`, ping), `MQTT_InterfaceClient.cs` (órdenes desde la interfaz: `f/o/order`, `o/ptu`, `c/*`), `MQTTLatencyLogger.cs`, `InfluxDBClient.cs`.
- **`MovimientoDelGemelo/`:** un `Controlador*_mqtt.cs` por subsistema (reciben un topic y mueven la escena) más `*_proxy.cs` (piezas móviles concretas).
- **`Interfaz/`:** menú, stock, monitor de sensores (`UI_SensorMonitor.cs`, base del futuro monitor de E/S), cámara, BBDD.
- **`Simulacion/`:** `SimuladorOffline.cs`, `GenerarJSONSimulacion.cs`.
- **`Trazabilidad/`:** detecta cuándo una pieza cambia de "padre" (de cinta a ventosa…).

## Cómo funciona hoy
Es una **marioneta**: recibe estados y anima, e incluso se inventa piezas cuando "debería haber una". Ver [[Marioneta vs planta]].
