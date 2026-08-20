using System;
using UnityEngine;
using BepInEx.Logging;

namespace GoodMorningRainMeadow
{
    /// <summary>
    /// 调试处理器，用于控制日志输出
    /// </summary>
    public static class DebugHandler
    {
        /// <summary>
        /// 调试开关，设置为false可以禁用所有日志输出
        /// 开着可以在BepInEx日志里确认聊天框检测是否生效，日志只在开关聊天时各一条，不会刷屏
        /// </summary>
        public static bool EnableLogging = false;
        
        /// <summary>
        /// 日志前缀
        /// </summary>
        private const string LOG_PREFIX = "[雨甸中文输入] ";
        
        /// <summary>
        /// BepInEx日志记录器
        /// </summary>
        private static ManualLogSource logger;
        
        /// <summary>
        /// 初始化调试处理器
        /// </summary>
        /// <param name="logSource">BepInEx日志源</param>
        public static void Initialize(ManualLogSource logSource)
        {
            logger = logSource;
            Log("调试处理器已初始化");
        }
        
        /// <summary>
        /// 输出普通日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public static void Log(string message)
        {
            if (!EnableLogging) return;
            
            string formattedMessage = $"{LOG_PREFIX}{message}";
            
            UnityEngine.Debug.Log(formattedMessage);
        }
        
        /// <summary>
        /// 输出警告日志
        /// </summary>
        /// <param name="message">警告消息</param>
        public static void LogWarning(string message)
        {
            if (!EnableLogging) return;
            
            string formattedMessage = $"{LOG_PREFIX}警告: {message}";
            
            UnityEngine.Debug.LogWarning(formattedMessage);
        }
        
        /// <summary>
        /// 输出错误日志
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="exception">异常对象</param>
        public static void LogError(string message, Exception exception = null)
        {
            if (!EnableLogging) return;
            
            string formattedMessage = $"{LOG_PREFIX}错误: {message}";
            if (exception != null)
            {
                formattedMessage += $"\n{exception.Message}\n{exception.StackTrace}";
            }
            UnityEngine.Debug.LogError(formattedMessage);
        }
    }
} 