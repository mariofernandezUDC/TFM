---
tags: [decisión]
---

# D006 · Protocolo `vio` de E/S (TXT 5, SLD)

- **Fecha:** 2026-11-05 · **Estado:** ✅ Aceptada (de momento solo TXT 5, la [[SLD - Clasificadora]])

## Contexto
La [[Librería fischertechnik virtual]] (Python) y la maqueta virtual (Unity) tienen que intercambiar el estado de las E/S del TXT 5 por MQTT. Hay que fijar cómo se empaquetan los mensajes, cada cuánto se envían, qué pasa si se corta la comunicación y en qué unidades viajan los valores. Es el paso F0.3 de [[F0 - Prueba de concepto SLD]]. Explicación y ejemplos: [[Protocolo vio]].

## Opciones
- A. **Un mensaje con la imagen completa** de entradas y otro con la de salidas, como la [[Imagen de E-S]] de un PLC.
- B. **Un topic por borne** (`vio/txt5/I1`, `vio/txt5/I2`…).

## Decisión

### D006.1 · Empaquetado: imagen completa (opción A)
- `vio/txt5/in`: **entradas**, de la maqueta a Python. Se envía **de forma periódica**.
- `vio/txt5/out`: **salidas**, de Python a la maqueta. Se envía **cuando cambia** alguna salida.
- Cada mensaje es una foto de **un mismo instante**: todas las entradas son coherentes entre sí.

### D006.2 · C1 como estado, imagen a 100 Hz
- `C1` viaja como **estado del interruptor** (`true`/`false`), no como cuenta. `SLD.py` lo lee con `get_state()` y **cuenta él mismo los cambios**: cada cambio de estado suma 1, en los dos sentidos. Ver [[Contador por pulsos]].
- Frecuencia de `vio/txt5/in`: **100 Hz por defecto, configurable**.
- **Regla:** la imagen tiene que ser bastante más rápida que los cambios de `C1`. Si es lenta, se pierden cuentas y la pieza se pasa de su cilindro.

### D006.3 · Datos viejos y estado seguro
- **En Python:** si la última imagen recibida tiene **más de 0,5 s**, cualquier lectura de la librería **espera a una imagen nueva** y avisa por consola. Nunca trabaja con datos viejos.
- **En Unity:** si deja de recibir órdenes, pone las salidas en **estado seguro**: motor parado y válvulas cerradas.

### D006.4 · Valores iguales a los de la API real, sin conversión
- `I2` en la escala de `get_voltage()`: los umbrales de la SLD van de 200 a 2000. La unidad está pendiente de confirmar.
- `M1` = `{"speed": 0–512, "dir": "CW"/"CCW", "running": bool}`.
- `O5`–`O8` = `bool`.
- Barreras (`I1`, `I3`, `I6`–`I8`) y `C1` = `bool`.

## Por qué
- **D006.1.** Con un topic por borne, las entradas llegarían sueltas y de instantes distintos. La lógica podría ver, por ejemplo, la barrera `I3` de un instante y el color `I2` de otro. La imagen completa evita eso, igual que en un PLC.
- **D006.2.** `SLD.py` no se toca ([[D001 Sustituir el hardware, no la lógica]]). Por eso la librería entrega lo mismo que el hardware real, un estado, y el conteo lo sigue haciendo el código original.
- **D006.3.** Es mejor que la lógica se pare y avise a que actúe sobre un mundo que ya no existe. En el otro sentido, una maqueta sin órdenes no debe dejar la cinta en marcha.
- **D006.4.** Si los valores son los mismos que devuelve la API real, la librería virtual no convierte nada y lo que aprende el alumno vale tal cual en la planta real.

## Consecuencias
- La librería virtual guarda la última imagen y responde a `is_dark()`, `get_state()` o `get_voltage()` desde memoria.
- El riesgo de pulsos perdidos queda controlado por la regla de D006.2, pero **hay que medirlo** (ver "Pendientes").
- El protocolo se ampliará a los demás TXT en F1 (`vio/txtN/in`, `vio/txtN/out`).

## ⚠️ Puntos abiertos (detectados al documentar)

**1. ¿Cómo sabe Unity que Python sigue vivo?**
D006.1 dice que `vio/txt5/out` se envía **solo cuando cambia**, y D006.3 dice que Unity pasa a estado seguro **si deja de recibir órdenes**. Juntas, chocan: si la cinta lleva un rato en marcha a 350 y nada cambia, Python no envía nada, y Unity podría creer que se ha cortado la comunicación y parar la cinta. Hay que decidir cómo sabe Unity que Python sigue vivo. Por ejemplo, reenviar la imagen de salidas cada cierto tiempo aunque no cambie, o un latido aparte. 🔧 **Lo decide Mario.**

**2. La prueba a mano del hito 1.** El hito 1 de [[F0 - Prueba de concepto SLD]] se prueba publicando `vio/txt5/in` a mano en MQTT Explorer, sin Unity. Pero con D006.3, una imagen publicada una vez tiene más de 0,5 s enseguida, y la librería se queda esperando una nueva. Hay que decidir cómo se prueba sin Unity. Por ejemplo, un pequeño script que repita la imagen a 100 Hz, o una opción de la librería para desactivar el límite en pruebas. **Lo decide Mario.**

## Pendientes
- [ ] 🔧 **Diseño de Mario:** velocidad de la cinta v = (valor / 512) × v_max, con v_max medido.
- [ ] 🔧 **Diseño de Mario:** mm de cinta por cada cuenta de `C1`.
- [ ] 🔧 **Diseño de Mario:** comprobar si la relación es lineal, midiendo a velocidad 350 y a 512.
- [ ] **Tarea futura:** sacar de las grabaciones reales (`datosPieza*.json` o Influx) el tiempo T desde que la cinta arranca a 350 hasta que para, por color, para validar los 100 Hz.
- [ ] Confirmar la unidad de `get_voltage()`.
- [ ] Comprobar con `git log` qué parte de `vscode/lib/SLD.py` es de Fischertechnik (lógica generada por ROBO Pro Coding) y qué añadió Adrián (comentarios en español, `sld_cylinder_state`). Afecta a la licencia: ver "Licencia" en [[Riesgos]].
- [ ] Resolver los dos puntos abiertos de arriba.

Relacionado: [[Protocolo vio]], [[Imagen de E-S]], [[Contador por pulsos]], [[Hilos y sondeo (polling)]], [[Librería fischertechnik virtual]], [[F0 - Prueba de concepto SLD]] (F0.3).
