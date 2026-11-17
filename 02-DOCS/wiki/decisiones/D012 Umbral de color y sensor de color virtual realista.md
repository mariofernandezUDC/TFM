---
tags: [decisión, validación, F5]
---

# D012 · Umbral de color y sensor de color virtual realista

- **Fecha:** 2026-11-17 · **Estado:** 📝 **Propuesta** (decide Mario; no se aplica todavía)

## Contexto
En la maqueta real ([[D011 Validación con la planta real]], [[2026-11-17]]):
- FactoryMain separa rojo y azul en **1460**: `set_calib_data_SLD()` (`vscode/lib/SLD.py:155-170`) ignora el archivo de calibración y fija ese valor.
- Los ejercicios del alumno usan **1400**. Es el valor por defecto de `SLD.py`, y los ejercicios dicen que es «el que usa la fábrica».
- `soluciones/nivel4.py` sin cambios: **rojas 0/3** en su rampa (todas a la azul). El mismo programa con 1460: **rojas 3/3**.
- Las rojas reales leen cerca del umbral. Con FactoryMain, 4 de 11 rojas se fueron a la azul por piezas que leían alto.

En la virtual, el sensor de color da un valor fijo por color: `tensionRoja = 1275 mV` (`Unity/Assets/Scripts/PlantaVirtual/ParametrosSLD.cs:100`, «valor de prueba, no medido»). Una roja virtual cae siempre en su rampa con 1400 y con 1460, así que **el alumno nunca ve en la virtual el problema que sí tiene la maqueta**.

## Opciones
- A. **Dejar todo como está.** El programa que funciona en la virtual falla con las rojas en la real.
- B. **Solo el umbral:** los ejercicios pasan a 1460 (o al de la calibración).
- C. **Umbral y sensor realista:** B, y además el sensor virtual da a las rojas valores como los reales (≈ 1400-1460, con ruido).

## Propuesta
**C.**
1. **Los ejercicios usan el umbral de la planta real**, 1460 o el que dé la calibración, no 1400.
   - Archivos con 1400 hoy: `virtual/alumno/ejercicios/nivel3_color.py`, `nivel4_expulsar.py`, `soluciones/nivel3.py`, `soluciones/nivel4.py`, `ejemplos/clasificar_por_color.py`, `plantilla_sld.py`, `mi_sld.py` y `virtual/maqueta_manual.py`.
2. **El sensor de color virtual da valores como los reales para las rojas**: ≈ 1400-1460, con ruido de pieza a pieza (`ParametrosSLD.cs` / `SensorColorVirtual.cs`). Así, un programa con 1400 falla en la virtual con algunas rojas igual que en la maqueta.
   - Los valores de blanca y azul siguen igual hasta medirlos con la consola de ROBO Pro (`Color: …`).

## Por qué
- **Evidencia:** rojas 0/3 con 1400 y 3/3 con 1460 en la maqueta real.
- Es lo que pide el tutor: el mismo código tiene que hacer lo mismo en las dos plantas. Con A, un programa «correcto» en la virtual falla en la real.
- Con C, el alumno aprende algo real: los umbrales se calibran con la planta, no se copian.

## Antes de aplicarla
- [ ] Mario decide (C, B o A).
- [ ] Medir las tensiones reales por color: lanzar FactoryMain desde ROBO Pro Coding y apuntar los `Color: …` en `virtual/validacion/plantillas/real_consola.csv`.
- [ ] Decidir el modelo de ruido (rango y reparto) a partir de esas medidas.
- [ ] Ajustar los tests de los niveles (`virtual/tests/test_ejercicios.py`) al umbral nuevo.

Relacionado: [[D006 Protocolo vio]], [[D011 Validación con la planta real]], [[F5 - Modo programador]].
