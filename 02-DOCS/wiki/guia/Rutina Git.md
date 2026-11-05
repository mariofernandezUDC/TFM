---
tags: [guía, git]
---

# Rutina Git (chuleta)

> Conceptos en [[Git para principiantes]] · Reglas en [[D005 Gestión del repositorio]]
> **Dónde se escriben los comandos:** en VS Code abre la carpeta `TFM-UNITY\Github\TFM` y luego *Terminal → New Terminal*. Todos los comandos se escriben ahí.

## 🚦 Semáforo: cuándo parar
Si en la salida de cualquier comando aparece alguna de estas palabras, **no hagas nada más** y pégale la salida a Claude:
`CONFLICT` · `error` · `fatal` · `rejected` · `diverged` · `detached HEAD` · `Your branch is behind`

## ⛔ Nunca
- `git push upstream …`
- `git push --force`
- `git reset --hard` o `git clean` sin preguntar antes
- Guardar cambios en la escena `GemeloDigital_LearningFactory.unity`

---

## A. Preparación (una sola vez)

**A1. Comprobar quién eres para Git**
```bash
git config user.name
git config user.email
```
Tiene que salir tu nombre y tu correo de la UDC. Si no sale nada:
```bash
git config --global user.name "Mario Fernández"
git config --global user.email "mario.fernandez3@udc.es"
```

**A2. Ver dónde estás**
```bash
git status
git branch -a
git remote -v
```
Lo esperado: `On branch main`, `nothing to commit, working tree clean`, y dos remotos, `origin` (tuyo) y `upstream` (Adrián).

**A3. Crear tu rama de trabajo** · ✅ Hecho (la rama `planta-virtual` ya existe en local y en `origin`)
```bash
git checkout -b planta-virtual
git push -u origin planta-virtual
```
Lo esperado: `Switched to a new branch 'planta-virtual'` y, tras el push, `branch 'planta-virtual' set up to track 'origin/planta-virtual'`.

---

## B. Al empezar cada sesión
```bash
git status                 # ¿estoy en planta-virtual y limpio?
git checkout planta-virtual   # solo si no lo estás
git pull                   # por si subiste algo desde otro ordenador
```

## C. Al terminar un paso (un commit por paso)
```bash
git status                       # 1. ¿qué he cambiado?
git add <archivo1> <archivo2>    # 2. marca SOLO lo tuyo (¡con sus .meta!)
git status                       # 3. comprueba que está en verde lo correcto
git commit -m "F0.4: librería virtual mínima para la SLD"
git push                         # 4. súbelo a tu GitHub
```
**Mensaje del commit:** empieza por el paso de la guía (F0.x) y explica *qué* y *por qué* en una línea.

💡 Evita `git add .` (lo añade todo) hasta que tengas soltura: puede colar archivos que no son tuyos.

## D. Traer los avances de Adrián (una vez por semana)
```bash
git status                          # tiene que estar limpio
git fetch upstream                  # mira qué hay nuevo
git checkout main
git merge --ff-only upstream/main   # copia su main en la tuya
git push origin main
git checkout planta-virtual
git merge main                      # integra sus cambios en tu trabajo
git push
```
Si `merge --ff-only` falla o aparece `CONFLICT` → 🚦 para y pregunta.

## E. Proponerle un cambio a Adrián
Se hace desde la web de GitHub (Pull Request). Lo haremos juntos la primera vez.
