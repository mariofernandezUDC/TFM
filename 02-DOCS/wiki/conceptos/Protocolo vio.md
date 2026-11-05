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
| `vio/txt5/out` | Python → maqueta (Unity) | Todas las salidas | Cuando cambia alguna salida |

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
4. **Estado seguro:** si Unity deja de recibir órdenes, para el motor y cierra las válvulas.

⚠️ Dos puntos abiertos, apuntados en [[D006 Protocolo vio]]: la regla 4 choca con "`out` solo cuando cambia", y la regla 3 impide probar a mano con un único mensaje publicado en MQTT Explorer.

## ❓ Pregunta de comprensión
Con la cinta a velocidad 350, ¿cuántos cambios por segundo de `C1` aguantan los 100 Hz con margen? ¿Qué medida de la maqueta te hace falta para responder? (Pista: los mm de cinta por cuenta de `C1`.)

Relacionado: [[D006 Protocolo vio]], [[Imagen de E-S]], [[Hilos y sondeo (polling)]], [[F0 - Prueba de concepto SLD]] (F0.3).
