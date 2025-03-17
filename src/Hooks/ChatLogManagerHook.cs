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

        public static void Initialize(ManualLogSource logger)
        {
            try 
            {
                // 从RainMeadow程序集获取ChatLogManager类型
                Type chatLogManagerType = References.RainMeadowAssembly.GetType("RainMeadow.ChatLogManager");
                if (chatLogManagerType == null)
                {
                    throw new Exception("无法找到ChatLogManager类型");
                }

                // 获取LogMessage方法
                MethodInfo logMessageMethod = chatLogManagerType.GetMethod("LogMessage", 
                    BindingFlags.Public | BindingFlags.Static);
                if (logMessageMethod == null)
                {
                    throw new Exception("无法找到LogMessage方法");
                }

                chatLogMessageHook = new Hook(
                    logMessageMethod,
                    typeof(ChatLogManagerHook).GetMethod(nameof(HookLogMessage))
                );
                
                logger.LogInfo("ChatLogManager LogMessage Hook已成功安装");
            }
            catch (Exception ex)
            {
                logger.LogError($"ChatLogManager LogMessage Hook安装失败: {ex.Message}");
            }
        }

        public static void Cleanup()
        {
            chatLogMessageHook?.Dispose();
        }

        public static void HookLogMessage(
            Action<string, string> orig,
            string username,
            string message
        ) {
            // 调用原始方法
            orig(username, message);

            try
            {
                // 将消息转发给GChatHud
                if (GChatHud.Instance != null)
                {
                    // GChatHud.handleMessage(username, message);
                    // GChatHud.NewChatLine(username, message, 240, GHUDStatic.GHUDwhite);
                    GChatHud.Instance.AddMessage(username, message);
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
} 