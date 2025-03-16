﻿using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
// 移除GhostPlayer.Network引用，添加RainMeadow引用
using RainMeadow;
using GoodMorningRainMeadow;
using UnityEngine.Audio;
using Menu;
using HUD;

// 修复说明：
// 1. 替换了OnlineManager.lobby和OnlineManager.players的引用，改为使用MatchmakingManager.currentInstance
// 2. 使用反射安全地获取玩家名称，避免KeyNotFoundException异常
// 3. 添加了更多的错误处理和日志记录，提高代码稳定性
// 4. 增强了对字典访问的安全检查，防止KeyNotFoundException

namespace GhostPlayer.GHud
{
    /// <summary>
    /// GChatHud类：聊天系统的实现
    /// 负责显示和管理聊天消息，处理用户输入的聊天内容
    /// 继承自GHUDPart基类
    /// </summary>
    internal class GChatHud : GHUDPart
    {

        /// <summary>
        /// 下次删除延迟
        /// </summary>
        private float nextDelateDelay = 0f;
        /// <summary>
        /// 单例实例，方便全局访问
        /// </summary>
        public static GChatHud Instance { get; private set; }

        /// <summary>
        /// 命令输入事件，当用户输入以"/"开头的命令时触发
        /// </summary>
        public static event Action<string[]> OnCommandInput;

        /// <summary>
        /// Futile容器，用于添加UI元素
        /// </summary>
        public FContainer Container => hud.container;

        /// <summary>
        /// 最大显示的聊天消息数量
        /// </summary>
        public static int maxDisplayText = 20;

        /// <summary>
        /// 最大历史消息记录数量
        /// </summary>
        private const int MAX_HISTORY_MESSAGES = 30;

        /// <summary>
        /// 聊天消息行列表
        /// </summary>
        public List<ChatLine> lines = new List<ChatLine>();

        /// <summary>
        /// 历史消息记录列表
        /// </summary>
        private List<ChatMessageRecord> messageHistory = new List<ChatMessageRecord>();

        /// <summary>
        /// 上一次isLogToggled的状态
        /// </summary>
        private bool lastLogToggled = false;



        /// <summary>
        /// 玩家颜色字典，用于存储玩家名称与对应的颜色
        /// </summary>
        private Dictionary<string, Color> colorDictionary = new Dictionary<string, Color>();

        /// <summary>
        /// 是否显示历史消息
        /// </summary>
        private bool isHistoryEnabled = false;

        /// <summary>
        /// 上一帧的按键状态
        /// </summary>
        private bool lastKeyPressed = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="hud">所属的GHUD实例</param>
        public GChatHud(GHUD hud) : base(hud)
        {
            // 设置单例实例
            Instance = this;
            // 订阅输入框提交事件
            hud.OnInputFieldSubmit += Hud_OnInputFieldSubmit;


        }

