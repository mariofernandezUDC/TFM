// ParametroDrawer.cs · Dibuja cada Parametro de ParametrosSLD en dos líneas (F0.6):
//   1) nombre · valor · unidad · casilla PROVISIONAL
//   2) origen del valor (código, escena o suposición)
// Si el valor es provisional, el nombre sale en naranja.

using UnityEditor;
using UnityEngine;

namespace PlantaVirtual
{
    [CustomPropertyDrawer(typeof(Parametro))]
    public class ParametroDrawer : PropertyDrawer
    {
        private const float AnchoUnidad = 42f, AnchoProvisional = 96f;

        public override float GetPropertyHeight(SerializedProperty propiedad, GUIContent etiqueta)
            => EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing * 2f;

        public override void OnGUI(Rect pos, SerializedProperty propiedad, GUIContent etiqueta)
        {
            var valor = propiedad.FindPropertyRelative("valor");
            var unidad = propiedad.FindPropertyRelative("unidad");
            var provisional = propiedad.FindPropertyRelative("provisional");
            var origen = propiedad.FindPropertyRelative("origen");

            EditorGUI.BeginProperty(pos, etiqueta, propiedad);
            float alto = EditorGUIUtility.singleLineHeight;
            var linea1 = new Rect(pos.x, pos.y, pos.width, alto);
            var linea2 = new Rect(pos.x, pos.y + alto + EditorGUIUtility.standardVerticalSpacing, pos.width, alto);

            var estilo = new GUIStyle(EditorStyles.label);
            if (provisional.boolValue) estilo.normal.textColor = new Color(1f, 0.6f, 0.1f);
            var resto = EditorGUI.PrefixLabel(linea1, new GUIContent(etiqueta.text, origen.stringValue), estilo);

            var rValor = new Rect(resto.x, resto.y, resto.width - AnchoUnidad - AnchoProvisional, alto);
            var rUnidad = new Rect(rValor.xMax + 4f, resto.y, AnchoUnidad - 4f, alto);
            var rProv = new Rect(rUnidad.xMax, resto.y, AnchoProvisional, alto);
            int sangria = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(rValor, valor, GUIContent.none);
            EditorGUI.LabelField(rUnidad, unidad.stringValue);
            provisional.boolValue = EditorGUI.ToggleLeft(rProv, "PROVISIONAL", provisional.boolValue);
            EditorGUI.indentLevel = sangria;

            EditorGUI.indentLevel++;
            EditorGUI.PropertyField(linea2, origen, new GUIContent("Origen"));
            EditorGUI.indentLevel = sangria;
            EditorGUI.EndProperty();
        }
    }
}
