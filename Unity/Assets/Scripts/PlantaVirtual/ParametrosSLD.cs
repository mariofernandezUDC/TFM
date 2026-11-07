// ParametrosSLD.cs · Todos los valores mecánicos de la SLD virtual, en un solo sitio (F0.6).
//
// Se editan en el Inspector del asset ParametrosSLD.asset. Cada valor lleva:
//   - su unidad física (mm, mm/s, s, mV),
//   - la marca "provisional" (true hasta que se mida en la maqueta real, opción C de F0.6),
//   - su origen: "Código" (SLD.py), "Escena" (geometría de PlantaVirtual.unity) o "Suposición".
//
// Las posiciones sobre la cinta son mm desde el INICIO de la cinta (el extremo de
// la entrada, el de I1), medidos sobre su eje. Salen de los centros de malla
// leídos el 2026-11-06 (ver 02-DOCS/wiki/planta/SLD - Clasificadora.md).
// Los valores y la cinemática los decide Mario (D003).

using System;
using UnityEngine;

namespace PlantaVirtual
{
    /// <summary>Un valor mecánico con su unidad, su origen y si es provisional.</summary>
    [Serializable]
    public class Parametro
    {
        public float valor;
        public string unidad;
        public bool provisional = true;
        public string origen;

        public Parametro(float valor, string unidad, string origen)
        {
            this.valor = valor;
            this.unidad = unidad;
            this.origen = origen;
        }

        public static implicit operator float(Parametro p) => p.valor;
    }

    public enum PoliticaDescentrado { PermanecerEnCinta, CapturarSiElEjeCruzaLaPieza }

    [CreateAssetMenu(fileName = "ParametrosSLD", menuName = "Planta virtual/Parámetros SLD")]
    public class ParametrosSLD : ScriptableObject
    {
        [Header("Cinta")]
        public Parametro vMax = new Parametro(100f, "mm/s",
            "Suposición. v = (speed/512)·vMax. A 350 da 68 mm/s: un cambio de C1 cada 88 ms. Límite ≈ 290 mm/s para que cada estado de C1 dure ≥ 30 ms (3 imágenes in).");
        public Parametro longitudCinta = new Parametro(420.8f, "mm",
            "Escena: tamaño z de la malla 9V_kette_mit_raupenbelag (CintaSLD).");
        public Parametro anchoCinta = new Parametro(29.7f, "mm",
            "Escena: tamaño x de la malla de la cinta.");

        [Header("Pieza")]
        public Parametro diametroPieza = new Parametro(25f, "mm",
            "Escena: malla de Assets/Prefabs/PiezaBase.prefab (0,25 × escala 100).");
        public Parametro alturaPieza = new Parametro(12.5f, "mm",
            "Escena: malla de PiezaBase.prefab.");
        public Parametro puntoColocacion = new Parametro(5.1f, "mm",
            "Suposición: centro de la pieza nueva = I1 + 5 mm, para que tape el haz de I1 desde el principio.");

        [Header("Posiciones sobre la cinta (mm desde su inicio)")]
        public Parametro posI1 = new Parametro(0.1f, "mm", "Escena: centro de malla del sensor de entrada (z 2,2938).");
        public Parametro posI2 = new Parametro(120.1f, "mm", "Escena: centro de malla de 9V_128599_Farbsensor_step001 (z 2,4138).");
        public Parametro posI3 = new Parametro(225.1f, "mm", "Escena: centro de malla del sensor de clasificación (z 2,5188).");
        public Parametro posCilindroBlanco = new Parametro(247.6f, "mm", "Escena: centro de malla del cilindro blanco (z 2,5413).");
        public Parametro posCilindroRojo = new Parametro(307.7f, "mm", "Escena: centro de malla del cilindro rojo (z 2,6014).");
        public Parametro posCilindroAzul = new Parametro(367.6f, "mm", "Escena: centro de malla del cilindro azul (z 2,6613).");
        public Parametro alturaHazCinta = new Parametro(7.2f, "mm",
            "Escena: altura de los haces de I1 e I3 sobre la superficie de la cinta (centro de malla y 0,1231 − superficie 0,1159).");
        public Parametro margenHazCinta = new Parametro(10f, "mm",
            "Suposición: el emisor y el receptor quedan a esta distancia fuera de cada borde de la cinta.");

