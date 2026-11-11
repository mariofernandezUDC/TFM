# Encargo para Claude Code · Botón «Cargar programa» en la planta virtual

> Repositorio: `C:\Users\Mario\Desktop\TFM-UNITY\Github\TFM`, rama `planta-virtual`.
> Antes de nada, lee `02-DOCS/wiki/00 Contexto para Claude.md`, `02-DOCS/wiki/diario/2026-11-07.md` y `virtual/run_virtual.py`.

## Reglas (no negociables)
- Sigue [[D003 Forma de trabajo con Claude]] y [[D005 Gestión del repositorio]]. **No pisar a Adrián:** no toques `vscode/` ni la escena original. Trabaja solo en `PlantaVirtual.unity` y en las carpetas propias de la planta virtual (donde ya está `VirtualIO_TXT.cs`).
- **Un commit por paso** (P1, P2…), en `planta-virtual`. **Nunca hagas push**: lo hace Mario.
- Mario es nuevo en Git y quiere entenderlo todo. Para cada paso explícale **qué y por qué → dónde exactamente (archivo, clase, método) → cómo comprobarlo**. Termina cada paso con una pregunta ❓ de comprensión para la defensa.
- **No modifiques `virtual/run_virtual.py`** salvo que sea imprescindible. Si lo es, explica antes por qué. Después de cada paso, los tests tienen que seguir pasando (con Unity fuera de Play): `py -3.12 -m unittest discover -s virtual/tests -v`.
- Si al elegir una opción hay que decidir algo de mecánica o de movimiento, **pregunta a Mario** y no lo decidas tú.

## Objetivo
Hoy, para arrancar, el alumno tiene que tener Mosquitto en marcha, entrar en Play en Unity y escribir `py -3.12 virtual/run_virtual.py --programa <ruta>` en PowerShell. Lo que se quiere es **elegir el programa en un desplegable dentro de Unity, pulsar Cargar y verlo funcionar; pulsar Parar y ver los errores en la propia planta.** Es el prototipo que pidió el tutor.

---

## P1 · Nota de decisión D008
Crea `02-DOCS/wiki/decisiones/D008 Lanzador desde Unity.md`, con el formato de `plantillas/Plantilla - Decisión.md`. Contenido:

- **A. Unity lanza Python como un proceso aparte** (`System.Diagnostics.Process`) que ejecuta `run_virtual.py --programa <ruta>`. Se descarta meter Python dentro de Unity (pythonnet) por dos motivos: rompe el aislamiento de [[D001 Sustituir el hardware, no la lógica]] y vuelve a traer los problemas del GIL ya resueltos (`setswitchinterval`). Interfaz entre los dos: solo `vio` por MQTT, más la salida estándar del proceso. Si el tutor decide PLC, Unity lanzaría otro proceso que hable `vio` y la arquitectura no cambia.
- **B. Broker:** de momento, el servicio Mosquitto en 1883 (como ahora). En P6, `mosquitto.exe` portable lanzado por Unity en un **puerto propio (18830)**, para no chocar con otro Mosquitto que tenga el alumno. Se descartan MQTTnet dentro de Unity (otra DLL; ahora solo está el cliente M2Mqtt) y un broker en Python.
- **C. Elegir el programa:** un desplegable con los `.py` de `virtual/alumno/`, buscados de forma recursiva y excluyendo `pistas/`. En el desplegable se ve la ruta relativa (`ejercicios/nivel1_arrancar.py`). Así no hace falta un diálogo de abrir archivo, que no existe en un ejecutable de Unity sin plugins.
- **D. Parar = matar el proceso.** El latido se corta y Unity pasa a estado seguro en 0,5 s (D006.5). Para más adelante queda una parada limpia con `vio/sim/parar`.
- Enlázala desde `00 Contexto para Claude.md` (Decisiones tomadas) y desde `guia/F5 - Modo programador.md`.

**Commit:** `D008: lanzador de programas desde Unity`.

