using System;
using MonoMod.RuntimeDetour;
using System.Reflection;
using BepInEx.Logging;
using RainMeadow;

namespace GoodMorningRainMeadow
{
    public static class ChatHudHook
    {
        private static MonoMod.RuntimeDetour.Hook chatHudDrawHook;
        public static bool ShouldRenderChat = false;

        public static void Initialize(ManualLogSource logger)
        {
            try 
            {
                // 从RainMeadow程序集获取ChatHud类型
                Type chatHudType = References.RainMeadowAssembly.GetType("RainMeadow.ChatHud");
                if (chatHudType == null)
                {
                    throw new Exception("无法找到ChatHud类型");
                }

                chatHudDrawHook = new Hook(
                    chatHudType.GetMethod("Draw", 
                        BindingFlags.Public | BindingFlags.Instance),
                    typeof(ChatHudHook).GetMethod(nameof(HookChatHudDraw))
                );
                
                logger.LogInfo("ChatHud Draw Hook已成功安装");
            }
            catch (Exception ex)
            {
                logger.LogError($"ChatHud Draw Hook安装失败: {ex.Message}");
            }
        }

        public static void Cleanup()
        {
            chatHudDrawHook?.Dispose();
        }

        public static void HookChatHudDraw(
            Action<object, float> orig, 
            object self,
            float timeStacker
        ) {
            if (ShouldRenderChat)
            {
                orig(self, timeStacker);
            }
            // 如果ShouldRenderChat为false，则不执行任何渲染
        }
    }
} 