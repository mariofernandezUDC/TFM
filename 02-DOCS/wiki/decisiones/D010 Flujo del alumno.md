---
tags: [decisión, F5]
---

# D010 · Flujo del alumno

- **Fecha:** 2026-11-11 · **Estado:** ✅ Aceptada (flujo decidido por Mario; se construye en P2–P5 del [[Encargo - Flujo del alumno con VS Code]], F5.5). Un punto queda abierto para el tutor (ver «Abierto»).

## Contexto
Con [[D008 Lanzador desde Unity]] y [[D009 Dashboard del alumno]] el alumno ya carga y para su programa desde Unity. Pero solo ve los `.py` en el desplegable: nada le dice **dónde están** ni **con qué abrirlos**.

Hay que decidir cuatro cosas: dónde escribe el código, qué archivos puede tocar, con qué programa se abren y cómo llega a la planta lo que guarda.

## Opciones
- A. **El dashboard lleva el flujo y el código se escribe en VS Code**, sobre copias del alumno en `virtual/alumno/Mis programas/`.
- B. **Editor de código dentro de Unity** (una caja de texto en el dashboard).
- C. **Abrir el `.py` con el programa por defecto de Windows.**
- D. El alumno edita **directamente los ejercicios** de `ejercicios/`.

## Decisión
- **A. El dashboard lleva todo el flujo y el código se escribe en VS Code:**
  1. Al abrir, la planta ya enseña algo: el ejemplo elegido y una tarjeta **«Empieza aquí»**.
  2. El alumno elige un reto y pulsa **Empezar reto**: se crea su copia en `Mis programas/`, se abre en VS Code y queda elegida en el desplegable.
  3. Edita y guarda en VS Code. Con **Recargar al guardar** activado, la planta se reinicia sola con el programa nuevo.
  4. Ve «Resultado» y «Ciclo por pieza». Si cumple el objetivo, el reto queda **superado ✓**.
  5. Botones de rescate: **Abrir en VS Code**, **Abrir carpeta** y **Volver a empezar**.
- **Originales de solo lectura; copias en `virtual/alumno/Mis programas/`.** El alumno nunca edita `ejercicios/`. «Volver a empezar» copia otra vez el original encima de su copia, y antes pide confirmación.
- **Se abre con VS Code de forma explícita, nunca con «el programa por defecto».** Orden de búsqueda:
  1. `code` en el PATH;
  2. `%LOCALAPPDATA%\Programs\Microsoft VS Code\Code.exe`;
  3. Bloc de notas, con el aviso «Instala VS Code para trabajar mejor».
- **VS Code no va en el paquete del alumno** (tamaño y licencia). El alumno lo instala aparte.
- **Recargar al guardar:** un `FileSystemWatcher` vigila el archivo elegido. Espera ~300 ms antes de recargar. Viene **desactivado**; se activa con una casilla en la barra superior.

## Por qué
- **VS Code y no un editor dentro de Unity (B, descartada):** un editor hecho en Unity sería pobre (sin colores de sintaxis, sin errores al escribir, sin explorador de archivos) y daría mucho trabajo. Además, el flujo real es con un editor: en la maqueta, ROBO Pro Coding u otro. **El mismo archivo vale en las dos plantas** ([[D001 Sustituir el hardware, no la lógica]]).
- **No abrir con el programa por defecto (C, descartada):** en Windows, abrir un `.py` con el programa por defecto a menudo **lo ejecuta** en vez de abrirlo para editar. El alumno pulsaría «Abrir» y su programa arrancaría sin la planta.
- **Copias y no los originales (D, descartada):** si el alumno edita `ejercicios/`, rompe el enunciado para la siguiente vez y no puede volver a empezar. Con copias, el original siempre está limpio y «Volver a empezar» es solo copiarlo otra vez.
- **Retardo de ~300 ms al guardar:** los editores guardan en varios pasos (archivo temporal, renombrar, escribir). Sin retardo, la planta recargaría a mitad del guardado, a veces con el archivo vacío o dos veces seguidas.
- **Recargar al guardar desactivado por defecto:** que la planta se reinicie sola puede sorprender al alumno la primera vez. Primero aprende Cargar; la casilla es un atajo.

## Abierto (pregunta para el tutor, no se aplica todavía)
- **Sugerencias de IA en VS Code.** Un `.vscode/settings.json` en `Mis programas/` con `"editor.inlineSuggest.enabled": false` las desactivaría por defecto. Es la respuesta a lo visto el [[2026-11-07]]: al pulsar Tab, la sugerencia escribe el hueco del ejercicio. Apuntada en [[Preguntas para el tutor]].

## Consecuencias
- Hace falta una carpeta `virtual/alumno/Mis programas/`, fuera del repo salvo un `LEEME.md`: lo que escribe el alumno no se sube (P2).
- El desplegable se separa en dos grupos: **Ejemplos y retos** (solo lectura, con candado) y **Mis programas** (P2).
- Si el programa elegido es de solo lectura, «Abrir en VS Code» propone primero «Empezar reto» en vez de abrir el original (P3).
- Sin «Recargar al guardar», el estado muestra «Cambios sin cargar» con un punto ámbar hasta que se pulsa Cargar (P4).
- «Reto superado ✓» se guarda en un archivo local del alumno, no en el repo (P5).
- Lo que se abre en VS Code no se puede comprobar con un test automático: los tests comprueban el comando que se construye y el paso al Bloc de notas; abrir VS Code de verdad se prueba a mano (P3).

Relacionado: [[D001 Sustituir el hardware, no la lógica]], [[D004 Paquete autónomo para el alumno]], [[D008 Lanzador desde Unity]], [[D009 Dashboard del alumno]], [[F5 - Modo programador]], [[Encargo - Flujo del alumno con VS Code]], [[2026-11-07]].
