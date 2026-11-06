---
tags: [decisión, git]
---

# D005 · Gestión del repositorio para no pisar al compañero

- **Fecha:** 2026-11-02 · **Estado:** ✅ Aceptada · **Revisada:** 2026-11-05 (punto 9) · **Ampliada:** 2026-11-06 (Reglas para Unity)

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

## Reglas para Unity (2026-11-06)
Valen para **cualquier acceso a Unity**: el editor, la Unity CLI o un servidor MCP, tanto si lo hace Mario como si lo hace el asistente.

1. **Solo se crean o modifican** `Unity/Assets/Scenes/PlantaVirtual.unity` y `Unity/Assets/Scripts/PlantaVirtual/`.
2. **Prohibido modificar** la escena `GemeloDigital_LearningFactory.unity` y los scripts de Adrián. Si hace falta algo de ellos, se **copia** a `PlantaVirtual/` o **se pregunta**.
3. `Packages/manifest.json` y `ProjectSettings/` **solo con permiso explícito de Mario**, y en un **commit aparte**.
4. **Nunca** se cambia la versión de Unity del proyecto: **6000.3.6f1** (`ProjectSettings/ProjectVersion.txt`).
5. **Antes de cada cambio en una escena** se explica qué se cambia y por qué, dónde y cómo comprobarlo ([[D003 Forma de trabajo con Claude]]).

Cómo se aplican:
- Los `.meta` que Unity crea para lo nuevo de la regla 1 entran con ello: `Scripts/PlantaVirtual.meta`, `PlantaVirtual.unity.meta` y los `.meta` de cada script. Sin ellos se rompen las referencias entre archivos.
- Unity reescribe archivos por su cuenta al abrir el proyecto, al guardar o al importar: por ejemplo `ProjectSettings/*.asset`, `Packages/packages-lock.json` o `.meta` de otras carpetas. **Esos cambios no se suben** sin el permiso de la regla 3. Antes de cada commit se revisa `git status -- Unity` y se descarta lo que esté fuera de la regla 1.
- La regla 2 concreta el punto 5. Cuando una copia en `PlantaVirtual/` no basta, sigue valiendo heredar o envolver la clase, o proponerle el cambio a Adrián con un PR.
- La regla 3 se suma al punto 6: además del permiso de Mario, a Adrián se le sigue avisando.

## Por qué
- Si dos personas no tocan los mismos archivos, Git no puede generar conflictos.
- Las escenas de Unity son casi imposibles de mezclar a mano.
- Con merges pequeños y frecuentes, los conflictos también son pequeños.
- **Reglas para Unity.** Con la Unity CLI o un MCP, el asistente puede cambiar escenas y ajustes sin abrir el editor, y un cambio así no se ve hasta el siguiente `git status`. Una lista cerrada de lo que se puede tocar evita romper el gemelo de Adrián por accidente. El commit aparte para `manifest.json` y `ProjectSettings/` permite deshacer ese cambio solo, sin perder el resto. Y la misma versión de Unity evita que el proyecto se reimporte o que se reescriban sus archivos.

## Pendiente
- [ ] Aclarar de quién es la rama `adaptacion-vr` del fork y si alguien más hace push a `origin`.
- [ ] Contarle a Adrián qué carpetas va a tocar Mario.

## Revisiones
- **2026-11-05, punto 9.** Antes decía: "El cerebro (Obsidian) queda fuera del repositorio. Opcional: un repositorio privado aparte para la bóveda." Cambia porque la bóveda se incorpora a la wiki del harness (`02-DOCS/wiki`). Así viaja con el código de la rama `planta-virtual` y el asistente la lee al empezar cada sesión. Original: `02-DOCS/raw/boveda-obsidian/`.

Relacionado: [[D003 Forma de trabajo con Claude]], [[F0 - Prueba de concepto SLD]] (F0.2).
