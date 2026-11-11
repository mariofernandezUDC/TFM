---
tags: [decisión, F5]
---

# D009 · Dashboard del alumno

- **Fecha:** 2026-11-11 · **Estado:** ✅ Aceptada (diseño decidido por Mario; se construye en P4a, P4b y P4c del [[Encargo - Lanzador desde Unity]])

## Contexto
[[D008 Lanzador desde Unity]] deja al alumno cargar y parar su programa desde Unity. Falta la pantalla donde lo hace. Tiene que ser **más intuitiva que una consola**: lo que pasa se tiene que ver en la planta, no solo en una lista de texto.

Hay que decidir cinco cosas: qué ocupa el centro de la pantalla, con qué se hace la UI, qué estilo tiene, qué colores y letra usa, y qué programas ve el alumno.

## Opciones
- A. **Planta 3D en el centro** con paneles alrededor, dentro de Unity, hecha con **UI Toolkit** (UXML + USS).
- B. Lo mismo, pero con el **Canvas clásico** (uGUI).
- C. **Panel web aparte** (en el navegador) que habla con la planta por MQTT.
- D. Paneles con **desenfoque real** del fondo (efecto cristal esmerilado).

## Decisión
- **La planta 3D en el centro, en vivo, como protagonista.** Los paneles van alrededor: barra superior, una columna a cada lado y la salida abajo. La cámara se encuadra para que la SLD entera (cinta, 3 cilindros y 3 rampas) quepa en el hueco central sin que ningún panel la tape (`EncuadreCamara.cs`). No se mueve nada de la planta.
- **UI Toolkit** (opción A). Archivos propios en `Unity/Assets/Scripts/PlantaVirtual/UI/`. Nada de la UI de Adrián.
- **Paneles translúcidos sin desenfoque:** fondo oscuro, borde fino claro, radio 16 px, botones en píldora. Cada panel se puede plegar.
- **Acentos lima `#D4F23C`, cian `#3EE6E0` y ámbar `#F5B731`.** **Nunca blanco, rojo ni azul como acento:** son los colores de las piezas. Los errores van en rojo suave `#F09595`, que no se confunde con el rojo de una pieza.
- **Tipografía Outfit** (Google Fonts, licencia OFL). La licencia va junto a la fuente (`UI/Fuentes/OFL-Outfit.txt`).
- **Las soluciones no se le enseñan al alumno.** El desplegable no lista `ejercicios/soluciones/` ni `plantilla_sld.py` (`DashboardPlanta.EsDelProfesor`). Un futuro modo profesor (F4) podrá enseñarlos.

## Por qué
- **Planta en el centro:** el alumno aprende viendo qué hace la máquina con su programa. Si la planta queda pequeña o tapada, vuelve a mirar la consola. En P4b se encienden etiquetas (`I1`, `I3`…) sobre los sensores de la propia planta.
- **UI Toolkit frente al Canvas clásico (B, descartada):**
  - UI Toolkit da esquinas redondas, píldoras y estilos tipo CSS en un archivo `.uss`. Con el Canvas, cada forma redondeada necesita un sprite cortado en 9 partes.
  - La UI de Adrián es de Canvas. Con UI Toolkit no hay ninguna posibilidad de mezclarse con ella ([[D005 Gestión del repositorio]]).
  - El layout es flexible (flexbox): el mismo diseño sirve a 1920×1080 y a 1366×768.
- **Panel web aparte (C, descartada):**
  - El alumno tendría dos ventanas: la planta en una y los controles en otra. La planta dejaría de ser la protagonista.
  - Añade otra tecnología (HTML/JavaScript y MQTT por WebSocket en el broker) y otra pieza más que empaquetar en F6 ([[D004 Paquete autónomo para el alumno]]).
- **Desenfoque real (D, descartada):** necesita una pasada de render extra cada fotograma. Cuesta rendimiento en los portátiles de los alumnos, y UI Toolkit no lo trae hecho. Un fondo oscuro translúcido se lee igual de bien.
- **Acentos lima, cian y ámbar:** si un botón o un sensor encendido fuera rojo o azul, el alumno podría tomarlo por una pieza. Ninguna pieza es lima, cian ni ámbar.
- **Outfit:** es legible a tamaño pequeño y su licencia OFL deja meterla en el paquete del alumno.
- **Soluciones ocultas:** si el alumno ve la solución en el desplegable, el ejercicio pierde su sentido. Es el mismo problema que el autocompletar con IA ([[2026-11-07]]).

## Consecuencias
- **Opacidad en espacio de color lineal:** el proyecto mezcla la UI en espacio lineal. Por eso un alfa de 0,6 se veía casi transparente sobre las piezas rojas y blancas. El panel usa negro con alfa 0,85, que a la vista equivale a un negro al ~60 % (ajuste del 2026-11-11).
- **Encuadre:** vista a 55° hacia abajo desde el lado de los cilindros. Con el ángulo de la vista «Estación SLD» (162°) se miraba a lo largo de la cinta y salía en escorzo. Si se pliega un panel o crece la salida, la cámara se recoloca sola.
- **Salida en 4 líneas:** por defecto solo se ve lo que imprime el programa del alumno y los errores. El log de la librería (fecha, hilo, `#vio.py:NNN`) queda tras «Ver detalle».
- **Escalado:** la UI se escala desde 1920×1080, pero la escala nunca baja de 12/14 ≈ 0,86. La letra más pequeña del USS es de 14 px, así que en pantalla ningún texto baja de 12 px (P4b). A 1366×768 los paneles ocupan algo más y la planta se ve algo más pequeña.
- **Sensores visibles (P4b):** etiquetas `I1`, `I2`, `I3`, `I6`, `I7`, `I8` y `C1` sobre la planta y lista de E/S en el panel. Encendido = cian. Las barreras de rampa siguen encendidas mientras haya piezas en la rampa, igual que en la maqueta.
- **Cómo se cuenta el resultado (P4c):** cada pieza cuenta **una sola vez**, en el momento en que pasa a `EnRampa` (el evento de la pieza al llegar a una rampa). Bien = la rampa en la que cae es la de su color real. **No se cuenta por el estado de I6–I8**: esas barreras siguen tapadas mientras haya piezas en la rampa. El ciclo es el tiempo de planta desde que la pieza aparece en la cinta hasta que llega a la rampa. Al pulsar Cargar las cuentas vuelven a cero.
- **Retos:** niveles 1 y 2 = Fácil (lima), niveles 3 y 4 = Medio (cian). Avería (ámbar) queda preparada para F4; todavía no hay ejercicios de avería. Pinchar un reto elige su programa en el desplegable.
- **«Ciclo por pieza» enseña las 4 últimas piezas.** Con más barras, a 1366×768 la columna derecha llega a la salida.
- **El panel de averías no va en el dashboard del alumno.** Es F4 y lo activa el profesor.

Relacionado: [[D008 Lanzador desde Unity]], [[F5 - Modo programador]], [[Encargo - Lanzador desde Unity]], [[2026-11-11]].
