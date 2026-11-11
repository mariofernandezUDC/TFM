// LanzadorProgramaTest.cs · Tests de PlayMode del lanzador y del dashboard (D008, P2, P3 y P4a).
//
// Hacen lo mismo que las pruebas a mano, sin tocar Unity. Cada test carga
// PlantaVirtual.unity y llama a los mismos métodos que las teclas:
//   L → LanzadorPrograma.Lanzar()     P → LanzadorPrograma.Parar()
//   1, 2, 3 → SLD_Planta.ColocarPieza(Blanca / Roja / Azul)
//
// P2  Nivel4_ClasificaLasTresPiezas_YNoDejaPythonHuerfano
// P3  Parar_CortaElLatido_YLaCintaSePara
//     Cargar_ConUnProgramaEnMarcha_ParaElAnterior
//     ErrorDelAlumno_TerminaConCodigo1_YLaCintaSePara
// P4a Dashboard_CargarYParar_ConLosBotones (usa los botones del dashboard, no las teclas)
//     Dashboard_Cargar_PoneElRelojA0 (Cargar con otro programa en marcha)
// P4b Dashboard_EtiquetasSeEncienden_EnOrdenI1I3I7 (etiquetas 3D y panel de E/S)
//     Dashboard_Escala_TextoNoBajaDe12px_A1366x768
// P4c Dashboard_Resultado_Nivel4_TresBienCeroMal (cuenta por pieza, no por I6–I8)
//     Dashboard_Reto_AlPincharlo_EligeSuPrograma
// P4d Dashboard_Piezas_ConLasPildoras (Blanca, Roja, Azul y Quitar = teclas 1, 2, 3 y 0)
// P5  Dashboard_ErrorDelAlumno_EnRojoConArchivoYLinea (falla_a_mitad.py y falla_en_hilo.py)
//     Dashboard_ProgramaQueNoExiste_AvisaEnRojo
//
// Necesitan Mosquitto en marcha en el puerto de VirtualIO_TXT5 (1883).
//
// Por qué reflexión: los scripts de la planta están en Assembly-CSharp (sin asmdef)
// y un ensamblado de tests no puede referenciar Assembly-CSharp. Se buscan las
// clases por su nombre, sin cambiar la estructura del proyecto.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace PlantaVirtual.Tests
{
    public class LanzadorProgramaTest
    {
        private const string Escena = "Assets/Scenes/PlantaVirtual.unity";
        private const string Nivel4 = "ejercicios/soluciones/nivel4.py";
        private const string Ejemplo = "ejemplos/clasificar_por_color.py"; // lo que elige el alumno en el dashboard
        private const string FallaAMitad = "../tests/programas/falla_a_mitad.py"; // relativo a virtual/alumno/
        private const string FallaEnHilo = "../tests/programas/falla_en_hilo.py";
        private const float EsperaPython_s = 20f;    // hasta que llega el primer latido de Python
        private const float EsperaPorPieza_s = 60f;  // de la entrada de la cinta a la rampa
        private const float LatidoMaximo_s = 1.0f;   // D006.5: 0,5 s sin out → estado seguro (+ margen)
        private const int CodigoParado = -1;         // LanzadorPrograma.CodigoParadoDesdeUnity
        private const int SinCodigo = int.MinValue;  // LanzadorPrograma.SinCodigo

        // Lo que deja preparado CargarEscena().
        private Component _lanzador, _planta, _io;

        // ── Utilidades de reflexión ──────────────────────────────────────────

        private static Type Tipo(string nombre) =>
            Type.GetType("PlantaVirtual." + nombre + ", Assembly-CSharp", throwOnError: true);

        private static object Leer(object o, string miembro)
        {
            var t = o.GetType();
            var f = t.GetField(miembro, BindingFlags.Public | BindingFlags.Instance);
            if (f != null) return f.GetValue(o);
            var p = t.GetProperty(miembro, BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(p, $"{t.Name} no tiene el miembro público {miembro}");
            return p.GetValue(o);
        }

        private static void Escribir(object o, string campo, object valor) =>
            o.GetType().GetField(campo, BindingFlags.Public | BindingFlags.Instance).SetValue(o, valor);

        private static object Llamar(object o, string metodo, params object[] args) =>
            o.GetType().GetMethod(metodo, BindingFlags.Public | BindingFlags.Instance).Invoke(o, args);

        private int Pid => (int)Leer(_lanzador, "pid");
        private int UltimoCodigo => (int)Leer(_lanzador, "ultimoCodigoSalida");
        private bool EstadoSeguro => (bool)Leer(_io, "estadoSeguro");
        private float VelocidadCinta => (float)Leer(Leer(_planta, "cinta"), "velocidad_mm_s");

        // ── Errores en consola ───────────────────────────────────────────────

        // La escena es una copia de la de Adrián: sus scripts MQTT escriben errores
        // (host CHANGE_ME sin configurar) que no tienen que ver con estos tests. Por
        // defecto el Test Framework falla con CUALQUIER Debug.LogError, así que se
        // desactiva esa regla y se vigilan solo los errores de la planta virtual.
        private static readonly string[] PrefijosPropios = { "[Lanzador]", "[programa]", "[vio]", "[SLD_Planta]", "[Pieza]" };
        private readonly List<string> _erroresPropios = new List<string>();

        private void AlRecibirLog(string mensaje, string pila, LogType tipo)
        {
            if (tipo != LogType.Error && tipo != LogType.Exception) return;
            foreach (var prefijo in PrefijosPropios)
                if (mensaje.StartsWith(prefijo)) { _erroresPropios.Add(mensaje); return; }
        }

        [SetUp]
        public void Preparar()
        {
            LogAssert.ignoreFailingMessages = true;
            _erroresPropios.Clear();
            Application.logMessageReceived += AlRecibirLog;
        }

        [TearDown]
        public void Recoger()
        {
            Application.logMessageReceived -= AlRecibirLog;
        }

        private void SinErroresPropios() =>
            Assert.IsEmpty(_erroresPropios, "Errores de la planta virtual:\n" + string.Join("\n", _erroresPropios));

        // ── Pasos comunes ────────────────────────────────────────────────────

        private IEnumerator CargarEscena()
        {
#if UNITY_EDITOR
            LogAssert.ignoreFailingMessages = true;
            var carga = EditorSceneManager.LoadSceneAsyncInPlayMode(Escena, new LoadSceneParameters(LoadSceneMode.Single));
            while (!carga.isDone) yield return null;
            yield return null; // un frame para que corran los Start()
            LogAssert.ignoreFailingMessages = true;
#else
            Assert.Ignore("Estos tests solo corren en el editor.");
            yield break;
#endif
            _lanzador = Object.FindFirstObjectByType(Tipo("LanzadorPrograma")) as Component;
            _planta = Object.FindFirstObjectByType(Tipo("SLD_Planta")) as Component;
            _io = Object.FindFirstObjectByType(Tipo("VirtualIO_TXT")) as Component;
            Assert.IsNotNull(_lanzador, "No hay LanzadorPrograma en la escena");
            Assert.IsNotNull(_planta, "No hay SLD_Planta en la escena");
            Assert.IsNotNull(_io, "No hay VirtualIO_TXT en la escena");
        }

        /// <summary>Lanza un programa (lo que hace la tecla L) y devuelve su proceso.</summary>
        private Process Lanzar(string programa)
        {
            Escribir(_lanzador, "programa", programa);
            Llamar(_lanzador, "Lanzar");
            int pid = Pid;
            Assert.Greater(pid, 0, "Lanzar() no ha arrancado ningún proceso (mira la consola)");
            Debug.Log($"[Test] {programa} lanzado, PID {pid}");
            return Process.GetProcessById(pid);
        }

        /// <summary>Python está listo cuando su latido saca a Unity del estado seguro (D006.5).</summary>
        private IEnumerator EsperarLatido(Process proceso)
        {
            float t0 = Time.realtimeSinceStartup;
            while (EstadoSeguro)
            {
                SinErroresPropios();
                Assert.IsFalse(proceso.HasExited, $"Python ha terminado antes de tiempo (código {SafeExit(proceso)})");
                Assert.Less(Time.realtimeSinceStartup - t0, EsperaPython_s,
                    "No llega el latido de Python. ¿Está Mosquitto en marcha en el 1883?");
                yield return null;
            }
        }

        private object ColocarPieza(string color)
        {
            object valor = Enum.Parse(Tipo("ColorPieza"), color);
            Assert.IsTrue((bool)Llamar(_planta, "ColocarPieza", valor), $"No se ha podido colocar la pieza {color}");
            object pieza = null;
            foreach (var p in (IEnumerable)Leer(_planta, "Piezas")) pieza = p; // la última es la recién colocada
            return pieza;
        }

        private IEnumerator EsperarCintaEnMarcha(Process proceso)
        {
            float t0 = Time.realtimeSinceStartup;
            while (VelocidadCinta == 0f)
            {
                SinErroresPropios();
                Assert.IsFalse(proceso.HasExited, "Python ha terminado antes de arrancar la cinta");
                Assert.Less(Time.realtimeSinceStartup - t0, 10f, "La cinta no arranca");
                yield return null;
            }
        }

        // ── P2 ───────────────────────────────────────────────────────────────

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator Nivel4_ClasificaLasTresPiezas_YNoDejaPythonHuerfano()
        {
            yield return CargarEscena();
            Process proceso = Lanzar(Nivel4);
            int pid = proceso.Id;
            yield return EsperarLatido(proceso);

            // Blanca, roja y azul: cada una a su rampa.
            foreach (string color in new[] { "Blanca", "Roja", "Azul" })
            {
                object pieza = ColocarPieza(color);
                string nombre = ((Component)pieza).name;
                float t0 = Time.realtimeSinceStartup;
                while (Leer(pieza, "estado").ToString() != "EnRampa")
                {
                    SinErroresPropios();
                    Assert.IsFalse(proceso.HasExited, $"Python ha terminado con la pieza {color} en la cinta (código {SafeExit(proceso)})");
                    Assert.AreNotEqual("NoCapturada", Leer(pieza, "resultadoCaptura").ToString(),
                        $"{nombre} no capturada en {Leer(pieza, "cilindroIntentado")}, descentrado {Leer(pieza, "descentrado_mm")} mm");
                    Assert.Less(Time.realtimeSinceStartup - t0, EsperaPorPieza_s,
                        $"{nombre} no ha llegado a una rampa en {EsperaPorPieza_s} s (posición {Leer(pieza, "posicion_mm")} mm)");
                    yield return null;
                }
                Assert.AreEqual(color, Leer(pieza, "cilindroIntentado").ToString(), $"{nombre} ha caído en otra rampa");
                Debug.Log($"[Test] {nombre} en su rampa en {Time.realtimeSinceStartup - t0:F1} s, " +
                          $"descentrado {Leer(pieza, "descentrado_mm")} mm");
            }

            // Salir de Play: Unity desactiva los objetos y llama a OnDisable(), que es
            // donde LanzadorPrograma mata el proceso. Aquí se fuerza ese mismo camino.
            _lanzador.gameObject.SetActive(false);
            yield return null;

            SinErroresPropios();
            Assert.IsTrue(proceso.HasExited, $"El proceso {pid} sigue vivo después de OnDisable()");
            string tareas = Tasklist(pid);
            StringAssert.DoesNotContain("python", tareas.ToLowerInvariant(), $"tasklist todavía ve el PID {pid}:\n{tareas}");
            Debug.Log($"[Test] PID {pid} terminado. tasklist: {tareas.Trim()}");
        }

        // ── P3 ───────────────────────────────────────────────────────────────

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator Parar_CortaElLatido_YLaCintaSePara()
        {
            yield return CargarEscena();
            Process proceso = Lanzar(Nivel4);
            yield return EsperarLatido(proceso);
            ColocarPieza("Blanca");
            yield return EsperarCintaEnMarcha(proceso);

            // Lo que hace la tecla P.
            float t0 = Time.realtimeSinceStartup;
            Llamar(_lanzador, "Parar");
            Assert.IsTrue(proceso.HasExited, "Parar() ha vuelto y el proceso sigue vivo");
            Assert.AreEqual(-1, Pid, "Después de Parar() no tiene que quedar PID");
            Assert.AreEqual(CodigoParado, UltimoCodigo, "Parar() tiene que apuntar el código de «parado desde Unity»");

            // Sin latido, VirtualIO_TXT pasa a estado seguro (D006.5).
            while (!EstadoSeguro)
            {
                Assert.Less(Time.realtimeSinceStartup - t0, 3f, "Unity no pasa a estado seguro después de Parar()");
                yield return null;
            }
            float tSeguro = Time.realtimeSinceStartup - t0;
            yield return null; // un frame para que la planta aplique el estado seguro
            Debug.Log($"[Test] Estado seguro {tSeguro:F2} s después de Parar(). Cinta a {VelocidadCinta} mm/s.");

            SinErroresPropios();
            Assert.LessOrEqual(tSeguro, LatidoMaximo_s, "El estado seguro ha tardado demasiado");
            Assert.AreEqual(0f, VelocidadCinta, "La cinta sigue en marcha en estado seguro");
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator Cargar_ConUnProgramaEnMarcha_ParaElAnterior()
        {
            yield return CargarEscena();
            Process primero = Lanzar(Nivel4);
            yield return EsperarLatido(primero);

            Process segundo = Lanzar(Nivel4); // segundo Cargar sin haber parado
            Assert.IsTrue(primero.HasExited, $"El primer programa (PID {primero.Id}) sigue vivo");
            Assert.AreNotEqual(primero.Id, segundo.Id, "No se ha lanzado un proceso nuevo");
            Assert.IsFalse(segundo.HasExited, "El segundo programa no está en marcha");
            yield return EsperarLatido(segundo);

            Llamar(_lanzador, "Parar");
            Assert.IsTrue(segundo.HasExited, "El segundo programa sigue vivo después de Parar()");
            SinErroresPropios();
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator ErrorDelAlumno_TerminaConCodigo1_YLaCintaSePara()
        {
            yield return CargarEscena();
            Process proceso = Lanzar(FallaAMitad); // arranca la cinta y en I3 llama a set_sped
            yield return EsperarLatido(proceso);
            ColocarPieza("Blanca");

            float t0 = Time.realtimeSinceStartup;
            while (UltimoCodigo == SinCodigo) // lo apunta Update() cuando llega el evento Exited
            {
                Assert.Less(Time.realtimeSinceStartup - t0, 30f, "El programa con error no termina");
                yield return null;
            }
            Debug.Log($"[Test] falla_a_mitad.py ha terminado con código {UltimoCodigo}");
            Assert.AreEqual(1, UltimoCodigo, "Un error del alumno tiene que terminar con código 1");

            // run_virtual.py para las salidas antes de salir, y sin latido Unity pasa a estado seguro.
            t0 = Time.realtimeSinceStartup;
            while (!EstadoSeguro)
            {
                Assert.Less(Time.realtimeSinceStartup - t0, 3f, "Unity no pasa a estado seguro");
                yield return null;
            }
            yield return null;
            Assert.AreEqual(0f, VelocidadCinta, "La cinta sigue en marcha después del error");
            SinErroresPropios();
        }

        // ── P4a ──────────────────────────────────────────────────────────────

        /// <summary>Pulsa un botón de UI Toolkit como lo haría el alumno (evento de «submit»).</summary>
        private static void Pulsar(Button boton)
        {
            Assert.IsNotNull(boton, "No encuentro el botón en el dashboard");
            using (var e = NavigationSubmitEvent.GetPooled())
            {
                e.target = boton;
                boton.SendEvent(e);
            }
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator Dashboard_CargarYParar_ConLosBotones()
        {
            yield return CargarEscena();
            var dashboard = Object.FindFirstObjectByType(Tipo("DashboardPlanta")) as Component;
            Assert.IsNotNull(dashboard, "No hay DashboardPlanta en la escena");
            VisualElement raiz = dashboard.GetComponent<UIDocument>().rootVisualElement;
            Label estado = raiz.Q<Label>("estadoTexto");
            yield return null;

            // Desplegable: abrir y elegir el ejemplo.
            Pulsar(raiz.Q<Button>("programaSeleccionado"));
            yield return null;
            Assert.IsFalse(raiz.Q("listaProgramas").ClassListContains("lista-programas--oculta"), "La lista de programas no se abre");
            var programas = ((IEnumerable)Leer(dashboard, "Programas")).Cast<string>().ToList();
            Assert.Contains(Ejemplo, programas, "La lista no tiene " + Ejemplo);
            Assert.IsFalse(programas.Any(p => p.Contains("pistas/")), "La lista no debe tener nada de pistas/ (D008-C)");
            Assert.IsFalse(programas.Any(p => p.Contains("soluciones/")), "El alumno no debe ver las soluciones en la lista");
            Assert.IsFalse(programas.Contains("plantilla_sld.py"), "El alumno no debe ver la plantilla en la lista");
            Pulsar(raiz.Q<Button>("programa:" + Ejemplo));
            yield return null;
            Assert.AreEqual(Ejemplo, Leer(dashboard, "programaElegido"));
            Assert.IsTrue(raiz.Q("listaProgramas").ClassListContains("lista-programas--oculta"), "La lista no se cierra al elegir");

            // Cargar.
            Pulsar(raiz.Q<Button>("cargar"));
            yield return null;
            int pid = Pid;
            Assert.Greater(pid, 0, "El botón Cargar no ha lanzado el programa");
            Process proceso = Process.GetProcessById(pid);
            yield return EsperarLatido(proceso);
            StringAssert.StartsWith("En marcha", estado.text);

            // La salida del programa llega a la caja de abajo, y el log de la librería
            // (fecha, [hilo], #vio.py:NNN) queda oculto tras «Ver detalle».
            float t0 = Time.realtimeSinceStartup;
            while (!((string)Leer(dashboard, "TextoSalidaVisible")).Contains("Clasificador por color en marcha"))
            {
                Assert.Less(Time.realtimeSinceStartup - t0, 10f, "La salida del programa no aparece en el dashboard");
                yield return null;
            }
            string visible = (string)Leer(dashboard, "TextoSalidaVisible");
            StringAssert.DoesNotMatch(@"(?m)^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2},\d{3} \[.*\] (DEBUG|INFO|WARNING)", visible,
                "Se ven líneas de log de la librería sin pulsar «Ver detalle»");
            StringAssert.DoesNotContain("Ejecutando tu programa:", visible, "Se ve la línea del lanzador sin pulsar «Ver detalle»");
            StringAssert.Contains("#vio.py:", (string)Leer(dashboard, "TextoSalida"), "El log de la librería no llega al detalle");

            // Parar.
            Pulsar(raiz.Q<Button>("parar"));
            yield return null;
            Assert.IsTrue(proceso.HasExited, "El botón Parar no ha parado el programa");
            Assert.AreEqual("Parado", estado.text);
            t0 = Time.realtimeSinceStartup;
            while (!EstadoSeguro)
            {
                Assert.Less(Time.realtimeSinceStartup - t0, 3f, "Unity no pasa a estado seguro después de Parar");
                yield return null;
            }
            SinErroresPropios();
        }

        /// <summary>
        /// Cargar con un programa ya en marcha pone el reloj del estado a 00:00.
        /// Antes seguía contando: el lanzador pasa de un programa a otro sin dejar
        /// de estar «en marcha» ningún frame.
        /// </summary>
        [UnityTest]
        [Timeout(60000)]
        public IEnumerator Dashboard_Cargar_PoneElRelojA0()
        {
            yield return CargarEscena();
            var dashboard = Object.FindFirstObjectByType(Tipo("DashboardPlanta")) as Component;
            VisualElement raiz = dashboard.GetComponent<UIDocument>().rootVisualElement;
            Label estado = raiz.Q<Label>("estadoTexto");
            yield return null;
            Llamar(dashboard, "Elegir", Ejemplo);

            Pulsar(raiz.Q<Button>("cargar"));
            yield return null;
            Process primero = Process.GetProcessById(Pid);
            yield return EsperarLatido(primero);
            float t0 = Time.realtimeSinceStartup;
            while (estado.text != "En marcha · 00:02")
            {
                Assert.Less(Time.realtimeSinceStartup - t0, 10f, $"El reloj no llega a 00:02 (marca «{estado.text}»)");
                yield return null;
            }

            Pulsar(raiz.Q<Button>("cargar"));
            yield return null;
            Assert.AreNotEqual(primero.Id, Pid, "Cargar no ha lanzado un programa nuevo");
            Assert.AreEqual("En marcha · 00:00", estado.text, "Al pulsar Cargar el reloj no vuelve a 00:00");

            Pulsar(raiz.Q<Button>("parar"));
            yield return null;
            Assert.AreEqual("Parado", estado.text);
            SinErroresPropios();
        }

        // ── P4b ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Con nivel4 en marcha y una pieza roja, las etiquetas de la planta se encienden
        /// por primera vez en el orden I1 → I3 → I7, y la fila I7 del panel también.
        /// </summary>
        [UnityTest]
        [Timeout(120000)]
        public IEnumerator Dashboard_EtiquetasSeEncienden_EnOrdenI1I3I7()
        {
            yield return CargarEscena();
            var sensores = Object.FindFirstObjectByType(Tipo("SensoresDashboard")) as Component;
            Assert.IsNotNull(sensores, "No hay SensoresDashboard en la escena");
            bool Encendida(string codigo) => (bool)Llamar(sensores, "EtiquetaEncendida", codigo);
            bool FilaEncendida(string codigo) => (bool)Llamar(sensores, "FilaEncendida", codigo);

            Process proceso = Lanzar(Nivel4);
            yield return EsperarLatido(proceso);
            yield return null;
            foreach (string c in new[] { "I1", "I3", "I6", "I7", "I8" })
                Assert.IsFalse(Encendida(c), $"La etiqueta {c} está encendida sin piezas en la cinta");

            object pieza = ColocarPieza("Roja");
            var orden = new List<string>();
            bool filaI7 = false;
            float t0 = Time.realtimeSinceStartup;
            while (Leer(pieza, "estado").ToString() != "EnRampa" || !orden.Contains("I7"))
            {
                foreach (string c in new[] { "I1", "I3", "I6", "I7", "I8" })
                    if (!orden.Contains(c) && Encendida(c)) orden.Add(c);
                filaI7 |= FilaEncendida("I7");
                SinErroresPropios();
                Assert.Less(Time.realtimeSinceStartup - t0, EsperaPorPieza_s, "La pieza roja no ha llegado a su rampa. Orden visto: " + string.Join(" → ", orden));
                yield return null;
            }
            Debug.Log("[Test] Etiquetas encendidas en orden: " + string.Join(" → ", orden));
            CollectionAssert.AreEqual(new[] { "I1", "I3", "I7" }, orden, "Orden de encendido de las etiquetas");
            Assert.IsTrue(filaI7, "La fila I7 del panel «Sensores y salidas» no se ha encendido");
            SinErroresPropios();
        }

        /// <summary>A 1366×768 la escala del dashboard no deja ningún texto por debajo de 12 px.</summary>
        [Test]
        public void Dashboard_Escala_TextoNoBajaDe12px_A1366x768()
        {
            var calcular = Tipo("DashboardPlanta").GetMethod("CalcularEscala", BindingFlags.Public | BindingFlags.Static);
            var referencia = new Vector2(1920, 1080);
            float e1366 = (float)calcular.Invoke(null, new object[] { 1366f, 768f, referencia, 12f, 14f });
            float e1920 = (float)calcular.Invoke(null, new object[] { 1920f, 1080f, referencia, 12f, 14f });
            Assert.GreaterOrEqual(14f * e1366, 12f - 1e-3f, $"A 1366×768 la letra de 14 px queda en {14f * e1366:F1} px");
            Assert.AreEqual(1f, e1920, 1e-4f, "A 1920×1080 la escala tiene que ser 1");
        }

        // ── P4c ──────────────────────────────────────────────────────────────

        /// <summary>
        /// nivel4 con blanca, roja y azul: 3 bien, 0 mal, 0 en cinta y un ciclo por pieza
        /// parecido al del test de P2 (2,7 / 4,3 / 5,3 s). Cada pieza se cuenta una sola vez,
        /// aunque su barrera de rampa siga tapada los frames siguientes.
        /// </summary>
        [UnityTest]
        [Timeout(180000)]
        public IEnumerator Dashboard_Resultado_Nivel4_TresBienCeroMal()
        {
            yield return CargarEscena();
            var resultado = Object.FindFirstObjectByType(Tipo("ResultadoDashboard")) as Component;
            Assert.IsNotNull(resultado, "No hay ResultadoDashboard en la escena");
            Process proceso = Lanzar(Nivel4);
            yield return EsperarLatido(proceso);

            foreach (string color in new[] { "Blanca", "Roja", "Azul" })
            {
                object pieza = ColocarPieza(color);
                float t0 = Time.realtimeSinceStartup;
                while (Leer(pieza, "estado").ToString() != "EnRampa")
                {
                    SinErroresPropios();
                    Assert.Less(Time.realtimeSinceStartup - t0, EsperaPorPieza_s, $"La pieza {color} no ha llegado a su rampa");
                    yield return null;
                }
            }
            // Unos frames más con las tres barreras de rampa tapadas: no tiene que contar nada nuevo.
            for (int i = 0; i < 30; i++) yield return null;

            Assert.AreEqual(3, (int)Leer(resultado, "bien"), "Piezas bien");
            Assert.AreEqual(0, (int)Leer(resultado, "mal"), "Piezas mal");
            Assert.AreEqual(0, (int)Leer(resultado, "enCinta"), "Piezas en cinta");
            var llegadas = ((IEnumerable)Leer(resultado, "Llegadas")).Cast<object>().ToList();
            Assert.AreEqual(3, llegadas.Count, "Cada pieza se cuenta una sola vez");
            var esperados = new[] { 2.7f, 4.3f, 5.3f };
            for (int i = 0; i < 3; i++)
            {
                float ciclo = (float)Leer(llegadas[i], "ciclo_s");
                Debug.Log($"[Test] {Leer(llegadas[i], "nombre")}: ciclo {ciclo:F2} s");
                Assert.AreEqual(esperados[i], ciclo, 0.5f, $"Ciclo de {Leer(llegadas[i], "nombre")}");
            }
            SinErroresPropios();
        }

        /// <summary>Pinchar el reto del nivel 2 lo elige en el desplegable.</summary>
        [UnityTest]
        public IEnumerator Dashboard_Reto_AlPincharlo_EligeSuPrograma()
        {
            yield return CargarEscena();
            var dashboard = Object.FindFirstObjectByType(Tipo("DashboardPlanta")) as Component;
            VisualElement raiz = dashboard.GetComponent<UIDocument>().rootVisualElement;
            yield return null; // los retos se construyen en Start
            const string ruta = "ejercicios/nivel2_parar.py";
            Pulsar(raiz.Q<Button>("reto:" + ruta));
            yield return null;
            Assert.AreEqual(ruta, Leer(dashboard, "programaElegido"));
            Assert.AreEqual(ruta, raiz.Q<Label>("programaTexto").text);
            Assert.IsTrue(raiz.Q("reto:" + ruta).ClassListContains("reto--elegido"), "La tarjeta elegida no se marca");
            Assert.IsNull(raiz.Q("reto:ejercicios/soluciones/nivel4.py"), "Un reto no puede apuntar a una solución");
        }

        // ── P4d ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Las píldoras Blanca, Roja, Azul y Quitar hacen lo mismo que las teclas 1, 2, 3 y 0.
        /// Sin programa en marcha la cinta está parada: cada pieza se queda a la entrada.
        /// Una segunda pieza con la entrada ocupada no entra y se avisa en «Salida».
        /// </summary>
        [UnityTest]
        public IEnumerator Dashboard_Piezas_ConLasPildoras()
        {
            yield return CargarEscena();
            var dashboard = Object.FindFirstObjectByType(Tipo("DashboardPlanta")) as Component;
            VisualElement raiz = dashboard.GetComponent<UIDocument>().rootVisualElement;
            yield return null;
            List<object> Piezas() => ((IEnumerable)Leer(_planta, "Piezas")).Cast<object>().Where(p => (Object)p != null).ToList();

            foreach (string color in new[] { "Blanca", "Roja", "Azul" })
            {
                Pulsar(raiz.Q<Button>("pieza:" + color));
                yield return null;
                var piezas = Piezas();
                Assert.AreEqual(1, piezas.Count, $"La píldora {color} no pone una pieza");
                Assert.AreEqual(color, Leer(piezas[0], "ColorReal").ToString(), $"La píldora {color} pone una pieza de otro color");

                // Con la entrada ocupada no entra otra, y se avisa.
                Pulsar(raiz.Q<Button>("pieza:" + color));
                yield return null;
                Assert.AreEqual(1, Piezas().Count, "Con la entrada ocupada ha entrado otra pieza");
                StringAssert.Contains("Ya hay una pieza al principio de la cinta", (string)Leer(dashboard, "TextoSalidaVisible"));

                Pulsar(raiz.Q<Button>("pieza:Quitar"));
                yield return null; // Destroy() surte efecto al final del frame
                yield return null;
                Assert.AreEqual(0, Piezas().Count, "Quitar no quita las piezas");
            }

            // Las píldoras quedan fuera del hueco: no tapan la planta que encuadra la cámara.
            Rect piezasRect = raiz.Q("piezas").worldBound, hueco = raiz.Q("hueco").worldBound;
            Assert.LessOrEqual(piezasRect.yMax, hueco.yMin + 0.5f, "Las píldoras se meten en el hueco de la planta");
            Assert.IsFalse(raiz.Q<Button>("pieza:Blanca").focusable, "Las píldoras no deben quedarse con el foco");
            SinErroresPropios();
        }

        // ── P5 ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Cargar desde el dashboard un programa que falla: el bloque de error sale en rojo
        /// (a la vista, sin «Ver detalle») con el archivo y la línea, la salida se amplía,
        /// el estado pasa a «Error» y la planta se para.
        /// </summary>
        [UnityTest]
        [Timeout(120000)]
        public IEnumerator Dashboard_ErrorDelAlumno_EnRojoConArchivoYLinea(
            [Values(FallaAMitad + "|falla_a_mitad.py, línea 16|",
                    FallaEnHilo + "|falla_en_hilo.py, línea 13|(en el hilo vigilar_i3)")] string caso)
        {
            string[] c = caso.Split('|');
            string programa = c[0], archivoYLinea = c[1], hilo = c[2];

            yield return CargarEscena();
            var dashboard = Object.FindFirstObjectByType(Tipo("DashboardPlanta")) as Component;
            VisualElement raiz = dashboard.GetComponent<UIDocument>().rootVisualElement;
            Label estado = raiz.Q<Label>("estadoTexto");
            yield return null;

            Llamar(dashboard, "Elegir", programa);
            Pulsar(raiz.Q<Button>("cargar"));
            yield return null;
            Assert.Greater(Pid, 0, "El botón Cargar no ha lanzado " + programa);
            Process proceso = Process.GetProcessById(Pid);
            yield return EsperarLatido(proceso);
            ColocarPieza("Blanca"); // I1 arranca la cinta; en I3 llega el error

            float t0 = Time.realtimeSinceStartup;
            while (estado.text != "Error")
            {
                Assert.Less(Time.realtimeSinceStartup - t0, EsperaPorPieza_s, $"El estado no pasa a «Error» (está en «{estado.text}»)");
                yield return null;
            }
            yield return null; // las últimas líneas del bloque

            string rojo = (string)Leer(dashboard, "TextoSalidaError");
            string visible = (string)Leer(dashboard, "TextoSalidaVisible");
            Debug.Log("[Test] Error en rojo:\n" + rojo);
            StringAssert.Contains("ERROR en tu programa" + (hilo.Length > 0 ? " " + hilo : ""), rojo, "No sale en rojo la línea del error");
            StringAssert.Contains(archivoYLinea, rojo, "El error no dice el archivo y la línea");
            StringAssert.Contains("La planta se ha parado", rojo, "No sale en rojo el aviso de planta parada");
            StringAssert.Contains(archivoYLinea, visible, "El error queda oculto tras «Ver detalle»");
            Assert.IsTrue((bool)Leer(dashboard, "SalidaAmpliada"), "La salida no se amplía para enseñar el error entero");
            Assert.AreEqual(1, UltimoCodigo, "Código de salida");

            t0 = Time.realtimeSinceStartup;
            while (VelocidadCinta != 0f || !EstadoSeguro)
            {
                Assert.Less(Time.realtimeSinceStartup - t0, 3f, "La planta no se para después del error");
                yield return null;
            }
            SinErroresPropios();
        }

        /// <summary>Un programa que no existe: aviso claro en rojo en «Salida» y estado «Error».</summary>
        [UnityTest]
        public IEnumerator Dashboard_ProgramaQueNoExiste_AvisaEnRojo()
        {
            yield return CargarEscena();
            var dashboard = Object.FindFirstObjectByType(Tipo("DashboardPlanta")) as Component;
            VisualElement raiz = dashboard.GetComponent<UIDocument>().rootVisualElement;
            yield return null;

            Llamar(dashboard, "Elegir", "no_existe.py");
            Pulsar(raiz.Q<Button>("cargar"));
            yield return null;
            StringAssert.Contains("No encuentro el programa no_existe.py", (string)Leer(dashboard, "TextoSalidaError"),
                "El fallo al arrancar no sale en rojo en «Salida»");
            Assert.AreEqual("Error", raiz.Q<Label>("estadoTexto").text);
            Assert.AreEqual(-2, UltimoCodigo, "Código de salida (no ha llegado a arrancar)");
        }

        // ── Utilidades ───────────────────────────────────────────────────────

        private static string SafeExit(Process p)
        {
            try { return p.ExitCode.ToString(); } catch { return "?"; }
        }

        /// <summary>Salida de «tasklist /FI "PID eq N"»: si el proceso no existe, no sale ninguna fila con python.exe.</summary>
        private static string Tasklist(int pid)
        {
            var psi = new ProcessStartInfo("tasklist", $"/FI \"PID eq {pid}\" /FO CSV /NH")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };
            using (var p = Process.Start(psi))
            {
                string salida = p.StandardOutput.ReadToEnd();
                p.WaitForExit(5000);
                return salida;
            }
        }
    }
}
