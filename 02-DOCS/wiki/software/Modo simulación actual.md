---
tags: [software, unity]
---

# Modo simulación actual (`SimuladorOffline.cs`)

- `GenerarJSONSimulacion.cs` descarga de InfluxDB todos los mensajes MQTT de una ventana de tiempo en la que una pieza recorrió la planta real, y los guarda en `Assets/DatosSimulacion/datosPieza{Blanca,Roja,Azul}.json`.
- `SimuladorOffline.cs` **reproduce** esos mensajes con los tiempos originales, inyectándolos en `MQTTClient`. Para el resto de scripts es como si llegaran de la planta.

## Limitación
Es un **vídeo**, no una planta: no reacciona a nada. No se puede programar, mover un eje ni provocar un fallo. Ver [[Marioneta vs planta]].

## Valor para el TFM
Esas grabaciones son **trazas reales de referencia** para validar la planta virtual (F7 del [[Plan de trabajo]]).
