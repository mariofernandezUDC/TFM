---
tags: [decisión]
---

# D006 · Protocolo `vio` de E/S (TXT 5, SLD)

- **Fecha:** 2026-11-05 · **Estado:** ✅ Aceptada (de momento solo TXT 5, la [[SLD - Clasificadora]]) · **Ampliada:** 2026-11-05 con D006.5 y D006.6

## Contexto
La [[Librería fischertechnik virtual]] (Python) y la maqueta virtual (Unity) tienen que intercambiar el estado de las E/S del TXT 5 por MQTT. Hay que fijar cómo se empaquetan los mensajes, cada cuánto se envían, qué pasa si se corta la comunicación y en qué unidades viajan los valores. Es el paso F0.3 de [[F0 - Prueba de concepto SLD]]. Explicación y ejemplos: [[Protocolo vio]].

## Opciones
- A. **Un mensaje con la imagen completa** de entradas y otro con la de salidas, como la [[Imagen de E-S]] de un PLC.
- B. **Un topic por borne** (`vio/txt5/I1`, `vio/txt5/I2`…).

Para el latido (D006.5): reenviar `vio/txt5/out` de forma periódica, o un topic de latido aparte.
Para la prueba sin Unity (D006.6): una herramienta que haga de maqueta a mano, o desactivar la regla de 0,5 s en pruebas.

## Decisión

### D006.1 · Empaquetado: imagen completa (opción A)
- `vio/txt5/in`: **entradas**, de la maqueta a Python. Se envía **de forma periódica**.
- `vio/txt5/out`: **salidas**, de Python a la maqueta. Se envía **cuando cambia** alguna salida **y además cada 100 ms** aunque no cambie (D006.5).
- Cada mensaje es una foto de **un mismo instante**: todas las entradas son coherentes entre sí.

### D006.2 · C1 como estado, imagen a 100 Hz
- `C1` viaja como **estado del interruptor** (`true`/`false`), no como cuenta. `SLD.py` lo lee con `get_state()` y **cuenta él mismo los cambios**: cada cambio de estado suma 1, en los dos sentidos. Ver [[Contador por pulsos]].
- Frecuencia de `vio/txt5/in`: **100 Hz por defecto, configurable**.
- **Regla:** la imagen tiene que ser bastante más rápida que los cambios de `C1`. Si es lenta, se pierden cuentas y la pieza se pasa de su cilindro.

### D006.3 · Datos viejos y estado seguro
- **En Python:** si la última imagen recibida tiene **más de 0,5 s**, cualquier lectura de la librería **espera a una imagen nueva** y avisa por consola. Nunca trabaja con datos viejos.
- **En Unity:** si no recibe nada por `vio/txt5/out` durante **más de 0,5 s**, pone las salidas en **estado seguro**: motor parado y válvulas cerradas (D006.5).

### D006.4 · Valores iguales a los de la API real, sin conversión
- `I2` en la escala de `get_voltage()`: los umbrales de la SLD van de 200 a 2000. La unidad está pendiente de confirmar.
- `M1` = `{"speed": 0–512, "dir": "CW"/"CCW", "running": bool}`.
- `O5`–`O8` = `bool`.
- Barreras (`I1`, `I3`, `I6`–`I8`) y `C1` = `bool`.

### D006.5 · Latido: `out` periódico
- Python envía `vio/txt5/out` **cuando cambia y además cada 100 ms** aunque no cambie.
- Unity pasa a **estado seguro** si no recibe nada durante **más de 0,5 s**.
- **Descartado:** un topic de latido aparte.

### D006.6 · Prueba sin Unity: `maqueta_manual.py`
- **No se desactiva** la regla de 0,5 s de D006.3, ni siquiera en pruebas.
- Se hará una herramienta, **`maqueta_manual.py`**, que:
  - publica la imagen de entradas `vio/txt5/in` a **100 Hz**;
  - permite cambiar desde el teclado `I1`, `I3`, `I6`–`I8`, `C1` e `I2`;
  - muestra lo que llega por `vio/txt5/out`.
