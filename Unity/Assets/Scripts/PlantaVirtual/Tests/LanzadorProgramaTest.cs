// LanzadorProgramaTest.cs · Test de PlayMode del lanzador (D008, paso P2).
//
// Hace lo mismo que la prueba a mano de P2, sin tocar Unity:
//   1. carga PlantaVirtual.unity;
//   2. LanzadorPrograma.Lanzar() con ejercicios/soluciones/nivel4.py (lo que hace la tecla L);
//   3. mete la blanca, la roja y la azul con SLD_Planta.ColocarPieza (lo que hacen las teclas 1, 2 y 3);
//   4. comprueba que cada pieza acaba en SU rampa;
//   5. apaga el lanzador como al salir de Play (OnDisable) y comprueba que el python.exe ya no existe.
//
// Necesita Mosquitto en marcha en el puerto de VirtualIO_TXT5 (1883).
//
// Por qué reflexión: los scripts de la planta están en Assembly-CSharp (sin asmdef)
// y un ensamblado de tests no puede referenciar Assembly-CSharp. Se buscan las
// clases por su nombre, sin cambiar la estructura del proyecto.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
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
        private const string Programa = "ejercicios/soluciones/nivel4.py";
        private const float EsperaPython_s = 20f;    // hasta que llega el primer latido de Python
        private const float EsperaPorPieza_s = 60f;  // de la entrada de la cinta a la rampa

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

        private static object Llamar(object o, string metodo, params object[] args) =>
            o.GetType().GetMethod(metodo, BindingFlags.Public | BindingFlags.Instance).Invoke(o, args);

        // La escena es una copia de la de Adrián: sus scripts MQTT escriben errores
        // (host CHANGE_ME sin configurar) que no tienen que ver con este test. Por
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

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator Nivel4_ClasificaLasTresPiezas_YNoDejaPythonHuerfano()
        {
#if UNITY_EDITOR
            // ── 1. Cargar la escena ──────────────────────────────────────────
            LogAssert.ignoreFailingMessages = true;
            var carga = EditorSceneManager.LoadSceneAsyncInPlayMode(Escena, new LoadSceneParameters(LoadSceneMode.Single));
            while (!carga.isDone) yield return null;
            yield return null; // un frame para que corran los Start()
            LogAssert.ignoreFailingMessages = true;

            var lanzador = Object.FindFirstObjectByType(Tipo("LanzadorPrograma")) as Component;
            var planta = Object.FindFirstObjectByType(Tipo("SLD_Planta")) as Component;
            var io = Object.FindFirstObjectByType(Tipo("VirtualIO_TXT")) as Component;
            Assert.IsNotNull(lanzador, "No hay LanzadorPrograma en la escena");
            Assert.IsNotNull(planta, "No hay SLD_Planta en la escena");
            Assert.IsNotNull(io, "No hay VirtualIO_TXT en la escena");

            // ── 2. Lanzar el programa (lo mismo que la tecla L) ──────────────
            lanzador.GetType().GetField("programa").SetValue(lanzador, Programa);
            Llamar(lanzador, "Lanzar");
            int pid = (int)Leer(lanzador, "pid");
            Assert.Greater(pid, 0, "Lanzar() no ha arrancado ningún proceso (mira la consola)");
            Process proceso = Process.GetProcessById(pid);
            Debug.Log($"[Test] Python lanzado, PID {pid}");

            // Python está listo cuando su latido saca a Unity del estado seguro (D006.5).
            float t0 = Time.realtimeSinceStartup;
            while ((bool)Leer(io, "estadoSeguro"))
            {
                SinErroresPropios();
                Assert.IsFalse(proceso.HasExited, $"Python ha terminado antes de tiempo (código {SafeExit(proceso)})");
                Assert.Less(Time.realtimeSinceStartup - t0, EsperaPython_s,
                    "No llega el latido de Python. ¿Está Mosquitto en marcha en el 1883?");
                yield return null;
            }

            // ── 3 y 4. Blanca, roja y azul: cada una a su rampa ──────────────
            Type tColor = Tipo("ColorPieza");
            foreach (string color in new[] { "Blanca", "Roja", "Azul" })
            {
                object valor = Enum.Parse(tColor, color);
                Assert.IsTrue((bool)Llamar(planta, "ColocarPieza", valor), $"No se ha podido colocar la pieza {color}");
                var piezas = (IEnumerable)Leer(planta, "Piezas");
                object pieza = null;
                foreach (var p in piezas) pieza = p; // la última es la recién colocada
                string nombre = ((Component)pieza).name;

                t0 = Time.realtimeSinceStartup;
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

            // ── 5. Salir de Play: no queda python.exe ────────────────────────
            // Al salir de Play Unity desactiva los objetos y llama a OnDisable(), que es
            // donde LanzadorPrograma mata el proceso. Aquí se fuerza ese mismo camino.
            lanzador.gameObject.SetActive(false);
            yield return null;

            SinErroresPropios();
            Assert.IsTrue(proceso.HasExited, $"El proceso {pid} sigue vivo después de OnDisable()");
            string tareas = Tasklist(pid);
            StringAssert.DoesNotContain("python", tareas.ToLowerInvariant(),
                $"tasklist todavía ve el PID {pid}:\n{tareas}");
            Debug.Log($"[Test] PID {pid} terminado. tasklist: {tareas.Trim()}");
            proceso.Dispose();
#else
            Assert.Ignore("Este test solo corre en el editor.");
            yield break;
#endif
        }

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
