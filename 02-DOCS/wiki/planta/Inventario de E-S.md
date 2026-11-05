---
tags: [planta, referencia]
---

# Inventario de E/S

Extraído de `vscode/lib/controller.py` (ver [[Lógica de control en Python]]). Es la **especificación de lo que tiene que simular la planta virtual**.

> **Fuente única de las E/S.** Las notas de cada estación no copian estas tablas: las insertan desde aquí con `![[Inventario de E-S#…]]`. Si cambia un puerto, se corrige solo en esta nota.

## SSC – Cámara y semáforo (TXT 0, maestro)

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

## HBW – Almacén (TXT 1)

| Puerto | Tipo | Función |
|---|---|---|
| I1 | Barrera de luz (fototransistor) | Fototransistor que detecta una pieza llegando por fuera del almacen |
| I4 | Barrera de luz (fototransistor) | Fototransistor que detecta una pieza dentro del almacen |
| I5 | Final de carrera | Interruptor que avisa cuando el brazo del almacen llega a la posicion de coger la pieza |
| I6 | Final de carrera | Interruptor que avisa cuando el brazo del almacen termina de retraerse |
| I7 | Final de carrera | Interruptor que avisa cuando el brazo del almacen termina de extenderse |
| I8 | Final de carrera | Interruptor que avisa cuando el brazo del almacen termina de subir |
| M1 | Motor | Motor de la cinta que mete y saca piezas del almacen |
| M2 | Motor con encoder | Motor que mueve el brazo del almacen de lado a lado, para elegir la columna del estante |
| M3 | Motor | Motor que estira y retrae el brazo del almacen para coger o dejar la pieza |
| M4 | Motor con encoder | Motor que sube y baja el brazo del almacen, para elegir la fila del estante |
| C2 | Contador de encoder | Cuenta el movimiento lateral del brazo, para saber en que columna esta |
| C4 | Contador de encoder | Cuenta la subida y bajada del brazo, para saber en que fila esta |

## VGR – Robot de vacío + DPS (TXT 2)

| Puerto | Tipo | Función |
|---|---|---|
| I1 | Final de carrera | Interruptor que avisa cuando el brazo termina de girar hasta el tope |
| I2 | Final de carrera | Interruptor que avisa cuando el brazo termina de subir |
| I3 | Final de carrera | Interruptor que avisa cuando el brazo termina de retraerse |
| I7 | Barrera de luz (fototransistor) | Fototransistor que detecta la pieza en la salida de la estacion DPS |
| I8 | Sensor de color | Sensor que lee el color de la pieza en la entrada de la estacion DPS |
| O7 | Compresor | Bomba que genera el vacio para que la ventosa agarre la pieza |
| O8 | Electroválvula | Valvula que abre o cierra el paso de vacio hacia la ventosa |
| M1 | Motor con encoder | Motor que gira el brazo hacia cada estacion |
| M2 | Motor con encoder | Motor que sube y baja el brazo |
| M3 | Motor con encoder | Motor que estira y retrae el brazo para alcanzar la pieza |
| C1 | Contador de encoder | Cuenta el giro del brazo, para saber hacia que estacion esta orientado |
| C2 | Contador de encoder | Cuenta la subida y bajada del brazo |
| C3 | Contador de encoder | Cuenta cuanto se ha estirado o retraido el brazo |
| C4 | Contador por barrera de luz | Cuenta las piezas que salen por la estacion DPS |

## MPO – Horno (TXT 3)

| Puerto | Tipo | Función |
|---|---|---|
| I1 | Final de carrera | Interruptor que avisa cuando la pieza esta dentro del horno |
| I2 | Final de carrera | Interruptor que avisa cuando la pieza vuelve a salir del horno |
| I3 | Final de carrera | Interruptor que avisa cuando el brazo llega a la posicion del horno |
| I5 | Barrera de luz (fototransistor) | Fototransistor que detecta la pieza en la entrada del horno |
| O5 | Electroválvula | Valvula de vacio de la ventosa del brazo del horno |
| O6 | Electroválvula | Valvula que hace bajar el brazo del horno |
| O7 | Electroválvula | Valvula que sube la puerta del horno |
| O8 | LED | LED que simula el horno encendido (se enciende mientras "cuece" la pieza) |
| M1 | Motor | Motor que estira o retrae la plataforma que mete la pieza en el horno |
| M2 | Motor | Motor que mueve el brazo del horno |

## MPO – Mesa giratoria, fresa y cinta (TXT 4)

| Puerto | Tipo | Función |
|---|---|---|
| I1 | Final de carrera | Interruptor que marca cuando la mesa giratoria queda frente al brazo |
| I2 | Final de carrera | Interruptor que marca cuando la mesa giratoria queda frente a la fresa |
| I3 | Final de carrera | Interruptor que marca cuando la mesa giratoria queda frente a la cinta |
| I4 | Barrera de luz (fototransistor) | Fototransistor que detecta la pieza al final de la cinta |
| I5 | Final de carrera | Interruptor que avisa de la posicion del brazo sobre la mesa giratoria |
| O7 | Electroválvula | Cilindro neumatico que empuja la pieza fuera de la mesa giratoria |
| O8 | Compresor | Compresor que da presion de aire a la ventosa del brazo, al brazo del horno, a la puerta del horno y al cilindro de la mesa giratoria |
| M1 | Motor | Motor que gira la mesa giratoria |
| M2 | Motor | Motor de la fresa que mecaniza la pieza |
| M3 | Motor | Motor de la cinta que saca la pieza de esta estacion |

## SLD – Clasificadora (TXT 5)

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

**Totales por tipo:** Final de carrera ×17, Barrera de luz (fototransistor) ×10, Motor con encoder ×8, Electroválvula ×8, Contador de encoder ×7, Motor ×7, LED ×5, Compresor ×3, Sensor de color ×2, LDR ×1, Sensor ambiental BME680 (I2C) ×1, Cámara USB ×1, Contador por barrera de luz ×1, Contador por final de carrera ×1. **Total: 72 puntos de E/S** en 6 controladores TXT 4.0.


Conceptos por tipo: [[Motor con encoder]] · [[Final de carrera]] · [[Barrera de luz]] · [[Sensor de color]] · [[Neumática - válvula y compresor]] · [[Contador por pulsos]]