## P2 · `LanzadorPrograma.cs`, con un programa fijo
- Un `MonoBehaviour` nuevo en la carpeta de scripts de la planta virtual, añadido a `PlantaVirtual.unity`.
- **Ruta de Python:** no lances `py -3.12`, porque `py.exe` abre `python.exe` como proceso hijo y al matar `py` puede quedarse vivo el hijo, con la cinta en marcha. Saca una vez la ruta real con `py -3.12 -c "import sys; print(sys.executable)"` y guárdala en un campo serializado `rutaPython` del Inspector (con ese valor por defecto, o resuelta al arrancar si está vacía).
- Para la raíz del repo usa `Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."))`. En la build se cambiará en F6.
- `ProcessStartInfo`: `UseShellExecute = false`, `RedirectStandardOutput/Error = true`, `CreateNoWindow = true`, `WorkingDirectory` = raíz del repo, y las variables de entorno `PYTHONUNBUFFERED=1` y `PYTHONIOENCODING=utf-8` (los mensajes llevan tildes).
- Los eventos `OutputDataReceived` llegan en otro hilo: mételos en una `ConcurrentQueue<string>` y vacíala en `Update()` con `Debug.Log`. **No toques objetos de Unity desde ese hilo.**
- Para probar: una tecla (p. ej. **L**) lanza `ejercicios/soluciones/nivel4.py`.
- Al salir de Play (`OnDisable` y `OnApplicationQuit`), mata el proceso si sigue vivo, para que no quede ningún Python huérfano.

**Comprobar:** Mosquitto en marcha → Play → **L** → teclas 1, 2, 3 → las tres piezas acaban en su rampa **sin haber abierto PowerShell**. Salir de Play → en el Administrador de tareas no queda ningún `python.exe`.

## P3 · Parar
- Un método `Parar()` (tecla provisional **P**): `Kill()` y espera breve a que termine.
- Si se pulsa Cargar con un programa ya en marcha, primero se para el anterior.
- Al terminar el proceso (`Exited`), registra el código de salida (0 = bien, 1 = error del alumno).

**Comprobar:** con la cinta en marcha, **P** → en `VirtualIO_TXT5` → Diagnóstico, «Estado Seguro» se activa a los ~0,5 s y la cinta se para.

## P4 · Dashboard de la planta (diseño decidido el 2026-11-11)

**Referencia visual:** `02-DOCS/attachments/F5/2026-11-11_referencia_dashboard.png` (estilo, no contenido).
Solo referencia de estilo, de un diseñador externo; no usar en la memoria.
**Idea:** la **planta 3D en el centro**, en vivo, como protagonista; los paneles alrededor, translúcidos. Tiene que ser más intuitivo que la consola: lo que pasa se ve **en la planta**, no solo en una lista.

### Decisiones de diseño (no cambiarlas sin preguntar a Mario)
- **UI Toolkit** (UXML + USS), no el Canvas clásico: esquinas muy redondas, píldoras y estilos tipo CSS. Archivos propios en la carpeta de la planta virtual (`UI/`), nada de la UI de Adrián.
- **Estilo:** fondo de panel oscuro translúcido (≈ blanco al 6 %), borde fino claro (≈ blanco al 16 %), radio 16 px, botones en píldora. **Sin desenfoque real** (cuesta rendimiento en portátiles de alumno).
- **Colores de acento: lima `#D4F23C`, cian `#3EE6E0` y ámbar `#F5B731`.** **Nunca blanco, rojo ni azul como acento**: son los colores de las piezas y el alumno los confundiría.
- **Tipografía Outfit** (Google Fonts, licencia OFL, se puede empaquetar). Guarda la licencia junto a la fuente.
- **La cámara se encuadra** para que la clasificadora quede en el hueco central entre las dos columnas. No se mueve nada de la planta.
- Cada panel se puede **plegar**.

### Disposición
- **Barra superior (píldora):** desplegable de programa (D008-C) · **Cargar** (lima) · **Parar** · estado (Parado / En marcha + tiempo / Error, a partir de `ultimoCodigoSalida`).
- **Columna izquierda:** «Resultado» (porcentaje de acierto grande, barra, contadores bien / mal / en cinta) y «Retos».
- **Centro:** la planta 3D, con píldoras flotantes «1 blanca · 2 roja · 3 azul» para meter piezas con el ratón.
- **Columna derecha:** «Ciclo por pieza» (media y barras redondeadas) y «Sensores y salidas» (lista de E/S del TXT 5 con punto encendido o apagado y su valor).
- **Abajo:** «Salida» (lo que imprime el programa; los errores, en P5).

