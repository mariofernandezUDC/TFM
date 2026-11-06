// CintaVirtual.cs · Cinta transportadora virtual, reutilizable en otras estaciones (F0.6).
//
// Arrastra las piezas que tiene encima de forma CINEMÁTICA (decisión de Mario):
//   v = (speed / 512) · vMax, con el signo de dir (CW = hacia el final de la cinta).
// Lleva la cuenta de los mm que ha avanzado (recorrido_mm); de ahí sale C1.
//
// Su posición en el mundo sale de las mallas de la cinta de la escena (sus eslabones): el inicio,
// el eje y la altura de la superficie. Las medidas (longitud, ancho, vMax) las
// recibe de fuera, de ParametrosSLD, para tener los valores en un solo sitio.
//
// En los dos extremos hay un tope (suposición): la pieza no se cae de la cinta,
// se queda parada contra el final.

using System.Collections.Generic;
using UnityEngine;

namespace PlantaVirtual
{
    public class CintaVirtual : MonoBehaviour
    {
        [Tooltip("Objeto de la cinta en la escena (sus eslabones son los hijos). De sus mallas salen el inicio, el eje y la altura.")]
        public Transform objetoCinta;
        [Tooltip("Sentido CW en el mundo: del inicio (entrada) hacia el final. Suposición: +z en la SLD.")]
        public Vector3 eje = Vector3.forward;

        [Header("Calculado al empezar")]
        [SoloLectura] public Vector3 inicioMundo;
        [SoloLectura, Tooltip("Altura (y) de la superficie de la cinta, en m")] public float alturaSuperficie;

        [Header("Estado")]
        [SoloLectura, Tooltip("Velocidad actual, mm/s (negativa = CCW)")] public float velocidad_mm_s;
        [SoloLectura, Tooltip("mm avanzados desde el inicio de Play (con signo)")] public double recorrido_mm;

        private float _longitud_mm, _ancho_mm, _vMax_mm_s;

        public float Longitud_mm => _longitud_mm;
        public float Ancho_mm => _ancho_mm;
        public Vector3 Lateral => Vector3.Cross(Vector3.up, eje).normalized; // +x en la SLD: hacia las rampas

        public void Configurar(float longitud_mm, float ancho_mm, float vMax_mm_s, double recorridoInicial_mm)
        {
            _longitud_mm = longitud_mm;
            _ancho_mm = ancho_mm;
            _vMax_mm_s = vMax_mm_s;
            recorrido_mm = recorridoInicial_mm;
            eje = eje.normalized;

            var mallas = objetoCinta != null ? objetoCinta.GetComponentsInChildren<Renderer>() : new Renderer[0];
            if (mallas.Length == 0)
            {
                Debug.LogError("[CintaVirtual] Falta el objeto de la cinta, o no tiene mallas.");
                return;
            }
            Bounds b = mallas[0].bounds;
            foreach (var r in mallas) b.Encapsulate(r.bounds);
            float mitad = Mathf.Abs(eje.x) * b.extents.x + Mathf.Abs(eje.y) * b.extents.y + Mathf.Abs(eje.z) * b.extents.z;
            inicioMundo = b.center - eje * mitad;
            alturaSuperficie = b.max.y;
        }

        /// <summary>
        /// Avanza la cinta dt segundos con la salida M1 y arrastra las piezas que están EnCinta.
        /// </summary>
        public void Avanzar(float dt, int speed, string dir, bool running, List<Pieza> piezas, float radioPieza_mm)
        {
            float v = running ? Mathf.Clamp(speed, 0, 512) / 512f * _vMax_mm_s : 0f;
            if (dir == "CCW") v = -v;
            velocidad_mm_s = v;
            if (v == 0f) return;

            float ds = v * dt;
            recorrido_mm += ds;

            foreach (var p in piezas)
            {
                if (p == null || p.estado != EstadoPieza.EnCinta || !EstaEncima(p)) continue;
                // Topes en los dos extremos (suposición).
                p.posicion_mm = Mathf.Clamp(p.posicion_mm + ds, 0f, _longitud_mm - radioPieza_mm);
            }
        }

        /// <summary>true si el centro de la pieza cae dentro del rectángulo de la cinta.</summary>
        public bool EstaEncima(Pieza p) =>
            p.posicion_mm >= 0f && p.posicion_mm <= _longitud_mm && Mathf.Abs(p.lateral_mm) <= _ancho_mm * 0.5f;

        /// <summary>Punto del mundo a s mm del inicio, lateral mm a un lado y altura mm sobre la superficie.</summary>
        public Vector3 PuntoMundo(float s_mm, float lateral_mm, float altura_mm)
        {
            Vector3 suelo = new Vector3(inicioMundo.x, alturaSuperficie, inicioMundo.z);
            return suelo + eje * (s_mm * 0.001f) + Lateral * (lateral_mm * 0.001f) + Vector3.up * (altura_mm * 0.001f);
        }

        void OnDrawGizmosSelected()
        {
            if (_longitud_mm <= 0f) return;
            Gizmos.color = Color.yellow;
            Vector3 a = PuntoMundo(0f, 0f, 0.5f), b = PuntoMundo(_longitud_mm, 0f, 0.5f);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawSphere(a, 0.002f);
        }
    }
}
