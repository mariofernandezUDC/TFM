// SLD_PlantaEditor.cs · Botones del Inspector de SLD_Planta para colocar y quitar piezas (F0.6).
// Solo funcionan en Play. Las teclas 1, 2, 3 y 0 hacen lo mismo.

using UnityEditor;
using UnityEngine;

namespace PlantaVirtual
{
    [CustomEditor(typeof(SLD_Planta))]
    public class SLD_PlantaEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var planta = (SLD_Planta)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Piezas (solo en Play · teclas 1, 2, 3 y 0)", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Pieza blanca")) planta.ColocarPieza(ColorPieza.Blanca);
                    if (GUILayout.Button("Pieza roja")) planta.ColocarPieza(ColorPieza.Roja);
                    if (GUILayout.Button("Pieza azul")) planta.ColocarPieza(ColorPieza.Azul);
                }
                if (GUILayout.Button("Quitar todas")) planta.QuitarTodas();
            }
            if (Application.isPlaying) Repaint();
        }
    }
}
