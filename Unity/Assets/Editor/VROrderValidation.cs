using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// End-to-end, real-time test: survives scene reloads, checks actual 3D pieces.
public static class VROrderValidation
{
    static readonly string[] Colors = { "BLUE", "RED", "WHITE" };
    static readonly List<string> report = new List<string>();
    static int index, oldSimulator, maxPieces;
    static double requested, nextSample, nextOrder;
    static bool started, fullStock, rampSeen, flightBodyDisabled;
    static string phase;

    [MenuItem("Tools/VR/Test three complete orders")]
    public static void Start()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play required");
        EditorApplication.update -= Tick;
        report.Clear(); index = 0; phase = "request"; nextOrder = 0;
        EditorApplication.update += Tick;
        Record("Start three orders at normal simulation speed: " + DateTime.Now.ToString("s"));
    }

    static void Record(string text)
    {
        report.Add(text);
        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/VR-orders-validation.txt", report);
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; Record("Interrupted: Play stopped"); return; }
        double now = EditorApplication.timeSinceStartup;
        if (now < nextSample) return;
        nextSample = now + 0.25;
        try
        {
            if (phase == "request")
            {
                if (now < nextOrder) return;
                var popup = UnityEngine.Object.FindFirstObjectByType<VRPiecePopup>();
                var sim = SimuladorOffline.Instance;
                if (popup == null || sim == null || sim.EnEjecucion) return;
                oldSimulator = sim.GetInstanceID(); requested = now;
                started = fullStock = rampSeen = flightBodyDisabled = false; maxPieces = 0;
                string label = Colors[index] == "BLUE" ? "AZUL" : Colors[index] == "RED" ? "ROJA" : "BLANCA";
                var canvas = Resources.FindObjectsOfTypeAll<Canvas>().First(c => c.name == "VR - Pedir pieza");
                var button = canvas.GetComponentsInChildren<Button>(true).First(b => b.name == label);
                phase = "running";
                Record("REQUEST " + Colors[index] + " via popup button; previous simulator=" + oldSimulator);
                button.onClick.Invoke();
                return;
            }
            var current = SimuladorOffline.Instance;
            if (current == null) return;
            if (current.EnEjecucion && !started)
            {
                started = true;
                Record(Colors[index] + " started; clean scene=" + (current.GetInstanceID() != oldSimulator));
            }
            if (!started && now - requested > 20) throw new Exception("Order did not start: " + Colors[index]);
            var spawn = ControladorSpawnPiecesHBW_mqtt.Instance;
            if (spawn != null && !fullStock)
            {
                int occupied = spawn.puntosDeHueco.Count(h => h != null && h.childCount > 0 &&
                    h.GetChild(0).Cast<Transform>().Any(t => t.name.ToLowerInvariant().Contains("pieza") && t.gameObject.activeInHierarchy));
                if (occupied == 9) { fullStock = true; Record(Colors[index] + " warehouse full: 9/9"); }
            }
            var rig = UnityEngine.Object.FindFirstObjectByType<VRPlantRig>();
            if (rig != null) flightBodyDisabled = !rig.origin.GetComponentsInChildren<Collider>(true).Any(c => c.enabled);
            var pieces = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t => t.name.ToLowerInvariant().Contains("pieza") && t.GetComponent<Renderer>() != null).ToArray();
            maxPieces = Math.Max(maxPieces, pieces.Length);
            var ramps = UnityEngine.Object.FindFirstObjectByType<ControladorCintaSLD_mqtt>();
            if (ramps != null)
            {
                var destination = Colors[index] == "BLUE" ? ramps.finRampaAzul : Colors[index] == "RED" ? ramps.finRampaRoja : ramps.finRampaBlanca;
                if (destination != null && destination.GetComponentsInChildren<Renderer>().Any(r => r.name.ToLowerInvariant().Contains("pieza")))
                    if (!rampSeen) { rampSeen = true; Record(Colors[index] + " physical piece reached correct ramp at " + (now - requested).ToString("F1") + " s"); }
            }
            if (now - requested > 220) throw new Exception("Order timeout: " + Colors[index]);
            if (!started || current.EnEjecucion) return;
            Record(Colors[index] + " END: fullStock=" + fullStock + "; correctRamp=" + rampSeen +
                "; XR colliders disabled=" + flightBodyDisabled + "; max visible piece renderers=" + maxPieces);
            if (!fullStock || !rampSeen || !flightBodyDisabled) throw new Exception("Physical validation failed: " + Colors[index]);
            if (++index == Colors.Length)
            {
                Record("PASS: all three complete orders, including consecutive scene resets.");
                EditorApplication.update -= Tick;
                return;
            }
            phase = "request"; nextOrder = now + 2;
        }
        catch (Exception ex) { Record("FAIL: " + ex.Message); EditorApplication.update -= Tick; Debug.LogException(ex); }
    }
}
