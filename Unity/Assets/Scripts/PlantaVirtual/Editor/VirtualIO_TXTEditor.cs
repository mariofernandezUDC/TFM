// VirtualIO_TXTEditor.cs · Inspector de VirtualIO_TXT (F0.6).
// Igual que el normal, pero en modo "Planta" las entradas se ven en gris y no se
// pueden editar: las escribe la planta virtual. En modo "Manual" se editan como siempre.

using UnityEditor;

namespace PlantaVirtual
{
    [CustomEditor(typeof(VirtualIO_TXT))]
    public class VirtualIO_TXTEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var origen = serializedObject.FindProperty("origenEntradas");
            bool modoPlanta = origen.enumValueIndex == (int)VirtualIO_TXT.OrigenEntradas.Planta;

            var it = serializedObject.GetIterator();
            bool entrar = true;
            while (it.NextVisible(entrar))
            {
                entrar = false;
                bool soloLectura = it.name == "m_Script" || (modoPlanta && it.name == "entradas");
                using (new EditorGUI.DisabledScope(soloLectura))
                    EditorGUILayout.PropertyField(it, true);
            }
            if (modoPlanta)
                EditorGUILayout.HelpBox("Modo Planta: las entradas las escribe SLD_Planta. Cambia a Manual para editarlas a mano.", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
            if (UnityEngine.Application.isPlaying) Repaint();
        }
    }
}
