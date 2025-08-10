using System;
using UnityEngine;
using static VRC.SDKBase.VRC_AvatarParameterDriver;

namespace top.kuriko.Unity.VRChat.NDMF.AvatarParameterBinder
{
    [Serializable]
    public class BindSetting : ICloneable
    {
        [SerializeField]
        public ConditionMode Mode = ConditionMode.If;
        [SerializeField]
        public float Threshold;
        [SerializeField]
        public float Threshold2;
        [SerializeField]
        public ChangeType ChangeType = ChangeType.Set;

        // set/add
        [SerializeField]
        public float Value;

        // copy
        [SerializeField]
        public bool ConvertRange = false;
        // copy: convert range
        [SerializeField]
        public float SrcMin = 0f;
        [SerializeField]
        public float SrcMax = 1f;
        [SerializeField]
        public float DstMin = 0f;
        [SerializeField]
        public float DstMax = 1f;

        // random
        [SerializeField]
        public float RandomChance = 1f;
        [SerializeField]
        public float RandomMin = 0f;
        [SerializeField]
        public float RandomMax = 1f;

        public BindSetting() { }
        public BindSetting(BindSetting other)
        {
            Mode = other.Mode;
            Threshold = other.Threshold;
            Threshold2 = other.Threshold2;
            ChangeType = other.ChangeType;

            Value = other.Value;

            ConvertRange = other.ConvertRange;
            SrcMin = other.SrcMin;
            SrcMax = other.SrcMax;
            DstMin = other.DstMin;
            DstMax = other.DstMax;

            RandomChance = other.RandomChance;
            RandomMin = other.RandomMin;
            RandomMax = other.RandomMax;
        }

        public virtual BindSetting Clone() => new(this);
        object ICloneable.Clone() => Clone();
    }
}
