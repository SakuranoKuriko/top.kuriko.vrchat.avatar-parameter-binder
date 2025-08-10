using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using VRC.SDK3.Avatars.Components;
using nadena.dev.ndmf;
using System.Linq;
using top.kuriko.Common;
using System;

namespace top.kuriko.Unity.VRChat.NDMF.AvatarParameterBinder.Editor
{
    public class AvatarParamCache
    {
        static GameObject AvatarObject;
        static Dictionary<string, ProvidedParameter> Cache = new();

        public static void Update(SerializedObject obj, bool forceUpdate = false)
        {
            if (AvatarObject == null || forceUpdate)
            {
                AvatarObject = obj.GetAvatar() is VRCAvatarDescriptor a ? a.gameObject : null;
                Cache = AvatarObject == null ? new() :
                    ParameterInfo.ForUI
                        .GetParametersForObject(AvatarObject)
                        .SelectMany(p => p.SubParameters())
                        .Where(p => !p.EffectiveName.IsNullOrEmpty())
                        .Where(p => !p.IsHidden)
                        .DistinctBy(p => p.EffectiveName)
                        .ToDictionary(p => p.EffectiveName);
            }
        }
        public static void Update(SerializedProperty prop, bool forceUpdate = false)
        => Update(prop.serializedObject, forceUpdate);

        public static ProvidedParameter Get(string name)
        => Cache.TryGetValue(name, out var p) ? p : null;

        public static Func<string, ProvidedParameter> GetFunc { get; set; } = Get;
    }
}
