﻿using System.Security.Permissions;
using BepInEx;
using UnityEngine;
using HarmonyLib;
using HUD;
using RWCustom;
using System;
using System.Linq;
using System.Reflection;
using Rewired;
using BepInEx.Logging;
using System.Collections.Generic;
using System.Collections;
using GhostPlayer;
using MonoMod.RuntimeDetour;
using GhostPlayer.GHud;
using static Player;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace GoodMorningRainMeadow
{
	[BepInPlugin("goodmorainmeadow", "早上好雨甸,我有中文输入", "1.0.0")]
	public class Plugin : BaseUnityPlugin
	{
		internal static ManualLogSource logger;
		private static Plugin instance;

		public void OnEnable()
		{
			instance = this;
			logger = Logger;
			
			// 初始化调试处理器
			DebugHandler.Initialize(Logger);
			
			// 初始化配置管理器
			ConfigManager.Initialize(Config);
			
			DebugHandler.Log("雨甸中文输入补丁已加载");
			
			On.RainWorld.OnModsInit += RainWorld_OnModsInit;
		}
		
		public void OnDisable()
		{
			// 清理ChatHud Hook
			ChatHudHook.Cleanup();
			// 清理ChatLogManager Hook
			ChatLogManagerHook.Cleanup();
		}
		
		private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
		{
			orig(self);
			
			// 初始化引用
			try
			{
				References.Initialize();
				DebugHandler.Log("已初始化程序集引用");
				
				// 初始化ChatHud Hook
				ChatHudHook.Initialize(Logger);
				DebugHandler.Log("已初始化ChatHud Hook");

				// 初始化ChatLogManager Hook
				ChatLogManagerHook.Initialize(Logger);
				DebugHandler.Log("已初始化ChatLogManager Hook");

				// 添加Player.Update的Hook
				On.Player.Update += Player_Update;
				DebugHandler.Log("已添加Player输入Hook");
			}
			catch (Exception ex)
			{
				DebugHandler.LogError("初始化过程中发生错误", ex);
				return;
			}
			
			// 注册按键事件
			On.RainWorldGame.Update += RainWorldGame_Update;
			On.RainWorldGame.ctor += RainWorldGame_ctor;
			
			DebugHandler.Log("雨甸中文输入已初始化");
		}
		
		private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
		{
			// 如果输入框激活，则禁用玩家输入
			if (GHUD.Instance != null && GHUD.Instance.LockInput)
			{
				// 保存原始输入状态
				var origInput = self.input[0];
				// 清空输入
				self.input[0] = new InputPackage(
					false, // jmp
					Options.ControlSetup.Preset.None, // crouchToggle
					0, // x
					0, // y
					false, // thrw
					false, // pckp
					false, // map
					false, // mp
					false // custom
				);
				// 调用原始更新方法
				orig(self, eu);
				// 恢复输入状态
				self.input[0] = origInput;
			}
			else
			{
				orig(self, eu);
			}
		}
		
		private void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
		{
			orig(self, manager);
			
			// 创建GHUDTest实例，用于初始化GHUD
			try
			{
				var testObject = new GameObject("GHUDTest");
				var test = testObject.AddComponent<GhostPlayer.GHUDTest>();
				UnityEngine.Object.DontDestroyOnLoad(testObject);
				DebugHandler.Log("已创建GHUDTest实例，中文输入已启用");
			}
			catch (Exception ex)
			{
				DebugHandler.LogError("创建GHUDTest实例失败", ex);
			}
		}
		
		private void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
		{
			orig(self);
			// 我们不再需要在这里处理输入框的激活，因为GHUDTest类会处理这个逻辑
		}
	}
}