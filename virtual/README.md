# virtual/ · Librería fischertechnik virtual (F0.4)

Hace que `vscode/lib/SLD.py`, **sin cambiar una línea**, funcione en el PC sin la maqueta.
Habla por MQTT con una maqueta falsa, con el protocolo `vio` (ver `02-DOCS/wiki/decisiones/D006 Protocolo vio.md`).

## Qué necesitas
- Python 3.12 (`py -3.12`) con `paho-mqtt` 2.2.0: `py -3.12 -m pip install paho-mqtt==2.2.0`
- Mosquitto en marcha en `localhost:1883`. En PowerShell: `Get-Service mosquitto` tiene que decir `Running`.

## Probar el hito 1 a mano

Abre **dos** ventanas de PowerShell en la carpeta `Github/TFM`.

1. En la ventana 1, arranca la SLD:
   ```powershell
   py -3.12 virtual/run_sld_virtual.py
   ```
   Dirá que espera la primera imagen de entradas. Es normal: todavía no hay maqueta.
2. En la ventana 2, arranca la maqueta:
   ```powershell
   py -3.12 virtual/maqueta_manual.py
   ```
   La ventana 1 dirá "primera imagen de entradas recibida". En la ventana 2 verás llegar `vio/txt5/out`.
3. Clasifica una pieza blanca en la ventana 2:

   | Pulsa | Qué pasa en `vio/txt5/out` |
   |---|---|
   | `1` (I1 oscuro) | `M1` speed 512, running true |
   | `1` otra vez | I1 vuelve a luz (la pieza ha pasado) |
   | `b` (I2 en blanco) | Nada todavía. **Tiene que ser antes de `3`**: el color solo se lee entre I1 e I3 |
   | `3` (I3 oscuro) | `M1` speed 0 con running **true**, `O8` true, y después `M1` speed 350 running true |
   | `c` seis veces | `M1` running false, `O5` true unos 0,5 s y después false, y `O8` false |
   | `3` y `n` | Deja I3 en luz e I2 en "sin pieza" para la siguiente |

4. Para probar la regla de 0,5 s, cierra la maqueta con `q`. En menos de 1 s, la ventana 1 avisa de que la imagen ha caducado y deja de enviar `out`. Vuelve a abrir la maqueta y la SLD sigue sola.
5. Ctrl+C en la ventana 1 para la SLD.

Opciones: `--broker HOST --puerto N` en los dos scripts, `--hz` en la maqueta y `--log DEBUG` en la SLD (enseña los pasos de `SLD.py`).

## Cómo lo usa un alumno: tu propio programa de la SLD

Escribes tu programa en Python con **la misma API que en el TXT real** (`TXT_SLD_E5_I1_photo_transistor.is_dark()`, `TXT_SLD_E5_M1_encodermotor.set_speed()`…) y lo ejecutas contra la planta virtual en vez de `SLD.py`.

1. **Copia la plantilla** con tu nombre. Por ejemplo, en la carpeta `Github/TFM`:
   ```powershell
   Copy-Item virtual/alumno/plantilla_sld.py virtual/alumno/mi_sld.py
   ```
   Al principio tiene la lista de bornes de la SLD: qué es cada uno y qué métodos tiene. Al final hay un bucle mínimo: arranca la cinta cuando I1 se oscurece y la para en I3.
2. **Abre Unity**: `Assets/Scenes/PlantaVirtual.unity` y Play. En la consola debe salir `[vio] Conectado a localhost:1883`.
3. **Ejecuta tu programa**:
   ```powershell
   py -3.12 virtual/run_virtual.py --programa virtual/alumno/mi_sld.py
   ```
4. **Pon una pieza** en Unity: selecciona la SLD y usa los botones del Inspector, o pulsa `1`, `2` o `3` (blanca, roja o azul) en la ventana Game. Verás la cinta obedecer a tu programa.
5. **Para** con Ctrl+C. La cinta se para sola.
6. **Cambia tu programa** y vuelve al paso 3. Unity puede seguir en Play.

Ejemplo resuelto, que clasifica las piezas por color: `py -3.12 virtual/run_virtual.py --programa virtual/alumno/ejemplos/clasificar_por_color.py`.

### Cómo se escribe el programa
- **De arriba abajo**, como un `main.py` de ROBO Pro Coding. Primero `from lib.controller import *` y después tu código. Así son la plantilla y el ejemplo.
- **Alumnos avanzados:** también funciona con una función `main()` y al final `if __name__ == "__main__": main()`. El lanzador ejecuta el archivo como `__main__`, igual que el TXT, así que los dos estilos valen aquí y en la maqueta.
- Si tu programa importa otros archivos tuyos, ponlos en la misma carpeta.

### Si tu programa tiene un error
El lanzador **para la planta enseguida**: motor parado y válvulas y compresor apagados. Después enseña dónde está el error y termina:
```
======================================================================
ERROR en tu programa: mi_sld.py, línea 16
    TXT_SLD_E5_M1_encodermotor.set_sped(350, Motor.CW)
AttributeError: 'EncoderMotorVirtual' object has no attribute 'set_sped'. Did you mean: 'set_speed'?
La planta se ha parado: motor parado y válvulas y compresor apagados.
======================================================================
```
Sin latido, Unity pasa a **estado seguro** a los 0,5 s (D006.5). Lo ves en `VirtualIO_TXT5` → Diagnóstico → Estado Seguro. Pasa lo mismo si el error está en un hilo tuyo, si tu programa termina o si lo paras con Ctrl+C: la cinta nunca se queda en marcha.

