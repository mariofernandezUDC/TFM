---
tags: [concepto, software]
---

# MQTT

Protocolo de mensajería **publicación/suscripción** muy ligero, estándar en IoT e Industria 4.0.

- **Broker:** el servidor que reparte los mensajes (aquí Mosquitto, puerto 1884; ver `GUIA_CONEXION.md`).
- **Topic:** el "canal", jerárquico (`dt/sld/belt`). Admite comodines: `+` (un nivel) y `#` (todos los niveles).
- **QoS 0/1/2:** desde "como mucho una vez" hasta "exactamente una vez".
- **Retain:** el broker guarda el último mensaje del topic y se lo entrega a quien se suscriba después.

## En este TFM
- [[Contrato MQTT]] existente (`dt/*`, `f/*`, `i/*`…).
- Nuevo canal `vio/*` para la [[Imagen de E-S]].
- 🛠️ Herramienta recomendada: **MQTT Explorer**, para ver en directo todos los topics y publicar mensajes a mano.

❓ ¿Por qué tiene sentido `retain=True` en las posiciones de los ejes y no en el ping?
