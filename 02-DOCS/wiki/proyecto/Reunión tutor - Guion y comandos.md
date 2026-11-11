---
tags: [proyecto, tutor, guía]
---

# Reunión con el tutor · Guion y comandos

Guion para enseñar al tutor lo hecho en F0 y en F5 (2026-11-07), con todos los comandos en orden. Los comandos son de **PowerShell** y se escriben desde la carpeta `Github/TFM`, salvo que se diga otra cosa.

> **Ensaya la demo entera una vez el día antes, desde el ordenador apagado.** Si algo falla en directo, no lo depures delante del tutor: enseña la captura (`02-DOCS/attachments/F0/`) y sigue.

Relacionado: [[Preguntas para el tutor]], [[F0 - Prueba de concepto SLD]], [[F5 - Modo programador]], [[2026-11-07]].

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
(Si pide permisos, abre PowerShell como administrador).

**Comprobar que Python y los tests están bien (con Unity CERRADO o fuera de Play):**
```powershell
py -3.12 -m unittest discover -s virtual/tests -v
```
Todo `ok`. Si Unity está en Play, los tests con maqueta fallan porque Unity también publica en `vio/txt5/in`.

**Unity:**
1. Unity Hub → proyecto `Unity/` (versión 6000.3.6f1).
2. Abrir la escena `Assets/Scenes/PlantaVirtual.unity` (**no** la de Adrián).
3. No le des a Play todavía.

**Ten abiertos en VS Code:** `vscode/lib/SLD.py`, `virtual/alumno/ejemplos/clasificar_por_color.py`, `virtual/alumno/ejercicios/nivel1_arrancar.py`.

---

## 1. Contexto (1 min, sin comandos)

> «He convertido el gemelo digital de Adrián en una planta virtual: el mismo programa que controla la clasificadora real controla ahora la de Unity, sin cambiar una línea. Lo de Adrián reproduce grabaciones (sombra digital); lo mío cierra el lazo: el programa manda, Unity simula la física y devuelve los sensores (puesta en marcha virtual).»

Ver [[Marioneta vs planta]], [[D001 Sustituir el hardware, no la lógica]].

---

## 2. Demo 1 · El programa de fábrica, sin tocar (D001)

**Demostrar que SLD.py no está modificado:**
```powershell
git log --oneline -3 -- vscode/lib/SLD.py
git diff --stat main -- vscode/
```
- El primero enseña que el último commit que tocó `SLD.py` es de Adrián.
- El segundo no tiene que sacar nada: no hay cambios en `vscode/` respecto a `main`.

**Lanzar:**
1. Unity: **Play**. En la consola: `[vio] Conectado a localhost:1883`.
2. Ventana 1:
   ```powershell
   py -3.12 virtual/run_virtual.py
   ```
   Sin argumento lanza `SLD.py` original.
3. Clic en la ventana **Game** de Unity y pulsa **1** (pieza blanca). Después **3** (azul) y **2** (roja). **0** quita todas.
4. Señala: la cinta arranca sola en I1, el color se lee en I2, para en el cilindro contando C1, el cilindro empuja y la pieza cae a su rampa.
5. Para con `Ctrl+C` en la ventana 1. La cinta se para sola.

**Si pregunta por las entradas y salidas en vivo:** selecciona `VirtualIO_TXT5` en la jerarquía. En el Inspector se ven Entradas, Salidas y Diagnóstico (Hz de `in`, edad del último `out`, estado seguro).

---

## 3. Demo 2 · El programa de un alumno (D002, modo programador)

```powershell
py -3.12 virtual/run_virtual.py --programa virtual/alumno/ejemplos/clasificar_por_color.py
```
1. Enseña el archivo en VS Code: Python de arriba abajo, sin hilos ni clases, con la misma API que el TXT real (`TXT_SLD_E5_I1_photo_transistor.is_dark()`…).
2. En Game: **1** y **3**. Las piezas se clasifican.
3. `Ctrl+C`.

> «El alumno cambia el programa con un solo argumento. Unity puede seguir en Play mientras corrige y vuelve a lanzar.»

---

## 4. Demo 3 · Qué pasa si el alumno se equivoca (seguridad)

**Error a mitad de programa:**
```powershell
py -3.12 virtual/run_virtual.py --programa virtual/tests/programas/falla_a_mitad.py
```
Señala el mensaje: archivo, línea, error y «La planta se ha parado: motor parado y válvulas y compresor apagados». El código de salida es 1.

**Error dentro de un hilo del alumno:**
```powershell
py -3.12 virtual/run_virtual.py --programa virtual/tests/programas/falla_en_hilo.py
```

**Corte brusco de la comunicación (latido, D006):**
1. Lanza el ejemplo otra vez y mete una pieza para que la cinta se mueva.
2. Cierra la ventana de PowerShell de golpe (la X, no `Ctrl+C`).
3. En `VirtualIO_TXT5` → Diagnóstico → **Estado Seguro**: Unity lo activa a los 0,5 s sin latido y para todo.

> «Es la misma lógica que un watchdog de PLC: si se pierde la comunicación, la planta va a estado seguro.»

