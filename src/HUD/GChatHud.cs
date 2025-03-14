using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
// 移除GhostPlayer.Network引用，添加RainMeadow引用
using RainMeadow;

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
        public static int maxDisplayText = 7;

        /// <summary>
        /// 聊天消息行列表
        /// </summary>
        public List<ChatLine> lines = new List<ChatLine>();

        /// <summary>
        /// ChatHud适配器，用于连接GChatHud和Rain Meadow的ChatLogManager
        /// </summary>
        private GChatHudAdapter chatHudAdapter;

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
            
            try
            {
                // 创建适配器并订阅ChatLogManager
                chatHudAdapter = new GChatHudAdapter(this);
                Debug.Log("[雨甸中文输入] GChatHud已创建适配器，可以接收消息");
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] 创建ChatHud适配器失败: " + ex.Message);
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 处理输入框提交事件
        /// </summary>
        /// <param name="value">提交的文本</param>
        /// <param name="caretPos">光标位置</param>
        private void Hud_OnInputFieldSubmit(string value, int caretPos)
        {
            try
            {
                // 去除首尾空白
                value = value.Trim();
                // 检查是否在线
                bool online = (MatchmakingManager.currentInstance != null);
                
                // 如果是命令（以/开头）
                if (value.Length > 0 && value[0] == '/')
                {
                    // 触发命令输入事件
                    if (OnCommandInput != null)
                        OnCommandInput(value.Split(' ').Where(i => !string.IsNullOrWhiteSpace(value)).ToArray());
                }
                // 如果是普通消息
                else if (value.Length > 0)
                {
                    // 获取当前玩家名称
                    string playerName = "Player";
                    if (MatchmakingManager.currentInstance != null)
                    {
                        // 尝试获取玩家名称
                        try
                        {
                            // 使用MatchmakingManager获取玩家名称
                            // 由于无法直接访问selfLobbyPlayer，使用其他方式获取玩家名称
                            var playerManager = MatchmakingManager.currentInstance;
                            if (playerManager != null)
                            {
                                // 尝试使用反射获取玩家名称
                                try
                                {
                                    var selfPlayerField = playerManager.GetType().GetField("selfLobbyPlayer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                    if (selfPlayerField != null)
                                    {
                                        var selfPlayer = selfPlayerField.GetValue(playerManager);
                                        if (selfPlayer != null)
                                        {
                                            var nameProperty = selfPlayer.GetType().GetProperty("name");
                                            if (nameProperty != null)
                                            {
                                                var name = nameProperty.GetValue(selfPlayer) as string;
                                                if (!string.IsNullOrEmpty(name))
                                                {
                                                    playerName = name;
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogWarning($"[雨甸中文输入] 通过反射获取玩家名称失败: {ex.Message}");
                                }
                            }
                        }
                        catch
                        {
                            // 如果获取失败，使用默认名称
                            playerName = "Player";
                            Debug.LogWarning("[雨甸中文输入] 无法获取玩家名称，使用默认名称");
                        }
                    }

                    // 创建新的聊天消息
                    NewChatLine($"[{playerName}]", value, (value.Length) * 4 + 240,
                        online ? GHUDStatic.GHUDgreen : GHUDStatic.GHUDyellow);

                    // 使用Rain Meadow的系统发送消息
                    if (online)
                    {
                        try
                        {
                            Debug.Log($"[雨甸中文输入] 尝试发送消息: {value}");
                            if (MatchmakingManager.currentInstance != null)
                            {
                                MatchmakingManager.currentInstance.SendChatMessage(value);
                                Debug.Log($"[雨甸中文输入] 消息已发送: {value}");
                            }
                            else
                            {
                                Debug.LogWarning("[雨甸中文输入] MatchmakingManager.currentInstance为空，无法发送消息");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[雨甸中文输入] 发送消息失败: {ex.Message}");
                            Debug.LogException(ex);
                        }
                    }
                    else
                    {
                        Debug.Log("[雨甸中文输入] 不在线状态，仅显示本地消息");
                    }
                }

                // 关闭输入框
                if (hud.inputField != null)
                {
                    hud.inputField.DeactivateInputField();
                    hud.inputField.text = "";
                    // 不直接设置activated变量，而是通过DeactivateInputField方法关闭输入框
                    Debug.Log("[雨甸中文输入] 消息发送后关闭输入框");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] 处理输入框提交事件失败: " + ex.Message);
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 更新方法，每帧调用
        /// </summary>
        public override void Update()
        {
            // 更新所有聊天行
            for(int i = lines.Count - 1; i >= 0; i--)
            {
                lines[i].Update();
            }

            // 检查是否在线
            if (MatchmakingManager.currentInstance == null)
                return;

            // 这里不再需要处理消息队列，因为Rain Meadow的ChatLogManager会直接调用AddMessage
        }

        /// <summary>
        /// 绘制方法，每帧调用
        /// </summary>
        /// <param name="timeStacker">时间插值器，用于平滑动画</param>
        public override void Draw(float timeStacker)
        {
            // 绘制所有聊天行
            foreach(var line in lines)
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
            for(int i = lines.Count - 1;i >= 0; i--)
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
            // 在列表开头插入新消息
            Instance.lines.Insert(0, new ChatLine(Instance, color, name, message, life));
            // 更新所有消息的位置
            Instance.UpdateLinePoses();
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
                
                // 特殊处理系统消息
                if (string.IsNullOrEmpty(username))
                {
                    Debug.Log("[雨甸中文输入] 处理系统消息");
                    NewChatLine("", message, message.Length * 4 + 240, GHUDStatic.GHUDyellow);
                }
                else
                {
                    Debug.Log($"[雨甸中文输入] 处理用户消息: [{username}]");
                    NewChatLine($"[{username}]", message, (username.Length + message.Length) * 4 + 240, Color.white);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[雨甸中文输入] 添加消息失败: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 更新所有聊天行的位置
        /// </summary>
        public void UpdateLinePoses()
        {
            for (int i = 0; i < Instance.lines.Count; i++)
                Instance.lines[i].UpdatePos(i);
        }

        /// <summary>
        /// 聊天行类，表示单条聊天消息
        /// </summary>
        internal class ChatLine
        {
            // 渐变计数器
            static int gradiantCounter = 20;
            // 所属的GChatHud实例
            GChatHud owner;

            // 透明度相关变量
            float setAlpha;
            float alpha;
            float lastAlpha;

            // 生命周期相关变量
            int delay;
            int life;
            int maxLife;

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
            /// <param name="name">发送者名称</param>
            /// <param name="message">消息内容</param>
            /// <param name="maxLife">最大生命周期</param>
            public ChatLine(GChatHud owner, Color color, string name, string message, int maxLife)
            {
                this.owner = owner;
                this.maxLife = maxLife;
                
                // 创建发送者名称标签
                this.name = new FLabel(Custom.GetFont(), name)
                {
                    color = color,
                    anchorX = 0,
                    anchorY = 0,
                    scale = 1.3f,
                    isVisible = true,
                    alpha = 0f
                };
                
                // 创建消息内容标签
                this.message = new FLabel(Custom.GetFont(), message)
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
            /// 更新方法，处理动画和生命周期
            /// </summary>
            public void Update()
            {
                // 如果达到最大生命周期，开始淡出
                if (life == maxLife)
                    setAlpha = 0f;
                // 如果超过最大生命周期且几乎不可见，则销毁
                if (life > maxLife && alpha < 0.01f)
                    Destroy();
                // 增加生命计数
                life++;

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
                else
                {
                    int lastDelay = 0;
                    if(currentDelaySetting != null)
                    {
                        lastDelay = currentDelaySetting.Value.delay;
                        setPos = currentDelaySetting.Value.pos;
                        if (life < maxLife && currentDelaySetting.Value.alpha != -1)
                            setAlpha = currentDelaySetting.Value.alpha;
                        currentDelaySetting = null;
                    }
                    if(delaySettings.Count > 0)
                    {
                        currentDelaySetting = delaySettings.Dequeue();
                        delay = currentDelaySetting.Value.delay - lastDelay;
                    }
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

                // 计算名称和消息之间的间距
                float gap = Mathf.Max(name.textRect.width * name.scale + 40f, 120f);

                // 设置位置
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
                delayS.delay = Mathf.Max(0, 2 * (index - 1));
                delayS.pos = new Vector2(80f, 125f + index * 19f);
                delayS.alpha = -1f;

                // 如果是新消息（索引为0）且未达到最大生命周期
                if (index == 0 && life < maxLife)
                {
                    pos = delayS.pos;
                    lastPos = delayS.pos;
                    setAlpha = 1f;
                    delayS.alpha = 1f;
                }
                // 如果超出最大显示数量且未达到最大生命周期，加速生命周期
                else if (index >= GChatHud.maxDisplayText && life < maxLife)
                    life = Mathf.Min(life + 20, maxLife);
                // 如果未达到最大生命周期，设置为完全不透明
                else if(life < maxLife)
                    delayS.alpha = 1f;

                // 将延迟设置添加到队列
                delaySettings.Enqueue(delayS);
            }

            /// <summary>
            /// 销毁方法，清理资源
            /// </summary>
            public void Destroy()
            {
                // 隐藏标签
                name.isVisible = false;
                message.isVisible = false;
                // 从容器中移除
                name.RemoveFromContainer();
                message.RemoveFromContainer();

                // 从列表中移除
                if(owner.lines.Contains(this))
                    owner.lines.Remove(this);

                // 更新所有消息的位置
                owner.UpdateLinePoses();
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
        }

        /// <summary>
        /// ChatHud适配器类，用于连接GChatHud和Rain Meadow的ChatLogManager
        /// </summary>
        private class GChatHudAdapter
        {
            // 使用internal而不是private，这样ChatHudProxy可以访问
            internal GChatHud gChatHud;
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

            /// <summary>
            /// 处理消息
            /// </summary>
            /// <param name="user">用户名</param>
            /// <param name="message">消息内容</param>
            public void HandleMessage(string user, string message)
            {
                try
                {
                    Debug.Log($"[雨甸中文输入] GChatHudAdapter.HandleMessage被调用: 用户={user}, 消息={message}");
                    
                    if (gChatHud != null)
                    {
                        Debug.Log("[雨甸中文输入] gChatHud不为空");
                        
                        if (!gChatHud.slatedForDeletion)
                        {
                            Debug.Log("[雨甸中文输入] gChatHud未标记为删除，转发消息");
                            gChatHud.AddMessage(user, message);
                            Debug.Log($"[雨甸中文输入] 消息已转发: {user}: {message}");
                        }
                        else
                        {
                            Debug.LogWarning("[雨甸中文输入] gChatHud已标记为删除，无法转发消息");
                        }
                    }
                    else
                    {
                        Debug.LogError("[雨甸中文输入] gChatHud为空，无法转发消息");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("[雨甸中文输入] 转发消息失败: " + ex.Message);
                    Debug.LogException(ex);
                }
            }
        }
        
        /// <summary>
        /// ChatHud代理类，实现与ChatHud相同的接口
        /// </summary>
        private class ChatHudProxy
        {
            private GChatHudAdapter adapter;
            
            public ChatHudProxy(GChatHudAdapter adapter)
            {
                this.adapter = adapter;
            }
            
            // 实现与ChatHud相同的Active属性
            public bool Active => adapter.gChatHud != null && !adapter.gChatHud.slatedForDeletion;
            
            // 实现与ChatHud相同的AddMessage方法
            public void AddMessage(string user, string message)
            {
                try
                {
                    UnityEngine.Debug.Log($"[雨甸中文输入] ChatHudProxy.AddMessage被调用: 用户={user}, 消息={message}");
                    adapter.HandleMessage(user, message);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[雨甸中文输入] ChatHudProxy处理消息失败: {ex.Message}");
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }
    }
}
