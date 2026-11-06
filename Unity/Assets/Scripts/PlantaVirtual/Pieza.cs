// Pieza.cs · Una pieza de la planta virtual, con su color real (F0.6, D007).
//
// El color se fija UNA vez, al crearla, y nadie lo puede cambiar después:
// en la planta virtual la pieza no se "pinta" desde fuera, como hacían las
// marionetas. Es el dato que la planta conoce y SLD.py no.
//
// La pieza es una pequeña máquina de estados. No se mueve sola: la mueven la
// cinta, los cilindros y las rampas, que cambian su estado y su posición.
//
//   EnCinta ──(la empuja un cilindro)──▶ Empujada ──(cae en una rampa)──▶ EnRampa
//      Empujada retorna a EnCinta si el empuje termina antes de alcanzar el borde.
//      cualquier estado ──(Quitar todas)──▶ se destruye

using UnityEngine;

namespace PlantaVirtual
{
    public enum ColorPieza { Blanca, Roja, Azul }

    public enum ResultadoCaptura { SinIntento, EnCurso, Capturada, NoCapturada }

    public enum EstadoPieza { EnCinta, Empujada, EnRampa }

    [DisallowMultipleComponent]
    public class Pieza : MonoBehaviour
    {
        [SerializeField, SoloLectura] private ColorPieza colorReal;
        [SoloLectura] public EstadoPieza estado = EstadoPieza.EnCinta;

        [SoloLectura, Tooltip("Centro de la pieza a lo largo de la cinta, en mm desde su inicio")]
        public float posicion_mm;
        [SoloLectura, Tooltip("Centro de la pieza a un lado del eje de la cinta, en mm (+ = hacia las rampas)")]
        public float lateral_mm;
        [SoloLectura, Tooltip("Altura del centro de la pieza sobre la superficie de la cinta, en mm")]
        public float altura_mm;

        [SoloLectura] public ResultadoCaptura resultadoCaptura;
        [SoloLectura] public ColorPieza cilindroIntentado;
        [SoloLectura] public float descentrado_mm;

        private bool _inicializada;

        public ColorPieza ColorReal => colorReal;

        /// <summary>Fija el color real y el aspecto. Solo se puede llamar una vez.</summary>
        public void Inicializar(ColorPieza color, Material materialBase)
        {
            if (_inicializada)
            {
                Debug.LogError($"[Pieza] {name}: el color real no se puede cambiar (D007).");
                return;
            }
            _inicializada = true;
            colorReal = color;

            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null && materialBase != null)
            {
                var m = new Material(materialBase) { name = $"Pieza{color}" };
                Color c = ColorVisible(color);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); // URP
                if (m.HasProperty("_Color")) m.SetColor("_Color", c);
                renderer.material = m;
            }
        }

        /// <summary>Cambia de estado. Solo se permiten los pasos del diagrama de arriba.</summary>
        public bool CambiarEstado(EstadoPieza nuevo)
        {
            bool permitido =
                (estado == EstadoPieza.EnCinta && nuevo == EstadoPieza.Empujada) ||
                (estado == EstadoPieza.Empujada && (nuevo == EstadoPieza.EnRampa || nuevo == EstadoPieza.EnCinta));
            if (!permitido)
            {
                Debug.LogWarning($"[Pieza] {name}: paso no permitido {estado} → {nuevo}.");
                return false;
            }
            estado = nuevo;
            return true;
        }

        public static Color ColorVisible(ColorPieza c) => c switch
        {
            ColorPieza.Blanca => new Color(0.95f, 0.95f, 0.95f),
            ColorPieza.Roja => new Color(0.85f, 0.10f, 0.10f),
            _ => new Color(0.10f, 0.30f, 0.90f),
        };

        void OnDestroy()
        {
            var r = GetComponent<MeshRenderer>();
            if (r != null && _inicializada) Destroy(r.material);
        }
    }
}
