---
tags: [planta, estación, F0]
---

# SLD — Cinta clasificadora (TXT 5)

> Estación elegida para la prueba de concepto: [[F0 - Prueba de concepto SLD]]. Es autocontenida, tiene un flujo claro y usa casi todos los tipos de componente.

## Qué hace
Recibe la pieza que sale de la cinta del [[MPO - Horno y mesa giratoria]], **lee su color** mientras avanza, la lleva hasta los cilindros y la **expulsa a la rampa de su color**. Después el [[VGR - Robot de vacío]] la recoge de la rampa.

## E/S
| Puerto | Tipo | Función |
|---|---|---|
| I1 | Barrera de luz (fototransistor) | Fototransistor que marca cuando la pieza llega al punto de lectura de color |
| I2 | Sensor de color | Sensor que lee el color de la pieza (blanco, rojo o azul) |
| I3 | Barrera de luz (fototransistor) | Fototransistor situado junto a los cilindros de expulsion |
| I6 | Barrera de luz (fototransistor) | Fototransistor del carril de piezas blancas |
| I7 | Barrera de luz (fototransistor) | Fototransistor del carril de piezas rojas |
| I8 | Barrera de luz (fototransistor) | Fototransistor del carril de piezas azules |
| O5 | Electroválvula | Cilindro que empuja fuera de la cinta las piezas blancas |
| O6 | Electroválvula | Cilindro que empuja fuera de la cinta las piezas rojas |
| O7 | Electroválvula | Cilindro que empuja fuera de la cinta las piezas azules |
| O8 | Compresor | Compresor que da presion de aire a los tres cilindros de expulsion |
| M1 | Motor con encoder | Motor de la cinta de clasificacion |
| C1 | Contador por final de carrera | Mini switch contador para detectar la posicion de la pieza en la cinta de clasificacion |

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

## En Unity hoy
- `ControladorCintaSLD_mqtt.cs`: mueve los eslabones de la cinta según `dt/sld/belt`, **pinta** la pieza del color que dice el cilindro, la traslada a su rampa y genera una pieza de repuesto si "debería haber una".
- `ControladorCilindrosSLD_mqtt.cs`: mueve los pistones y crea o destruye piezas en las rampas. El [[VGR - Robot de vacío]] lee sus propiedades (`IsWhiteSensorActivo`, `spawnPoint*`).
- Ambos son **marionetas**: ver [[Marioneta vs planta]].

## 🔧 Diseño de Mario (planta virtual)
- ❓ ¿Cómo convertir la velocidad del motor (0–512) en velocidad lineal de la cinta (mm/s)?
- ❓ ¿Cuántos pulsos de `C1` hay por mm de cinta? ¿Dónde están físicamente `I1`, `I2`, `I3` y los 3 cilindros, medidos sobre la cinta?
- ❓ ¿Cuánto tarda un cilindro en extenderse? ¿Qué pasa si la válvula se abre sin compresor?
- ❓ ¿Cómo se mueve la pieza sobre la cinta: arrastrada de forma cinemática o con física?
