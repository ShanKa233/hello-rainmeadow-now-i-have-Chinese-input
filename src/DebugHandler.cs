using System;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Logging;

namespace GoodMorningRainMeadow
{
    /// <summary>
    /// 调试处理器，用于集中管理调试日志输出
    /// </summary>
    public static class DebugHandler
    {
        // 调试开关，设置为false可以禁用所有日志输出
        public static bool EnableLogging = true;
        
        // 详细日志开关，控制是否输出详细的调试信息
        public static bool VerboseLogging = false;
        
        // 日志前缀
        private const string LOG_PREFIX = "[雨甸中文输入] ";
        
        // 日志记录器
        private static ManualLogSource logger;
        
        // 日志缓存，用于在需要时查看最近的日志
        private static readonly List<string> logCache = new List<string>(100);
        private const int MAX_LOG_CACHE = 100;
        
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
            
            // 添加到缓存
            AddToCache(formattedMessage);
            
            // 输出到BepInEx日志
            if (logger != null)
            {
                logger.LogInfo(message);
            }
            
            // 输出到Unity控制台
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
            
            // 添加到缓存
            AddToCache(formattedMessage);
            
            // 输出到BepInEx日志
            if (logger != null)
            {
                logger.LogWarning(message);
            }
            
            // 输出到Unity控制台
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
            
            // 添加到缓存
            AddToCache(formattedMessage);
            
            // 输出到BepInEx日志
            if (logger != null)
            {
                logger.LogError(message);
                if (exception != null)
                {
                    logger.LogError(exception);
                }
            }
            
            // 输出到Unity控制台
            UnityEngine.Debug.LogError(formattedMessage);
        }
        
        /// <summary>
        /// 输出详细日志，仅在VerboseLogging为true时输出
        /// </summary>
        /// <param name="message">详细日志消息</param>
        public static void LogVerbose(string message)
        {
            if (!EnableLogging || !VerboseLogging) return;
            
            string formattedMessage = $"{LOG_PREFIX}详细: {message}";
            
            // 添加到缓存
            AddToCache(formattedMessage);
            
            // 输出到BepInEx日志
            if (logger != null)
            {
                logger.LogDebug(message);
            }
            
            // 输出到Unity控制台
            UnityEngine.Debug.Log(formattedMessage);
        }
        
        /// <summary>
        /// 添加日志到缓存
        /// </summary>
        /// <param name="message">日志消息</param>
        private static void AddToCache(string message)
        {
            logCache.Add($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
            
            // 保持缓存大小不超过最大值
            if (logCache.Count > MAX_LOG_CACHE)
            {
                logCache.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// 获取最近的日志
        /// </summary>
        /// <param name="count">要获取的日志数量</param>
        /// <returns>最近的日志列表</returns>
        public static List<string> GetRecentLogs(int count = 10)
        {
            count = Math.Min(count, logCache.Count);
            return logCache.GetRange(logCache.Count - count, count);
        }
        
        /// <summary>
        /// 将最近的日志输出到控制台
        /// </summary>
        /// <param name="count">要输出的日志数量</param>
        public static void DumpRecentLogs(int count = 10)
        {
            var logs = GetRecentLogs(count);
            UnityEngine.Debug.Log($"{LOG_PREFIX}最近 {logs.Count} 条日志:");
            foreach (var log in logs)
            {
                UnityEngine.Debug.Log(log);
            }
        }
        
        /// <summary>
        /// 清空日志缓存
        /// </summary>
        public static void ClearLogCache()
        {
            logCache.Clear();
            Log("日志缓存已清空");
        }
    }
} 