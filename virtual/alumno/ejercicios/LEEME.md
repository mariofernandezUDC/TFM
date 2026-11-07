# Ejercicios de la SLD, paso a paso

Cuatro niveles cortos. Cada uno se resuelve con pocas líneas y trae ya hecho el anterior: si un nivel no te sale, mira su solución y pasa al siguiente.

| Nivel | Archivo | Qué tiene que pasar en la planta | Líneas |
|---|---|---|---|
| 1 | `nivel1_arrancar.py` | La cinta arranca cuando llega una pieza a I1 | ~2 |
| 2 | `nivel2_parar.py` | La cinta se para delante de los cilindros (I3) | ~3 |
| 3 | `nivel3_color.py` | Escribe en pantalla si la pieza es blanca, roja o azul | ~7 |
| 4 | `nivel4_expulsar.py` | La pieza acaba en su rampa | ~10 |

## Cómo se trabaja
1. Abre Unity: `Assets/Scenes/PlantaVirtual.unity` y Play.
2. Abre el nivel y lee el enunciado de arriba. Escribe tu código **solo entre las marcas** `>>>>> TU CÓDIGO AQUÍ` y `FIN DE TU CÓDIGO`. Borra la línea que dice «borra esta línea».
3. Ejecútalo desde la carpeta `Github/TFM`:
   ```powershell
   py -3.12 virtual/run_virtual.py --programa virtual/alumno/ejercicios/nivel1_arrancar.py
   ```
4. En la ventana Game de Unity: tecla **1** pieza blanca, **2** roja, **3** azul y **0** quita todas.
5. Mira la planta. ¿Hace lo que pide el enunciado? Ctrl+C para parar, corrige y vuelve al paso 3.

- Si te equivocas al escribir, el programa se para, **la cinta se para sola** y verás el archivo y la línea del error.
- Si ejecutas un nivel sin rellenar, no se rompe: te avisa de que falta tu código.
- ¿Atascado? Primero `pistas/nivelN.md`. Si sigue sin salir, `soluciones/nivelN.py`.

Lo que escribes aquí es la misma API del TXT 4.0 real. El mismo archivo debería servir en la maqueta, con el `controller.py` de la fábrica (pendiente de comprobar: ver `virtual/README.md`).