- El **hito 1** se prueba con ella, en vez de con MQTT Explorer. Es una tarea de F0.4.

## Por qué
- **D006.1.** Con un topic por borne, las entradas llegarían sueltas y de instantes distintos. La lógica podría ver, por ejemplo, la barrera `I3` de un instante y el color `I2` de otro. La imagen completa evita eso, igual que en un PLC.
- **D006.2.** `SLD.py` no se toca ([[D001 Sustituir el hardware, no la lógica]]). Por eso la librería entrega lo mismo que el hardware real, un estado, y el conteo lo sigue haciendo el código original.
- **D006.3.** Es mejor que la lógica se pare y avise a que actúe sobre un mundo que ya no existe. En el otro sentido, una maqueta sin órdenes no debe dejar la cinta en marcha.
- **D006.4.** Si los valores son los mismos que devuelve la API real, la librería virtual no convierte nada y lo que aprende el alumno vale tal cual en la planta real.
- **D006.5.** Si `out` solo se enviara al cambiar, Unity no distinguiría "nada cambia" de "Python se ha caído". Reenviar la imagen cada 100 ms resuelve las dos cosas con el mismo mensaje. Un topic de latido aparte sería otra pieza que mantener, sin ninguna ventaja.
- **D006.6.** Si la regla de 0,5 s se desactivara en pruebas, lo probado no sería lo que funciona de verdad. Con `maqueta_manual.py`, la librería recibe el mismo flujo a 100 Hz que le dará Unity, y la regla se prueba también.

## Consecuencias
- La librería virtual guarda la última imagen y responde a `is_dark()`, `get_state()` o `get_voltage()` desde memoria.
- El riesgo de pulsos perdidos queda controlado por la regla de D006.2, pero **hay que medirlo** (ver "Pendientes").
- Con el latido de 100 ms y el límite de 0,5 s, Unity aguanta unos 5 mensajes perdidos seguidos antes de pasar a estado seguro.
- F0.4 suma una tarea: `maqueta_manual.py`.
- El protocolo se ampliará a los demás TXT en F1 (`vio/txtN/in`, `vio/txtN/out`).

## Puntos abiertos (cerrados el 2026-11-05)
1. ~~¿Cómo sabe Unity que Python sigue vivo?~~ → D006.5.
2. ~~¿Cómo se prueba el hito 1 sin Unity con la regla de 0,5 s?~~ → D006.6.

## Pendientes
- [ ] 🔧 **Diseño de Mario:** velocidad de la cinta v = (valor / 512) × v_max, con v_max medido.
- [ ] 🔧 **Diseño de Mario:** mm de cinta por cada cuenta de `C1`.
- [ ] 🔧 **Diseño de Mario:** comprobar si la relación es lineal, midiendo a velocidad 350 y a 512.
- [ ] **Tarea futura:** sacar de las grabaciones reales (`datosPieza*.json` o Influx) el tiempo T desde que la cinta arranca a 350 hasta que para, por color, para validar los 100 Hz.
- [ ] Confirmar la unidad de `get_voltage()`.
- [ ] Comprobar con `git log` qué parte de `vscode/lib/SLD.py` es de Fischertechnik (lógica generada por ROBO Pro Coding) y qué añadió Adrián (comentarios en español, `sld_cylinder_state`). Afecta a la licencia: ver "Licencia" en [[Riesgos]].
- [ ] Escribir `maqueta_manual.py` (tarea de F0.4, D006.6).

Relacionado: [[Protocolo vio]], [[Imagen de E-S]], [[Contador por pulsos]], [[Hilos y sondeo (polling)]], [[Librería fischertechnik virtual]], [[F0 - Prueba de concepto SLD]] (F0.3, F0.4 y hito 1).
