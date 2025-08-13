using System;
using UnityEngine;

namespace top.kuriko.Unity.VRChat.NDMF.AvatarParameterBinder
{
    [Serializable]
    public class BindSetting
    {
        [SerializeField]
        public int Accuracy = 2;
        [SerializeField]
        public bool ConvertRange = false;
        [SerializeField]
        public float SrcMin;
        [SerializeField]
        public float SrcMax;
        [SerializeField]
        public float DstMin;
        [SerializeField]
        public float DstMax;
    }
}
