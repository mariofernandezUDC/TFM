---
tags: [decisión, git]
---

# D005 · Gestión del repositorio para no pisar al compañero

- **Fecha:** 2026-11-02 · **Estado:** ✅ Aceptada · **Revisada:** 2026-11-05 (punto 9)

## Contexto
El repositorio de partida es de Adrián (`upstream`) y él sigue trabajando en él. Mario trabaja en su propio fork (`origin`). Hay que poder traer los avances de Adrián sin conflictos y sin estropear su trabajo. Conceptos en [[Git para principiantes]].

## Decisión
1. **Fork + upstream:** Mario solo hace push a `origin`. **Nunca** a `upstream`.
2. **Ramas:** `main` es una copia exacta de la `main` de Adrián, y no se trabaja en ella. Todo el TFM va en la rama **`planta-virtual`**.
3. **Sincronización frecuente** (como mínimo semanal) con `upstream`: ver [[Rutina Git]].
4. **Carpetas propias:** `Unity/Assets/Scripts/PlantaVirtual/`, `Unity/Assets/Scenes/PlantaVirtual.unity` y `virtual/` (Python).
5. **No se modifica** la escena `GemeloDigital_LearningFactory.unity` ni los scripts de Adrián. Si hace falta algo de ellos:
   - Se hereda o se envuelve la clase desde la carpeta propia, o
   - Se le propone el cambio a Adrián con un **Pull Request** a su repositorio.
6. **Archivos compartidos** (`ProjectSettings/`, `Packages/manifest.json`): se avisa a Adrián antes de cambiarlos.
7. **Misma versión de Unity:** 6000.3.6f1.
8. **Errores en su código** (por ejemplo el umbral rojo/azul de la [[SLD - Clasificadora]]): se le avisa o se abre un *Issue*, no se corrige en nuestra rama.
9. **El cerebro (Obsidian)** vive en `02-DOCS/wiki`, dentro de la rama `planta-virtual`, y viaja con los push de Mario a su fork (`origin`). Nunca llega a `upstream`. La configuración local de Obsidian (`.obsidian/`) no se sube: está en `.gitignore`.

## Por qué
- Si dos personas no tocan los mismos archivos, Git no puede generar conflictos.
- Las escenas de Unity son casi imposibles de mezclar a mano.
- Con merges pequeños y frecuentes, los conflictos también son pequeños.

## Pendiente
- [ ] Aclarar de quién es la rama `adaptacion-vr` del fork y si alguien más hace push a `origin`.
- [ ] Contarle a Adrián qué carpetas va a tocar Mario.

## Revisiones
- **2026-11-05, punto 9.** Antes decía: "El cerebro (Obsidian) queda fuera del repositorio. Opcional: un repositorio privado aparte para la bóveda." Cambia porque la bóveda se incorpora a la wiki del harness (`02-DOCS/wiki`). Así viaja con el código de la rama `planta-virtual` y el asistente la lee al empezar cada sesión. Original: `02-DOCS/raw/boveda-obsidian/`.

Relacionado: [[D003 Forma de trabajo con Claude]], [[F0 - Prueba de concepto SLD]] (F0.2).
