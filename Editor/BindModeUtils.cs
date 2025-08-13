using System.Collections.Generic;
using System;
using System.Linq;

namespace top.kuriko.Unity.VRChat.NDMF.AvatarParameterBinder.Editor
{
    public static class BindModeUtils
    {
        public static readonly BindMode[] Modes = (BindMode[])Enum.GetValues(typeof(BindMode));
        static readonly IReadOnlyDictionary<BindMode, int> ModeIndexes
        = Modes.Select((v, i) => (v, i)).ToDictionary(v => v.v, v => v.i);

        public static int GetIndex(this BindMode mode) => ModeIndexes[mode];

        public static bool IsBiDirection(this BindMode mode) => mode == BindMode.LocalToRemote || mode == BindMode.RemoteToLocal;
    }
}
