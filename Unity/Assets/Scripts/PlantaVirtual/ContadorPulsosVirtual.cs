// ContadorPulsosVirtual.cs · Pulsador contador (mini switch) virtual, reutilizable (F0.6).
//
// El pulsador cambia de estado cada 'mmPorCambio' mm que avanza la cinta:
//   estado = floor(recorrido / mmPorCambio) es impar.
// Entrega un ESTADO, no una cuenta: contar los cambios lo sigue haciendo
// SLD.py, igual que con el hardware real (D006.2).

using System;
using UnityEngine;

namespace PlantaVirtual
{
    [Serializable]
    public class ContadorPulsosVirtual
    {
        public string nombre;
        [SoloLectura] public bool estado;
        [SoloLectura, Tooltip("Cambios de estado desde el inicio de Play (solo para diagnóstico)")] public long cambios;

        private long _tramoAnterior = long.MinValue;

        public ContadorPulsosVirtual(string nombre) { this.nombre = nombre; }

        public bool Medir(double recorrido_mm, float mmPorCambio)
        {
            if (mmPorCambio <= 0f) return estado;
            long tramo = (long)Math.Floor(recorrido_mm / mmPorCambio);
            if (_tramoAnterior != long.MinValue && tramo != _tramoAnterior)
                cambios += Math.Abs(tramo - _tramoAnterior);
            _tramoAnterior = tramo;
            estado = (tramo & 1L) == 1L;
            return estado;
        }
    }
}