### ¿Funciona igual en el TXT real? (D001)
El mismo archivo, sin cambios, debería funcionar en el TXT de la SLD. ⚠️ **Supuesto pendiente de comprobar en la maqueta:** en el TXT, tu `lib/` tiene que tener el mismo `controller.py` de la fábrica (`vscode/lib/controller.py`), que es de donde salen los nombres `TXT_SLD_E5_…`. Si ROBO Pro Coding genera otro `lib/controller.py` para la SLD sola, con otros nombres, habrá que adaptar la plantilla.

> En la prueba con Unity del 2026-11-07, el ejemplo dejó la blanca y la azul en su rampa, pero la roja no: su cilindro no la capturó. Es el sesgo de la roja que ya está apuntado en el diario del 2026-11-06. Es un problema de la planta, no del programa.

## Prueba automática
```powershell
py -3.12 -m unittest discover -s virtual/tests -v
```
- `test_vio.py`: reglas del protocolo sin broker (mensajes inválidos, caducidad, D006.7).
- `test_hito1.py`: lanza `run_sld_virtual.py`, lo maneja con la misma clase que la maqueta y comprueba los pasos de arriba. Si Mosquitto no responde, se salta.
- `test_programa_alumno.py`: lanza `run_virtual.py --programa`. Comprueba que la plantilla arranca y para la cinta y que el ejemplo clasifica una blanca. Con los programas de `tests/programas/`, que fallan a mitad (en el hilo principal o en un hilo del alumno), comprueba que la cinta se para, que deja de llegar `out` en menos de 1 s, que el código de salida es 1 y que el error se ve claro.

## Cómo está hecho

| Archivo | Qué hace |
|---|---|
| `fischertechnik/factories.py` | Lo que llama `vscode/lib/controller.py`: `init_*`, `*_factory.create_*`, `initialized()` |
| `fischertechnik/dispositivos.py` | Los bornes: sensores, válvulas, motores y contadores |
| `fischertechnik/vio.py` | Enlace MQTT del TXT 5: imagen de entradas, caducidad de 0,5 s y latido de 100 ms |
| `fischertechnik/controller/Motor.py` | `Motor.CW` y `Motor.CCW` |
| `fischertechnik/mqtt/` | `Constants.CONTROLLER_ID` y un `FTCloudClient` desactivado |
| `lib/display.py` | `display.set_attr()` imprime. En el TXT lo pone el sistema |
| `run_virtual.py` | Prepara `sys.path`, los niveles de log y el reloj de 1 ms. Sin argumento lanza `thread_SLD`. Con `--programa` ejecuta el programa del alumno y, si falla, para la planta |
| `run_sld_virtual.py` | Lo mismo que `run_virtual.py` sin `--programa`. Se mantiene para los comandos del hito 1 |
| `alumno/plantilla_sld.py`, `alumno/ejemplos/` | Plantilla comentada y ejemplo resuelto para el alumno |
| `maqueta_manual.py` | Maqueta falsa con teclado. Publica `vio/txt5/in` a 100 Hz |
| `medir_vio.py` | Mide desde fuera el ritmo real de `vio/txt5/in` (F0.5). Cumple con una media de 98–102 Hz y ningún hueco de más de 30 ms |

## Probar con Unity en vez de la maqueta (F0.5)
1. En Unity, abre `Assets/Scenes/PlantaVirtual.unity` y dale a Play. En la consola debe salir `[vio] Conectado a localhost:1883`.
2. Arranca la SLD: `py -3.12 virtual/run_sld_virtual.py`.
3. Selecciona el objeto `VirtualIO_TXT5`. En el Inspector cambias las **Entradas**, y ves las **Salidas** y el **Diagnóstico** (Hz de `in`, edad del último `out`, estado seguro).
4. Sigue los mismos pasos de la tabla de arriba: `I1`, `I2` = 675 (blanco), `I3` y seis cambios de `C1`.
5. Para medir los 100 Hz: `py -3.12 virtual/medir_vio.py` (60 s; `--segundos 20` para menos).

- **Solo el TXT 5 está conectado.** Los demás TXT se crean igual, porque `controller.py` crea los 6 al importarse. Son "tontos": las entradas dan `False` o 0 y las salidas no hacen nada.
- **`lib.display` y el namespace package.** `vscode/lib/` no tiene `__init__.py`, y `virtual/lib/` tampoco. Por eso Python junta las dos carpetas en un solo paquete `lib`: `lib.display` sale de `virtual/` y el resto de `vscode/`. **Si algún día se añade `vscode/lib/__init__.py`, esto deja de funcionar.** El plan B es meter el módulo a mano desde el lanzador: `sys.modules["lib.display"] = ...`.
- **Reloj de 1 ms en Windows.** Por defecto Windows despierta los `sleep` cada unos 15,6 ms. La imagen salía a unos 50 Hz en vez de 100, y `SLD.py` perdía cambios de `C1` (medido: 26 de 30). Los dos scripts piden un reloj de 1 ms (`timeBeginPeriod(1)`) mientras funcionan.
- **No se escribe nada en `vscode/`.** El lanzador desactiva los `.pyc` (`sys.dont_write_bytecode`).
- Los valores de I2 de la maqueta (675, 1275, 1700 y 2500 sin pieza) son **valores de prueba, no medidos**. Son el centro de cada rango de los umbrales de `thread_SLD`.
