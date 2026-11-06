// SensorColorVirtual.cs · Sensor de color virtual, reutilizable (F0.6).
//
// Da una tensión (mV) según el color REAL de la pieza que tiene debajo: la
// planta conoce el color, SLD.py solo ve la tensión, como con el sensor real.
// Sin pieza, la tensión de "sin pieza". Ruido gaussiano opcional, apagado por
// defecto (σ = 0). Con ruido, el mínimo que busca SLD.py sale sesgado hacia
// abajo: es un dato para la memoria.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlantaVirtual
{
    [Serializable]
    public class SensorColorVirtual
    {
        public string nombre;
        [SoloLectura, Tooltip("Última tensión medida, mV")] public int tension;
        [SoloLectura] public string vePieza = "-";

        private readonly System.Random _azar = new System.Random();

        public SensorColorVirtual(string nombre) { this.nombre = nombre; }

        /// <summary>
        /// Mide sobre la cinta: la pieza EnCinta más cercana a posicion_mm (eje de la cinta)
        /// cuenta si su centro está a menos de radio_mm.
        /// </summary>
        public int Medir(IReadOnlyList<Pieza> piezas, float posicion_mm, ParametrosSLD p)
        {
            Pieza cerca = null;
            float mejor = float.MaxValue;
            foreach (var pieza in piezas)
            {
                if (pieza == null || pieza.estado != EstadoPieza.EnCinta) continue;
                float ds = pieza.posicion_mm - posicion_mm, dl = pieza.lateral_mm;
                float d = Mathf.Sqrt(ds * ds + dl * dl);
                if (d < p.radioDeteccionColor && d < mejor) { mejor = d; cerca = pieza; }
            }

            float v = cerca == null ? p.tensionSinPieza.valor : cerca.ColorReal switch
            {
                ColorPieza.Blanca => p.tensionBlanca.valor,
                ColorPieza.Roja => p.tensionRoja.valor,
                _ => p.tensionAzul.valor,
            };
            if (p.ruidoColor > 0f) v += p.ruidoColor * Gauss();
            tension = Mathf.Clamp(Mathf.RoundToInt(v), 0, 3000);
            vePieza = cerca == null ? "-" : cerca.name;
            return tension;
        }

        private float Gauss()
        {
            double u1 = 1.0 - _azar.NextDouble(), u2 = _azar.NextDouble();
            return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
        }
    }
}
