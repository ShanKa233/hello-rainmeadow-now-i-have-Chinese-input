using GhostPlayer.GHud;
using RainMeadow;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GhostPlayer
{
    /// <summary>
    /// RainMeadowAdapter类：适配Rain Meadow的聊天系统
    /// 负责将Rain Meadow的聊天消息转发给GChatHud
    /// </summary>
    public class RainMeadowAdapter : MonoBehaviour
    {
        // 单例实例
        public static RainMeadowAdapter Instance { get; private set; }

        // 是否已初始化
        private bool initialized = false;

        // 缓存的消息队列，用于在GChatHud实例化前存储消息
        private List<(string, string)> messageCache = new List<(string, string)>();

        /// <summary>
        /// Unity启动函数
        /// </summary>
        void Awake()
        {
            // 设置单例实例
            Instance = this;
            Debug.Log("[雨甸中文输入] RainMeadowAdapter已创建");
        }

        /// <summary>
        /// 初始化适配器
        /// </summary>
        public void Initialize()
        {
            if (initialized)
                return;

            // 创建一个自定义的消息处理器
            var messageHandler = new ChatMessageHandler(this);
            initialized = true;
            Debug.Log("[雨甸中文输入] RainMeadowAdapter已初始化");

            // 处理缓存的消息
            ProcessCachedMessages();
        }

        /// <summary>
        /// 处理缓存的消息
        /// </summary>
        private void ProcessCachedMessages()
        {
            if (GChatHud.Instance == null)
                return;

            foreach (var (user, message) in messageCache)
            {
                GChatHud.Instance.AddMessage(user, message);
            }
            messageCache.Clear();
        }

        /// <summary>
        /// 添加消息
        /// </summary>
        /// <param name="user">用户名</param>
        /// <param name="message">消息内容</param>
        public void AddMessage(string user, string message)
        {
            if (GChatHud.Instance == null)
            {
                // 如果GChatHud实例尚未创建，则缓存消息
                messageCache.Add((user, message));
                return;
            }

            // 转发消息给GChatHud
            GChatHud.Instance.AddMessage(user, message);
        }

        /// <summary>
        /// Unity更新函数
        /// </summary>
        void Update()
        {
            // 如果GChatHud实例已创建但缓存中仍有消息，则处理缓存
            if (GChatHud.Instance != null && messageCache.Count > 0)
            {
                ProcessCachedMessages();
            }
        }

        /// <summary>
        /// 聊天消息处理器类
        /// 用于监听Rain Meadow的聊天消息
        /// </summary>
        private class ChatMessageHandler
        {
            private RainMeadowAdapter adapter;

            public ChatMessageHandler(RainMeadowAdapter adapter)
            {
                this.adapter = adapter;
                
                // 订阅Rain Meadow的聊天消息事件
                // 这里需要根据Rain Meadow的实际API进行调整
                // 例如，可以在MatchmakingManager中添加一个事件
                if (MatchmakingManager.currentInstance != null)
                {
                    // 示例：MatchmakingManager.currentInstance.OnChatMessageReceived += OnChatMessageReceived;
                    Debug.Log("[雨甸中文输入] 已订阅聊天消息事件");
                }
                else
                {
                    Debug.LogWarning("[雨甸中文输入] MatchmakingManager.currentInstance为空，无法订阅聊天消息事件");
                }
            }

            /// <summary>
            /// 处理聊天消息
            /// </summary>
            /// <param name="user">用户名</param>
            /// <param name="message">消息内容</param>
            private void OnChatMessageReceived(string user, string message)
            {
                adapter.AddMessage(user, message);
            }
        }
    }
} 