---
tags: [guía, F5]
---

# F5 · Modo programador

> **Meta:** un alumno escribe su propio programa de control de la SLD, con la misma API que en el TXT 4.0 real ([[D001 Sustituir el hardware, no la lógica]]), y lo ve funcionar en la planta virtual.
> Librería: [[Librería fischertechnik virtual]] · Uso paso a paso: `virtual/README.md` · Plan: [[Plan de trabajo]]
> Cargar el programa desde Unity: [[D008 Lanzador desde Unity]] · Dashboard: [[D009 Dashboard del alumno]]

---

## F5.1 · Cargar código · ✅ 2026-11-07 (`724f2b1`)
- **Qué hay:** `virtual/run_virtual.py --programa ruta.py` ejecuta el programa del alumno en lugar de `SLD.py`. Sin argumento sigue lanzando `SLD.py`.
- Si el programa falla, el lanzador para la planta en el acto y enseña el archivo y la línea del error. Unity pasa a estado seguro.
- Plantilla `virtual/alumno/plantilla_sld.py` y ejemplo resuelto `virtual/alumno/ejemplos/clasificar_por_color.py`.

## F5.2 · Ejercicios por niveles para grado · ✅ 2026-11-07
- **Para quién:** alumnos de grado que saben algo de Python, pero se dispersan si el ejercicio es difícil.
- **Dónde:** `virtual/alumno/ejercicios/` (empieza por `LEEME.md`).

| Nivel | Qué tiene que pasar | Líneas |
|---|---|---|
| 1 | La cinta arranca cuando llega una pieza a I1 | ~2 |
| 2 | Se para delante de los cilindros (I3) | ~3 |
| 3 | Escribe si la pieza es blanca, roja o azul | ~7 |
| 4 | La pieza acaba en su rampa | ~10 |

- Cada nivel trae el anterior resuelto, un hueco marcado, una pista (`pistas/`) y la solución aparte (`soluciones/`). Sin hilos ni conceptos avanzados.
- Una plantilla sin rellenar se ejecuta sin errores y avisa de que falta código.
- Las cuentas de C1 del nivel 4 son las de la fábrica (6/15/26). **No se ajustan a la planta virtual** (D001).
- **Cómo comprobarlo:** `py -3.12 -m unittest discover -s virtual/tests -v` (`test_ejercicios.py`). En Unity, la solución del nivel 4 deja las tres piezas en su rampa.

## F5.3 · ¿Programas de ROBO Pro Coding en bloques? · ⏳ prueba pendiente
- **Lo encontrado:** el detalle está en [[Librería fischertechnik virtual]], sección «¿Se puede cargar el Python que genera ROBO Pro Coding?». Resumen:
  - El repo tiene el proyecto `ROBO Pro Coding/FactoryMain-DigitalTwin.ft`, con programas en bloques (`.blockly`) y el Python generado.
  - Dentro del proyecto de la fábrica, el Python generado debería cargarse tal cual.
  - En un proyecto nuevo solo para la SLD, probablemente no, porque la librería virtual solo conecta el TXT 5.
- **🔧 Prueba para Mario (~10 min):**
  1. En ROBO Pro Coding, un proyecto nuevo con un TXT 4.0 configurado como la SLD y el bloque del nivel 1.
  2. Exportar el `.ft` y descomprimirlo.
  3. `py -3.12 virtual/run_virtual.py --programa <carpeta>/main.py`.
  4. Anotar los nombres de `lib/controller.py`, el número de `create_graphical_controller(...)` y si la cinta arranca en Unity.
- **Después:** si falla, decidir el arreglo en la librería virtual («un único TXT → `vio/txt5`»). No está implementado.
- **❓ Comprobación:** ¿por qué un programa hecho en un proyecto solo para la SLD puede no mover la cinta virtual, aunque use los mismos bloques?

## F5.4 · Cargar desde Unity · 🚧 desde 2026-11-11
- **Qué es:** el alumno elige su programa en un desplegable dentro de Unity, pulsa **Cargar** y lo ve funcionar; pulsa **Parar** y la planta pasa a estado seguro. Sin PowerShell. Es el prototipo que pidió el tutor.
- **Decisiones:** [[D008 Lanzador desde Unity]] (Unity lanza `run_virtual.py --programa` como proceso aparte; Parar = matar el proceso) y [[D009 Dashboard del alumno]] (planta en el centro, UI Toolkit, soluciones ocultas al alumno).
- **Pasos:** [[Encargo - Lanzador desde Unity]].

| Paso | Qué | Estado |
|---|---|---|
| P1 | Nota D008 | ✅ `f14c4e8` |
| P2 | `LanzadorPrograma.cs` y test automático | ✅ `752617f` … `027b87a` |
| P3 | Parar, Cargar para el anterior, código de salida | ✅ `27defdc` |
| P4a | Esqueleto del dashboard y ajustes | ✅ `4ced235`, `651ce47` |
| P4b | Sensores visibles en el panel y sobre la planta; texto ≥ 12 px a 1366×768 | ✅ |
| P4c | Resultado, ciclo por pieza y retos (cuenta por pieza, no por I6–I8) | ✅ |
| P4d | Píldoras para meter piezas con el ratón (Blanca, Roja, Azul, Quitar = teclas 1, 2, 3, 0) | ✅ |
| P5 | Errores visibles en rojo (también si no llega a arrancar) | ✅ |
| P6 | Broker portable en el puerto 18830 (pedir confirmación a Mario) | ⏳ |

- **Dónde:** `Unity/Assets/Scripts/PlantaVirtual/LanzadorPrograma.cs` y `UI/` (`DashboardPlanta.cs`, `EncuadreCamara.cs`, `Dashboard.uxml`, `Dashboard.uss`), en la escena `PlantaVirtual.unity`.
- **Cómo comprobarlo:** con Mosquitto en marcha, los tests de PlayMode de `LanzadorProgramaTest` (13 en verde el 2026-11-11). A mano: Play → elegir `ejemplos/clasificar_por_color.py` → Cargar → píldoras Blanca, Roja y Azul (o teclas 1, 2, 3) → las piezas acaban en su rampa → Parar.
- El comando de PowerShell (F5.1) sigue funcionando como plan B.
- Capturas: [[2026-11-11]].
- **❓ Comprobación:** si el programa del alumno entra en un `while True:` sin salida, ¿qué pasa al pulsar Parar y por qué la planta queda segura?

## Pendiente
- [ ] Comprobar en la maqueta que el mismo archivo funciona en el TXT real (supuesto de `lib/controller.py`).
- [ ] Medir en la maqueta el sesgo de la roja y devolver `toleranciaCaptura` a un valor medido. Ahora está en 10 mm, provisional.
- [ ] Monitor de E/S para el alumno ([[Plan de trabajo]]).
