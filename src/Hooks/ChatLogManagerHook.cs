using System;
using System.Reflection;
using BepInEx.Logging;
using GhostPlayer.GHud;
using MonoMod.RuntimeDetour;

namespace GoodMorningRainMeadow
{
    public static class ChatLogManagerHook
    {
        private static Hook chatLogMessageHook;

        public static void Initialize()
        {
            // 从RainMeadow程序集获取ChatLogManager类型
            Type chatLogManagerType = typeof(RainMeadow.ChatLogManager);
            MethodInfo logMessageMethod = chatLogManagerType.GetMethod("LogMessage",
                BindingFlags.Public | BindingFlags.Static);

            chatLogMessageHook = new Hook(
                logMessageMethod,
                typeof(ChatLogManagerHook).GetMethod(nameof(HookLogMessage))
            );
        }
        public static void Cleanup()
        {
            chatLogMessageHook?.Dispose();
        }

        public static void HookLogMessage(
            Action<string, string> orig,
            string username,
            string message
        )
        {
            // 调用原始方法
            orig(username, message);
            // 将消息转发给GChatHud
            if (GChatHud.Instance != null)
            {
                // GChatHud.handleMessage(username, message);
                // GChatHud.NewChatLine(username, message, 240, GHUDStatic.GHUDwhite);
                GChatHud.Instance.AddMessage(username, message);
            }
        }
    }
}