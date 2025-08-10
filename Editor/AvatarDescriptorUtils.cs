using top.kuriko.Unity.Common.Editor;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace top.kuriko.Unity.VRChat.NDMF.AvatarParameterBinder.Editor
{
    public static class AvatarDescriptorUtils
    {
        public static VRCAvatarDescriptor GetAvatar(this SerializedObject obj)
        => obj.targetObject is Component c
           && c.GetComponentInParent<VRCAvatarDescriptor>() is VRCAvatarDescriptor a
           ? a : null;
        public static VRCAvatarDescriptor GetAvatar(this SerializedProperty prop)
        => GetAvatar(prop.serializedObject);
        public static VRCAvatarDescriptor GetAvatar<T>(this SerializedProperty<T> prop) where T : new()
        => GetAvatar(prop.serializedObject);
    }
}