        /// <summary>
        /// 处理输入框提交事件
        /// </summary>
        /// <param name="value">提交的文本</param>
        /// <param name="caretPos">光标位置</param>
        private void Hud_OnInputFieldSubmit(string value, int caretPos)
        {
            // 去除首尾空白
            value = value.Trim();
            if (string.IsNullOrEmpty(value))
                return;

            // 检查是否在线
            bool online = (MatchmakingManager.currentInstance != null);

            // 如果是命令（以/开头）
            if (value[0] == '/')
            {
                // 触发命令输入事件
                if (OnCommandInput != null)
                    OnCommandInput(value.Split(' ').Where(i => !string.IsNullOrWhiteSpace(i)).ToArray());
                return;
            }

            // 获取当前玩家名称
            string playerName = "Player";
            if (online)
            {
                // 尝试获取玩家名称
                var playerManager = MatchmakingManager.currentInstance;
                try
                {
                    // 获取selfPlayer对象
                    var selfPlayerField = playerManager.GetType().GetField("selfPlayer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (selfPlayerField != null)
                    {
                        var selfPlayer = selfPlayerField.GetValue(playerManager);
                        if (selfPlayer != null)
                        {
                            // 尝试获取playerID
                            var playerIDField = selfPlayer.GetType().GetField("playerID", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (playerIDField != null)
                            {
                                var playerID = playerIDField.GetValue(selfPlayer);
                                if (playerID != null)
                                {
                                    playerName = playerID.ToString();
                                    DebugHandler.Log($"[雨甸中文输入] 成功获取玩家ID: {playerName}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugHandler.LogWarning($"[雨甸中文输入] 通过反射获取玩家ID失败: {ex.Message}");
                }
            }

            // 如果不在线，直接显示本地消息
            if (!online)
            {
                NewChatLine($"[{playerName}]", value, (value.Length) * 4 + 240, GHUDStatic.GHUDyellow);
            }

            // 使用Rain Meadow的系统发送消息
            if (online)
            {
                try
                {
                    DebugHandler.Log($"[雨甸中文输入] 尝试发送消息: {value}");
                    MatchmakingManager.currentInstance.SendChatMessage(value);
                    DebugHandler.Log($"[雨甸中文输入] 消息已发送: {value}");
                }
                catch (KeyNotFoundException knfEx)
                {
                    DebugHandler.LogError($"[雨甸中文输入] 发送消息时遇到KeyNotFoundException: {knfEx.Message}");
                    Debug.LogException(knfEx);
                    // 发送失败时显示本地消息
                    NewChatLine($"[{playerName}]", value, (value.Length) * 4 + 240, GHUDStatic.GHUDyellow);
                }
                catch (Exception ex)
                {
                    DebugHandler.LogError($"[雨甸中文输入] 发送消息失败: {ex.Message}");
                    Debug.LogException(ex);
                    // 发送失败时显示本地消息
                    NewChatLine($"[{playerName}]", value, (value.Length) * 4 + 240, GHUDStatic.GHUDyellow);
                }
            }
        }

        /// <summary>
        /// 更新方法，每帧调用
        /// </summary>
        public override void Update()
        {
            // 如果历史消息显示被禁用，确保所有历史消息都被移除
            if (!isHistoryEnabled)
            {
                for (int i = lines.Count - 1; i >= 0; i--)
                {
                    if (lines[i].IsHistoryMessage)
                    {
                        lines[i].ForceDestroy();
                    }
                }
            }

            // 控制消息的显示和消失
            ManageMessageDisplay();

            // 更新所有聊天行
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                lines[i].Update();
            }

            // 如果历史消息显示已启用，但没有任何消息显示，则自动关闭历史消息显示
            if (isHistoryEnabled && lines.Count == 0)
            {
                isHistoryEnabled = false;
                DebugHandler.Log("[雨甸中文输入] 所有消息已消失，自动关闭历史消息显示");
            }

            // 检查是否在线
            if (MatchmakingManager.currentInstance == null)
                return;

            try
            {
                // 获取当前按键状态
                bool currentKeyPressed = Input.GetKey(RainMeadow.RainMeadow.rainMeadowOptions.ChatLogKey.Value);

                if (currentKeyPressed && !lastKeyPressed)
                {
                    // 切换历史消息显示状态
                    ToggleHistoryMessages();
                }
                lastKeyPressed = currentKeyPressed;
            }
            catch (Exception ex)
            {
                DebugHandler.LogError($"[雨甸中文输入] 获取聊天日志按键失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 管理消息的显示和消失
        /// </summary>
        private void ManageMessageDisplay()
        {
            // 获取所有非历史消息
            var nonHistoryMessages = lines.Where(l => !l.IsHistoryMessage).ToList();

            // 如果没有非历史消息，直接返回
            if (nonHistoryMessages.Count == 0)
                return;

            // 减少删除延迟计时器
            if (nextDelateDelay > 0)
            {
                nextDelateDelay -= 1f;
                
                // 当计时器归零时，删除最顶部的消息
                if (nextDelateDelay <= 0)
                {
                    // 如果显示历史消息，则删除最顶部的历史消息
                    if (isHistoryEnabled && messageHistory.Count > 0)
                    {
                        // 查找最顶部的历史消息
                        var historyMessages = lines.Where(l => l.IsHistoryMessage).ToList();
                        if (historyMessages.Count > 0)
                        {
                            var oldestHistoryMessage = historyMessages[0];
                            oldestHistoryMessage.ForceDestroy();
                            DebugHandler.Log("[雨甸中文输入] 计时器到期，删除了最顶部的历史消息");
                            
                            // 从历史记录中也删除这条消息
                            if (messageHistory.Count > 0)
                            {
                                messageHistory.RemoveAt(0);
                                DebugHandler.Log("[雨甸中文输入] 从历史记录中删除了最顶部的消息");
                            }
                            
                            // 重置计时器，如果还有历史消息，则基于下一条消息的长度
                            if (messageHistory.Count > 0)
                            {
                                var nextOldestMessage = messageHistory[0];
                                nextDelateDelay = Math.Max(10f, nextOldestMessage.Message.Length * 2f + 50f);
                            }
                            else
                            {
                                nextDelateDelay = 10f;
                            }
                            DebugHandler.Log($"[雨甸中文输入] 重置删除计时器为: {nextDelateDelay}");
                        }
                    }
                    // 否则删除最顶部的普通消息
                    else if (nonHistoryMessages.Count > 0)
                    {
                        // 获取最顶部的消息（最老的消息）
                        var oldestMessage = nonHistoryMessages[0];
                        oldestMessage.ForceDestroy();
                        DebugHandler.Log("[雨甸中文输入] 计时器到期，删除了最顶部的普通消息");
                        
                        // 重置计时器为之前的一半，最少为10
                        if (nonHistoryMessages.Count > 1)
                        {
                            nextDelateDelay = Math.Max(10f, nextDelateDelay / 2f);
                            DebugHandler.Log($"[雨甸中文输入] 重置删除计时器为: {nextDelateDelay}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 切换历史消息显示状态
        /// </summary>
        public void ToggleHistoryMessages()
        {
            // 切换历史消息显示状态
            isHistoryEnabled = !isHistoryEnabled;

            if (isHistoryEnabled)
            {
                // 启用历史消息显示
                ShowHistoryMessages();
                DebugHandler.Log("[雨甸中文输入] 已启用历史消息显示");
            }
            else
            {
                // 禁用历史消息显示
                HideHistoryMessages();

                // 确保所有历史消息都被立即移除
                for (int i = lines.Count - 1; i >= 0; i--)
                {
                    if (lines[i].IsHistoryMessage)
                    {
                        lines[i].ForceDestroy();
                        DebugHandler.Log("[雨甸中文输入] 强制移除历史消息");
                    }
                }

                DebugHandler.Log("[雨甸中文输入] 已禁用历史消息显示");
            }

            // 重新分配所有非历史消息的生命周期
            ReallocateMessageLifecycles();
        }

        /// <summary>
        /// 重新分配所有非历史消息的生命周期
        /// </summary>
        private void ReallocateMessageLifecycles()
        {
            // 获取所有非历史消息
            var nonHistoryMessages = lines.Where(l => !l.IsHistoryMessage).ToList();

            // 如果没有非历史消息，直接返回
            if (nonHistoryMessages.Count == 0)
                return;

            // 按照在lines中的顺序（从老到新）重新分配生命周期
            // 最老的消息（第一条）将最先消失
            for (int i = 0; i < nonHistoryMessages.Count; i++)
            {
                var msg = nonHistoryMessages[i];
                // 计算新的生命周期：基础生命周期 + 索引 * 20
                // 这样每条消息之间的生命周期差距为20tick
                int newLife = 240 + (i * 20);
                msg.ResetLife(newLife);
                DebugHandler.Log($"[雨甸中文输入] 重新分配消息生命周期: 索引 {i}, 新生命周期 {newLife}");
            }

            // 更新所有消息的位置
            UpdateLinePoses();
        }

        /// <summary>
        /// 显示历史消息
        /// </summary>
        private void ShowHistoryMessages()
        {
            // 保存当前的非历史消息
            var currentMessages = lines.Where(l => !l.IsHistoryMessage).ToList();

            // 清空当前所有消息
            lines.Clear();

            // 按照时间顺序（从老到新）添加历史消息
            for (int i = 0; i < messageHistory.Count; i++)
            {
                var record = messageHistory[i];
                // 创建新的聊天行，使用较长的生命周期
                ChatLine historyLine = new ChatLine(this, record.Color, record.Name, record.Message);
                historyLine.SetAsHistoryMessage();
                lines.Add(historyLine);
            }
            
            // 根据最顶部消息的长度设置删除计时器
            if (messageHistory.Count > 0)
            {
                var oldestMessage = messageHistory[0];
                // 设置较长的计时器，确保历史消息不会立即消失
                nextDelateDelay = oldestMessage.Message.Length * 3f + 100f;
                DebugHandler.Log($"[雨甸中文输入] 显示历史消息时设置删除计时器为: {nextDelateDelay} (基于最顶部消息长度: {oldestMessage.Message.Length})");
            }
            else
            {
                nextDelateDelay = 200f;
                DebugHandler.Log("[雨甸中文输入] 没有历史消息，设置默认计时器为: 200");
            }
            
            // 按照添加顺序（从老到新）添加当前消息
            foreach (var msg in currentMessages)
            {
                lines.Add(msg);
            }

            // 更新所有消息的位置
            UpdateLinePoses();

            // 重新分配所有非历史消息的生命周期
            ReallocateMessageLifecycles();
        }

        /// <summary>
        /// 隐藏历史消息
        /// </summary>
        private void HideHistoryMessages()
        {
            // 先移除所有历史消息
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (lines[i].IsHistoryMessage)
                {
                    lines[i].ForceDestroy();
                }
            }

            // 保存当前的非历史消息
            var currentMessages = lines.Where(l => !l.IsHistoryMessage).ToList();

            // 清空当前所有消息
            lines.Clear();

            // 按照添加顺序（从老到新）添加当前消息
            foreach (var msg in currentMessages)
            {
                lines.Add(msg);
            }

            // 更新所有消息的位置
            UpdateLinePoses();

            // 重新分配所有非历史消息的生命周期
            ReallocateMessageLifecycles();

            // 确保所有历史消息都被销毁
            foreach (var historyMessage in messageHistory)
            {
                DebugHandler.Log($"[雨甸中文输入] 历史消息已隐藏: {historyMessage.Name} - {historyMessage.Message}");
            }
        }

        /// <summary>
        /// 添加消息到历史记录
        /// </summary>
        private void AddToHistory(string name, string message, Color color)
        {
            // 如果历史记录已满，移除最早的消息
            if (messageHistory.Count >= MAX_HISTORY_MESSAGES)
            {
                messageHistory.RemoveAt(0);
            }

            // 添加新消息到历史记录
            messageHistory.Add(new ChatMessageRecord(name, message, color));
        }

        /// <summary>
        /// 绘制方法，每帧调用
        /// </summary>
        /// <param name="timeStacker">时间插值器，用于平滑动画</param>
        public override void Draw(float timeStacker)
        {
            // 绘制所有聊天行
            foreach (var line in lines)
            {
                line.Draw(timeStacker);
            }
        }

        /// <summary>
        /// 清理精灵资源
        /// </summary>
        public override void ClearSprites()
        {
            base.ClearSprites();
            // 销毁所有聊天行
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                lines[i].Destroy();
            }
        }

        /// <summary>
        /// 创建新的聊天消息
        /// </summary>
        /// <param name="name">发送者名称</param>
        /// <param name="message">消息内容</param>
        /// <param name="life">消息显示时间</param>
        /// <param name="color">消息颜色</param>
        public static void NewChatLine(string name, string message, int life, Color color)
        {
            // 如果实例不存在，则不处理
            if (Instance == null)
                return;

            // 如果是历史消息（原来使用int.MaxValue），使用较长但有限的生命周期
            bool isHistoryMessage = false;
            if (life == int.MaxValue)
            {
                life = 600; // 约10秒
                isHistoryMessage = true;
            }
            else
            {
                // 根据消息长度计算生命周期
                life = message.Length * 4 + 240;
                DebugHandler.Log($"[雨甸中文输入] 新消息生命周期: {life} (消息长度: {message.Length})");
            }

            ChatLine newLine = new ChatLine(Instance, color, name, message);

            // 如果是历史消息，设置标记
            if (isHistoryMessage)
            {
                newLine.SetAsHistoryMessage();
            }

            // 添加到消息列表
            Instance.lines.Add(newLine);

            // 更新所有消息的位置
            Instance.UpdateLinePoses();

            // 不再在这里添加到历史记录，而是在消息被删除时添加
            // Instance.AddToHistory(name, message, color);
            
            // 根据消息长度设置下次删除延迟
            if (!isHistoryMessage)
            {
                // 设置删除计时器，基于消息长度
                Instance.nextDelateDelay = message.Length * 3f + 60f;
                DebugHandler.Log($"[雨甸中文输入] 设置删除计时器为: {Instance.nextDelateDelay} (基于消息长度: {message.Length})");
            }
        }

        /// <summary>
        /// 播放消息提示音
        /// </summary>
        private static void PlayMessageSound()
        {
            // 获取当前场景
            var rainWorld = UnityEngine.Object.FindObjectOfType<RainWorld>();
            if (rainWorld != null && rainWorld.options != null)
            {
                // 使用游戏自带的声音系统播放提示音
                float volume = rainWorld.options.soundEffectsVolume;
                if (volume > 0)
                {
                    // 触发一个简单的UI事件来播放声音
                    if (rainWorld.processManager?.currentMainLoop is RainWorldGame game)
                    {
                        // 使用游戏内置的声音系统播放提示音
                        float randomVolume = UnityEngine.Random.Range(0.5f, 0.8f);
                        float randomPitch = UnityEngine.Random.Range(0.8f, 1.2f);
                        game.cameras[0].room.PlaySound(SoundID.MENU_Add_Level, randomVolume, randomPitch, 0.5f);
                        DebugHandler.Log($"[雨甸中文输入] 播放了消息提示音 音量:{randomVolume:F2} 音高:{randomPitch:F2}");
                    }
                }
            }
        }

        /// <summary>
        /// 添加消息方法，供Rain Meadow的ChatLogManager调用
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="message">消息内容</param>
        public void AddMessage(string username, string message)
        {
            try
            {
                Debug.Log($"[雨甸中文输入] GChatHud.AddMessage被调用: 用户={username}, 消息={message}");

                // 添加空值检查
                if (string.IsNullOrEmpty(message))
                {
                    Debug.LogWarning("[雨甸中文输入] 收到空消息内容，忽略此消息");
                    return;
                }

                // 系统消息处理（用户名为null或空字符串）
                if (string.IsNullOrEmpty(username))
                {
                    // 系统消息使用黄色，与ChatLogOverlay中的SYSTEM_COLOR一致
                    Color systemColor = new Color(1f, 1f, 0.3333333f);
                    // 系统消息不显示用户名前缀，直接显示消息内容
                    NewChatLine("", message, message.Length * 4 + 240, systemColor);
                    return;
                }

                // 尝试从在线玩家获取颜色
                try
                {
                    // 清空颜色字典
                    colorDictionary.Clear();

                    // 获取所有玩家的颜色信息
                    foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
                    {
                        if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo)
                        {
                            if (!colorDictionary.ContainsKey(opo.owner.id.name) && opo.TryGetData<SlugcatCustomization>(out var customization))
                            {
                                colorDictionary.Add(opo.owner.id.name, customization.bodyColor);
                            }
                        }
                    }

                    // 获取玩家颜色，如果不存在则使用白色
                    var color = colorDictionary.TryGetValue(username, out var colorOrig) ? colorOrig : Color.white;
                    var colorNew = color;

                    // 调整颜色亮度，确保可见性
                    float H = 0f;
                    float S = 0f;
                    float V = 0f;
                    Color.RGBToHSV(color, out H, out S, out V);
                    if (V < 0.8f) { colorNew = Color.HSVToRGB(H, S, 0.8f); }

                    // 创建新的聊天行
                    NewChatLine($"[{username}]", message, message.Length * 4 + 240, colorNew);
                    PlayMessageSound();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[雨甸中文输入] 获取玩家颜色失败: {ex.Message}");
                    // 使用默认白色创建聊天行
                    NewChatLine($"[{username}]", message, message.Length * 4 + 240, GHUDStatic.GHUDwhite);
                    PlayMessageSound();
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[雨甸中文输入] 添加消息失败: {ex.Message}");
                UnityEngine.Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 更新所有聊天行的位置
        /// </summary>
        private void UpdateLinePoses()
        {
            // 按照位置顺序（从上到下）更新位置
            for (int i = 0; i < lines.Count; i++)
            {
                lines[i].UpdatePos(i);
            }
        }

        /// <summary>
        /// 聊天行类，表示一条聊天消息
        /// </summary>
        internal class ChatLine
        {
            // 所属的GChatHud实例
            GChatHud owner;

            // 透明度相关变量
            public float setAlpha;
            float alpha;
            float lastAlpha;

            // 生命周期相关变量
            int delay;

            // 是否为历史消息
            public bool IsHistoryMessage { get; private set; }

            // 位置相关变量
            Vector2 setPos;
            Vector2 pos;
            Vector2 lastPos;

            // UI元素
            FLabel name;
            FLabel message;

            // 延迟设置队列
            Queue<DelaySetting> delaySettings = new Queue<DelaySetting>();
            DelaySetting? currentDelaySetting;

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="owner">所属的GChatHud实例</param>
            /// <param name="color">消息颜色</param>
            /// <param name="nameText">发送者名称</param>
            /// <param name="messageText">消息内容</param>
            /// <param name="life">消息显示时间</param>
            public ChatLine(GChatHud owner, Color color, string nameText, string messageText)
            {
                this.owner = owner;

                // 创建发送者名称标签
                this.name = new FLabel(Custom.GetFont(), nameText)
                {
                    color = color,
                    anchorX = 0,
                    anchorY = 0,
                    scale = 1.3f,
                    isVisible = true,
                    alpha = 0f
                };

                // 创建消息内容标签
                this.message = new FLabel(Custom.GetFont(), messageText)
                {
                    color = color,
                    anchorX = 0,
                    anchorY = 0,
                    scale = 1.3f,
                    isVisible = true,
                    alpha = 0f
                };

                // 添加到容器
                owner.Container.AddChild(this.message);
                owner.Container.AddChild(this.name);


            }

            /// <summary>
            /// 重置消息的生命周期
            /// </summary>
            /// <param name="newLife">新的生命周期</param>
            public void ResetLife(int newLife)
            {
                setAlpha = 1f;
                alpha = 1f;
                lastAlpha = 1f;
                DebugHandler.Log($"[雨甸中文输入] 重置消息生命周期为 {newLife}");
            }

            /// <summary>
            /// 更新方法，处理动画和生命周期
            /// </summary>
            public void Update()
            {
                // 如果是历史消息且历史消息显示被禁用，则立即销毁
                if (IsHistoryMessage && !owner.isHistoryEnabled)
                {
                    ForceDestroy();
                    DebugHandler.Log("[雨甸中文输入] 历史消息已被立即销毁");
                    return;
                }

                // 平滑透明度变化
                lastAlpha = alpha;
                alpha = Mathf.Lerp(lastAlpha, setAlpha, 0.25f);

                // 平滑位置变化
                lastPos = pos;
                pos = Vector2.Lerp(lastPos, setPos, 0.15f);

                // 处理延迟设置
                if (delay > 0)
                {
                    delay--;
                    return;
                }

                // 处理延迟设置队列
                if (currentDelaySetting == null && delaySettings.Count > 0)
                {
                    currentDelaySetting = delaySettings.Dequeue();
                    delay = currentDelaySetting.Value.delay;
                }

                if (currentDelaySetting != null)
                {
                    if (currentDelaySetting.Value.alpha != -1f)
                    {
                        setAlpha = currentDelaySetting.Value.alpha;
                    }
                    setPos = currentDelaySetting.Value.pos;
                    currentDelaySetting = null;
                }

                // 如果透明度几乎为0，则销毁
                if (alpha < 0.01f && setAlpha < 0.01f)
                {
                    Destroy();
                }
            }

            /// <summary>
            /// 绘制方法，渲染聊天行
            /// </summary>
            /// <param name="timeStacker">时间插值器，用于平滑动画</param>
            public void Draw(float timeStacker)
            {
                // 计算插值后的位置和透明度
                Vector2 s_pos = Vector2.Lerp(lastPos, pos, timeStacker);
                float s_alpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);

                // 系统消息处理（name为空字符串）
                if (string.IsNullOrEmpty(name.text))
                {
                    // 系统消息只显示消息内容，不显示用户名
                    name.isVisible = false;
                    message.SetPosition(s_pos);
                    message.alpha = s_alpha;
                    message.scale = s_alpha * 1.1f;
                    return;
                }

                // 用户消息处理
                // 计算名称和消息之间的间距
                float gap = Mathf.Max(name.textRect.width * name.scale + 40f, 120f);

                // 设置位置
                name.isVisible = true;
                name.SetPosition(s_pos);
                message.SetPosition(s_pos + Vector2.right * gap * s_alpha);

                // 设置透明度
                name.alpha = s_alpha;
                message.alpha = s_alpha;

                // 设置缩放
                name.scale = s_alpha * 1.1f;
                message.scale = s_alpha * 1.1f;
            }

            /// <summary>
            /// 更新位置，根据索引计算位置
            /// </summary>
            /// <param name="index">在列表中的索引</param>
            public void UpdatePos(int index)
            {
                // 创建延迟设置
                DelaySetting delayS = new DelaySetting();
                delayS.delay = Mathf.Max(0, 2 * index);

                // 计算位置（索引0在底部，往上递增）
                // 反转索引，使最新的消息在底部
                int posIndex = owner.lines.Count - 1 - index;
                delayS.pos = new Vector2(80f, 125f + posIndex * 19f);
                delayS.alpha = 1f; // 默认设置为完全不透明

                // 将延迟设置添加到队列
                delaySettings.Enqueue(delayS);
            }
            /// <summary>
            /// 强制销毁消息，立即从显示中移除
            /// </summary>
            public void ForceDestroy()
            {
                // 立即设置透明度为0
                setAlpha = 0f;
                alpha = 0f;
                lastAlpha = 0f;

                // 从容器中移除UI元素
                if (name != null)
                {
                    name.RemoveFromContainer();
                }

                if (message != null)
                {
                    message.RemoveFromContainer();
                }

                // 如果不是历史消息，则添加到历史记录
                if (!IsHistoryMessage)
                {
                    // 获取消息内容
                    string nameText = name != null ? name.text : "";
                    string messageText = message != null ? message.text : "";
                    Color color = name != null ? name.color : Color.white;
                    
                    // 添加到历史记录
                    owner.AddToHistory(nameText, messageText, color);
                    DebugHandler.Log($"[雨甸中文输入] 将消息添加到历史记录: {nameText} - {messageText}");
                }

                // 从消息列表中移除
                owner.lines.Remove(this);

                DebugHandler.Log("[雨甸中文输入] 强制销毁了一条消息");
            }

            /// <summary>
            /// 销毁消息，从显示中移除
            /// </summary>
            public void Destroy()
            {
                // 从容器中移除UI元素
                if (name != null)
                {
                    name.RemoveFromContainer();
                }

                if (message != null)
                {
                    message.RemoveFromContainer();
                }

                // 从消息列表中移除
                owner.lines.Remove(this);

                // 更新所有消息的位置
                owner.UpdateLinePoses();

                // 重新分配所有非历史消息的生命周期
                owner.ReallocateMessageLifecycles();

                DebugHandler.Log("[雨甸中文输入] 销毁了一条消息");
            }

            /// <summary>
            /// 延迟设置结构体，用于动画效果
            /// </summary>
            public struct DelaySetting
            {
                /// <summary>
                /// 延迟帧数
                /// </summary>
                public int delay;

                /// <summary>
                /// 目标位置
                /// </summary>
                public Vector2 pos;

                /// <summary>
                /// 目标透明度，-1表示不改变
                /// </summary>
                public float alpha;
            }

            /// <summary>
            /// 设置为历史消息
            /// </summary>
            public void SetAsHistoryMessage()
            {
                IsHistoryMessage = true;
            }
        }

        /// <summary>
        /// ChatHud适配器类，用于连接GChatHud和Rain Meadow的ChatLogManager
        /// </summary>
        private class GChatHudAdapter
        {
            /// <summary>
            /// GChatHud实例引用
            /// </summary>
            internal GChatHud gChatHud;

            /// <summary>
            /// 聊天代理对象
            /// </summary>
            private object chatHudProxy;

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="gChatHud">要适配的GChatHud实例</param>
            public GChatHudAdapter(GChatHud gChatHud)
            {
                this.gChatHud = gChatHud;
                try
                {
                    // 创建一个代理对象，用于接收消息
                    chatHudProxy = new ChatHudProxy(this);
                    Debug.Log("[雨甸中文输入] 已创建ChatHudProxy对象");

                    // 使用反射调用ChatLogManager.Subscribe方法
                    Type chatLogManagerType = typeof(ChatLogManager);
                    Debug.Log($"[雨甸中文输入] ChatLogManager类型: {chatLogManagerType.FullName}");

                    MethodInfo subscribeMethod = chatLogManagerType.GetMethod("Subscribe");
                    if (subscribeMethod != null)
                    {
                        Debug.Log($"[雨甸中文输入] 找到Subscribe方法: {subscribeMethod.Name}");
                        Debug.Log($"[雨甸中文输入] Subscribe方法参数类型: {subscribeMethod.GetParameters()[0].ParameterType.FullName}");

                        subscribeMethod.Invoke(null, new object[] { chatHudProxy });
                        Debug.Log("[雨甸中文输入] ChatHud适配器已订阅ChatLogManager");
                    }
                    else
                    {
                        Debug.LogError("[雨甸中文输入] 无法找到ChatLogManager.Subscribe方法");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("[雨甸中文输入] 订阅ChatLogManager失败: " + ex.Message);
                    Debug.LogException(ex);
                }
            }

            /// <summary>
            /// 析构函数，确保在销毁时取消订阅
            /// </summary>
            ~GChatHudAdapter()
            {
                try
                {
                    if (chatHudProxy != null)
                    {
                        // 使用反射调用ChatLogManager.Unsubscribe方法
                        Type chatLogManagerType = typeof(ChatLogManager);
                        MethodInfo unsubscribeMethod = chatLogManagerType.GetMethod("Unsubscribe");
                        if (unsubscribeMethod != null)
                        {
                            unsubscribeMethod.Invoke(null, new object[] { chatHudProxy });
                            Debug.Log("[雨甸中文输入] ChatHud适配器已取消订阅ChatLogManager");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("[雨甸中文输入] 取消订阅ChatLogManager失败: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// ChatHud代理类，实现与ChatHud相同的接口
        /// </summary>
        private class ChatHudProxy
        {
            /// <summary>
            /// 适配器引用
            /// </summary>
            private GChatHudAdapter adapter;

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="adapter">适配器实例</param>
            public ChatHudProxy(GChatHudAdapter adapter)
            {
                this.adapter = adapter;
            }

            /// <summary>
            /// 活动状态属性，表示代理是否可用
            /// </summary>
            public bool Active => adapter.gChatHud != null && !adapter.gChatHud.slatedForDeletion;

            /// <summary>
            /// 添加消息方法，接收来自ChatLogManager的消息
            /// </summary>
            /// <param name="user">用户名</param>
            /// <param name="message">消息内容</param>
            public void AddMessage(string user, string message)
            {
                try
                {
                    UnityEngine.Debug.Log($"[雨甸中文输入] ChatHudProxy.AddMessage被调用: 用户={user}, 消息={message}");

                    // 添加额外的空值检查
                    if (adapter == null || adapter.gChatHud == null)
                    {
                        UnityEngine.Debug.LogError("[雨甸中文输入] ChatHudProxy.adapter或gChatHud为空");
                        return;
                    }

                    adapter.gChatHud.AddMessage(user, message);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[雨甸中文输入] ChatHudProxy处理消息失败: {ex.Message}");
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }

        /// <summary>
        /// 聊天消息记录类，用于存储历史消息
        /// </summary>
        private class ChatMessageRecord
        {
            /// <summary>
            /// 发送者名称
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// 消息内容
            /// </summary>
            public string Message { get; private set; }

            /// <summary>
            /// 消息颜色
            /// </summary>
            public Color Color { get; private set; }

            /// <summary>
            /// 消息时间戳
            /// </summary>
            public DateTime Timestamp { get; private set; }

            /// <summary>
            /// 构造函数
            /// </summary>
            public ChatMessageRecord(string name, string message, Color color)
            {
                Name = name;
                Message = message;
                Color = color;
                Timestamp = DateTime.Now;
            }
        }
    }
}
