using System;
using BepInEx.Configuration;
using UnityEngine;

namespace GoodMorningRainMeadow
{
    /// <summary>
    /// 配置管理器，用于管理插件配置
    /// </summary>
    public static class ConfigManager
    {
        // 配置项
        public static ConfigEntry<bool> EnableLogging;
        public static ConfigEntry<bool> VerboseLogging;
        
        /// <summary>
        /// 初始化配置管理器
        /// </summary>
        /// <param name="config">BepInEx配置</param>
        public static void Initialize(ConfigFile config)
        {
            // 调试设置
            EnableLogging = config.Bind(
                "调试", 
                "启用日志", 
                false, 
                "是否启用日志输出，发布时可以设置为false以提高性能"
            );
            
            VerboseLogging = config.Bind(
                "调试", 
                "详细日志", 
                false, 
                "是否启用详细日志输出，仅在调试时使用"
            );
            
            // 应用配置到调试处理器
            ApplyConfig();
            
            // 监听配置变更
            EnableLogging.SettingChanged += (sender, args) => ApplyConfig();
            VerboseLogging.SettingChanged += (sender, args) => ApplyConfig();
            
            DebugHandler.Log("配置管理器已初始化");
        }
        
        /// <summary>
        /// 应用配置到相关组件
        /// </summary>
        private static void ApplyConfig()
        {
            // 应用调试设置
            DebugHandler.EnableLogging = EnableLogging.Value;
            DebugHandler.VerboseLogging = VerboseLogging.Value;
            
            DebugHandler.Log($"应用配置: 启用日志={EnableLogging.Value}, 详细日志={VerboseLogging.Value}");
        }
    }
} 