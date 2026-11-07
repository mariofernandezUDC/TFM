---
tags: [guía, F5]
---

# F5 · Modo programador

> **Meta:** un alumno escribe su propio programa de control de la SLD, con la misma API que en el TXT 4.0 real ([[D001 Sustituir el hardware, no la lógica]]), y lo ve funcionar en la planta virtual.
> Librería: [[Librería fischertechnik virtual]] · Uso paso a paso: `virtual/README.md` · Plan: [[Plan de trabajo]]

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

## Pendiente
- [ ] Comprobar en la maqueta que el mismo archivo funciona en el TXT real (supuesto de `lib/controller.py`).
- [ ] Medir en la maqueta el sesgo de la roja y devolver `toleranciaCaptura` a un valor medido. Ahora está en 10 mm, provisional.
- [ ] Monitor de E/S para el alumno ([[Plan de trabajo]]).
