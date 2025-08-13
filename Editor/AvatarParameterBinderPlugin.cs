using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor.Animations;
using UnityEngine;
using top.kuriko.Common;
using VRC.SDK3.Avatars.Components;
using UnityEditor;
using top.kuriko.Unity.Common;
using nadena.dev.modular_avatar.core;
using UnityObj = UnityEngine.Object;
using ParamType = UnityEngine.AnimatorControllerParameterType;

[assembly: ExportsPlugin(typeof(top.kuriko.Unity.VRChat.NDMF.AvatarParameterBinder.Editor.AvatarParameterBinderPlugin))]



namespace top.kuriko.Unity.VRChat.NDMF.AvatarParameterBinder.Editor
{
    public class AvatarParameterBinderPlugin : Plugin<AvatarParameterBinderPlugin>
    {
        public override string QualifiedName => "top.kuriko.vrchat.avatar_parameter_binder";

        public override string DisplayName => "Avatar Parameter Binder Plugin";

        const string IsLocalParamName = "IsLocal";

        protected override void Configure()
            => InPhase(BuildPhase.Generating)
                .BeforePlugin("nadena.dev.modular-avatar")
                .Run("Avatar Parameter Binder", ctx =>
                {
                    var parameters = ParameterInfo.ForContext(ctx)
                        .GetParametersForObject(ctx.AvatarRootObject)
                        .SelectMany(p => p.SubParameters())
                        .Concat(AvatarParamUtils.BuiltInParams.Values)
                        .DistinctBy(p => p.EffectiveName)
                        .ToDictionary(p => p.EffectiveName, p => (Param: p, Type: p.ParameterType ?? ParamType.Float));
                    var binders = ctx.AvatarRootObject
                        .GetComponentsInChildren<AvatarParameterBinder>()
                        .ToList();
                    if (binders.Count == 0)
                        return;
                    var binderSettings = binders
                        .SelectMany(d => d.Settings)
                        .ToList();
                    var paramNames = binderSettings
                        .SelectMany(d => d.Conditions.Select(c => c.ParameterName))
                        .Concat(binderSettings.Select(d => d.Src))
                        .Concat(binderSettings.Select(d => d.Dst))
                        .Distinct()
                        .ToHashSet();
                    var invParaNames = paramNames
                        .Where(n => !parameters.ContainsKey(n))
                        .ToList();
                    if (invParaNames.Any())
                        throw new InvalidOperationException($"Parameters not found:\r\n{string.Join("\r\n", invParaNames)}");
                    parameters = parameters.Where(p => paramNames.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value);
                    ParamType? getParamType(string name) => parameters[name].Type;

                    var animator = AnimationUtils.CreateAnimator();
                    // 添加用到的参数
                    animator.AddParameter(new() { name = IsLocalParamName, type = ParamType.Bool });
                    foreach (var (name, (_, type)) in parameters)
                        animator.AddParameter(new()
                        {
                            name = name,
                            type = type,
                        });
                    for (var i = 0; i < binderSettings.Count; i++)
                    {
                        var set = binderSettings[i];
                        switch (set.BindMode)
                        {
                            case BindMode.LocalAndRemote:
                            case BindMode.LocalOnly:
                            case BindMode.RemoteOnly:
                            case BindMode.LocalToRemote:
                            case BindMode.RemoteToLocal:
                                break;
                            default:
                                continue;
                        }
                        var layer = animator.AddLastLayer("Avatar Parameter Binder #" + (i + 1));
                        var init = layer.AddState("Init");
                        layer.SetStatePosition(init, new(0, 200));
                        var directionIndex = 0;
                        var stateIndex = 0;
                        void addSingleDirection(
                            string src,
                            string dst,
                            bool onLocal,
                            bool onRemote,
                            BindSetting syncset
                            )
                        {
                            var srcParam = parameters[src];
                            var dstParam = parameters[dst];
                            var idle = layer.AddState(onLocal ? (onRemote ? "Idle" : "Idle (Local)") : "Idle (Remote)");
                            layer.SetStatePosition(idle, new(300, directionIndex * 200));
                            directionIndex++;
                            if (onLocal && onRemote)
                                init.AddTransitionTo(idle);
                            else init.AddTransitionTo(idle, new Condition(IsLocalParamName, onLocal).ToAnimatorCondition().Value);
                            var cs = set.Conditions.AsEnumerable();
                            var acc = syncset.Accuracy.LimitToRange(2, srcParam.Type.GetMaxSyncAccuracy());
                            List<AnimatorState> states = null;
                            var driver = syncset.ToDriveParam(src, dst);
                            void proc(int index, params Condition[] conditions)
                            {
                                var state = states[index];
                                state.AddDriver(driver);
                                layer.SetStatePosition(state, new(600, stateIndex * 100));
                                stateIndex++;
                                foreach (var c in cs.Concat(conditions).ToAnimatorConditions(getParamType))
                                {
                                    idle.AddTransitionTo(state, c);
                                    for (var i = 0; i < states.Count; i++)
                                        if (index != i)
                                            states[i].AddTransitionTo(state, c);
                                }
                            }
                            switch (srcParam.Type)
                            {
                                case ParamType.Bool:
                                case ParamType.Trigger:
                                    states = new()
                                    {
                                        layer.AddState($"{src} = False"),
                                        layer.AddState($"{src} = True"),
                                    };
                                    proc(0, new Condition(src, false));
                                    proc(1, new Condition(src, true));
                                    break;
                                case ParamType.Int:
                                    {
                                        switch (acc)
                                        {
                                            case 2:
                                            case 4:
                                            case 8:
                                            case 16:
                                            case 32:
                                            case 64:
                                            case 128:
                                                {
                                                    var step = 256 / acc;
                                                    states = Enumerable.Range(0, acc)
                                                        .Select(i => layer.AddState($"{src} in [{step * i}, {step * (i + 1) - 1}]"))
                                                        .ToList();
                                                    for (int i = 0; i < acc; i++)
                                                        proc(i, new Condition(src, ConditionMode.InRange, step * i, step * (i + 1) - 1));
                                                    break;
                                                }
                                            case 256:
                                                states = Enumerable.Range(0, acc)
                                                    .Select(i => layer.AddState($"{src} = {i}"))
                                                    .ToList();
                                                for (var i = 0; i < acc; i++)
                                                    proc(i, new Condition(src, ConditionMode.Equals, i));
                                                break;
                                            default:
                                                {
                                                    var step = 256f / acc;
                                                    var ids = Enumerable.Range(0, acc)
                                                        .Select(i => (int)MathF.Round(i * step))
                                                        .ToList();
                                                    var ranges = new List<(int l, int r)>();
                                                    for (var i = 1; i < ids.Count; i++)
                                                        ranges.Add((ids[i-1], ids[i]));
                                                    states = ranges
                                                        .Select(r => layer.AddState((r.l + 1 == r.r) ? $"{src} = {r.l}" : $"{src} in [{r.l}, {r.r}]"))
                                                        .ToList();
                                                    for (var i = 0; i < ranges.Count; i++)
                                                        proc(i, ranges
                                                            .Select(r => (r.l+1 == r.r)
                                                                ? new Condition(src, ConditionMode.Equals, r.l)
                                                                : new Condition(src, ConditionMode.InRange, r.l, r.r - 1))
                                                            .ToArray()
                                                        );
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                                case ParamType.Float:
                                    {
                                        var step = 2f / acc;
                                        states = Enumerable.Range(0, acc)
                                            .Select(i => layer.AddState($"{src} in ({step * i}, {step * (i + 1)})"))
                                            .ToList();
                                        for (var i = 0; i < acc; i++)
                                            proc(i, new Condition(src, ConditionMode.Greater, step * i), new Condition(src, ConditionMode.Less, step * (i + 1)));
                                    }
                                    break;
                            }
                        }
                        switch (set.BindMode)
                        {
                            case BindMode.LocalAndRemote:
                                addSingleDirection(set.Src, set.Dst, true, true, set.SyncSetting);
                                break;
                            case BindMode.LocalOnly:
                            case BindMode.RemoteOnly:
                                {
                                    var m = set.BindMode == BindMode.LocalOnly;
                                    addSingleDirection(set.Src, set.Dst, m, !m, set.SyncSetting);
                                    break;
                                }
                            case BindMode.LocalToRemote:
                            case BindMode.RemoteToLocal:
                                {
                                    var m = set.BindMode == BindMode.LocalToRemote;
                                    addSingleDirection(set.Src, set.Dst, m, !m, set.SyncSetting);
                                    addSingleDirection(set.Dst, set.Src, !m, m, set.ReverseSyncSetting);
                                    break;
                                }
                        }
                    }
                    //                var states = new List<AnimatorState>();
                    //                for (var j = 0; j < bss.Count; j++)
                    //                    states.Add(layer.AddState("#" + j));
                    //                for (var j = 0; j < bss.Count; j++)
                    //                {
                    //                    var state = states[j];
                    //                    var bs = bss[j];
                    //                    layer.SetStatePosition(state, new(450, j * 200));
                    //                    var bsc = bs.GetCondition(set);
                    //                    var cs2 = cs.Append(bsc);
                    //                    foreach (var acs in cs2.ToAnimatorConditions(GetParamType))
                    //                    {
                    //                        idle.AddTransitionTo(state, acs);
                    //                        for (var k = 0; k < bss.Count; k++)
                    //                            if (j != k)
                    //                                states[k].AddTransitionTo(state, acs);
                    //                        if (!noreturn)
                    //                            foreach (var c in cs2.Select(c => c.Invert()).ToAnimatorConditions(GetParamType))
                    //                                state.AddTransitionTo(idle, c);
                    //                    }
                    //                    state.AddDriver(localOnly, bs.ToDriveParam(set));
                    //                }
                    //                layer.SetStatePosition(idle, new(0, 200));

                    var merger = ctx.AvatarRootObject.AddComponent<ModularAvatarMergeAnimator>();
                    merger.animator = animator;
                    merger.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
                    merger.matchAvatarWriteDefaults = true;

                    foreach (var binder in binders)
                        UnityObj.DestroyImmediate(binder);
                });
    }
}
