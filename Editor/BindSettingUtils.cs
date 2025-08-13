using System;
using UnityEngine;
using static VRC.SDKBase.VRC_AvatarParameterDriver;
using DriveParam = VRC.SDKBase.VRC_AvatarParameterDriver.Parameter;
using ParamType = UnityEngine.AnimatorControllerParameterType;

namespace top.kuriko.Unity.VRChat.NDMF.AvatarParameterBinder.Editor
{
    public static class BindSettingUtils
    {
        public static DriveParam ToDriveParam(this BindSetting set, string src, string dst)
        => !set.ConvertRange ? AvatarParamUtils.CreateDriveParamCopy(src, dst)
            : AvatarParamUtils.CreateDriveParamCopy(src, dst,
                MathF.Min(set.SrcMin, set.SrcMax),
                MathF.Max(set.SrcMin, set.SrcMax),
                MathF.Min(set.DstMin, set.DstMax),
                MathF.Max(set.DstMin, set.DstMax)
                );

        public static int GetMaxSyncAccuracy(this ParamType? type)
        => type switch
        {
            ParamType.Float => 201,
            ParamType.Int => 256,
            ParamType.Bool or ParamType.Trigger => 2,
            _ => 201,
        };
        public static int GetMaxSyncAccuracy(this ParamType type)
        => GetMaxSyncAccuracy((ParamType?)type);
    }
}
