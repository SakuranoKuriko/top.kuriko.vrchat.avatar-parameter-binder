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

        readonly Dictionary<int, BindMode> BindModes = new();
        readonly Dictionary<int, ParamType?> SrcTypes = new();
        readonly Dictionary<int, ParamType?> DstTypes = new();

        readonly Dictionary<int, ReorderableList<BindSetting>> BindSettingsCache = new();
        ReorderableList<BindSetting> GetBindSettings(int bsindex, SerializedProperty el)
        {
            if (!BindSettingsCache.TryGetValue(bsindex, out var bss))
            {
                var header = i18n.BindSetting;
                var label = new GUIContent(i18n.Parameter);
                bss = new(el.FindPropertyRelative(nameof(BinderSetting.BindSettings)))
                {
                    Header = header,
                    elementHeight = LineHeight,
                    onRemoveCallback = list => BindSettingsCache.Remove(bsindex),
                    drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        var el = bss.GetArrayElementAtIndex(index);
                        rect.y += 2;
                        rect.height -= 2;

                        if (!BindModes.ContainsKey(bsindex))
                        {
                            EditorGUI.LabelField(rect, "Error!");
                            return;
                        }

                        var bindMode = BindModes[bsindex];
                        var srcType = SrcTypes.TryGetValue(bsindex, out var srct) ? srct : null;
                        var dstType = DstTypes.TryGetValue(bsindex, out var dstt) ? dstt : null;

                        var xoff = 0f;
                        switch (bindMode)
                        {
                            case BindMode.LocalAndRemote:
                            case BindMode.LocalOnly:
                            case BindMode.RemoteOnly:
                                {
                                    var localOnly = bindMode == BindMode.LocalOnly;
                                    var mode = el.FindProperty<ConditionMode>(nameof(BindSetting.Mode));
                                    var modev = mode.Value;
                                    if (srcType != null)
                                        modev = modev.ValidOrDefault(srcType.Value);
                                    {
                                        var label = new GUIContent(i18n.BindCondition + " [");
                                        var sz = label.CalcSize(EditorStyles.label);
                                        EditorGUI.LabelField(new(rect.x + xoff, rect.y, sz.x, InlineHeight), label);
                                        xoff += sz.x + Spacing;
                                    }
                                    switch (srcType)
                                    {
                                        case ParamType.Float:
                                        case ParamType.Int:
                                            {
                                                var conds = srcType.Value.GetConditions();
                                                var condops = srcType.Value.GetConditionOperators();
                                                mode.Value = modev = conds[EditorGUI.Popup(
                                                    new(rect.x + xoff, rect.y, ConditionUtils.PopupWidth, InlineHeight),
                                                    conds.IndexOf(modev),
                                                    condops
                                                    )];
                                                xoff += ConditionUtils.PopupWidth + Spacing;
                                                break;
                                            }
                                        case ParamType.Bool:
                                        case ParamType.Trigger:
                                            {
                                                var (changed, value) = EditorUtils.GUIChangeCheckField(
                                                    () => EditorGUI.Toggle(
                                                    new(rect.x + xoff, rect.y, CheckBoxSize, InlineHeight),
                                                    modev == ConditionMode.If
                                                    ));
                                                xoff += CheckBoxSize + Spacing;
                                                modev = value ? ConditionMode.If : ConditionMode.IfNot;
                                                if (changed)
                                                    mode.Value = modev;
                                            }
                                            break;
                                        case null:
                                            mode.Value = modev = ConditionUtils.Modes[EditorGUI.Popup(
                                                    new(rect.x + xoff, rect.y, ConditionUtils.PopupWidth, InlineHeight),
                                                    modev.GetIndex(),
                                                    ConditionUtils.Operators
                                                )];
                                            xoff += ConditionUtils.PopupWidth + Spacing;
                                            break;
                                    }
                                    var srcMin = 0;
                                    var srcMax = srcType == ParamType.Int ? 255 : 1;
                                    if (modev == ConditionMode.InRange || modev == ConditionMode.OutOfRange)
                                    {
                                        var labell = modev == ConditionMode.InRange ? "[" : "(";
                                        var labelr = modev == ConditionMode.InRange ? "]" : ")";
                                        var labelw = new GUIContent(labell).CalcSize(EditorStyles.label).x;
                                        EditorGUI.LabelField(new(rect.x + xoff, rect.y, labelw, InlineHeight), labell);
                                        xoff += labelw + Spacing;
                                        var th = el.FindProperty<float>(nameof(BindSetting.Threshold));
                                        {
                                            var (changed, value) = EditorUtils.GUIChangeCheckField(() => EditorGUI.FloatField(
                                                new(rect.x + xoff, rect.y, 40, InlineHeight),
                                                th.Value.LimitToRange(srcMin, srcMax)
                                                ));
                                            xoff += 40 + Spacing;
                                            if (changed)
                                                th.Value = value;
                                        }
                                        {
                                            var label = new GUIContent("～");
                                            var w = label.CalcSize(EditorStyles.label).x;
                                            EditorGUI.LabelField(new(rect.x + xoff, rect.y, w, InlineHeight), label);
                                            xoff += w + Spacing;
                                        }
                                        var th2 = el.FindProperty<float>(nameof(BindSetting.Threshold2));
                                        {
                                            var (changed, value) = EditorUtils.GUIChangeCheckField(() => EditorGUI.FloatField(
                                                new(rect.x + xoff, rect.y, 40, InlineHeight),
                                                th2.Value.LimitToRange(srcMin, srcMax)
                                                ));
                                            xoff += 40 + Spacing;
                                            if (changed)
                                                th2.Value = value;
                                        }
                                        EditorGUI.LabelField(new(rect.x + xoff, rect.y, labelw, InlineHeight), labelr);
                                        xoff += labelw + Spacing;
                                    }
                                    else if (srcType != ParamType.Bool && srcType != ParamType.Trigger)
                                    {
                                        var th = el.FindProperty<float>(nameof(BindSetting.Threshold));
                                        var (changed, value) = EditorUtils.GUIChangeCheckField(() => EditorGUI.FloatField(
                                            new(rect.x + xoff, rect.y, 40, InlineHeight),
                                            th.Value.LimitToRange(srcMin, srcMax)
                                            ));
                                        xoff += 40 + Spacing;
                                        if (changed)
                                            th.Value = value;
                                    }
                                    {
                                        var label = new GUIContent("]  >>>  " + i18n.TargetValue + " [");
                                        var sz = label.CalcSize(EditorStyles.label);
                                        EditorGUI.LabelField(new(rect.x + xoff, rect.y, sz.x, InlineHeight), label);
                                        xoff += sz.x + Spacing;
                                    }
                                    var cht = el.FindProperty<ChangeType>(nameof(BindSetting.ChangeType));
                                    var chtv = cht.Value;
                                    {
                                        var cts = dstType?.GetChangeTypes() ?? AvatarParamUtils.ChangeTypes;
                                        var (changed, value) = EditorUtils.GUIChangeCheckField(() => EditorGUI.Popup(
                                            new(rect.x + xoff, rect.y, 65, InlineHeight),
                                            cts.IndexOf(chtv),
                                            cts.Select(c => c switch
                                            {
                                                ChangeType.Set => i18n.ChangeMode_Set,
                                                ChangeType.Add => i18n.ChangeMode_Add,
                                                ChangeType.Random => i18n.ChangeMode_Random,
                                                ChangeType.Copy => i18n.ChangeMode_Copy,
                                                _ => "?"
                                            }).ToArray()
                                            ));
                                        chtv = cts[value];
                                        if (changed)
                                            cht.Value = chtv;
                                        xoff += 65 + Spacing;
                                    }
                                    var toBool = dstType == ParamType.Bool;
                                    var dstRangeEndLabelw = 0f;
                                    {
                                        var label = new GUIContent("]");
                                        dstRangeEndLabelw = label.CalcSize(EditorStyles.label).x + Spacing;
                                    }
                                    var dstMin = 0;
                                    var dstMax = dstType == ParamType.Int ? 255 : 1;
                                    switch (chtv)
                                    {
                                        case ChangeType.Set:
                                        case ChangeType.Add:
                                            {
                                                var val = el.FindProperty<float>(nameof(BindSetting.Value));
                                                if (toBool)
                                                {
                                                    var (changed, value) = EditorUtils.GUIChangeCheckField(
                                                        () => EditorGUI.Toggle(new(rect.x + xoff, rect.y, CheckBoxSize, InlineHeight), val.Value != 0));
                                                    xoff += CheckBoxSize + Spacing;
                                                    if (changed)
                                                        val.Value = value ? 1 : 0;
                                                }
                                                else
                                                {
                                                    var w = Mathf.Min(rect.width - xoff - dstRangeEndLabelw, 60);
                                                    var (changed, value) = EditorUtils.GUIChangeCheckField(() => EditorGUI.FloatField(
                                                        new(rect.x + xoff, rect.y, w, InlineHeight),
                                                        val.Value.LimitToRange(dstMin, dstMax)
                                                        ));
                                                    xoff += w + Spacing;
                                                    if (changed)
                                                        val.Value = value;
                                                }
                                                break;
                                            }
                                        case ChangeType.Random:
                                            {
                                                if (toBool)
                                                {
                                                    var chance = el.FindProperty<float>(nameof(BindSetting.RandomChance));
                                                    var w = Mathf.Min(rect.width - xoff - dstRangeEndLabelw, 400);
                                                    chance.Value = EditorGUI.Slider(new(rect.x + xoff, rect.y, w, InlineHeight), chance.Value, 0, 1);
                                                    xoff += w + Spacing;
                                                }
                                                else
                                                {
                                                    var min = el.FindProperty<float>(nameof(BindSetting.RandomMin));
                                                    var max = el.FindProperty<float>(nameof(BindSetting.RandomMax));
                                                    var aequalsLabelw = 0f;
                                                    {
                                                        var label3 = new GUIContent("～");
                                                        aequalsLabelw = label3.CalcSize(EditorStyles.label).x;
                                                    }
                                                    var w = Mathf.Min((rect.width - xoff - aequalsLabelw - Spacing - dstRangeEndLabelw) / 2, 60);
                                                    {
                                                        var (changed, value) = EditorUtils.GUIChangeCheckField(() => EditorGUI.FloatField(
                                                            new(rect.x + xoff, rect.y, w, InlineHeight),
                                                            min.Value.LimitToRange(dstMin, dstMax)
                                                            ));
                                                        xoff += w + Spacing;
                                                        if (changed)
                                                            min.Value = value;
                                                    }
                                                    EditorGUI.LabelField(new(rect.x + xoff, rect.y, aequalsLabelw, InlineHeight), "～");
                                                    xoff += aequalsLabelw + Spacing;
                                                    {
                                                        var (changed, value) = EditorUtils.GUIChangeCheckField(() => EditorGUI.FloatField(
                                                            new(rect.x + xoff, rect.y, w, InlineHeight),
                                                            max.Value.LimitToRange(dstMin, dstMax)
                                                            ));
                                                        xoff += w + Spacing;
                                                        if (changed)
                                                            max.Value = value;
                                                    }
                                                }
                                                break;
                                            }
                                    }

                                    EditorGUI.LabelField(new(rect.x + xoff, rect.y, dstRangeEndLabelw, InlineHeight), "]");
                                    xoff += dstRangeEndLabelw;
                                    break;
                                }
                            case BindMode.LocalToRemote:
                            case BindMode.RemoteToLocal:
                                {
                                    break;
                                }
                        }
                    },
                };
                BindSettingsCache[bsindex] = bss;
            }
            return bss;
        }

        public float DrawSettingsList(int index, bool measureOnly) => DrawSettingsList(new(0, 0, 500, 0), index, measureOnly);
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
            {
                var label = new GUIContent(i18n.BindMode);
                var sz = label.CalcSize(EditorStyles.label);
                if (!measureOnly)
                    EditorGUI.LabelField(new(rect.x + Spacing, rect.y, sz.x, InlineHeight), label, EditorStyles.label);
                xoff = Spacing + sz.x + Spacing * 2;
            }
            var mode = el.FindProperty<BindMode>(nameof(BinderSetting.BindMode));
            var modev = measureOnly ? mode.Value : BindModeUtils.Modes[EditorGUI.Popup(
                new(rect.x + xoff, rect.y, rect.width - xoff, InlineHeight),
                mode.Value.GetIndex(),
                BindModeUtils.Modes.Select(m => m switch
                {
                    BindMode.LocalAndRemote => i18n.BindMode_LocalAndRemote,
                    BindMode.LocalOnly => i18n.BindMode_LocalOnly,
                    BindMode.RemoteOnly => i18n.BindMode_RemoteOnly,
                    BindMode.LocalToRemote => i18n.BindMode_LocalToRemote,
                    BindMode.RemoteToLocal => i18n.BindMode_RemoteToLocal,
                    _ => "?",
                }).ToArray()
                )];
            mode.Value = modev;
            nextLine();
            // conditions
            var cs = GetConditions(index, el);
            var csh = cs.GetHeight();
            if (!measureOnly)
                cs.DoList(new(rect.x, rect.y, rect.width, csh));
            rect.y += csh + LineSpacing * 2;
            xoff = 0;
            // src
            var labelw = 0f;
            {
                var label1 = new GUIContent(i18n.SourceParameter);
                var label2 = new GUIContent(i18n.DestinationParameter);
                labelw = Mathf.Max(label1.CalcSize(EditorStyles.label).x, label2.CalcSize(EditorStyles.label).x);
            }
            if (!measureOnly)
                EditorGUI.LabelField(new(rect.x + Spacing, rect.y, labelw, InlineHeight), i18n.SourceParameter);
            xoff += Spacing + labelw + Spacing;
            var src = el.FindProperty<string>(nameof(BinderSetting.Src));
            var srcParam = AvatarParamCache.Get(src.Value);
            var srcIsSync = srcParam?.WantSynced;
            var syncLabelw = 0f;
            {
                var label = new GUIContent(i18n.Sync);
                var sz = label.CalcSize(EditorStyles.label);
                syncLabelw = sz.x + Spacing;
            }
            src.DrawParameter(new(rect.x + xoff, rect.y, rect.width - (srcIsSync != null ? (xoff + syncLabelw + CheckBoxSize + Spacing) : xoff), InlineHeight));
            if (!measureOnly && srcIsSync != null)
            {
                GUI.enabled = false;
                EditorGUI.Toggle(new(rect.x + rect.width - syncLabelw - CheckBoxSize, rect.y, CheckBoxSize, InlineHeight), srcIsSync.Value);
                EditorGUI.LabelField(new(rect.x + rect.width - syncLabelw, rect.y, syncLabelw, InlineHeight), i18n.Sync);
                GUI.enabled = true;
            }
            nextLine();
            // dst
            if (!measureOnly)
                EditorGUI.LabelField(new(rect.x + Spacing, rect.y, labelw, InlineHeight), i18n.DestinationParameter);
            xoff += Spacing + labelw + Spacing;
            var dst = el.FindProperty<string>(nameof(BinderSetting.Dst));
            var dstParam = AvatarParamCache.Get(dst.Value);
            var dstIsSync = dstParam?.WantSynced;
            dst.DrawParameter(new(rect.x + xoff, rect.y, rect.width - (dstIsSync != null ? (xoff + syncLabelw + CheckBoxSize + Spacing) : xoff), InlineHeight));
            if (!measureOnly && dstIsSync != null)
            {
                GUI.enabled = false;
                EditorGUI.Toggle(new(rect.x + rect.width - syncLabelw - CheckBoxSize, rect.y, CheckBoxSize, InlineHeight), dstIsSync.Value);
                EditorGUI.LabelField(new(rect.x + rect.width - syncLabelw, rect.y, syncLabelw, InlineHeight), i18n.Sync);
                GUI.enabled = true;
            }
            nextLine();
            // bind settings
            var srcType = srcParam?.ParameterType;
            switch (modev)
            {
                case BindMode.LocalAndRemote:
                case BindMode.LocalOnly:
                case BindMode.RemoteOnly:
                    {
                        BindModes[index] = modev;
                        SrcTypes[index] = srcType;
                        DstTypes[index] = dstParam?.ParameterType;
                        var bss = GetBindSettings(index, el);
                        var bssh = bss.GetHeight();
                        if (!measureOnly)
                            bss.DoList(new(rect.x, rect.y, rect.width, bssh));
                        rect.y += bssh + LineSpacing * 2;
                        if (!measureOnly)
                        {
                            MessageType tipType = MessageType.Info;
                            string tipMsg = i18n.AllBindingConditionsMutuallyExclusive;
                            if (srcType != null)
                            {
                                var bcs = Enumerable.Range(0, bss.count)
                                    .Select(i =>
                                    {
                                        var bs = bss.GetArrayElementAtIndex(i);
                                        var mode = bs.FindProperty<ConditionMode>(nameof(BindSetting.Mode)).Value.ValidOrDefault(srcType.Value);
                                        var val = bs.FindProperty<float>(nameof(BindSetting.Threshold)).Value;
                                        var val2 = bs.FindProperty<float>(nameof(BindSetting.Threshold2)).Value;
                                        return (mode, val, val2);
                                    })
                                    .ToList();
                                if (bcs.Count > 1)
                                {
                                    switch (srcType)
                                    {
                                        case ParamType.Float:
                                            {
                                                if (bcs.Any(bc => bc.mode == ConditionMode.InRange || bc.mode == ConditionMode.OutOfRange))
                                                {
                                                    tipType = MessageType.Error;
                                                    tipMsg = i18n.AllBindingConditionsMustBeMutuallyExclusive_InOutRange;
                                                    break;
                                                }
                                                if (bcs.Count > 2 || bcs[0].mode == bcs[1].mode)
                                                {
                                                    tipType = MessageType.Error;
                                                    tipMsg = i18n.AllBindingConditionsMustBeMutuallyExclusive;
                                                    break;
                                                }
                                                var ge = bcs[0].mode == ConditionMode.Greater ? bcs[0].val : bcs[1].val;
                                                var le = bcs[0].mode == ConditionMode.Greater ? bcs[1].val : bcs[0].val;
                                                if (ge < le)
                                                {
                                                    tipType = MessageType.Error;
                                                    tipMsg = i18n.AllBindingConditionsMustBeMutuallyExclusive_OverlappingScope;
                                                    break;
                                                }
                                                if (bcs.GetRangeInfo(ParamType.Float).IsOverlapping)
                                                {
                                                    tipType = MessageType.Error;
                                                    tipMsg = i18n.AllBindingConditionsMustBeMutuallyExclusive;
                                                }
                                            }
                                            break;
                                        case ParamType.Int:
                                            {
                                                var vlst = Enumerable.Range(0, 256).ToHashSet();
                                                if (bcs.Any(bc => bc.mode == ConditionMode.InRange || bc.mode == ConditionMode.OutOfRange))
                                                {
                                                    tipType = MessageType.Error;
                                                    tipMsg = i18n.AllBindingConditionsMustBeMutuallyExclusive_InOutRange;
                                                    break;
                                                }
                                                if (bcs.Any(bc => bc.mode == ConditionMode.LessEquals && (int)bc.val == 255))
                                                {
                                                    tipType = MessageType.Error;
                                                    tipMsg = i18n.AllBindingConditionsMustBeMutuallyExclusive_IntLE255;
                                                    break;
                                                }
                                                if (bcs.Any(bc => bc.mode == ConditionMode.GreaterEquals && (int)bc.val == 0))
                                                {
                                                    tipType = MessageType.Error;
                                                    tipMsg = i18n.AllBindingConditionsMustBeMutuallyExclusive_IntGE0;
                                                    break;
                                                }
                                                if (bcs.GetRangeInfo(ParamType.Int).IsOverlapping)
                                                {
                                                    tipType = MessageType.Error;
                                                    tipMsg = i18n.AllBindingConditionsMustBeMutuallyExclusive;
                                                }
                                            }
                                            break;
                                        case ParamType.Bool:
                                        case ParamType.Trigger:
                                            if (bcs.GroupBy(c => c.mode).Any(g => g.Count() > 1))
                                            {
                                                tipType = MessageType.Error;
                                                tipMsg = i18n.AllBindingConditionsMustBeMutuallyExclusive;
                                            }
                                            break;
                                    }
                                }
                                else if (bcs[0].mode == ConditionMode.InRange)
                                    tipMsg = i18n.ParameterValueInRangeTips;
                                else if (bcs[0].mode == ConditionMode.OutOfRange)
                                    tipMsg = i18n.ParameterValueOutOfRangeTips;
                            }
                            else
                            {
                                tipType = MessageType.Warning;
                                tipMsg = i18n.PleaseManualCheckBindingConditionsMutuallyExclusive;
                            }
                            EditorGUI.HelpBox(new(rect.x, rect.y, rect.width, LineHeight * 2 - LineSpacing), tipMsg, tipType);
                        }
                        rect.y += LineHeight * 2;
                        break;
                    }
                case BindMode.LocalToRemote:
                case BindMode.RemoteToLocal:
                    break;
            }
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
                        BindSettingsCache.Remove(oldIndex);
                        BindSettingsCache.Remove(newIndex);
                    },
                };
            }
            SettingsList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
