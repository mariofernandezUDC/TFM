---
tags: [proyecto, tutor, guía]
---

# Reunión con el tutor · Guion y comandos

Guion para enseñar al tutor lo hecho en F0 y en F5 (actualizado el 2026-11-11). **La demo se hace con el dashboard de Unity** ([[D008 Lanzador desde Unity]], [[D009 Dashboard del alumno]]): elegir el programa, pulsar **Cargar** y verlo en la planta. Los comandos de **PowerShell** quedan como **plan B** de cada demo. Se escriben desde la carpeta `Github/TFM`.

> **Ensaya la demo entera una vez el día antes, desde el ordenador apagado.** Si algo falla en directo, no lo depures delante del tutor: usa el plan B o enseña la captura (`02-DOCS/attachments/F5/`) y sigue.

Relacionado: [[Preguntas para el tutor]], [[F0 - Prueba de concepto SLD]], [[F5 - Modo programador]], [[2026-11-07]], [[2026-11-11]].

---

## 0. Preparar antes de que llegue (5 min)

**Ventana 1 de PowerShell:**
```powershell
cd C:\Users\Mario\Desktop\TFM-UNITY\Github\TFM
Get-Service mosquitto
```
Tiene que decir `Running`. Si no:
```powershell
Start-Service mosquitto
```
(Si pide permisos, abre PowerShell como administrador). Mosquitto sigue haciendo falta: el broker portable es P6, todavía pendiente.

**Comprobar que Python y los tests están bien (con Unity CERRADO o fuera de Play):**
```powershell
py -3.12 -m unittest discover -s virtual/tests -v
```
Todo `ok` (20 tests). Si Unity está en Play, los tests con maqueta fallan porque Unity también publica en `vio/txt5/in`.

**Unity:**
1. Unity Hub → proyecto `Unity/` (versión 6000.3.6f1).
2. Abrir la escena `Assets/Scenes/PlantaVirtual.unity` (**no** la de Adrián).
3. Ventana **Game** a pantalla grande (Maximize On Play) y resolución **1920×1080** o **Full HD**.
4. Pulsa **Play**. En la consola: `[vio] Conectado a localhost:1883`. El dashboard aparece con la planta en el centro.

**Ten abiertos en VS Code:** `vscode/lib/SLD.py`, `virtual/alumno/ejemplos/clasificar_por_color.py`, `virtual/alumno/mi_nivel1.py`.

---

## 1. Contexto (1 min, sin comandos)

> «He convertido el gemelo digital de Adrián en una planta virtual: el mismo programa que controla la clasificadora real controla ahora la de Unity, sin cambiar una línea. Lo de Adrián reproduce grabaciones (sombra digital); lo mío cierra el lazo: el programa manda, Unity simula la física y devuelve los sensores (puesta en marcha virtual). Y el alumno lo usa desde un panel, sin consola.»

Ver [[Marioneta vs planta]], [[D001 Sustituir el hardware, no la lógica]].

**Señala el dashboard** mientras hablas: la planta en el centro; arriba, el desplegable, **Cargar** y **Parar**; a los lados, resultado, retos, ciclo por pieza y sensores; abajo, la salida del programa.

---

## 2. Demo 1 · El programa de fábrica, sin tocar (D001)

Esta demo se hace **por PowerShell a propósito**: `SLD.py` es el programa de fábrica (`vscode/lib/`), no un programa de alumno, y el dashboard solo lista los de `virtual/alumno/`.

**Demostrar que SLD.py no está modificado:**
```powershell
git log --oneline -3 -- vscode/lib/SLD.py
git diff --stat main -- vscode/
```
- El primero enseña que el último commit que tocó `SLD.py` es de Adrián.
- El segundo no tiene que sacar nada: no hay cambios en `vscode/` respecto a `main`.

