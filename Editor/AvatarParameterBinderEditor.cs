using System.Collections.Generic;
using System.Linq;
using top.kuriko.Common;
using top.kuriko.Unity.Common.Editor;
using UnityEditor;
using UnityEngine;
using static VRC.SDKBase.VRC_AvatarParameterDriver;
using DriveParam = VRC.SDKBase.VRC_AvatarParameterDriver.Parameter;
using ParamType = UnityEngine.AnimatorControllerParameterType;
using static top.kuriko.Unity.Common.Editor.EditorLayout;
using top.kuriko.Unity.Common;
using System;
using nadena.dev.ndmf;

namespace top.kuriko.Unity.VRChat.NDMF.AvatarParameterBinder.Editor
{
    [CustomEditor(typeof(AvatarParameterBinder))]
    public partial class AvatarParameterBinderEditor : UnityEditor.Editor
    {
        SerializedProperty Settings;
        ReorderableList<BinderSetting> SettingsList;
        static I18N i18n => I18N.i18n;

        void OnEnable()
        {
            AvatarParamCache.Update(serializedObject, true);
            Settings = serializedObject.FindProperty(nameof(AvatarParameterBinder.Settings));
        }

        readonly Dictionary<int, ReorderableList<Condition>> ConditionsCache = new();
        ReorderableList<Condition> GetConditions(int index, SerializedProperty el)
        {
            if (!ConditionsCache.TryGetValue(index, out var cs))
            {
                var header = i18n.PreCondition;
                var label = new GUIContent(i18n.Parameter);
                cs = new ReorderableList<Condition>(el.FindPropertyRelative(nameof(BinderSetting.Conditions)))
                {
                    Header = header,
                    onRemoveCallback = list => ConditionsCache.Remove(index),
                    drawElementCallback = (rect, index, isActive, isFocused) =>
                        EditorGUI.PropertyField(new(rect.x, rect.y + 1, rect.width, rect.height - 2), cs.GetArrayElementAtIndex(index), label),
                    elementHeight = EditorGUIUtility.singleLineHeight,
                };
                ConditionsCache[index] = cs;
            }
            return cs;
        }

