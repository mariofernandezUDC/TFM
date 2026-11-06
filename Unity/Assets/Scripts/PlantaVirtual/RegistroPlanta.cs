// RegistroPlanta.cs · Registro de lo que pasa en la planta virtual, para medir sin molestar (F0.6).
//
// Apunta una línea cada vez que cambia una entrada o una salida del TXT, y los
// eventos (pieza colocada, cinta parada con su descentrado...). Se guarda en
// memoria y se vuelca a un CSV cuando se pide.
//
// Sirve para medir un ciclo entero sin consultar a Unity mientras pasa: cada
// consulta por la CLI congela un frame y altera lo que se mide.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace PlantaVirtual
{
    [Serializable]
    public class RegistroPlanta
    {
        [Tooltip("Apunta los cambios (desactívalo si no hace falta)")] public bool activo = true;
        [SoloLectura] public int lineas;
        [SoloLectura, Tooltip("Último evento apuntado")] public string ultimoEvento = "-";

        private const int MaxLineas = 200000;
        private readonly List<string> _lineas = new List<string>();
        private string _firmaAnterior;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public const string Cabecera =
            "t_s;tipo;I1;I2;I3;I6;I7;I8;C1;M1_speed;M1_dir;M1_running;O5;O6;O7;O8;seguro;detalle";

        public IReadOnlyList<string> Lineas => _lineas;

        public void Limpiar()
        {
            _lineas.Clear();
            _firmaAnterior = null;
            lineas = 0;
            ultimoEvento = "-";
        }

        /// <summary>Apunta una línea "estado" si cambió alguna E/S desde la última vez.</summary>
        public void Muestrear(double t, EntradasTXT5 e, SalidasTXT5 s, bool seguro)
        {
            if (!activo) return;
            string firma = Firma(e, s, seguro);
            if (firma == _firmaAnterior) return;
            _firmaAnterior = firma;
            Anadir(t, "estado", firma, "");
        }

        /// <summary>Apunta un evento con texto libre (sin punto y coma).</summary>
        public void Evento(double t, EntradasTXT5 e, SalidasTXT5 s, bool seguro, string detalle)
        {
            ultimoEvento = $"{t.ToString("F3", Inv)} s · {detalle}";
            if (!activo) return;
            Anadir(t, "evento", Firma(e, s, seguro), detalle.Replace(';', ','));
        }

        public string Volcar(string ruta)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            var sb = new StringBuilder(Cabecera).Append('\n');
            foreach (var l in _lineas) sb.Append(l).Append('\n');
            File.WriteAllText(ruta, sb.ToString(), new UTF8Encoding(false));
            return ruta;
        }

        private void Anadir(double t, string tipo, string firma, string detalle)
        {
            if (_lineas.Count >= MaxLineas) return;
            _lineas.Add($"{t.ToString("F3", Inv)};{tipo};{firma};{detalle}");
            lineas = _lineas.Count;
        }

        private static string Firma(EntradasTXT5 e, SalidasTXT5 s, bool seguro) =>
            $"{B(e.I1)};{e.I2};{B(e.I3)};{B(e.I6)};{B(e.I7)};{B(e.I8)};{B(e.C1)};" +
            $"{s.M1_speed};{s.M1_dir};{B(s.M1_running)};{B(s.O5)};{B(s.O6)};{B(s.O7)};{B(s.O8)};{B(seguro)}";

        private static string B(bool b) => b ? "1" : "0";
    }
}
