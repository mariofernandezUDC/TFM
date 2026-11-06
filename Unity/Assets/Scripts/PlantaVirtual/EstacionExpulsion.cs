using System;
using UnityEngine;

namespace PlantaVirtual
{
    [Serializable]
    public class EstacionExpulsion
    {
        public Transform vastago;
        public Renderer placa, lampara, fototransistor;
        public BarreraLuzVirtual barrera = new BarreraLuzVirtual("Rampa");
        [SoloLectura] public float extension_mm;

        private Vector3 reposo;
        private Vector3 destino_mm;
        private bool ordenAnterior;
        private Pieza piezaEnEmpuje;

        public void Configurar(CintaVirtual cinta, ParametrosSLD parametros)
        {
            reposo = vastago.position;
            barrera.Colocar(lampara.bounds.center, fototransistor.bounds.center);
            // El centro descansa sobre la placa. Mover un sensor solo cambia la lectura.
            Vector3 origen = cinta.PuntoMundo(0, 0, 0);
            destino_mm = new Vector3(Vector3.Dot(placa.bounds.center - origen, cinta.eje) * 1000f,
                Vector3.Dot(placa.bounds.center - origen, cinta.Lateral) * 1000f,
                (placa.bounds.max.y - origen.y) * 1000f + parametros.alturaPieza * 0.5f);
        }

        public void Avanzar(float dt, bool orden, ColorPieza salida, float posicion, SLD_Planta planta)
        {
            var parametros = planta.parametros;
            if (orden && !ordenAnterior)
                IntentarCaptura(salida, posicion, planta);
            ordenAnterior = orden;
            float carrera = parametros.carreraCilindro;
            float objetivo = orden ? carrera : 0f;
            float tiempo = orden ? parametros.tiempoExtension : parametros.tiempoRetorno;
            float anterior = extension_mm;
            extension_mm = tiempo > 0f ? Mathf.MoveTowards(anterior, objetivo, carrera * dt / tiempo) : objetivo;
            vastago.position = reposo + planta.cinta.Lateral * (extension_mm * 0.001f);

            if (piezaEnEmpuje == null) return;
            if (orden)
                piezaEnEmpuje.lateral_mm += Mathf.Max(0f, extension_mm - anterior);
            if (piezaEnEmpuje.lateral_mm >= parametros.anchoCinta * 0.5f)
            {
                piezaEnEmpuje.CambiarEstado(EstadoPieza.EnRampa);
                piezaEnEmpuje.resultadoCaptura = ResultadoCaptura.Capturada;
                planta.Evento($"{piezaEnEmpuje.name}: capturada en {salida}, descentrado {piezaEnEmpuje.descentrado_mm:F3} mm");
                piezaEnEmpuje = null;
            }
            else if (!orden || extension_mm >= carrera)
            {
                piezaEnEmpuje.CambiarEstado(EstadoPieza.EnCinta);
                piezaEnEmpuje.resultadoCaptura = ResultadoCaptura.NoCapturada;
                planta.Evento($"{piezaEnEmpuje.name}: empuje insuficiente en {salida}, descentrado {piezaEnEmpuje.descentrado_mm:F3} mm");
                piezaEnEmpuje = null;
            }
        }

        private void IntentarCaptura(ColorPieza salida, float posicion, SLD_Planta planta)
        {
            var parametros = planta.parametros;
            Pieza candidata = null;
            float distancia = float.PositiveInfinity;
            foreach (var p in planta.Piezas)
            {
                if (p == null || p.estado != EstadoPieza.EnCinta || p.posicion_mm < parametros.posI3) continue;
                float d = Mathf.Abs(p.posicion_mm - posicion);
                if (d >= distancia) continue;
                candidata = p;
                distancia = d;
            }
            if (candidata == null) return;
            candidata.cilindroIntentado = salida;
            candidata.descentrado_mm = candidata.posicion_mm - posicion;
            bool fuera = distancia > parametros.toleranciaCaptura;
            // En el modo alternativo solo se permite el empuje si el eje aún cruza la pieza.
            bool contactoAlternativo = parametros.fueraDeTolerancia == PoliticaDescentrado.CapturarSiElEjeCruzaLaPieza
                                       && distancia <= parametros.RadioPieza;
            if (fuera && !contactoAlternativo)
            {
                candidata.resultadoCaptura = ResultadoCaptura.NoCapturada;
                planta.Evento($"{candidata.name}: no capturada en {salida}, descentrado {candidata.descentrado_mm:F3} mm");
                return;
            }
            candidata.resultadoCaptura = ResultadoCaptura.EnCurso;
            candidata.CambiarEstado(EstadoPieza.Empujada);
            piezaEnEmpuje = candidata;
        }

        public void Bajar(Pieza pieza, float avance_mm)
        {
            var actual = new Vector3(pieza.posicion_mm, pieza.lateral_mm, pieza.altura_mm);
            Vector3 siguiente = Vector3.MoveTowards(actual, destino_mm, avance_mm);
            pieza.posicion_mm = siguiente.x;
            pieza.lateral_mm = siguiente.y;
            pieza.altura_mm = siguiente.z;
        }
    }
}
