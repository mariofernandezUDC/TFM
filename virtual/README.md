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

## Prueba automática
```powershell
py -3.12 -m unittest discover -s virtual/tests -v
```
- `test_vio.py`: reglas del protocolo sin broker (mensajes inválidos, caducidad, D006.7).
- `test_hito1.py`: lanza `run_sld_virtual.py`, lo maneja con la misma clase que la maqueta y comprueba los pasos de arriba. Si Mosquitto no responde, se salta.

## Cómo está hecho

| Archivo | Qué hace |
|---|---|
| `fischertechnik/factories.py` | Lo que llama `vscode/lib/controller.py`: `init_*`, `*_factory.create_*`, `initialized()` |
| `fischertechnik/dispositivos.py` | Los bornes: sensores, válvulas, motores y contadores |
| `fischertechnik/vio.py` | Enlace MQTT del TXT 5: imagen de entradas, caducidad de 0,5 s y latido de 100 ms |
| `fischertechnik/controller/Motor.py` | `Motor.CW` y `Motor.CCW` |
| `fischertechnik/mqtt/` | `Constants.CONTROLLER_ID` y un `FTCloudClient` desactivado |
| `lib/display.py` | `display.set_attr()` imprime. En el TXT lo pone el sistema |
| `run_sld_virtual.py` | Prepara `sys.path` y los niveles de log, y lanza solo `thread_SLD` |
| `maqueta_manual.py` | Maqueta falsa con teclado. Publica `vio/txt5/in` a 100 Hz |

- **Solo el TXT 5 está conectado.** Los demás TXT se crean igual, porque `controller.py` crea los 6 al importarse. Son "tontos": las entradas dan `False` o 0 y las salidas no hacen nada.
- **`lib.display` y el namespace package.** `vscode/lib/` no tiene `__init__.py`, y `virtual/lib/` tampoco. Por eso Python junta las dos carpetas en un solo paquete `lib`: `lib.display` sale de `virtual/` y el resto de `vscode/`. **Si algún día se añade `vscode/lib/__init__.py`, esto deja de funcionar.** El plan B es meter el módulo a mano desde el lanzador: `sys.modules["lib.display"] = ...`.
- **Reloj de 1 ms en Windows.** Por defecto Windows despierta los `sleep` cada unos 15,6 ms. La imagen salía a unos 50 Hz en vez de 100, y `SLD.py` perdía cambios de `C1` (medido: 26 de 30). Los dos scripts piden un reloj de 1 ms (`timeBeginPeriod(1)`) mientras funcionan.
- **No se escribe nada en `vscode/`.** El lanzador desactiva los `.pyc` (`sys.dont_write_bytecode`).
- Los valores de I2 de la maqueta (675, 1275, 1700 y 2500 sin pieza) son **valores de prueba, no medidos**. Son el centro de cada rango de los umbrales de `thread_SLD`.
