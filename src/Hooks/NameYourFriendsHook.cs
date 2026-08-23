using System;
using System.Reflection;
using System.Text.RegularExpressions;
using MonoMod.RuntimeDetour;

namespace GoodMorningRainMeadow
{
    /// <summary>
    /// Hook NameYourFriends.FilterNameString：原版用[^ -~]白名单把所有
    /// 非ASCII字符（中文等）从生物名字里删掉，这里换成放宽版——
    /// 只删控制字符与&lt;&gt;\（防标签/路径注入），可见Unicode一律保留。
    /// 这一处覆盖NameYourFriends的两条调用路径：
    /// FriendNamingMenu的OnValueUpdate回调和NYF_SetName。
    ///
    /// 软依赖：NameYourFriends没装时静默跳过（遍历程序集找不到类型），
    /// 版本变了改名/改签名同样静默跳过，不影响本模组其它功能。
    /// </summary>
    internal static class NameYourFriendsHook
    {
        /// <summary>
        /// Hook实例必须保存为静态字段，否则被GC回收后hook会失效
        /// </summary>
        private static Hook hook;

        public static void Apply()
        {
            if (hook != null) return;

            try
            {
                // 零编译期引用：按类型全名在已加载程序集里找。
                // NameYourFriends是BepInEx插件，OnModsInit时程序集必然已加载。
                Type type = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = asm.GetType("NameYourFriends.NameYourFriends");
                    if (type != null) break;
                }
                if (type == null) return; // 没装NameYourFriends

                MethodInfo filter = type.GetMethod("FilterNameString",
                    BindingFlags.Static | BindingFlags.Public);
                if (filter == null) return; // 版本变了，静默放弃

                hook = new Hook(filter,
                    typeof(NameYourFriendsHook).GetMethod(nameof(FilterNameStringHook),
                        BindingFlags.Static | BindingFlags.NonPublic));
                ModLog.Log("已hook NameYourFriends.FilterNameString（放行中文名字）");
            }
            catch (Exception)
            {
                // hook失败不影响本模组其余功能
            }
        }

        /// <summary>
        /// 不调用orig：原版[^ -~]白名单会删掉所有非ASCII字符，
        /// 直接换成放宽版过滤。null行为与原版一致（Regex.Replace抛异常）。
        /// </summary>
        private static string FilterNameStringHook(Func<string, string> orig, string name)
        {
            return Regex.Replace(name, @"[\x00-\x1F\x7F<>\\]", "");
        }
    }
}