---

## 5. Demo 4 · Los ejercicios por niveles (F5)

```powershell
py -3.12 virtual/run_virtual.py --programa virtual/alumno/ejercicios/nivel1_arrancar.py
```
1. Sin rellenar, no se rompe: avisa de que falta código.
2. Enseña en VS Code el hueco `>>>>> TU CÓDIGO AQUÍ` … `FIN DE TU CÓDIGO`, y las carpetas `pistas/` y `soluciones/`.
3. Lanza la solución para ver que funciona:
   ```powershell
   py -3.12 virtual/run_virtual.py --programa virtual/alumno/ejercicios/soluciones/nivel4.py
   ```
   En Game: **1**, **2**, **3**. Cada pieza a su rampa.

Los cuatro niveles:

| Nivel | Archivo | Qué tiene que pasar |
|---|---|---|
| 1 | `nivel1_arrancar.py` | La cinta arranca cuando llega una pieza a I1 |
| 2 | `nivel2_parar.py` | Se para delante de los cilindros (I3) |
| 3 | `nivel3_color.py` | Escribe si la pieza es blanca, roja o azul |
| 4 | `nivel4_expulsar.py` | La pieza acaba en su rampa |

**Aquí va la reflexión crítica** (ver [[2026-11-07]]):
> «Lo he probado yo como alumno y he visto dos cosas. Una: arrancarlo es demasiado complejo (Mosquitto, Unity, un comando con la ruta); hay que llegar a un botón "Cargar programa" y un paquete autónomo ([[D004 Paquete autónomo para el alumno]]). Dos: con IA, el autocompletar resuelve el hueco, así que el reto no debería ser escribir código sino diagnosticar y poner en marcha: averías inyectadas, ajustar a una planta distinta y métricas de piezas bien clasificadas y tiempo de ciclo. ¿Qué te parece?»

---

## 6. Si pregunta «¿cómo funciona por dentro?»

```
Programa (SLD.py o del alumno)
   │ API del TXT
Librería fischertechnik virtual  (virtual/fischertechnik/)
   │ MQTT: vio/txt5/out → órdenes · vio/txt5/in ← sensores
Mosquitto (localhost:1883)
   │
Unity · PlantaVirtual (SLD_Planta, CintaVirtual, sensores virtuales)
```

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

- **Sesgo de la roja:** la planta virtual la para unos 8 mm antes de su cilindro. Es un error del **modelo de la planta**, no del programa: en la fábrica real `SLD.py` sí la clasifica. Las cuentas de C1 siguen en 6/15/26. `toleranciaCaptura` subida de 8 a **10 mm, provisional**: con 8 mm entraban 3 de 10 rojas; con 10 mm, 10 de 10. Se cierra midiendo la maqueta real.
- **Windows y el reloj:** `sleep` dura unos 15,6 ms por defecto; se perdían cambios de C1 (26 de 30). Arreglo: `timeBeginPeriod(1)`.
- **GIL de Python:** los bucles de espera de `SLD.py` retrasaban MQTT. Arreglo: `sys.setswitchinterval(0.001)`; retraso p95 de unos 105 ms a unos 5 ms.
- **Sensor rojo de la escena de Adrián desplazado 15 mm:** corregido solo en `PlantaVirtual`.
- **Todos los parámetros mecánicos son PROVISIONALES** (`ParametrosSLD.asset`) hasta medir la maqueta.
- **ROBO Pro Coding:** el `.ft` es un ZIP con bloques (Blockly) y Python. Cargar un programa en bloques está pendiente de probar (10 min, F5.3).

---

## 8. Lo que le pregunto (y apunto la respuesta)

1. **Alcance:** mi idea es extenderlo a **toda la planta** (6 estaciones). ¿Lo confirma? ¿Mínimo exigido?
2. **Alumnos:** asignatura, curso, cuánto Python saben, cuántas horas de práctica.
3. **Cómo programan:** bloques (ROBO Pro Coding), Python o PLC (IEC 61131-3).
4. **Retos:** ¿le parece bien orientarlos a diagnóstico y puesta en marcha en vez de a escribir código?
5. **Maqueta real:** ¿cuándo puedo medirla (velocidad, mm por pulso de C1, distancias, inercia) y probar ROBO Pro Coding?
6. **Plazos:** fecha de entrega y de defensa; cada cuánto quiere ver avances.
7. **Uso de IA:** normativa del máster y cómo declararlo en la memoria ([[D003 Forma de trabajo con Claude]]).
8. Pendientes de [[Preguntas para el tutor]]: VR obligatoria u opcional, redistribuir el código de Fischertechnik en el paquete del alumno.

**Al salir:** apunta las respuestas en el diario del día y convierte cada decisión en una nota nueva en `decisiones/` (D008 en adelante).

---

## 9. Al terminar

- `Ctrl+C` en las ventanas de Python, Stop en Unity.
- Si Unity ha reescrito archivos que no tocaste (fuente de TextMesh Pro, ProjectSettings), descártalos antes de cualquier commit:
  ```powershell
  git status
  ```
