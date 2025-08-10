using UnityEngine;
using UnityEditor;
using top.kuriko.Unity.Common.Editor;

namespace top.kuriko.Unity.VRChat.NDMF.AvatarParameterBinder.Editor
{
    public static class ParameterDrawer
    {
        public static void DrawParameter(this SerializedProperty prop, Rect rect)
        {
            EditorGUI.PropertyField(rect, prop, GUIContent.none);
            rect.x += rect.width - 35;
            rect.width = 35;

            var type = AvatarParamCache.Get(prop.stringValue)?.ParameterType?.ToString() ?? "?";
            var il = EditorGUI.indentLevel;
            EditorGUI.IndentedRect(rect);
            EditorGUI.indentLevel = 0;
            EditorGUI.LabelField(rect, type, EditorStyles.centeredGreyMiniLabel);
            EditorGUI.indentLevel = il;
        }

        public static void DrawParameter<T>(this SerializedProperty<T> prop, Rect rect)
        => DrawParameter(prop.prop, rect);
    }
}
