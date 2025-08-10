using System.Collections.Generic;
using System;
using System.Linq;
using static VRC.SDKBase.VRC_AvatarParameterDriver;

namespace top.kuriko.Unity.VRChat.NDMF.AvatarParameterBinder.Editor
{
    public static class ChangeTypeUtils
    {
        public static readonly ChangeType[] Types = Enum.GetValues(typeof(ChangeType))
            .Cast<ChangeType>()
            .ToArray();
        static readonly IReadOnlyDictionary<ChangeType, int> Indexes
        = Types.Select((v, i) => (v, i)).ToDictionary(v => v.v, v => v.i);

        public static int GetIndex(this ChangeType v) => Indexes[v];

    }
}
