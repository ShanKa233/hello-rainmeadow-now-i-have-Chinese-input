using System;
using UnityEngine;
using GhostPlayer.GHud;
using GoodMorningRainMeadow;
using RainMeadow;

namespace GhostPlayer
{
    /// <summary>
    /// GHUD测试类，用于验证GHUD是否正常工作
    /// </summary>
    public class GHUDTest : MonoBehaviour
    {
        // GHUD实例
        private GHUD ghud;
        
        // 是否已初始化
        private bool initialized = false;
        
        /// <summary>
        /// Unity启动函数
        /// </summary>
        void Start()
        {
            try
            {
                Debug.Log("[雨甸中文输入] GHUDTest开始初始化");
                
                // 创建GHUD实例
                var ghudObject = new GameObject("GHUD");
                ghud = ghudObject.AddComponent<GHUD>();
                DontDestroyOnLoad(ghudObject);
                
                initialized = true;
                Debug.Log("[雨甸中文输入] GHUDTest初始化成功");
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] GHUDTest初始化失败: " + ex.Message);
                Debug.LogException(ex);
            }
        }
        
        /// <summary>
        /// Unity更新函数
        /// </summary>
        void Update()
        {
            try
            {
                // 如果未初始化，则不处理
                if (!initialized || ghud == null)
                    return;
                
                // 检查是否按下了测试按键
                if (Input.GetKeyDown(KeyCode.F8))
                {
                    // 添加测试消息
                    // GChatHud.NewChatLine("[测试]", "这是一条测试消息", 240, GHUDStatic.GHUDgreen);
                    MatchmakingManager.currentInstance?.SendChatMessage("这是一条测试消息");
                    Debug.Log("[雨甸中文输入] 添加了测试消息");
                }
                
                // 检查是否按下了切换输入框的按键
                if (Input.GetKeyDown(ConfigManager.ToggleInputBoxKey.Value))
                {
                    // 激活输入框
                    if (ghud.inputField != null)
                    {
                        ghud.inputField.ActivateInputField();
                        ghud.inputField.Select();
                        Debug.Log("[雨甸中文输入] 激活了输入框");
                    }
                    else
                    {
                        Debug.LogWarning("[雨甸中文输入] 输入框为空");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] GHUDTest更新失败: " + ex.Message);
                Debug.LogException(ex);
            }
        }
    }
} 