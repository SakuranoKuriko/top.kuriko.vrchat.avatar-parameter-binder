using System;
using System.Collections.Generic;
using System.Linq;
using top.kuriko.Common;
using ParamType = UnityEngine.AnimatorControllerParameterType;
using DriveParam = VRC.SDKBase.VRC_AvatarParameterDriver.Parameter;
using static VRC.SDKBase.VRC_AvatarParameterDriver;

namespace top.kuriko.Unity.VRChat.NDMF.AvatarParameterBinder
{
    public delegate bool TryGetParameterType(string parameterName, out ParamType type);

    public static class AvatarParamUtils
    {
        public static DriveParam CreateDriveParamSet(string paramName, bool val)
        => new()
        {
            type = ChangeType.Set,
            name = paramName,
            value = val ? 1f : 0f,
        };
        public static DriveParam CreateDriveParamSet(string paramName, int val)
        => new()
        {
            type = ChangeType.Set,
            name = paramName,
            value = val,
        };
        public static DriveParam CreateDriveParamSet(string paramName, float val)
        => new()
        {
            type = ChangeType.Set,
            name = paramName,
            value = val,
        };
        public static DriveParam CreateDriveParamSetTrigger(string paramName)
        => new()
        {
            type = ChangeType.Set,
            name = paramName,
        };

        public static DriveParam CreateDriveParamAdd(string paramName, int addVal)
        => new()
        {
            type = ChangeType.Add,
            name = paramName,
            value = addVal,
        };
        public static DriveParam CreateDriveParamAdd(string paramName, float addVal)
        => new()
        {
            type = ChangeType.Add,
            name = paramName,
            value = addVal,
        };

        public static DriveParam CreateDriveParamRandom(string paramName, float chance)
        => new()
        {
            type = ChangeType.Random,
            name = paramName,
            chance = chance,
        };
        public static DriveParam CreateDriveParamRandom(string paramName, float chance, int min, int max)
        => new()
        {
            type = ChangeType.Random,
            name = paramName,
            chance = chance,
            valueMin = min,
            valueMax = max,
        };
        public static DriveParam CreateDriveParamRandom(string paramName, float chance, float min, float max)
        => new()
        {
            type = ChangeType.Random,
            name = paramName,
            chance = chance,
            valueMin = min,
            valueMax = max,
        };
        public static DriveParam CreateDriveParamRandomTrigger(string paramName, float chance)
        => new()
        {
            type = ChangeType.Random,
            name = paramName,
            chance = chance,
        };

        public static DriveParam CreateDriveParamCopy(string srcParam, string dstParam)
        => new()
        {
            type = ChangeType.Copy,
            source = srcParam,
            name = dstParam,
        };
        public static DriveParam CreateDriveParamCopy(string srcParam, string dstParam, float srcMin, float srcMax, float dstMin, float dstMax)
        => new()
        {
            type = ChangeType.Copy,
            source = srcParam,
            name = dstParam,
            convertRange = true,
            sourceMax = srcMax,
            sourceMin = srcMin,
            destMax = dstMax,
            destMin = dstMin,
        };

        public static DriveParam Clone(this DriveParam param)
        => new()
        {
            type = param.type,
            name = param.name,
            source = param.source,
            value = param.value,
            valueMin = param.valueMin,
            valueMax = param.valueMax,
            chance = param.chance,
            convertRange = param.convertRange,
            sourceMin = param.sourceMin,
            sourceMax = param.sourceMax,
            destMin = param.destMin,
            destMax = param.destMax,
            sourceParam = param.sourceParam,
            destParam = param.destParam,
        };

        public static IEnumerable<DriveParam> Invert(this IEnumerable<(DriveParam Param, ParamType Type)> src, bool keepInvertibleParams = false)
        {
            foreach (var (p, ptype) in src)
            {
                if (p.name.IsNullOrEmpty())
                    continue;
                switch (ptype)
                {
                    case ParamType.Float:
                    case ParamType.Int:
                        if (p.type == ChangeType.Add)
                        {
                            yield return CreateDriveParamAdd(p.name, -p.value);
                            continue;
                        }
                        break;
                    case ParamType.Bool:
                        if (p.type == ChangeType.Set)
                        {
                            yield return CreateDriveParamSet(p.name, p.value == 0f ? 1f : 0f);
                            continue;
                        }
                        break;
                }
                if (keepInvertibleParams)
                    yield return p;
            }
            yield break;
        }

        public static IEnumerable<DriveParam> Invert(this IEnumerable<DriveParam> src, TryGetParameterType getParamType, bool keepInvertibleParams = false)
        => src.Select(p =>
            {
                var ok = getParamType(p.name, out var type);
                return (p, ok, type: ok ? type : default);
            })
            .Where(p => p.ok)
            .Select(p => (p.p, p.type))
            .Invert(keepInvertibleParams);

        public static readonly ChangeType[] ChangeTypes = (ChangeType[])Enum.GetValues(typeof(ChangeType));

        static readonly IReadOnlyDictionary<ParamType, ChangeType[]> TypedChangeTypes
        = Enum.GetValues(typeof(ParamType))
            .Cast<ParamType>()
            .Select(t => (t, cts: t switch
            {
                ParamType.Bool => new[]
                {
                    ChangeType.Set,
                    ChangeType.Random,
                    //ChangeType.Copy,
                },
                ParamType.Trigger => new[]
                {
                    ChangeType.Set,
                    ChangeType.Random,
                },
                ParamType.Float or ParamType.Int => new[]
                {
                    ChangeType.Set,
                    ChangeType.Add,
                    ChangeType.Random,
                    //ChangeType.Copy,
                },
                _ => null,
            }))
            .Where(t => t.cts != null)
            .ToDictionary(t => t.t, t => t.cts);

        public static ChangeType[] GetChangeTypes(this ParamType type) => TypedChangeTypes[type];
    }
}
