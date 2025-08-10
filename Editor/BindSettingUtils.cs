using System;
using System.Security.Cryptography;
using UnityEngine;
using static VRC.SDKBase.VRC_AvatarParameterDriver;
using DriveParam = VRC.SDKBase.VRC_AvatarParameterDriver.Parameter;

namespace top.kuriko.Unity.VRChat.NDMF.AvatarParameterBinder.Editor
{
    public static class BindSettingUtils
    {
        public static DriveParam ToDriveParam(this BindSetting set, string src, string dst) => set.ChangeType switch
        {
            ChangeType.Set => AvatarParamUtils.CreateDriveParamSet(dst, set.Value),
            ChangeType.Add => AvatarParamUtils.CreateDriveParamAdd(dst, set.Value),
            ChangeType.Random => AvatarParamUtils.CreateDriveParamRandom(
                dst, set.RandomChance,
                Mathf.Min(Mathf.Min(set.RandomMin, 1), Mathf.Max(set.RandomMax, 0)),
                Mathf.Max(Mathf.Min(set.RandomMin, 1), Mathf.Max(set.RandomMax, 0))),
            ChangeType.Copy => AvatarParamUtils.CreateDriveParamCopy(
                src, dst,
                Mathf.Min(set.SrcMin, set.SrcMax),
                Mathf.Max(set.SrcMin, set.SrcMax),
                Mathf.Min(set.DstMin, set.DstMax),
                Mathf.Max(set.DstMin, set.DstMax)),
            _ => throw new ArgumentException(),
        };

        public static DriveParam ToDriveParam(this BindSetting set, string dst) => set.ChangeType switch
        {
            ChangeType.Set or ChangeType.Add => ToDriveParam(set, null, dst),
            ChangeType.Copy => throw new InvalidOperationException("please use overloaded function ToDriveParam(string src, string dst)"),
            _ => throw new ArgumentException(),
        };

        public static DriveParam ToDriveParam(this BindSetting set, BinderSetting bset) => ToDriveParam(set, bset.Src, bset.Dst);

        public static Condition GetCondition(this BindSetting set, string src)
        => new(src, set.Mode, set.Threshold, set.Threshold2);

        public static Condition GetCondition(this BindSetting set, BinderSetting bset)
        => new(bset.Src, set.Mode, set.Threshold, set.Threshold2);
    }
}