        [Header("Contador C1")]
        public Parametro mmPorCambioC1 = new Parametro(6.0f, "mm",
            "Ajuste por mínimos cuadrados de SLD.py (para en los cambios 6/15/26) a la geometría de la escena. No encaja exacto: 6,68 mm entre blanco y rojo, 5,45 entre rojo y azul (opción A de F0.6).");
        public bool fijarFaseC1 = false;
        public Parametro faseInicialC1 = new Parametro(0f, "mm",
            "Solo para tests repetibles: si fijarFaseC1, el recorrido de C1 empieza aquí al iniciar Play. Si no, la fase es la que quede, como en la planta real.");

        [Header("Cilindros y compresor")]
        public Parametro carreraCilindro = new Parametro(29.6f, "mm",
            "Escena: valores de reposo y estirado calibrados por Adrián en ControladorCilindrosSLD_mqtt (×100).");
        public Parametro tiempoExtension = new Parametro(0.15f, "s",
            "Suposición. Tiene que ser < 0,5 s, el pulso de válvula de eject* en SLD.py.");
        public Parametro tiempoRetorno = new Parametro(0.15f, "s",
            "Suposición: retorno por muelle al cerrar la válvula.");
        public Parametro tiempoPresion = new Parametro(0.2f, "s",
            "Suposición: O8 encendido este tiempo antes de que haya presión. SLD.py lo enciende > 1 s antes de abrir la válvula.");

        [Header("Rampas")]
        [Tooltip("PROVISIONAL · Unidad: opción sin dimensión. Origen: indicación de Mario para F0.6. Por defecto, una pieza fuera de tolerancia queda en la cinta.")]
        public PoliticaDescentrado fueraDeTolerancia;
        [SoloLectura] public string fichaFueraDeTolerancia = "PROVISIONAL | Unidad: sin dimensión | Origen: Mario, F0.6 paso 3.";
        public Parametro velocidadRampa = new Parametro(150f, "mm/s",
            "Suposición: la pieza baja por la rampa a esta velocidad (≈ 0,45 s). Debe llegar a la barrera antes de la comprobación de SLD.py (~1,0 s tras abrir la válvula).");
        // PROVISIONAL: subido de 8 a 10 mm el 2026-11-07 para absorber el sesgo de la roja (−8,4 mm),
        // pendiente de medir en la maqueta real. Decisión de Mario.
        public Parametro toleranciaCaptura = new Parametro(10f, "mm",
            "PROVISIONAL: subido de 8 a 10 mm el 2026-11-07 para absorber el sesgo de la roja (−8,4 mm), pendiente de medir en la maqueta real. Descentrado máximo, a lo largo de la cinta, entre la pieza y su cilindro para que entre en la rampa (opción A de F0.6).");

        [Header("Sensor de color I2")]
        public Parametro tensionBlanca = new Parametro(675f, "mV", "Valor de prueba de D006.7 (no medido). Rango blanco de SLD.py: 200 ≤ v < 1150.");
        public Parametro tensionRoja = new Parametro(1275f, "mV", "Valor de prueba de D006.7 (no medido). Rango rojo: 1150 ≤ v < 1400 (o 1460).");
        public Parametro tensionAzul = new Parametro(1700f, "mV", "Valor de prueba de D006.7 (no medido). Rango azul: umbral ≤ v < 2000.");
        public Parametro tensionSinPieza = new Parametro(2500f, "mV", "Valor de prueba de D006.7 (no medido).");
        public Parametro radioDeteccionColor = new Parametro(12.5f, "mm",
            "Suposición: el sensor ve la pieza si su centro está a menos de esta distancia del punto del sensor.");
        public Parametro ruidoColor = new Parametro(0f, "mV",
            "Suposición: desviación del ruido gaussiano. 0 = sin ruido (por defecto).");

        public float RadioPieza => diametroPieza.valor * 0.5f;
    }
}
