using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using static top.kuriko.Unity.Common.Editor.EditorLayout;
using top.kuriko.Unity.Common.Editor;
using top.kuriko.Common;

namespace top.kuriko.Unity.VRChat.NDMF.AvatarParameterBinder.Editor
{
    [CustomPropertyDrawer(typeof(Condition))]
    public class ConditionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty el, GUIContent label)
        {
            var param = el.FindProperty<string>(nameof(Condition.ParameterName));
            var mode = el.FindProperty<ConditionMode>(nameof(Condition.Mode));
            var val = el.FindProperty<float>(nameof(Condition.Threshold));
            var type = AvatarParamCache.GetFunc(param.Value)?.ParameterType;
            var xoff = 0f;
            {
                var sz = label.CalcSize(EditorStyles.label);
                EditorGUI.LabelField(new(rect.x + xoff, rect.y, sz.x, InlineHeight), label);
                xoff += sz.x + Spacing;
            }
            var modev = mode.Value;
            if ((int)modev < 0)
                modev = ConditionMode.If;
            var popupWidth = ConditionUtils.PopupWidth;
            var valWidth = 40;
            var rangeLabelw = new GUIContent("[").CalcSize(EditorStyles.label).x;
            var rangeLabelmw = new GUIContent("～").CalcSize(EditorStyles.label).x;
            var rangeWidth = popupWidth + Spacing + rangeLabelw + Spacing + valWidth + Spacing + rangeLabelmw + Spacing + valWidth + rangeLabelw + Spacing;
            var boolWidth = CheckBoxSize;
            var otherWidth = popupWidth + Spacing + valWidth;
            var vw = 0f;
            var isBool = AnimatorControllerParameterType.Bool.GetConditions().Contains(modev);
            var isRange = modev == ConditionMode.InRange || modev == ConditionMode.OutOfRange;
            if (type != null)
            {
                if (type == AnimatorControllerParameterType.Bool)
                    vw = boolWidth;
                else if (isRange)
                    vw = rangeWidth;
                else vw = otherWidth;
            }
            else if (isRange)
                vw = rangeWidth;
            else vw = isBool ? popupWidth : otherWidth;
            vw += Spacing;
            {
                var w = rect.width - xoff - vw;
                param.DrawParameter(new(rect.x + xoff, rect.y, w, InlineHeight));
                xoff += w + Spacing;
            }
            if (type != null)
            {
                var modes = type.Value.GetConditions();
                if (!modes.Contains(modev))
                {
                    switch (type)
                    {
                        case AnimatorControllerParameterType.Bool:
                        case AnimatorControllerParameterType.Trigger:
                            modev = ConditionMode.If;
                            break;
                        case AnimatorControllerParameterType.Int:
                            modev = ConditionMode.Equals;
                            break;
                        case AnimatorControllerParameterType.Float:
                            modev = ConditionMode.Greater;
                            break;
                    }
                }
                if (type == AnimatorControllerParameterType.Bool)
                {
                    var (changed, value) = EditorUtils.GUIChangeCheckField(
                        () => EditorGUI.Toggle(new(rect.x + xoff, rect.y, boolWidth, InlineHeight), modev == ConditionMode.If));
                    modev = value ? ConditionMode.If : ConditionMode.IfNot;
                    if (changed)
                        mode.Value = modev;
                    xoff += boolWidth + Spacing;
                    return;
                }
                else
                {
                    var ops = type.Value.GetConditionOperators();
                    var (changed, value) = EditorUtils.GUIChangeCheckField(
                        () => EditorGUI.Popup(new(rect.x + xoff, rect.y, popupWidth, InlineHeight), modes.IndexOf(modev), ops));
                    modev = modes[value];
                    if (changed)
                        mode.Value = modev;
                    xoff += popupWidth + Spacing;
                }
            }
            else
            {
                var (changed, value) = EditorUtils.GUIChangeCheckField(
                    () => EditorGUI.Popup(
                    new(rect.x + xoff, rect.y, popupWidth, InlineHeight),
                    modev.GetIndex(),
                    ConditionUtils.Modes.Select(m => m.GetOperator()).ToArray()
                    ));
                modev = ConditionUtils.Modes[value];
                if (changed)
                    mode.Value = modev;
                xoff += popupWidth + Spacing;
                if (isBool)
                    return;
            }
            if (isRange)
            {
                EditorGUI.LabelField(new(rect.x + xoff, rect.y, rangeLabelw, InlineHeight), modev == ConditionMode.InRange ? "[" : "(");
                xoff += rangeLabelw + Spacing;
            }
            EditorGUI.PropertyField(new(rect.x + xoff, rect.y, valWidth, InlineHeight), val, GUIContent.none);
            if (isRange)
            {
                xoff += valWidth + Spacing;
                EditorGUI.LabelField(new(rect.x + xoff, rect.y, rangeLabelmw, InlineHeight), "～");
                xoff += rangeLabelmw + Spacing;
                var val2 = el.FindProperty<float>(nameof(Condition.Threshold2));
                EditorGUI.PropertyField(new(rect.x + xoff, rect.y, valWidth, InlineHeight), val2, GUIContent.none);
                xoff += valWidth + Spacing;
                EditorGUI.LabelField(new(rect.x + xoff, rect.y, rangeLabelw, InlineHeight), modev == ConditionMode.InRange ? "]" : ")");
                xoff += rangeLabelw + Spacing;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUIUtility.singleLineHeight;
    }
}
