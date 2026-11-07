---
tags: [concepto, software, clave]
---

# Librería `fischertechnik` virtual

Un paquete Python que **se hace pasar por** la librería oficial del TXT 4.0: mismas clases y mismos métodos, pero lee y escribe en la [[Imagen de E-S]] de la planta virtual en lugar de en el hardware. Es la pieza que hace posible [[D001 Sustituir el hardware, no la lógica]].

## Superficie de la API usada en `lib/` (medida en el código)
| Tipo | Métodos |
|---|---|
| [[Motor con encoder]] | `set_speed`, `set_distance`, `start_sync`, `stop_sync`, `is_running`, `start`, `stop`, `get_speed` |
| Contador | `get_count`, `set_motor` |
| Motor | `set_speed`, `start`, `stop`, `get_speed` |
| [[Final de carrera]] | `is_closed`, `is_open`, `get_state` |
| [[Barrera de luz]] | `is_dark` |
| [[Sensor de color]] | `get_voltage` |
| [[Neumática - válvula y compresor]] | `on`, `off` |
| LED | `set_brightness` |
| LDR | `get_resistance` |
| BME680 | `get_temperature`, `get_humidity`, `get_pressure`, `get_indoor_air_quality_as_number`, `get_accuracy` |
| Fábricas | `fischertechnik.factories.init_*`, `*_factory.create_*`, `fischertechnik.controller.Motor` (CW/CCW) |
| A simular o desactivar | `fischertechnik.mqtt.FTCloudClient` y `MqttClient`, `Constants.CONTROLLER_ID`, `display`, `pynfc`, cámara, sonido |

## Cómo se "cuela"
Se coloca por delante en el `PYTHONPATH`. Así `import fischertechnik.factories` carga la versión virtual y **el código de las estaciones no cambia**.

## Estado: versión mínima de F0.4 (2026-11-06) ✅
Está en `Github/TFM/virtual/`. Cómo arrancarla: `virtual/README.md`.

| Pieza | Qué hace |
|---|---|
| `fischertechnik/factories.py` | Todo lo que llama `vscode/lib/controller.py`: `init()`, los 7 `init_*_factory()`, los 15 `create_*` distintos e `initialized()` |
| `fischertechnik/dispositivos.py` | Los bornes. Claves `I<n>`, `O<n>`, `M<n>` y `C<n>`, como en el [[Inventario de E-S]] |
| `fischertechnik/vio.py` | El enlace MQTT del TXT 5 con el [[Protocolo vio]] |
| `fischertechnik/controller/Motor.py`, `fischertechnik/mqtt/` | `Motor.CW`/`CCW`, `CONTROLLER_ID` y un `FTCloudClient` desactivado |
| `lib/display.py` | `display.set_attr()` imprime |
| `run_virtual.py` | Lanzador: `thread_SLD` sin argumento, o el programa del alumno con `--programa` |
| `run_sld_virtual.py` | Envoltorio de `run_virtual.py` sin `--programa` (comandos del hito 1) |
| `maqueta_manual.py` | Maqueta falsa con teclado (D006.6) |

- **Solo el TXT 5 está conectado.** Los otros 5 TXT son "tontos": las entradas dan un valor neutro (`False` o 0) y las salidas no hacen nada. Se crean igual porque `controller.py` crea las E/S de los 6 al importarse.
- Un borne que el protocolo aún no define (la LDR, el BME680, la cámara o los contadores de encoder) **da error si se crea en el TXT 5**. Así se ve enseguida qué falta en `vio` cuando se amplíe en F1.
- `set_distance`, `start_sync` y `stop_sync` dan `NotImplementedError`: no los usa la SLD. Quedan para F1.
- **Lecturas desde memoria:** las entradas se leen de la última imagen sin esperar a la red. Si la imagen tiene más de 0,5 s, la lectura espera a una nueva (D006.3). Mientras tanto no se envía `out` (D006.7).
- **`lib.display`:** `vscode/lib/` no tiene `__init__.py`. Es un *namespace package*, y `virtual/lib/display.py` se une a él sin tocar `vscode/`. Si alguien añade `vscode/lib/__init__.py`, deja de funcionar.

