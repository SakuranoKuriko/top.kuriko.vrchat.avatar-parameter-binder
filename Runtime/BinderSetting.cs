using System;
using UnityEngine;

namespace top.kuriko.Unity.VRChat.NDMF.AvatarParameterBinder
{
    [Serializable]
    public class BinderSetting
    {
        [SerializeField]
        public BindMode BindMode;
        [SerializeField]
        public Condition[] Conditions;
        [SerializeField]
        public string Src;
        [SerializeField]
        public string Dst;
        [SerializeField]
        public BindSetting[] BindSettings;
    }
}
