using UnityEngine;

namespace GoodMorningRainMeadow
{
    /// <summary>
    /// 极简日志封装：统一加前缀，Enabled 一键静默所有日志。
    /// </summary>
    public static class ModLog
    {
        /// <summary>
        /// 日志开关，设为 false 时所有日志静默
        /// </summary>
        public static bool Enabled = true;

        /// <summary>
        /// 日志前缀
        /// </summary>
        private const string LOG_PREFIX = "[早上好现在我有IME] ";

        public static void Log(string message)
        {
            if (Enabled) UnityEngine.Debug.Log(LOG_PREFIX + message);
        }

        public static void LogError(string message)
        {
            if (Enabled) UnityEngine.Debug.LogError(LOG_PREFIX + "错误: " + message);
        }
    }
}