### Lo que se aprendió al hacerla
- **El reloj de Windows importa.** Por defecto, `sleep` no baja de unos 15,6 ms. La imagen salía a 40–60 Hz y `SLD.py` perdió 4 de 30 cambios de `C1`. Con `timeBeginPeriod(1)` va a 100 Hz y no pierde ninguno. Es el riesgo de D006.2, medido. Ver [[Contador por pulsos]].
- **Los bucles sin pausa de `SLD.py`** (por ejemplo, `while counter <= 29`) leen miles de veces por segundo. Si cada lectura tomara un cerrojo, el hilo de MQTT tardaría en guardar la imagen nueva. Por eso la lectura con imagen fresca no lo toma.
- **Cada cambio de salida es un mensaje.** `ejectWhite` apaga `O5` y después `O8`, así que llegan dos `out` seguidos. `setConvBeltSpeedSLD(0)` deja la cinta a speed 0 con running **true**. La librería lo refleja tal cual: es lo que hace el código.

## Programa del alumno: «cargar código» (2026-11-07)
El alumno escribe su propio programa de la SLD con la misma API y lo lanza con `virtual/run_virtual.py --programa ruta.py`. Sin `--programa`, lanza `SLD.py` como antes. Paso a paso en `virtual/README.md`.

- **Estilo A (de arriba abajo)**, como un `main.py` de ROBO Pro Coding. El lanzador lo ejecuta como `__main__`, igual que el TXT, así que el estilo B (`main()` con la guarda `if __name__ == "__main__"`) también funciona.
- Plantilla `virtual/alumno/plantilla_sld.py` (bornes del TXT 5 y bucle mínimo) y ejemplo resuelto `virtual/alumno/ejemplos/clasificar_por_color.py`.
- **Error del alumno** (en el hilo principal o en uno suyo): `factories.parar_salidas()` pone todo a cero, envía un último `out` y bloquea las salidas. Después se muestran su archivo, su línea y el mensaje, y el proceso termina con código 1. Sin latido, Unity pasa a estado seguro a los 0,5 s (D006.5). Medido: cinta parada a ≤31 ms del fallo y proceso terminado a ≤47 ms (`tests/test_programa_alumno.py`). En Unity, estado seguro a 501 ms del último `out`.
- ⚠️ **Pendiente de comprobar en la maqueta:** se supone que en el TXT real el alumno tiene en `lib/` el mismo `controller.py` de la fábrica (`vscode/lib/controller.py`), con los nombres `TXT_SLD_E5_…`. Si ROBO Pro Coding genera otro `lib/controller.py` para la SLD sola, con otros nombres, la plantilla y el ejemplo habría que adaptarlos, y D001 no se cumpliría tal cual.
- En Unity, el ejemplo dejó la blanca y la azul en su rampa, pero **la roja no**: el cilindro no la capturó y siguió en la cinta. Es el mismo sesgo de la roja que apunta el diario [[2026-11-06]]. Con la tolerancia provisional de 10 mm (ver abajo), ya entra.

![[2026-11-07_programa_alumno_blanca_y_azul.png]]
*El ejemplo resuelto del alumno, ejecutado contra la planta virtual: la blanca y la azul en su rampa. La roja (al fondo, en la cinta) no la capturó su cilindro.*

![[2026-11-07_error_alumno_estado_seguro.png]]
*Un programa que falla a mitad: motor a 0, válvulas cerradas, `out` a 0 por segundo y Unity en estado seguro. La pieza queda parada en I3.*

