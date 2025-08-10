using UnityEngine;
using System;

namespace top.kuriko.Unity.VRChat.NDMF.AvatarParameterBinder
{
    [Serializable]
    public class Condition
    {
        [SerializeField]
        public string ParameterName;
        [SerializeField]
        public ConditionMode Mode;
        [SerializeField]
        public float Threshold;
        [SerializeField]
        public float Threshold2;

        public Condition() { }

        public Condition(string parameterName, ConditionMode mode, float threshold, float threshold2)
        => (ParameterName, Mode, Threshold, Threshold2) = (parameterName, mode, threshold, threshold2);

        public Condition(string parameterName, ConditionMode mode, float threshold) : this(parameterName, mode, threshold, 0) { }
        
        public Condition(string parameterName, bool boolMode) : this(parameterName, boolMode ? ConditionMode.If : ConditionMode.IfNot, 1f) { }

    }
}