### Pasos (un commit cada uno; para y espera a Mario después de cada uno)
- **P4a · Esqueleto.** Barra superior funcional (desplegable, Cargar, Parar, estado), salida abajo y encuadre de la cámara. Las columnas con sus tarjetas, pero vacías. Desactiva aquí `MQTT_InterfaceClient` en `PlantaVirtual.unity` (pendiente de D008; la escena es de Mario, no se toca nada de Adrián).
  **Comprobar:** test de PlayMode que use los botones (no las teclas) para cargar `soluciones/nivel4.py` y parar. Y captura de pantalla para Mario con la planta centrada.
- **P4b · Sensores visibles.** Panel «Sensores y salidas» con los datos de `VirtualIO_TXT5` y, **sobre la planta 3D**, una etiqueta (`I1`, `I3`, `I6`…) encima de cada sensor que se ilumina en cian cuando se activa. Es lo que hace la planta intuitiva.
  **Comprobar:** con una pieza en la cinta, las etiquetas se encienden en el orden I1 → I3 → I6/I7/I8.
- **P4c · Resultado, ciclo y retos.** Unity cuenta qué pieza cae en qué rampa (bien o mal) y el tiempo de entrada a rampa de cada una. «Retos» muestra los ejercicios como tarjetas (Fácil = lima, Medio = cian, Avería = ámbar); al pinchar una, se selecciona su programa en el desplegable.
  **Comprobar:** con `soluciones/nivel4.py` y las tres piezas, 3 bien, 0 mal y tiempos parecidos a los del test de P2 (2,7 / 4,3 / 5,3 s).
- Quita las teclas L y P, o déjalas solo en el editor.
- **El panel de averías NO va en P4:** es F4 y lo activa el profesor, no el alumno.

## P5 · Errores visibles
- `run_virtual.py` ya imprime el error entre dos líneas de `=` (función `mostrar_error`). Detecta ese bloque y enséñalo **en rojo** en el panel, con el estado en «Error».
- Si el proceso no llega a arrancar (Python no encontrado, ruta mal), muestra un mensaje claro en el panel, no solo en la consola.
- Los errores van en el panel «Salida» del dashboard de P4, en rojo suave (`#F09595`), sin chocar con el rojo de las piezas.

**Comprobar:** cargar `virtual/tests/programas/falla_a_mitad.py` y `falla_en_hilo.py` → el error sale en rojo con el archivo y la línea, y la planta se para.

> 🏁 **Hito para el tutor:** con P1–P5 ya se puede enseñar «Cargar programa». Captura del panel con un error y otra con las tres piezas clasificadas en `02-DOCS/attachments/F5/`.

## P6 · Broker portable (⚠️ pregunta a Mario antes de empezar)
Supone meter binarios de Mosquitto en el repo (p. ej. en `01-TOOLS/mosquitto/`) y revisar su licencia (EPL-2.0/EDL). **Pide confirmación antes de hacerlo.**
- Unity arranca `mosquitto.exe -p 18830` al entrar en Play y lo para al salir. Si el puerto está ocupado, lo dice en el panel.
- El cliente de `VirtualIO_TXT` y el `--puerto` que se pasa a `run_virtual.py` salen del mismo campo de configuración.

**Comprobar:** con el servicio de Windows parado (`Stop-Service mosquitto`), todo funciona igual.

## Al terminar
- Actualiza `guia/F5 - Modo programador.md` (nuevo apartado F5.4 «Cargar desde Unity»), `00 Contexto para Claude.md` (Estado actual) y el apartado 5 de `proyecto/Reunión tutor - Guion y comandos.md`: la demo pasa a hacerse con el panel y el comando queda como plan B.
- Escribe una nota de diario `diario/2026-11-11.md`.
- Dile a Mario qué commits hay listos para que haga él el push.
