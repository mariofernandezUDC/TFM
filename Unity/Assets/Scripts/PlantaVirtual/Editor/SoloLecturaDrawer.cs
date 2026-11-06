// SoloLecturaDrawer.cs · Dibuja en gris, sin dejar editarlos, los campos marcados con [SoloLectura].
// Lo usa VirtualIO_TXT para enseñar las salidas y el diagnóstico: los escribe el
// propio script en cada frame, así que editarlos a mano no tendría efecto.
// Está en una carpeta Editor/ porque solo existe dentro del editor de Unity.

using UnityEditor;
using UnityEngine;

namespace PlantaVirtual
{
    [CustomPropertyDrawer(typeof(SoloLecturaAttribute))]
    public class SoloLecturaDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect posicion, SerializedProperty propiedad, GUIContent etiqueta)
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUI.PropertyField(posicion, propiedad, etiqueta, true);
        }

        public override float GetPropertyHeight(SerializedProperty propiedad, GUIContent etiqueta)
            => EditorGUI.GetPropertyHeight(propiedad, etiqueta, true);
    }
}