        public float DrawSettingsList(int index, bool measureOnly) => DrawSettingsList(new(), index, measureOnly);
        public float DrawSettingsList(Rect rect, int index, bool measureOnly)
        {
            AvatarParamCache.Update(serializedObject);
            var el = Settings.GetArrayElementAtIndex(index);

            var inity = rect.y;
            rect.y += LineSpacing;
            // bind mode
            var xoff = 0f;
            void nextLine()
            {
                rect.y += LineHeight + LineSpacing;
                xoff = 0;
            }
            if (!measureOnly)
            {
                var label = new GUIContent(i18n.BindMode);
                var sz = label.CalcSize(EditorStyles.label);
                xoff += Spacing;
                EditorGUI.LabelField(new(rect.x + xoff, rect.y, sz.x, InlineHeight), label, EditorStyles.label);
                xoff = sz.x + Spacing * 2;
            }
            var mode = el.FindProperty<BindMode>(nameof(BinderSetting.BindMode));
            var modev = mode.Value;
            if (!measureOnly)
            {
                modev = BindModeUtils.Modes[EditorGUI.Popup(
                    new(rect.x + xoff, rect.y, rect.width - xoff, InlineHeight),
                    mode.Value.GetIndex(),
                    BindModeUtils.Modes.Select(m => m switch
                    {
                        BindMode.LocalAndRemote => i18n.BindMode_LocalAndRemote,
                        BindMode.LocalOnly => i18n.BindMode_LocalOnly,
                        BindMode.RemoteOnly => i18n.BindMode_RemoteOnly,
                        BindMode.LocalToRemote => i18n.BindMode_LocalToRemote,
                        BindMode.RemoteToLocal => i18n.BindMode_RemoteToLocal,
                        _ => throw new ArgumentException(),
                    }).ToArray()
                    )];
                mode.Value = modev;
            }
            nextLine();
            // conditions
            var cs = GetConditions(index, el);
            var csh = cs.GetHeight();
            if (!measureOnly)
                cs.DoList(new(rect.x, rect.y, rect.width, csh));
            rect.y += csh + LineSpacing * 2;
            xoff = 0;

            var oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = EditorStyles.label.CalcSizes(new[]
            {
                i18n.SourceParameter,
                i18n.SyncAccuracy,
                i18n.ConvertRange,
                i18n.DestinationParameter,
                i18n.ReverseSyncAccuracy,
                i18n.ConvertRangeReversed,
            }).Max(v => v.x) + Spacing;

            var bidi = modev.IsBiDirection();

            // param
            ProvidedParameter drawParam(SerializedProperty<string> paramel, string label)
            {
                var param = AvatarParamCache.Get(paramel.Value);
                if (!measureOnly)
                {
                    xoff += Spacing;
                    EditorGUI.LabelField(new(rect.x + xoff, rect.y, EditorGUIUtility.labelWidth, InlineHeight), label);
                    xoff += EditorGUIUtility.labelWidth + 2;
                    var sync = param?.WantSynced;
                    var label2 = new GUIContent(i18n.Sync);
                    var sz = label2.CalcSize(EditorStyles.label);
                    var syncLabelw = sz.x + Spacing;
                    var w = rect.width - (sync != null ? (xoff + syncLabelw + CheckBoxSize + Spacing * 2) : xoff);
                    paramel.DrawParameter(new(rect.x + xoff, rect.y, w, InlineHeight));
                    xoff += w + Spacing;
                    if (sync != null)
                    {
                        GUI.enabled = false;
                        EditorGUI.Toggle(new(rect.x + xoff, rect.y, CheckBoxSize, InlineHeight), sync.Value);
                        xoff += CheckBoxSize + Spacing;
                        EditorGUI.LabelField(new(rect.x + xoff, rect.y, syncLabelw, InlineHeight), i18n.Sync);
                        xoff += syncLabelw + Spacing;
                        GUI.enabled = true;
                    }
                }
                nextLine();
                return param;
            }
            // bind setting
            void drawBindSetting(
                ParamType? paramType,
                SerializedProperty bs,
                string accuracyLabel,
                string convRangeLabel
                )
            {
                // sync accuracy
                if (paramType != ParamType.Bool && paramType != ParamType.Trigger)
                {
                    if (!measureOnly)
                    {
                        xoff += Spacing;
                        var syncAcc = bs.FindProperty<int>(nameof(BindSetting.Accuracy));
                        var max = paramType.GetMaxSyncAccuracy();
                        var syncAccw = EditorGUIUtility.labelWidth + 100f;
                        var (changed, val) = EditorUtils.GUIChangeCheckField(() => EditorGUI.IntField(
                            new(rect.x + xoff, rect.y, syncAccw, InlineHeight),
                            accuracyLabel,
                            syncAcc.Value.LimitToRange(2, max)
                            ));
                        xoff += syncAccw + Spacing;
                        if (changed)
                            syncAcc.Value = val;
                        var label = new GUIContent((paramType == ParamType.Int ? $"{MathF.Round(256f/val, 3)}/{256}" : $"1/{val}") + $" ({100f / val:G}%)");
                        var w = label.CalcSize(EditorStyles.label).x;
                        EditorGUI.LabelField(new(rect.x + xoff, rect.y, w, InlineHeight), label);
                        xoff += w + Spacing;
                    }
                    nextLine();
                }
                // convert range
                {
                    var convRange = bs.FindProperty<bool>(nameof(BindSetting.ConvertRange));
                    var convRangev = convRange.Value;
                    if (!measureOnly)
                    {
                        var (changed, val) = EditorUtils.GUIChangeCheckField(() => EditorGUI.Toggle(
                            new(rect.x + Spacing, rect.y, rect.width, InlineHeight),
                            convRangeLabel,
                            convRangev
                            ));
                        convRangev = val;
                        if (changed)
                            convRange.Value = val;
                    }
                    if (convRangev)
                    {
                        nextLine();
                        if (!measureOnly)
                        {
                            var lw = EditorStyles.label.CalcSize("～").x;
                            var rw = EditorStyles.label.CalcSize("  >>>  ").x;
                            var w = (rect.width - lw * 2 - rw - Spacing * 8) / 4;
                            void drawVal(SerializedProperty<float> v)
                            {
                                var (changed, val) = EditorUtils.GUIChangeCheckField(() => EditorGUI.FloatField(
                                    new(rect.x + xoff, rect.y, w, InlineHeight),
                                    v.Value
                                    ));
                                xoff += w + Spacing;
                                if (changed)
                                    v.Value = val;
                            }
                            xoff += Spacing;
                            drawVal(bs.FindProperty<float>(nameof(BindSetting.SrcMin)));
                            EditorGUI.LabelField(new(rect.x + xoff, rect.y, lw, InlineHeight), "～");
                            xoff += lw + Spacing;
                            drawVal(bs.FindProperty<float>(nameof(BindSetting.SrcMax)));
                            EditorGUI.LabelField(new(rect.x + xoff, rect.y, rw, InlineHeight), "  >>>  ");
                            xoff += rw + Spacing;
                            drawVal(bs.FindProperty<float>(nameof(BindSetting.DstMin)));
                            EditorGUI.LabelField(new(rect.x + xoff, rect.y, lw, InlineHeight), "～");
                            xoff += lw + Spacing;
                            drawVal(bs.FindProperty<float>(nameof(BindSetting.DstMax)));
                        }
                    }
                }
                nextLine();
            }
            // src
            var src = el.FindProperty<string>(nameof(BinderSetting.Src));
            var srcParam = drawParam(src, i18n.SourceParameter);
            if (bidi && AvatarParamUtils.BuiltInParams.ContainsKey(src.Value))
            {
                if (!measureOnly)
                    EditorGUI.HelpBox(
                        new(rect.x, rect.y, rect.width, LineHeight * 2 - LineSpacing),
                        i18n.CannotWriteToBuiltInParam,
                        MessageType.Error
                        );
                rect.y += LineHeight * 2;
            }
            // bind setting
            drawBindSetting(
                srcParam?.ParameterType,
                el.FindPropertyRelative(nameof(BinderSetting.SyncSetting)),
                i18n.SyncAccuracy,
                i18n.ConvertRange
                );
            // dst
            var dst = el.FindProperty<string>(nameof(BinderSetting.Dst));
            var dstParam = drawParam(dst, i18n.DestinationParameter);
            if (AvatarParamUtils.BuiltInParams.ContainsKey(dst.Value))
            {
                if (!measureOnly)
                    EditorGUI.HelpBox(
                        new(rect.x, rect.y, rect.width, LineHeight * 2 - LineSpacing),
                        i18n.CannotWriteToBuiltInParam,
                        MessageType.Error
                        );
                rect.y += LineHeight * 2;
            }
            // reverse bind setting
            if (bidi)
                drawBindSetting(
                    dstParam?.ParameterType,
                    el.FindPropertyRelative(nameof(BinderSetting.ReverseSyncSetting)),
                    i18n.ReverseSyncAccuracy,
                    i18n.ConvertRangeReversed
                    );
            EditorGUIUtility.labelWidth = oldLabelWidth;
            return rect.y - inity;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (SettingsList.IsNull())
            {
                SettingsList = new(Settings)
                {
                    Header = i18n.BindSetting,
                    elementHeightCallback = index => DrawSettingsList(index, true),
                    drawElementCallback = (rect, index, isActive, isFocused) => DrawSettingsList(rect, index, false),
                    onReorderCallbackWithDetails = (list, oldIndex, newIndex) =>
                    {
                        ConditionsCache.Remove(oldIndex);
                        ConditionsCache.Remove(newIndex);
                    },
                };
            }
            SettingsList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