**Lanzar** (Unity ya en Play):
```powershell
py -3.12 virtual/run_virtual.py
```
1. Sin argumento lanza `SLD.py` original.
2. Clic en la ventana **Game** y pulsa **1** (pieza blanca). Después **3** (azul) y **2** (roja). **0** quita todas.
3. Señala en la planta las etiquetas de los sensores: **I1** se enciende en cian, después **I3**, después la barrera de su rampa (**I6**, **I7** o **I8**). El panel «Sensores y salidas» enseña lo mismo, con el motor **M1**, los cilindros **O5–O7** y el compresor **O8**.
4. «Resultado» y «Ciclo por pieza» cuentan las piezas aunque el programa venga de PowerShell: los cuenta Unity, no el programa.
5. Para con `Ctrl+C` en la ventana 1. La cinta se para sola.

---

## 3. Demo 2 · El programa de un alumno, con el dashboard (D008)

1. En el desplegable de arriba, elige `ejemplos/clasificar_por_color.py`.
2. Pulsa **Cargar**. El estado pasa a «En marcha» con el tiempo.
3. En Game: **1**, **2** y **3**. Cada pieza a su rampa.
4. Señala:
   - **Resultado:** 100 % de acierto, 3 bien, 0 mal.
   - **Ciclo por pieza:** el tiempo de cada pieza de la entrada a la rampa (unos 2,6 / 3,4 / 4,4 s) y la media.
   - **Salida:** solo lo que imprime el programa del alumno. **Ver detalle** enseña el log interno de la librería.
5. Enseña el archivo en VS Code: Python de arriba abajo, sin hilos ni clases, con la misma API que el TXT real (`TXT_SLD_E5_I1_photo_transistor.is_dark()`…).
6. Pulsa **Parar**. En 0,5 s la planta pasa a estado seguro: el motor **M1** se apaga en el panel.

> «El alumno no abre ninguna consola: elige su programa, pulsa Cargar y lo ve. Si lo corrige, vuelve a pulsar Cargar: el anterior se para solo.»

**Plan B (PowerShell):**
```powershell
py -3.12 virtual/run_virtual.py --programa virtual/alumno/ejemplos/clasificar_por_color.py
```
`Ctrl+C` para parar.

---

## 4. Demo 3 · Qué pasa si el alumno se equivoca (seguridad)

**Error a mitad de programa, como lo haría un alumno:**
1. En VS Code, en `virtual/alumno/mi_nivel1.py`, línea 35, cambia `set_speed` por `set_sped`. Guarda.
2. En el dashboard elige `mi_nivel1.py` y pulsa **Cargar**.
3. En Game: **1**. Cuando la pieza llega a I1, el programa falla justo antes de arrancar la cinta.
4. Señala:
   - En **Salida**, en rojo: «ERROR en tu programa: mi_nivel1.py, línea 35», la línea, el error y «La planta se ha parado: motor parado y válvulas y compresor apagados». La salida se amplía sola para que se lea entero.
   - El estado de arriba pasa a **Error** (en ámbar).
   - La cinta está parada.
5. **Deshaz el cambio:** en VS Code, `Ctrl+Z` y guarda. Si no, desde PowerShell:
   ```powershell
   git restore virtual/alumno/mi_nivel1.py
   ```

**Error dentro de un hilo del alumno** (plan B, por PowerShell: el programa de prueba no está en la carpeta del alumno):
```powershell
py -3.12 virtual/run_virtual.py --programa virtual/tests/programas/falla_en_hilo.py
```
Mete una pieza con **1**. El error sale con «(en el hilo vigilar_i3)» y la planta se para igual.

**Corte de la comunicación (latido, D006):**
1. Carga el ejemplo y mete una pieza para que la cinta se mueva.
2. Pulsa **Parar** en mitad del recorrido. Parar mata el proceso de Python sin avisarle.
3. A los **0,50 s** sin latido, Unity pasa a estado seguro: en «Sensores y salidas», **M1** y **O8** se apagan.

> «Es la misma lógica que un watchdog de PLC: si se pierde la comunicación, la planta va a estado seguro. No depende de que el programa del alumno colabore.»

**Plan B (PowerShell):** lanza el ejemplo con el comando de la demo 2 y cierra la ventana de PowerShell de golpe (la X, no `Ctrl+C`). En `VirtualIO_TXT5` → Diagnóstico, **Estado Seguro** se activa a los 0,5 s.

