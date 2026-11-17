# Encargo para Claude Code · Flujo del alumno: del dashboard a VS Code y vuelta (F5.5)

> Repositorio `Github/TFM`, rama `planta-virtual`. Lee antes `02-DOCS/wiki/00 Contexto para Claude.md`, [[D008 Lanzador desde Unity]] y [[D009 Dashboard del alumno]].
> Mismas reglas que en [[Encargo - Lanzador desde Unity]]: un commit por paso, **para después de cada paso**, nunca hagas push, explica qué y por qué → dónde → cómo comprobarlo, y una pregunta ❓ para la defensa. Mientras pasas los tests en modo batch, Mario no tiene Unity abierto (dos Unity a la vez cuelgan el PC).

## Objetivo
Hoy el alumno ve los `.py` en el desplegable, pero nada le dice dónde están ni con qué abrirlos. Lo que se quiere: **el dashboard lleva todo el flujo y el código se escribe en VS Code**:
1. Al abrir, la planta ya enseña algo (el ejemplo elegido y una tarjeta «Empieza aquí»).
2. El alumno elige un reto y pulsa **Empezar reto**: se crea su copia en `Mis programas/`, se abre en VS Code y queda elegida en el desplegable.
3. Edita y guarda en VS Code. Con **Recargar al guardar** activado, la planta se reinicia sola con el programa nuevo.
4. Ve Resultado y Ciclo; si cumple el objetivo, el reto queda **superado ✓**.
5. Botones de rescate: **Abrir en VS Code**, **Abrir carpeta**, **Volver a empezar** (restaura la copia desde el original).

**No se hace un editor de código dentro de Unity:** el flujo real es con un editor (en la maqueta, ROBO Pro Coding o un editor), y el mismo archivo vale en las dos plantas (D001).

---

## P1 · Nota de decisión D010 Flujo del alumno
`02-DOCS/wiki/decisiones/D010 Flujo del alumno.md`, con la plantilla de decisión. Recoge lo de arriba y:
- **Originales de solo lectura, copias en `virtual/alumno/Mis programas/`.** El alumno nunca edita `ejercicios/`. «Volver a empezar» = copiar otra vez el original (pide confirmación).
- **Abrir con VS Code de forma explícita**, nunca con «el programa por defecto»: en Windows, abrir un `.py` por defecto a menudo **lo ejecuta**. Orden: `code` en el PATH → `%LOCALAPPDATA%\Programs\Microsoft VS Code\Code.exe` → Bloc de notas con aviso «Instala VS Code para trabajar mejor». VS Code no se incluye en el paquete (tamaño y licencia).
- **Recargar al guardar:** `FileSystemWatcher` sobre el archivo elegido, con un retardo de ~300 ms (los editores guardan en varios pasos). Desactivado por defecto; casilla en la barra superior.
- **Abierto (pregunta para el tutor, no aplicar todavía):** un `.vscode/settings.json` en `Mis programas/` que desactive por defecto las sugerencias de IA (`editor.inlineSuggest.enabled: false`). Es la respuesta a lo visto el 2026-11-07 (el Tab resuelve el hueco). Añádelo a [[Preguntas para el tutor]].
- Alternativas descartadas: editor dentro de Unity (pobre y mucho trabajo), abrir con el programa por defecto (ejecuta el `.py`).

## P2 · Mis programas y «Empezar reto»
- Carpeta `virtual/alumno/Mis programas/` (en `.gitignore` salvo un `LEEME.md`: lo del alumno no va al repo). Mueve los `mi_*.py` de Mario **solo si él lo confirma**; si no, déjalos donde están.
- La tarjeta de cada reto muestra el enunciado y un botón **Empezar reto**: copia el ejercicio a `Mis programas/` (si ya existe, no lo pisa: lo abre) y lo deja elegido.
- El desplegable separa dos grupos: **Ejemplos y retos** (solo lectura, con un candado) y **Mis programas**.
- **Comprobar:** test de PlayMode: Empezar reto del nivel 1 crea la copia, no toca el original y la deja elegida; repetirlo no pisa la copia.

## P3 · Abrir en VS Code, Abrir carpeta y Volver a empezar
- Botones junto al desplegable. «Abrir en VS Code» abre la **carpeta** `Mis programas` con el archivo delante (`code "<carpeta>" "<archivo>"`), para que el alumno vea sus programas en el explorador de VS Code.
- Si el elegido es de solo lectura (ejemplo o reto), «Abrir en VS Code» propone primero «Empezar reto» en vez de abrir el original.
- **Comprobar:** a mano con Mario (un test no puede ver VS Code); en el test, que se construye bien el comando y que el fallback al Bloc de notas se activa si no hay VS Code.

## P4 · Recargar al guardar
- Casilla **Recargar al guardar** en la barra superior. Al guardar el archivo elegido: aviso en Salida («Guardado · recargando…») y Cargar automático (que ya para el anterior).
- Sin la casilla: el estado muestra «Cambios sin cargar» con un punto ámbar hasta que se pulsa Cargar.
- **Comprobar:** test: con la casilla, modificar el archivo desde el test relanza el programa (PID nuevo) en menos de 2 s; sin ella, aparece «Cambios sin cargar».

## P5 · Primera impresión y reto superado
- Al entrar en Play: el ejemplo elegido y una tarjeta **Empieza aquí** («Pulsa Cargar y mete una pieza con Blanca») que se cierra con una X y no vuelve a salir en esa sesión.
- **Reto superado ✓:** de momento, objetivo sencillo por reto (niveles 1 y 2: la cinta hace lo pedido; niveles 3 y 4: Resultado al 100 % con las tres piezas). Se guarda en un archivo local del alumno, no en el repo.
- **Comprobar:** test con la solución del nivel 4 copiada a Mis programas → reto superado.

## Al terminar
Diario del día, F5 - Modo programador (F5.5), 00 Contexto para Claude, y el guion de la reunión (la demo 4 pasa a «Empezar reto → VS Code → guardar → la planta se recarga»).
