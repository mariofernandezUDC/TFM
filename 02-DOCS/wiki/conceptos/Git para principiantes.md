---
tags: [concepto, git]
---

# Git para principiantes (en lenguaje de ingeniero)

Git es un **control de revisiones de planos**, pero para código. Cada palabra rara tiene su equivalente en una oficina técnica:

| Git | Equivalente en una oficina técnica |
|---|---|
| **Repositorio** | El archivo completo de planos del proyecto, con todo su historial |
| **Commit** | Una **revisión** del plano (Rev A, Rev B…): una foto fija de cómo estaba todo, con un comentario de qué cambió y por qué |
| **Rama** (*branch*) | Una **variante de diseño** que evoluciona por su cuenta sin tocar el diseño base |
| **`main`** | El diseño base aprobado |
| **Merge** | Incorporar a tu variante los cambios hechos en otra |
| **Conflicto** | Dos personas cambiaron **la misma cota del mismo plano** y Git no sabe cuál vale. Hay que decidirlo a mano |
| **Fork** | Tu **copia propia** del archivo de planos de un compañero, en tu cuenta de GitHub |
| **`origin`** | Tu fork en GitHub: `github.com/mariofernandezUDC/TFM` |
| **`upstream`** | El repositorio original de Adrián: `github.com/AdrianRomeroUDC/TFM` |
| **Push** | Subir tus revisiones a GitHub (solo a `origin`, nunca a `upstream`) |
| **Fetch** | *Mirar* qué revisiones nuevas hay en GitHub, sin aplicarlas todavía |
| **Pull** | Fetch + merge: traer las revisiones y aplicarlas |

## El dibujo mental

```
GitHub de Adrián (upstream)  ──fetch──►  tu PC  ──push──►  tu GitHub (origin)
        ▲                                 │
        └──── nunca haces push aquí ──────┘
```

En tu PC:
```
main            ●───●───●──────────●   (copia exacta de la de Adrián)
                         \          \  merge semanal
planta-virtual            ●──●──●────●──●   (tu TFM)
```

## Los 3 "sitios" de un archivo
1. **Modificado:** lo has cambiado, pero Git todavía no lo ha apuntado.
2. **Preparado** (*staged*, con `git add`): lo has marcado para la próxima revisión.
3. **Confirmado** (*committed*, con `git commit`): ya está en el historial.

`git status` te dice en cada momento qué archivo está en qué sitio. **Es el comando más importante: úsalo siempre antes y después de cualquier otra cosa.**

## Particularidad de Unity
- Cada archivo de `Assets/` tiene un **`.meta`** al lado (por ejemplo `SLD_Planta.cs` y `SLD_Planta.cs.meta`). **Siempre se suben juntos.** Si falta el `.meta`, Unity pierde las referencias.
- Las escenas (`.unity`) y los prefabs se pueden comparar como texto, pero mezclarlos es muy difícil. Por eso cada uno trabaja en sus propias escenas. Ver [[D005 Gestión del repositorio]].

Rutina práctica: [[Rutina Git]].