---

## 5. Demo 4 · Los ejercicios por niveles (F5)

1. En la tarjeta **Retos**, pincha **Nivel 1 · Arrancar** (Fácil, lima). El desplegable lo elige solo.
2. Pulsa **Cargar** y mete una pieza con **1**. Sin rellenar no se rompe: en «Salida» avisa de que falta código.
3. Enseña en VS Code el hueco `>>>>> TU CÓDIGO AQUÍ` … `FIN DE TU CÓDIGO`, y las carpetas `pistas/` y `soluciones/`.
4. Señala que **las soluciones no salen en el desplegable** ni en los retos (D009): el alumno no las tiene a un clic. Un futuro modo profesor (F4) podrá enseñarlas.

Los cuatro niveles:

| Nivel | Archivo | Qué tiene que pasar | Reto |
|---|---|---|---|
| 1 | `nivel1_arrancar.py` | La cinta arranca cuando llega una pieza a I1 | Fácil |
| 2 | `nivel2_parar.py` | Se para delante de los cilindros (I3) | Fácil |
| 3 | `nivel3_color.py` | Escribe si la pieza es blanca, roja o azul | Medio |
| 4 | `nivel4_expulsar.py` | La pieza acaba en su rampa | Medio |

**Plan B (PowerShell)**, también para enseñar que la solución del nivel 4 funciona:
```powershell
py -3.12 virtual/run_virtual.py --programa virtual/alumno/ejercicios/nivel1_arrancar.py
py -3.12 virtual/run_virtual.py --programa virtual/alumno/ejercicios/soluciones/nivel4.py
```
Con el segundo, en Game: **1**, **2**, **3**. Cada pieza a su rampa, y el dashboard la cuenta.

**Aquí va la reflexión crítica** (ver [[2026-11-07]]):
> «Lo probé yo como alumno y vi dos cosas. Una: arrancarlo era demasiado complejo (Mosquitto, Unity, un comando con la ruta). Por eso he hecho el dashboard con el botón Cargar; falta el paquete autónomo con su propio broker ([[D004 Paquete autónomo para el alumno]], P6 y F6). Dos: con IA, el autocompletar resuelve el hueco, así que el reto no debería ser escribir código sino diagnosticar y poner en marcha: averías inyectadas, ajustar a una planta distinta y métricas. Las métricas ya están: piezas bien y mal clasificadas y tiempo de ciclo. ¿Qué te parece?»

---

## 6. Si pregunta «¿cómo funciona por dentro?»

```
Dashboard de Unity (Cargar / Parar)
   │ lanza run_virtual.py como proceso aparte (LanzadorPrograma, D008)
Programa (SLD.py o del alumno)
   │ API del TXT
Librería fischertechnik virtual  (virtual/fischertechnik/)
   │ MQTT: vio/txt5/out → órdenes · vio/txt5/in ← sensores
Mosquitto (localhost:1883)
   │
Unity · PlantaVirtual (SLD_Planta, CintaVirtual, sensores virtuales)
```
- **Por qué un proceso aparte y no Python dentro de Unity:** se puede matar aunque el alumno escriba un `while True:` sin salida, y mantiene el aislamiento de D001: el programa solo habla con la planta por E/S.
- Unity lee la salida estándar del proceso para el panel «Salida».

**Ver los mensajes MQTT en vivo** (otra ventana, con la demo en marcha):
```powershell
& "C:\Program Files\mosquitto\mosquitto_sub.exe" -h localhost -t "vio/txt5/#" -v
```
Se ven pasar `in` (100 por segundo) y `out` (cada 100 ms). `Ctrl+C` para salir. Si la ruta de Mosquitto es otra, búscala antes de la reunión.

**Medir el ritmo de `in`:**
```powershell
py -3.12 virtual/medir_vio.py --segundos 20
```
Tiene que dar una media de 98–102 Hz y ningún hueco de más de 30 ms.

