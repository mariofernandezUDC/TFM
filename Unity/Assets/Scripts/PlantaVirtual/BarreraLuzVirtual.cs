// BarreraLuzVirtual.cs · Barrera de luz (fototransistor) virtual, reutilizable (F0.6).
//
// Un haz va del emisor al receptor. Si una Pieza lo corta, la barrera está a
// oscuras: oscura = true, que es lo que lee SLD.py como "hay pieza".
//
// Se mide con un RAYCAST en cada frame (decisión del plan de F0.6), no con un trigger:
// - pregunta justo cuando hace falta, sin Rigidbody y sin depender de los 50 Hz de la física;
// - si se quita una pieza, la barrera vuelve a luz en el frame siguiente
//   (con un trigger, OnTriggerExit no llegaría nunca);
// - no usa capas: filtrar por capa tocaría ProjectSettings/ (regla 3 de D005).
//   Se filtra por el componente Pieza, y el resto de colliders de la escena se ignora.

using System;
using UnityEngine;

namespace PlantaVirtual
{
    [Serializable]
    public class BarreraLuzVirtual
    {
        public string nombre;
        [SoloLectura, Tooltip("true = a oscuras (una pieza corta el haz)")] public bool oscura;
        [SoloLectura] public Vector3 emisor;
        [SoloLectura] public Vector3 receptor;

        private static readonly RaycastHit[] _impactos = new RaycastHit[16];

        public BarreraLuzVirtual(string nombre) { this.nombre = nombre; }

        public void Colocar(Vector3 emisor, Vector3 receptor)
        {
            this.emisor = emisor;
            this.receptor = receptor;
        }

        /// <summary>Lanza el haz y actualiza 'oscura'. Llamar después de Physics.SyncTransforms().</summary>
        public bool Medir()
        {
            Vector3 d = receptor - emisor;
            float dist = d.magnitude;
            oscura = false;
            if (dist <= 0f) return false;
            int n = Physics.RaycastNonAlloc(emisor, d / dist, _impactos, dist,
                                            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                if (_impactos[i].collider != null && _impactos[i].collider.GetComponentInParent<Pieza>() != null)
                {
                    oscura = true;
                    break;
                }
            }
            return oscura;
        }

        public void DibujarGizmo()
        {
            Gizmos.color = oscura ? Color.red : Color.green;
            Gizmos.DrawLine(emisor, receptor);
        }
    }
}
