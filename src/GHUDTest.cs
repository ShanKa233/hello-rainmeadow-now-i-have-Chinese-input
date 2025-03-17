using System;
using System.Collections.Generic;
using UnityEngine;
using GhostPlayer.GHud;
using GoodMorningRainMeadow;
using RainMeadow;
using RWCustom;

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
        
        // 上一个检测到的主循环类型
        private Type lastMainLoopType = null;
        
        /// <summary>
        /// Unity启动函数
        /// </summary>
        void Start()
        {
            try
            {
                Debug.Log("[雨甸中文输入] GHUDTest开始初始化");
                
                // 不在Start中创建GHUD实例，而是在Update中根据游戏状态创建
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
                if (!initialized)
                    return;
                
                // 检查游戏状态并管理GHUD实例
                ManageGHUDInstance();
                
                // 如果GHUD实例不存在，则不处理后续逻辑
                if (ghud == null)
                    return;
                
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
        
        /// <summary>
        /// 管理GHUD实例，根据游戏状态创建或销毁
        /// </summary>
        private void ManageGHUDInstance()
        {
            try
            {
                // 获取当前进程
                var currentProcess = Custom.rainWorld?.processManager?.currentMainLoop;
                
                // 获取当前进程类型
                Type currentType = currentProcess?.GetType();
                
                // 如果类型发生变化，记录日志
                if (currentType != lastMainLoopType)
                {
                    Debug.Log($"[雨甸中文输入] 游戏主循环类型变化: {lastMainLoopType?.Name ?? "null"} -> {currentType?.Name ?? "null"}");
                    lastMainLoopType = currentType;
                }
                
                // 检查是否在游戏内
                bool isInGame = currentProcess is RainWorldGame;
                
                // 如果在游戏内且GHUD实例不存在，则创建
                if (isInGame && (ghud == null || GHUD.Instance == null))
                {
                    Debug.Log("[雨甸中文输入] 检测到游戏场景，创建GHUD实例");
                    
                    // 检查是否已存在GHUD对象
                    var existingGHUD = GameObject.Find("GHUD");
                    if (existingGHUD != null)
                    {
                        Debug.Log("[雨甸中文输入] 发现现有GHUD对象，尝试使用");
                        ghud = existingGHUD.GetComponent<GHUD>();
                        
                        // 如果对象存在但组件不存在，则添加组件
                        if (ghud == null)
                        {
                            Debug.Log("[雨甸中文输入] GHUD对象存在但组件不存在，添加组件");
                            ghud = existingGHUD.AddComponent<GHUD>();
                        }
                    }
                    else
                    {
                        // 创建新的GHUD实例
                        var ghudObject = new GameObject("GHUD");
                        ghud = ghudObject.AddComponent<GHUD>();
                        Debug.Log("[雨甸中文输入] 已创建新的GHUD实例");
                    }
                }
                // 如果不在游戏内但GHUD实例存在，则不处理（GHUD会自行检测并销毁）
                else if (!isInGame && ghud != null)
                {
                    Debug.Log("[雨甸中文输入] 不在游戏场景中，GHUD将自行检测并销毁");
                    ghud = null; // 清除引用
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] 管理GHUD实例失败: " + ex.Message);
                Debug.LogException(ex);
            }
        }
    }
} 