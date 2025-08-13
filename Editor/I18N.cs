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
            public string Parameter = "参数";
            public string TargetValue = "目标";
            public string SourceParameter = "源参数";
            public string DestinationParameter = "目标参数";
            public string Sync = "同步";
            public string SyncAccuracy = "同步精度";
            public string ReverseSyncAccuracy = "同步精度（反向）";

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
            public string ConvertRangeReversed = "转换值范围（反向）";

            public string CannotWriteToBuiltInParam = "无法修改内置参数";
            public string ParameterValueInRangeTips = "In range（在范围中，包含上下界）";
            public string ParameterValueOutOfRangeTips = "Out of range（不在范围中，不包含上下界）";


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
