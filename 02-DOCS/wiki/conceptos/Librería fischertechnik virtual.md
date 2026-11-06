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
| `run_sld_virtual.py` | Lanza solo `thread_SLD`, con los niveles de log de `Log.py` |
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

## Reparto del trabajo
💻 Claude puede escribir la mayor parte (es repetitivo), explicándolo. Mario revisa y decide el protocolo (F0.3).
