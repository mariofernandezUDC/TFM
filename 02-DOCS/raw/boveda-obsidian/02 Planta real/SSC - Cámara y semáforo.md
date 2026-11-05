---
tags: [planta, estación]
---

# SSC — Cámara, semáforo y sensores ambientales (TXT 0, maestro)

Cámara USB con pan/tilt (2 motores con encoder y finales de carrera), semáforo de estado (LEDs `O6`–`O8`), LED de conexión `O5`, LDR (`I3`) y sensor ambiental BME680 (I2C).

## E/S
| Puerto | Tipo | Función |
|---|---|---|
| I1 | Final de carrera | Interruptor que avisa cuando la camara termina de girar hasta el tope |
| I2 | Final de carrera | Interruptor que avisa cuando la camara termina de subir o bajar hasta el tope |
| I3 | LDR | LDR: mide cuanta luz hay alrededor de la fabrica |
| I8 | Final de carrera | FIXME: en I8 no hay nada conectado en la planta real |
| O5 | LED | LED que indica que la estacion esta conectada y funcionando |
| O6 | LED | LED rojo del semaforo de alarmas |
| O7 | LED | LED amarillo del semaforo de alarmas |
| O8 | LED | LED verde del semaforo de alarmas |
| M1 | Motor con encoder | Motor que gira la camara a izquierda y derecha |
| M2 | Motor con encoder | Motor que sube y baja la inclinacion de la camara |
| C1 | Contador de encoder | Cuenta cuanto ha girado el motor de rotacion, para saber en que angulo esta la camara |
| C2 | Contador de encoder | Cuenta cuanto ha subido o bajado la camara |
| I2C_1 | Sensor ambiental BME680 (I2C) | Sensor que mide temperatura, humedad, presion y calidad del aire de la fabrica |
| USB1_1 | Cámara USB | Camara que graba y transmite la imagen de la fabrica |

## Código
- Python: `SSC_PTU.py`, `SSC_PTU_Axes1Ref.py`, `SSC_Lights.py`, `SSC_Publisher.py`, `SSC_Subscriber.py`.
- Unity: `ControladorCamaraSSC_mqtt.cs`, `ControladorLEDsSSC_mqtt.cs`, `UI_PTUController.cs`.

## Planta virtual
Baja prioridad. Valores sintéticos para el BME680 y la LDR, y la cámara como *render texture* (opcional).
