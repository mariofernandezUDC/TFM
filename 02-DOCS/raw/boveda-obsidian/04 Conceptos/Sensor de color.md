---
tags: [concepto, componente]
---

# Sensor de color

Sensor óptico que devuelve una **tensión analógica** según cuánta luz refleja la pieza: **más clara → menos tensión**.

- API: `get_voltage()`.
- Hay 2: en la [[SLD - Clasificadora]] (`I2`) y en la [[DPS - Entrada y salida]] (`I8` del TXT 2).

## Cómo lo usa la SLD
Mientras la pieza pasa, se queda con el **valor mínimo** y lo compara con los umbrales: blanco < 1150 ≤ rojo < ~1460 ≤ azul < 2000.

## Modelo virtual
- La pieza tiene un color → tensión nominal por color + **ruido**. ❓ Busca los valores reales en el histórico de Influx, en los logs de la SLD (`print("Color: ...")`) o midiendo en la maqueta.
- Mientras la pieza no está debajo, devuelve un valor de "sin pieza" (¿> 2000?).
- Fallo a simular (F4): sensor descalibrado → la pieza se clasifica mal.
