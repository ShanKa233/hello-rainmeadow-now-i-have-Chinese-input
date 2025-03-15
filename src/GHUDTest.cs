using System;
using System.Collections.Generic;
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
                    try
                    {
                        if (MatchmakingManager.currentInstance != null)
                        {
                            MatchmakingManager.currentInstance.SendChatMessage("这是一条测试消息");
                            Debug.Log("[雨甸中文输入] 添加了测试消息");
                        }
                        else
                        {
                            Debug.LogWarning("[雨甸中文输入] MatchmakingManager.currentInstance为空，无法发送测试消息");
                            // 如果不在线，直接添加本地消息
                            GChatHud.NewChatLine("[测试]", "这是一条测试消息", 240, GHUDStatic.GHUDgreen);
                        }
                    }
                    catch (KeyNotFoundException knfEx)
                    {
                        Debug.LogError($"[雨甸中文输入] 发送测试消息时遇到KeyNotFoundException: {knfEx.Message}");
                        Debug.LogException(knfEx);
                        // 出错时也添加本地消息
                        GChatHud.NewChatLine("[测试]", "这是一条测试消息", 240, GHUDStatic.GHUDgreen);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[雨甸中文输入] 发送测试消息失败: {ex.Message}");
                        Debug.LogException(ex);
                    }
                }
            }
            catch (KeyNotFoundException knfEx)
            {
                Debug.LogError($"[雨甸中文输入] GHUDTest更新时遇到KeyNotFoundException: {knfEx.Message}");
                Debug.LogException(knfEx);
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] GHUDTest更新失败: " + ex.Message);
                Debug.LogException(ex);
            }
        }
    }
} 