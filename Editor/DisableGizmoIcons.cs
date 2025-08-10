#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using top.kuriko.Unity.VRChat.NDMF.AvatarParameterBinder;

namespace top.kuriko.Unity.VRChat.NDMF.AvatarParameterBinder.Editor
{
    [InitializeOnLoad]
    class DisableGizmoIcons
    {
        static IEnumerable<Type> Types() => new Type[] { typeof(AvatarParameterBinder) };

        static DisableGizmoIcons()
        {
            EditorApplication.delayCall += Next;
        }

        static void Next()
        {
            EditorApplication.delayCall -= Next;
            foreach (var type in Types())
                GizmoUtility.SetIconEnabled(type, false);
        }
    }
}
#endif
