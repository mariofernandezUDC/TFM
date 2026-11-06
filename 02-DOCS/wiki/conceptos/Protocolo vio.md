---
tags: [concepto, software, mqtt]
---

# Protocolo `vio` (*virtual I/O*)

Es el idioma en el que se hablan la [[Librería fischertechnik virtual]] (Python) y la maqueta virtual (Unity) por [[MQTT]]. Lleva la [[Imagen de E-S]] de cada TXT. De momento solo está definido para el TXT 5 ([[SLD - Clasificadora]]). Las reglas y el porqué están en [[D006 Protocolo vio]].

> Analogía: es el cableado entre el armario eléctrico y la máquina, pero en mensajes. Cada mensaje es una foto de todos los bornes a la vez.

## Topics
| Topic | Sentido | Qué lleva | Cuándo se envía |
|---|---|---|---|
| `vio/txt5/in` | maqueta (Unity) → Python | Todas las entradas | Periódico, 100 Hz por defecto (configurable) |
| `vio/txt5/out` | Python → maqueta (Unity) | Todas las salidas | Cuando cambia alguna salida **y además cada 100 ms** (latido) |

Son topics nuevos. **No sustituyen** al [[Contrato MQTT]] `dt/*`, que sigue igual.

## Ejemplos de mensajes

**Entradas** (`vio/txt5/in`):
```json
{"I1": true, "I2": 1450, "I3": false, "I6": false, "I7": false, "I8": false, "C1": false}
```
Se lee así: hay una pieza tapando la barrera de entrada (`I1`), el [[Sensor de color]] marca 1450, la pieza todavía no ha llegado a los cilindros (`I3`), las tres rampas están vacías y el interruptor contador `C1` está abierto.

**Salidas** (`vio/txt5/out`):
```json
{"M1": {"speed": 350, "dir": "CW", "running": true}, "O5": false, "O6": false, "O7": false, "O8": true}
```
Se lee así: la cinta va a velocidad 350 en sentido CW, las tres válvulas de expulsión están cerradas y el compresor (`O8`) está encendido. Es el momento en que la SLD lleva la pieza hacia su cilindro contando pulsos.

## Valores (los mismos que da la API real)
| E/S | Valor | Origen en la API |
|---|---|---|
| `I1`, `I3`, `I6`, `I7`, `I8` | `bool` | `is_dark()` de la [[Barrera de luz]] |
| `I2` | número en la escala de `get_voltage()` (umbrales 200–2000; unidad por confirmar) | [[Sensor de color]] |
| `C1` | `bool`, estado del interruptor | `get_state()` del [[Final de carrera]]. `SLD.py` cuenta los cambios él mismo |
| `M1` | `{"speed": 0–512, "dir": "CW"/"CCW", "running": bool}` | [[Motor con encoder]] |
| `O5`–`O8` | `bool` | `on()` / `off()` de la [[Neumática - válvula y compresor]] |

## Reglas de funcionamiento
1. **Instantes coherentes:** un mensaje lleva la imagen entera. Nunca hay entradas sueltas de momentos distintos.
2. **La imagen va más rápida que `C1`:** si no, se pierden cuentas y la pieza se pasa. Ver [[Contador por pulsos]].
3. **Nada de datos viejos:** si la última imagen tiene más de 0,5 s, la librería espera a una nueva y avisa por consola.
4. **Latido:** Python reenvía `vio/txt5/out` cada 100 ms aunque no cambie. No hay topic de latido aparte: el propio mensaje de salidas sirve de latido.
5. **Estado seguro:** si Unity no recibe nada por `vio/txt5/out` durante más de 0,5 s, para el motor y cierra las válvulas.
6. **El latido significa "lazo de control sano"** (D006.7): mientras la imagen de entradas está caducada, Python **no envía `out`**, ni cambios ni latido. Así Unity pasa a estado seguro. Cuando vuelven las entradas, `out` se reanuda solo.
7. **Mensajes inválidos:** un `in` con JSON roto, al que le falta una clave o con un tipo incorrecto se descarta entero, con aviso, y no cuenta como imagen nueva. Las claves de más se ignoran.
8. QoS 0 y sin `retain` en los dos topics.
9. **Hombre muerto en Unity** (D006.8): si el hilo principal de Unity lleva más de 0,5 s sin avanzar (pausa o cuelgue), Unity deja de enviar `in`. Pausar Unity pausa todo el lazo.
10. **`out` inválido** (D006.8): Unity lo descarta entero, con aviso, y no cuenta como latido.

> Analogía: es el hombre muerto de un tren. Mientras Python «pisa el pedal» cada 100 ms, la cinta sigue. Si deja de pisarlo durante medio segundo, todo se para. Y Python solo pisa el pedal si ve la vía: sin entradas frescas, lo suelta.

## Probar sin Unity: `maqueta_manual.py`
La regla 3 no se desactiva nunca, ni en pruebas. Por eso no basta con publicar un mensaje a mano en MQTT Explorer: caducaría a los 0,5 s. En su lugar se usa `maqueta_manual.py`, una maqueta de mentira manejada desde el teclado:
- publica `vio/txt5/in` a 100 Hz, igual que hará Unity;
- permite cambiar `I1`, `I3`, `I6`–`I8`, `C1` e `I2` con el teclado;
- muestra lo que llega por `vio/txt5/out`.

Con ella se prueba el hito 1 de [[F0 - Prueba de concepto SLD]]. Ver [[D006 Protocolo vio]] (D006.6). Está en `virtual/maqueta_manual.py` (cómo usarla: `virtual/README.md`).

> ⚠️ En Windows hay que pedir un reloj de 1 ms (`timeBeginPeriod(1)`), o la imagen no llega a 100 Hz y se pierden cambios de `C1`. Medido en F0.4: ver "Consecuencias" en [[D006 Protocolo vio]].

## ❓ Pregunta de comprensión
Con la cinta a velocidad 350, ¿cuántos cambios por segundo de `C1` aguantan los 100 Hz con margen? ¿Qué medida de la maqueta te hace falta para responder? (Pista: los mm de cinta por cuenta de `C1`.)

Relacionado: [[D006 Protocolo vio]], [[Imagen de E-S]], [[Hilos y sondeo (polling)]], [[Librería fischertechnik virtual]], [[F0 - Prueba de concepto SLD]] (F0.3 y F0.4).
