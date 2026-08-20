using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Menu.Remix.MixedUI;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace GoodMorningRainMeadow
{
    /// <summary>
    /// 让全游戏的Remix文本框接受中文。
    ///
    /// 原版OpTextBox.set_value拿正则白名单卡死了非ASCII字符：
    ///   Accept.StringEng   -> ^[a-zA-Z/s]+$
    ///   Accept.StringASCII -> ^[ -~/s]+$
    /// 整串不匹配就直接return，值根本写不进去，而Accept枚举
    /// （Int/Float/StringEng/StringASCII）没有任何允许Unicode的档位，
    /// 所以光开输入法没用，字符会卡在最后一步。
    ///
    /// 选set_value而不是KeyboardAccept，是因为它是所有赋值路径的必经之处：
    /// 键盘输入、剪贴板粘贴、代码直接赋值都要过它。在这里放行一次，
    /// 全游戏的文本框就都覆盖到了，包括其它模组自己new出来的OpTextBox。
    /// 而且长度上限、空格规则、改值音效、OnValueChanged通知这些
    /// 仍然走原版逻辑，不需要在模组里重写一遍。
    ///
    /// 这里手搓ILHook而不用IL.OpTextBox.set_value，是因为HookGen不给属性访问器
    /// 生成钩子——HOOKS-Assembly-CSharp里只有KeyboardAccept、Change这类普通方法，
    /// set_value根本不存在，只能靠反射拿到MethodInfo自己挂。
    /// </summary>
    internal static class RemixUnicodeHook
    {
        private static readonly Regex NonAscii = new Regex(@"[^\x00-\x7F]", RegexOptions.Compiled);

        /// <summary>
        /// ILHook被回收时会自动解挂，必须拿静态字段按住它
        /// </summary>
        private static ILHook hook;

        public static void Apply()
        {
            if (hook != null) return;

            try
            {
                // DeclaredOnly：value在UIconfig和OpTextBox里各有一份，
                // 带正则校验的是OpTextBox这份override
                MethodInfo setter = typeof(OpTextBox)
                    .GetProperty("value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    ?.GetSetMethod();

                if (setter == null)
                {
                    DebugHandler.LogError("找不到OpTextBox.set_value，Remix文本框的中文输入不会生效");
                    return;
                }

                hook = new ILHook(setter, AllowUnicode);
            }
            catch (Exception ex)
            {
                DebugHandler.LogError("挂载Remix文本框的中文支持失败", ex);
            }
        }

        /// <summary>
        /// 把set_value里的ASCII白名单校验换成放行非ASCII的版本。
        /// 只改这两处call，方法其余部分原样保留。
        /// </summary>
        private static void AllowUnicode(ILContext il)
        {
            MethodInfo relaxed = typeof(RemixUnicodeHook)
                .GetMethod(nameof(IsMatchIgnoringNonAscii), BindingFlags.NonPublic | BindingFlags.Static);

            var cursor = new ILCursor(il);
            int patched = 0;

            while (cursor.TryGotoNext(MoveType.Before, IsAsciiWhitelistCheck))
            {
                cursor.Next.Operand = il.Import(relaxed);
                cursor.Index++;
                patched++;
            }

            if (patched == 0)
            {
                DebugHandler.LogError(
                    "OpTextBox.set_value里没找到正则校验，原版结构可能变了，Remix文本框的中文输入不会生效");
                return;
            }

            DebugHandler.Log($"已放行Remix文本框的非ASCII字符（改写{patched}处校验）");
        }

        /// <summary>
        /// 认准Regex.IsMatch(string, string)。带RegexOptions的重载参数表不同，
        /// 直接换掉operand会让IL对不上，所以要卡参数个数。
        /// </summary>
        private static bool IsAsciiWhitelistCheck(Instruction instr)
        {
            return instr.MatchCall(typeof(Regex), nameof(Regex.IsMatch))
                && instr.Operand is MethodReference method
                && method.Parameters.Count == 2;
        }

        /// <summary>
        /// 顶替原版的Regex.IsMatch。
        /// 白名单正则一碰到中文就整串不匹配，这里把非ASCII字符先摘掉，
        /// 只拿剩下的ASCII部分按原规则校验，
        /// 等价于"非ASCII一律放行，ASCII部分规则照旧"。
        ///
        /// 这样StringEng框依然拒收数字、StringASCII框依然拒收控制字符，
        /// 放宽的仅仅是Unicode这一档。
        /// </summary>
        private static bool IsMatchIgnoringNonAscii(string input, string pattern)
        {
            if (Regex.IsMatch(input, pattern)) return true;

            string ascii = NonAscii.Replace(input, "");
            // 整串都是非ASCII时没有可校验的部分，直接放行
            return ascii.Length == 0 || Regex.IsMatch(ascii, pattern);
        }
    }
}
