---
tags: [planta, estación, F0]
---

# SLD — Cinta clasificadora (TXT 5)

> Estación elegida para la prueba de concepto: [[F0 - Prueba de concepto SLD]]. Es autocontenida, tiene un flujo claro y usa casi todos los tipos de componente.

## Qué hace
Recibe la pieza que sale de la cinta del [[MPO - Horno y mesa giratoria]], **lee su color** mientras avanza, la lleva hasta los cilindros y la **expulsa a la rampa de su color**. Después el [[VGR - Robot de vacío]] la recoge de la rampa.

## E/S

Tabla en el inventario (fuente única, no se duplica aquí):

![[Inventario de E-S#SLD – Clasificadora (TXT 5)]]

## Lógica (`vscode/lib/SLD.py`, función `thread_SLD`)
Bucle infinito, cada 0,5 s:
1. Si `I1` (barrera de entrada) está a oscuras → hay pieza → estado 2 (clasificando).
2. Arranca la cinta (`M1` a 512) y, **mientras `I3` no detecte la pieza**, lee el [[Sensor de color]] `I2` y se queda con el **valor mínimo** (cuanto más clara la pieza, menos tensión).
3. Para la cinta y enciende el compresor `O8`.
4. Arranca la cinta a 350 y **cuenta pulsos** del final de carrera `C1` comparando su estado con el anterior (ver [[Contador por pulsos]]).
5. Según el color, para al llegar a cierto número de pulsos y expulsa:

| Color | Rango de tensión | Para tras… | Válvula | Barrera de rampa |
|---|---|---|---|---|
| Blanco | 200 ≤ v < 1150 | >5 pulsos | `O5` | `I6` |
| Rojo | 1150 ≤ v < umbral rojo/azul | >14 pulsos | `O6` | `I7` |
| Azul | umbral ≤ v < 2000 | >25 pulsos | `O7` | `I8` |
| Fuera de rango | <200 o ≥2000 | — | — | → error SLD (estado 4) |

6. `eject*()`: válvula ON 0,5 s → OFF → compresor OFF → espera 0,5 s → comprueba que la barrera de la rampa ve la pieza (OK = 1, si no error = 4).

### Detalles curiosos (útiles para la memoria)
- **Umbral rojo/azul inconsistente:** `thread_SLD` arranca con 1400; `set_calib_data_SLD` lo cambia a **1460** (la lectura desde el JSON está comentada); y `data/FactoryCalibDefault.json` dice **1600**, pero no se usa.
- **Sin timeout:** `while not isEjectionTriggered()` espera para siempre si la pieza no llega a `I3`. Buen caso para la inyección de fallos (F4).
- El estado se publica a la nube (`SLD_MQTT.py`) y al gemelo (`Digital_Twin.py`: `dt/sld/belt`, `dt/sld/cylinder`). Ver [[Contrato MQTT]].

## Diagrama de estados

Entregable de F0.1 en [[F0 - Prueba de concepto SLD]].

```mermaid
stateDiagram-v2
    [*] --> Reposo
    Reposo --> LeerColor : I1 a oscuras
    LeerColor --> Posicionar : I3 a oscuras
    Posicionar --> Expulsar : cuentas de C1 > 5 / 14 / 25 según color
    Posicionar --> Error : color < 200 o >= 2000 (tras 30 cuentas)
    Expulsar --> ComprobarRampa : válvula 0,5 s y espera 0,5 s
    ComprobarRampa --> Reposo : I6 / I7 / I8 a oscuras
    Error --> Reposo : botón ACK
    note right of Reposo : estado 1
    note right of LeerColor : estado 2 · M1 a 512
    note right of Posicionar : estado 2 · O8 ON · M1 a 350
    note right of Error : estado 4
```

### Dos niveles
- **Estado oficial**, el que se publica (`_set_state_SLD`, `state_code`): 1 = reposo, 2 = clasificando, 4 = error.
- **Fases internas**: `LeerColor`, `Posicionar`, `Expulsar` y `ComprobarRampa`. No se publican. Las cuatro van dentro del estado oficial 2.

### Cuándo se comprueba el rango de color
El rango no se comprueba al leer el color. Se comprueba al final del posicionamiento. Con un color fuera de rango, el bucle `while counter <= 29` cuenta 30 pulsos de `C1` sin expulsar nada. Solo al salir del bucle mira si el valor es `< 200` o `>= 2000` y marca el error (`set_factory_error_state('SLD')`).

### El código hace algo distinto de lo que parece (útil para la memoria y para F4)
1. **El error de rampa nunca se publica.** En `ejectWhite`, `ejectRed` y `ejectBlue`, si la barrera de la rampa no ve la pieza, el código pone `state_code = 4`. Pero `thread_SLD` llama justo después a `_set_state_SLD(1, 0)` (línea 125), y eso lo sobrescribe con 1. Además, `eject*` cambia `state_code` directamente, sin pasar por `_set_state_SLD`, así que no fuerza una publicación inmediata. Solo podría verse si la publicación periódica de cada 10 s (`thread_update_SLD`) cayera justo en ese instante.
2. **El motor puede quedarse en marcha.** Con un color fuera de rango, el bucle de posicionamiento cae siempre en `else: pass`. Nadie en `SLD.py` llama a `stop()` de `M1`, así que la cinta sigue a 350 al pasar a error. ⏳ Falta comprobar si otro módulo lo para al entrar en error.

### Decisión de diseño: dónde se detecta la rampa equivocada
La detección de "pieza en la rampa equivocada" va en la **planta virtual (Unity)**, no en la [[Librería fischertechnik virtual]]. La librería imita los bornes y no conoce el color real de la pieza. La planta sí lo conoce.

## En Unity hoy
- `ControladorCintaSLD_mqtt.cs`: mueve los eslabones de la cinta según `dt/sld/belt`, **pinta** la pieza del color que dice el cilindro, la traslada a su rampa y genera una pieza de repuesto si "debería haber una".
- `ControladorCilindrosSLD_mqtt.cs`: mueve los pistones y crea o destruye piezas en las rampas. El [[VGR - Robot de vacío]] lee sus propiedades (`IsWhiteSensorActivo`, `spawnPoint*`).
- Ambos son **marionetas**: ver [[Marioneta vs planta]].

## 🔧 Diseño de Mario (planta virtual)
- ❓ ¿Cómo convertir la velocidad del motor (0–512) en velocidad lineal de la cinta (mm/s)?
- ❓ ¿Cuántos pulsos de `C1` hay por mm de cinta? ¿Dónde están físicamente `I1`, `I2`, `I3` y los 3 cilindros, medidos sobre la cinta?
- ❓ ¿Cuánto tarda un cilindro en extenderse? ¿Qué pasa si la válvula se abre sin compresor?
- ❓ ¿Cómo se mueve la pieza sobre la cinta: arrastrada de forma cinemática o con física?
