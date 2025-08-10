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
        public override string QualifiedName => "top.kuriko.unity.vrchat.ndmf.avatar_parameter_binder";

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
                        .DistinctBy(p => p.EffectiveName)
                        .ToDictionary(p => p.EffectiveName, p => (Param: p, Type: p.ParameterType ?? ParamType.Float));
                    var binders = ctx.AvatarRootObject
                        .GetComponentsInChildren<AvatarParameterBinder>()
                        .ToList();
                    if (binders.Count == 0)
                        return;
                    var bindSettings = binders
                        .SelectMany(d => d.Settings)
                        .ToList();
                    var paramNames = bindSettings
                        .SelectMany(d => d.Conditions.Select(c => c.ParameterName))
                        .Concat(bindSettings.Select(d => d.Src))
                        .Concat(bindSettings.Select(d => d.Dst))
                        .Distinct()
                        .ToHashSet();
                    var invParaNames = paramNames
                        .Where(n => !parameters.ContainsKey(n))
                        .ToList();
                    if (invParaNames.Any())
                        throw new InvalidOperationException($"Parameters not found:\r\n{string.Join("\r\n", invParaNames)}");
                    parameters = parameters.Where(p => paramNames.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value);
                    ParamType GetParamType(string name) => parameters[name].Type;

                    var animator = AnimationUtils.CreateAnimator();
                    // 添加用到的参数
                    foreach (var (name, (_, type)) in parameters)
                        animator.AddParameter(new()
                        {
                            name = name,
                            type = type,
                        });
                    for (var i = 0; i < bindSettings.Count; i++)
                    {
                        var set = bindSettings[i];
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
                        var idle = layer.AddState("idle");
                        var src = parameters[set.Src];
                        var dst = parameters[set.Dst];
                        var bss = set.BindSettings
                            .Reverse()
                            .Distinct((x, y) => x.Mode == y.Mode && x.Threshold.ApproximateEquals(y.Threshold))
                            .Select(bs =>
                            {
                                var nbs = new BindSetting(bs)
                                {
                                    RandomMin = Mathf.Min(bs.RandomMin, 1),
                                    RandomMax = Mathf.Max(bs.RandomMax, 0),
                                };
                                if (dst.Type != ParamType.Int)
                                {
                                    nbs.SrcMin = Mathf.Min(bs.SrcMin, 1);
                                    nbs.SrcMax = Mathf.Max(bs.SrcMax, 0);
                                    nbs.DstMin = Mathf.Min(bs.DstMin, 1);
                                    nbs.DstMax = Mathf.Max(bs.DstMax, 0);
                                }
                                return nbs;
                            })
                            .Reverse()
                            .ToList();
                        switch (set.BindMode)
                        {
                            case BindMode.LocalAndRemote:
                            case BindMode.LocalOnly:
                            case BindMode.RemoteOnly:
                                {
                                    var localOnly = set.BindMode == BindMode.LocalOnly;
                                    var cs = set.Conditions.AsEnumerable();
                                    if (set.BindMode != BindMode.LocalAndRemote)
                                        cs = cs.Append(new(IsLocalParamName, localOnly));
                                    var (noreturn, overlapping) = bss.Select(x => (x.Mode, x.Threshold, x.Threshold2)).GetRangeInfo(src.Type);
                                    if (overlapping)
                                        throw new ArgumentException($"bind setting: value range overlapping\r\nsrc: {src.Param}, type: {src.Type}");
                                    var states = new List<AnimatorState>();
                                    for (var j = 0; j < bss.Count; j++)
                                        states.Add(layer.AddState("#" + j));
                                    for (var j = 0; j < bss.Count; j++)
                                    {
                                        var state = states[j];
                                        var bs = bss[j];
                                        layer.SetStatePosition(state, new(450, j * 200));
                                        var bsc = bs.GetCondition(set);
                                        var cs2 = cs.Append(bsc);
                                        foreach (var acs in cs2.ToAnimatorConditions(GetParamType))
                                        {
                                            idle.AddTransitionTo(state, acs);
                                            for (var k = 0; k < bss.Count; k++)
                                                if (j != k)
                                                    states[k].AddTransitionTo(state, acs);
                                            if (!noreturn)
                                                foreach (var c in cs2.Select(c => c.Invert()).ToAnimatorConditions(GetParamType))
                                                    state.AddTransitionTo(idle, c);
                                        }
                                        state.AddDriver(localOnly, bs.ToDriveParam(set));
                                    }
                                    layer.SetStatePosition(idle, new(0, 200));
                                    break;
                                }
                            case BindMode.LocalToRemote:
                            case BindMode.RemoteToLocal:
                                {
                                    //var linit = layer.AddState("init (local)");
                                    //var rinit = layer.AddState("init (remote)");
                                    //init.AddTransitionTo(linit, new Condition(IsLocalParamName, true));
                                    //init.AddTransitionTo(rinit, new Condition(IsLocalParamName, false));
                                    //var lmet = layer.AddState("condition met (local)");
                                    //var rmet = layer.AddState("condition met (remote)");
                                    //var lnmet = layer.AddState("condition not met (local)");
                                    //var rnmet = layer.AddState("condition not met (remote)");
                                    //var toRemote = set.BindMode == BindMode.LocalToRemote;
                                    //if (toRemote)
                                    //{
                                    //    AddSingleDirectionSync(
                                    //        linit,
                                    //        lmet,
                                    //        lnmet,
                                    //        BindMode.LocalOnly,
                                    //        set.Conditions,
                                    //        set.Src,
                                    //        set.Dst,
                                    //        set.SyncTarget1,
                                    //        set.UseSyncTarget2 ? set.SyncTarget2 : null
                                    //        );
                                    //    AddSingleDirectionSync(
                                    //        rinit,
                                    //        rmet,
                                    //        rnmet,
                                    //        BindMode.RemoteOnly,
                                    //        set.Conditions,
                                    //        set.Dst,
                                    //        set.Src,
                                    //        set.ReverseSyncTarget1,
                                    //        set.UseReverseSyncTarget2 ? set.ReverseSyncTarget2 : null
                                    //        );
                                    //}
                                    //else
                                    //{
                                    //    AddSingleDirectionSync(
                                    //        rinit,
                                    //        rmet,
                                    //        rnmet,
                                    //        BindMode.RemoteOnly,
                                    //        set.Conditions,
                                    //        set.Src,
                                    //        set.Dst,
                                    //        set.SyncTarget1,
                                    //        set.UseSyncTarget2 ? set.SyncTarget2 : null
                                    //        );
                                    //    AddSingleDirectionSync(
                                    //        linit,
                                    //        lmet,
                                    //        lnmet,
                                    //        BindMode.LocalOnly,
                                    //        set.Conditions,
                                    //        set.Dst,
                                    //        set.Src,
                                    //        set.ReverseSyncTarget1,
                                    //        set.UseReverseSyncTarget2 ? set.ReverseSyncTarget2 : null
                                    //        );
                                    //}
                                    break;
                                }
                        }
                    }
                    var merger = ctx.AvatarRootObject.AddComponent<ModularAvatarMergeAnimator>();
                    merger.animator = animator;
                    merger.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
                    merger.matchAvatarWriteDefaults = true;

                    foreach (var binder in binders)
                        UnityObj.DestroyImmediate(binder);
                });
    }
}
