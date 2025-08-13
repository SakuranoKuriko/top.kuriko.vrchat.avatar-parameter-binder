using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace top.kuriko.Unity.VRChat.NDMF.AvatarParameterBinder
{
    public static class AvatarParamDriverUtils
    {
        public static void AddDriver(this AnimatorState state, bool localOnly, IEnumerable<VRC_AvatarParameterDriver.Parameter> parameters)
        {
            var driver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
            driver.parameters = parameters.ToList();
            driver.localOnly = localOnly;
            state.behaviours = (state.behaviours ?? Array.Empty<StateMachineBehaviour>())
                .Append(driver)
                .ToArray();
        }

        public static void AddDriver(this AnimatorState state, bool localOnly, params VRC_AvatarParameterDriver.Parameter[] parameters)
        => AddDriver(state, localOnly, parameters.AsEnumerable());
        
        public static void AddDriver(this AnimatorState state, IEnumerable<VRC_AvatarParameterDriver.Parameter> parameters)
        => AddDriver(state, false, parameters);

        public static void AddDriver(this AnimatorState state, params VRC_AvatarParameterDriver.Parameter[] parameters)
        => AddDriver(state, false, parameters.AsEnumerable());

        public static void AddIsLocalInitDriver(this AnimatorState state, string paramName)
        => AddDriver(state, true, AvatarParamUtils.CreateDriveParamSet(paramName, 1f));

    }
}
