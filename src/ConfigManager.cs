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
        public static ConfigEntry<bool> DisableMeadowChat;
        public static ConfigEntry<bool> EnableChineseInput;
        public static ConfigEntry<KeyCode> ToggleInputBoxKey;
        
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
                true, 
                "是否启用日志输出，发布时可以设置为false以提高性能"
            );
            
            VerboseLogging = config.Bind(
                "调试", 
                "详细日志", 
                false, 
                "是否启用详细日志输出，仅在调试时使用"
            );
            
            // 功能设置
            DisableMeadowChat = config.Bind(
                "功能", 
                "隐藏雨甸聊天框", 
                false, 
                "是否隐藏Rain Meadow的原生聊天框显示（不会影响消息接收）"
            );
            
            EnableChineseInput = config.Bind(
                "功能", 
                "启用中文输入", 
                false, 
                "是否启用中文输入功能"
            );
            
            ToggleInputBoxKey = config.Bind(
                "按键", 
                "切换输入框按键", 
                KeyCode.T, 
                "用于切换中文输入框显示的按键"
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
            
            DebugHandler.Log($"应用配置: 启用日志={EnableLogging.Value}, 详细日志={VerboseLogging.Value}, 启用中文输入={EnableChineseInput.Value}");
        }
        
        /// <summary>
        /// 获取T键的Rewired按键代码
        /// </summary>
        /// <returns>Rewired按键代码</returns>
        public static int GetToggleInputBoxKeyCode()
        {
            // 默认T键的Rewired代码是46
            switch (ToggleInputBoxKey.Value)
            {
                case KeyCode.T: return 46;
                // 可以根据需要添加更多按键映射
                default: return 46; // 默认使用T键
            }
        }
    }
} 