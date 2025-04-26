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
    public class GHUD : MonoBehaviour
    {
        // GHUD实例
        private GHud.GHUD ghud;

        // 是否已初始化
        private bool initialized = false;

        // 上一个检测到的主循环类型
        private Type lastMainLoopType = null;

        /// <summary>
        /// Unity启动函数
        /// </summary>
        void Start()
        {
            initialized = true;
        }

        /// <summary>
        /// Unity更新函数
        /// </summary>
        void Update()
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

        /// <summary>
        /// 管理GHUD实例，根据游戏状态创建或销毁
        /// </summary>
        private void ManageGHUDInstance()
        {
            var currentProcess = Custom.rainWorld?.processManager?.currentMainLoop;

            // 获取当前进程类型
            Type currentType = currentProcess?.GetType();

            // 如果类型发生变化，记录日志
            if (currentType != lastMainLoopType)
            {
                lastMainLoopType = currentType;
                // 如果类型发生变化,销毁GHUD实例
                if (ghud != null)
                {
                    Destroy(ghud.gameObject);
                    ghud = null;
                }
            }

            // 检查是否在游戏内
            bool isInGame = OnlineManager.lobby != null;

            // 如果在游戏内且GHUD实例不存在，则创建
            if (isInGame && (ghud == null || GHud.GHUD.Instance == null))
            {

                // 检查是否已存在GHUD对象
                var existingGHUD = GameObject.Find("GHUD");
                if (existingGHUD != null)
                {
                    ghud = existingGHUD.GetComponent<GHud.GHUD>();

                    // 如果对象存在但组件不存在，则添加组件
                    if (ghud == null)
                    {
                        ghud = existingGHUD.AddComponent<GHud.GHUD>();
                    }
                }
                else
                {
                    // 创建新的GHUD实例
                    var ghudObject = new GameObject("GHUD");
                    ghud = ghudObject.AddComponent<GHud.GHUD>();
                }
            }
            // 如果不在游戏内但GHUD实例存在，则不处理（GHUD会自行检测并销毁）
            else if (!isInGame && ghud != null)
            {
                ghud = null; // 清除引用
            }
        }
    }
}