---
tags: [concepto, clave]
---

# Marioneta vs planta

La idea más importante para entender el salto del TFM.

| | **Marioneta** (Unity hoy) | **Planta** (objetivo) |
|---|---|---|
| ¿Quién decide qué pasa? | Los mensajes que llegan (`dt/*`) | La física simplificada del modelo |
| Color de la pieza | Se **pinta** con el color que dice el mensaje | La pieza **tiene** un color, y el [[Sensor de color]] lo lee |
| Sensores | Muestran lo que dice el mensaje | Se **calculan**: la pieza tapa la [[Barrera de luz]] |
| Piezas que faltan | Se inventan (*fallback spawner*) | No pueden aparecer de la nada |
| Si el control falla | No se nota | Se ve: la pieza no llega, el eje choca con su tope |

**Causalidad invertida:** en la marioneta, el sensor dice "hay pieza" y Unity dibuja una pieza. En la planta, hay una pieza, y por eso el sensor dice "hay pieza".

Ejemplo concreto: [[SLD - Clasificadora]] (`ControladorCintaSLD_mqtt.cs` pinta la pieza con el color del cilindro).

❓ Explica con tus palabras por qué un alumno no puede aprender a programar contra una marioneta.
