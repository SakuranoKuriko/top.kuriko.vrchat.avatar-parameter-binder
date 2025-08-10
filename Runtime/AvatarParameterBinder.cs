using UnityEngine;
using VRC.SDKBase;

namespace top.kuriko.Unity.VRChat.NDMF.AvatarParameterBinder
{
    public class AvatarParameterBinder : MonoBehaviour, IEditorOnly
    {
        [SerializeField]
        public BinderSetting[] Settings;
    }
}
