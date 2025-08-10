using System;
using System.Globalization;
using System.IO;
using System.Text;
using top.kuriko.Common;
using UnityEngine;

namespace top.kuriko.Unity.VRChat.NDMF.AvatarParameterBinder.Editor
{
    public partial class AvatarParameterBinderEditor
    {
        [Serializable]
        public class I18N
        {
            public string BindSetting = "绑定设置";
            public string BindMode = "模式";
            public string PreCondition = "前置条件";
            public string BindCondition = "绑定条件";
            public string Parameter = "参数";
            public string TargetValue = "目标";
            public string SourceParameter = "源参数";
            public string DestinationParameter = "目标参数";
            public string Sync = "同步";

            public string BindMode_LocalAndRemote = "本地及远程";
            public string BindMode_LocalOnly = "仅本地";
            public string BindMode_RemoteOnly = "仅远程";
            public string BindMode_LocalToRemote = "本地至远程";
            public string BindMode_RemoteToLocal = "远程至本地";

            public string ChangeMode_Set = "设置";
            public string ChangeMode_Add = "添加";
            public string ChangeMode_Random = "随机";
            public string ChangeMode_Copy = "复制";

            public string Min = "最小";
            public string Max = "最大";
            public string ConvertRange = "转换值范围";

            public string ParameterValueInRangeTips = "In range（在范围中，包含上下界）";
            public string ParameterValueOutOfRangeTips = "Out of range（不在范围中，不包含上下界）";
            public string AllBindingConditionsMutuallyExclusive = "所有绑定条件互斥";
            public string AllBindingConditionsMustBeMutuallyExclusive = "所有绑定条件必须互斥";
            public string AllBindingConditionsMustBeMutuallyExclusive_OverlappingScope = "所有绑定条件必须互斥：\r\n绑定条件指定的范围有重叠";
            public string AllBindingConditionsMustBeMutuallyExclusive_InOutRange = "使用 In range（在范围中，包含上下界）或 Out of range（不在范围中，不包含上下界）绑定条件时，\r\n只能使用一个绑定设置";
            public string AllBindingConditionsMustBeMutuallyExclusive_SeeConsole = "所有绑定条件必须互斥：\r\n详细请检查Unity控制台";
            public string AllBindingConditionsMustBeMutuallyExclusive_IntGE0 = "所有绑定条件必须互斥：\r\n\"int >= 0\" 已包含了允许范围内的所有数值";
            public string AllBindingConditionsMustBeMutuallyExclusive_IntLE255 = "所有绑定条件必须互斥：\r\n\"int <= 255\" 已包含了允许范围内的所有数值";
            public string PleaseManualCheckBindingConditionsMutuallyExclusive = "无法检查绑定条件（无法获得源参数类型），请手动检查并确定所有绑定条件互斥";


            public static I18N i18n = Load();

            public static I18N Load()
            {
                const string dir1 = "Assets/Kuriko.top/AvatarParameterBinder/lang";
                const string dir2 = "Packages/Kuriko.top/AvatarParameterBinder/lang";
                var ci = CultureInfo.GetCultureInfo(nadena.dev.ndmf.localization.LanguagePrefs.Language) ?? CultureInfo.CurrentUICulture;
                while (ci != null && !ci.Name.IsNullOrEmpty())
                {
                    foreach (var fn in new[] { $"{dir1}/{ci}.json", $"{dir2}/{ci}.json" })
                    {
                        if (File.Exists(fn))
                        {
                            try
                            {
                                return JsonUtility.FromJson<I18N>(Encoding.UTF8.GetString(File.ReadAllBytes(fn)));
                            }
                            catch (Exception) { }
                        }
                    }
                    ci = ci.Parent;
                }
                return new();
            }
        }
    }
}