**Sin Unity, con la maqueta de teclado** (por si Unity falla en directo). Dos ventanas:
```powershell
# Ventana 1
py -3.12 virtual/run_sld_virtual.py
# Ventana 2
py -3.12 virtual/maqueta_manual.py
```
En la ventana 2: `1`, `1`, `b`, `3`, `c` seis veces → `O5` se abre 0,5 s. `q` para salir. Detalle en `virtual/README.md`.

---

## 7. Datos para tener en la cabeza

- **Tests automáticos:** 12 de PlayMode en Unity (lanzador y dashboard) y 20 de Python. El de la solución del nivel 4 mide **2,7 / 4,3 / 5,3 s** de la entrada a la rampa (blanca, roja, azul) y el estado seguro llega a los **0,50 s** de pulsar Parar.
- **Sesgo de la roja:** la planta virtual la para unos 8–9 mm antes de su cilindro (−8,8 mm en el test del 2026-11-11). Es un error del **modelo de la planta**, no del programa: en la fábrica real `SLD.py` sí la clasifica. Las cuentas de C1 siguen en 6/15/26. `toleranciaCaptura` subida de 8 a **10 mm, provisional**: con 8 mm entraban 3 de 10 rojas; con 10 mm, 10 de 10. Se cierra midiendo la maqueta real.
- **Windows y el reloj:** `sleep` dura unos 15,6 ms por defecto; se perdían cambios de C1 (26 de 30). Arreglo: `timeBeginPeriod(1)`.
- **GIL de Python:** los bucles de espera de `SLD.py` retrasaban MQTT. Arreglo: `sys.setswitchinterval(0.001)`; retraso p95 de unos 105 ms a unos 5 ms.
- **Sensor rojo de la escena de Adrián desplazado 15 mm:** corregido solo en `PlantaVirtual`.
- **Todos los parámetros mecánicos son PROVISIONALES** (`ParametrosSLD.asset`) hasta medir la maqueta.
- **Dashboard (D009):** UI Toolkit; acentos lima, cian y ámbar, nunca blanco, rojo ni azul (los colores de las piezas). Se lee también en un portátil de 1366×768 (ningún texto baja de 12 px).
- **ROBO Pro Coding:** el `.ft` es un ZIP con bloques (Blockly) y Python. Cargar un programa en bloques está pendiente de probar (10 min, F5.3).

---

## 8. Lo que le pregunto (y apunto la respuesta)

1. **Alcance:** mi idea es extenderlo a **toda la planta** (6 estaciones). ¿Lo confirma? ¿Mínimo exigido?
2. **Alumnos:** asignatura, curso, cuánto Python saben, cuántas horas de práctica.
3. **Cómo programan:** bloques (ROBO Pro Coding), Python o PLC (IEC 61131-3). Con PLC, Unity lanzaría otro proceso que hable `vio`; la arquitectura no cambia.
4. **Retos:** ¿le parece bien orientarlos a diagnóstico y puesta en marcha en vez de a escribir código? ¿Qué averías querría inyectar (F4)?
5. **Maqueta real:** ¿cuándo puedo medirla (velocidad, mm por pulso de C1, distancias, inercia) y probar ROBO Pro Coding?
6. **Plazos:** fecha de entrega y de defensa; cada cuánto quiere ver avances.
7. **Uso de IA:** normativa del máster y cómo declararlo en la memoria ([[D003 Forma de trabajo con Claude]]).
8. Pendientes de [[Preguntas para el tutor]]: VR obligatoria u opcional, redistribuir el código de Fischertechnik y Mosquitto (P6) en el paquete del alumno.

**Al salir:** apunta las respuestas en el diario del día y convierte cada decisión en una nota nueva en `decisiones/` (D010 en adelante).

---

## 9. Al terminar

- **Parar** en el dashboard (o `Ctrl+C` en las ventanas de Python) y Stop en Unity.
- Comprueba que `mi_nivel1.py` ha vuelto a como estaba (demo 3).
- Si Unity ha reescrito archivos que no tocaste, descártalos antes de cualquier commit. Cómo reconocerlos: [[Rutina Git]], apartado «Ruido de Unity».
  ```powershell
  git status
  ```
