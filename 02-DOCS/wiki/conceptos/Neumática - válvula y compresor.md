---
tags: [concepto, componente]
---

# Neumática: electroválvula y compresor

- El **compresor** genera presión (o vacío, en la ventosa del VGR).
- La **electroválvula** deja pasar el aire hacia un cilindro o una ventosa.
- API: `on()`, `off()`.
- En la planta hay 3 compresores y 8 válvulas.

## Regla física
El cilindro solo se mueve si **compresor ON y válvula ON**. Esto lo aprende el alumno cuando olvida encender el compresor.

## Modelo virtual
🔧 **Diseño de Mario:**
- ❓ ¿Tiempo de extensión y retracción del cilindro? ¿Lineal o con constante de tiempo?
- ❓ ¿La presión cae justo al apagar el compresor o queda aire en el depósito?
- ❓ El compresor del MPO (TXT 4) alimenta actuadores del TXT 3: dependencia entre controladores. Ver [[MPO - Horno y mesa giratoria]].
- ❓ Ventosa: ¿qué condiciones hacen falta para que "agarre"? (vacío + contacto + tiempo)