### Tolerancia de captura a 10 mm (2026-11-07, PROVISIONAL)
- `toleranciaCaptura` en `ParametrosSLD.asset` pasa de 8 a **10 mm**, para que la roja entre mientras no se mida la maqueta. Ver el sesgo del 06-11 en [[2026-11-06]]. Las cuentas de C1 de los programas **no se tocan**: siguen en 6/15/26, como en la fábrica (D001). El sesgo se considera un **error del modelo de la planta**, pendiente de medir en la maqueta.
- **Medida:** 10 ciclos rojos con `SLD.py` original → **10/10 capturados**. Descentrado de −3,2 a −9,0 mm; 8 de 10 entre −7,8 y −9,0 mm, y 2 en −3,2/−3,3 mm. Con 8 mm habrían entrado 3 de 10. Datos: [[2026-11-07_roja_tolerancia10_x10.csv]].
- **Lo aprendido al medir:** las dos primeras series dieron fallos falsos (la cinta pasaba de largo hasta +100 mm). La causa eran **mis consultas por la CLI de Unity**, que congelaban el editor (frame de hasta 4,5 s, 140 frames recortados). En la serie buena solo se envió por la CLI la orden de colocar cada pieza y se volcó el CSV al final: peor frame de 69 ms. Es lo mismo que avisa `RegistroPlanta.cs`: medir sin consultar a Unity mientras pasa el ciclo.
- Con 10 mm, la solución del nivel 4 y el ejemplo resuelto llevan a su rampa la blanca, la roja y la azul. El ejemplo, además, clasificó 3 de 3 rojas.

![[2026-11-07_nivel4_tres_piezas_en_su_rampa.png]]
*Solución del nivel 4 con las cuentas de la fábrica (6/15/26) y la tolerancia provisional de 10 mm: blanca, roja y azul, cada una en su rampa.*

### ¿Se puede cargar el Python que genera ROBO Pro Coding? (2026-11-07)
**Lo que hay en el repo.** `ROBO Pro Coding/FactoryMain-DigitalTwin.ft` es un ZIP con un proyecto de ROBO Pro Coding (`mode: PROFESSIONAL`, `controller: TXT4`). Trae `FactoryMain.py` y 40 módulos `lib/*.py`, casi todos con su programa en bloques `.blockly` (XML de Blockly). `SLD.blockly` tiene 250 bloques, 28 de ellos `util_python` con Python escrito a mano. `Digital_Twin.py` e `Influx_Collector.py` no tienen bloques. La configuración del hardware está en bloques, en `lib/controller.blockly`.

**Lo que se sabe seguro.**
- Los `.py` del `.ft` son idénticos a los de `vscode/`, pero llevan los docstrings en español de `e3904f7`, que no están en los bloques. **No son la salida directa de los bloques**: se editaron y se volvieron a meter en el `.ft`.
- Los nombres salen de la configuración: `controller_name` + `_E` + `mode` + puerto + tipo. `TXT_SLD` con `mode 5` da `TXT_SLD_E5_…`. El TXT principal (`mode 0`) se crea con `create_graphical_controller()` sin número y sus nombres terminan en `_M_` (`TXT_SSC_M_…`).

**Lo que se deduce (sin comprobar).**
- Un programa en bloques hecho dentro del proyecto de la fábrica (SLD como extensión 5) debería cargarse tal cual con `run_virtual.py --programa`, como ya pasa con `SLD.py`.
- Un proyecto **nuevo solo para la SLD** probablemente **no**. Su TXT sería el principal: `create_graphical_controller()` sin número y nombres `TXT_SLD_M_…`. La librería virtual solo conecta el TXT 5, así que ese TXT 0 sería «tonto» y la cinta no se movería.

**⚠️ Prueba pendiente (Mario, ~10 min con ROBO Pro Coding).**
1. Crear un proyecto nuevo con un TXT 4.0 configurado como la SLD (I1, I2 color, I3, I6–I8, O5–O8, M1 con encoder, C1) y el bloque del nivel 1: «esperar I1 a oscuras → motor M1 a 512 → arrancar».
2. Exportar el `.ft` y descomprimirlo en una carpeta.
3. `py -3.12 virtual/run_virtual.py --programa <carpeta>/main.py`. El lanzador pone esa carpeta en `sys.path` y se usa su `lib/controller.py`.
4. Anotar: los nombres de `controller.py`, el número de `create_graphical_controller(...)` y si la cinta arranca en Unity.

Si falla como se espera, el arreglo solo toca la librería virtual: «si el programa crea un único TXT, conectarlo a `vio/txt5`». **No está implementado:** se decide con el resultado de la prueba.

## Reparto del trabajo
💻 Claude puede escribir la mayor parte (es repetitivo), explicándolo. Mario revisa y decide el protocolo (F0.3).
