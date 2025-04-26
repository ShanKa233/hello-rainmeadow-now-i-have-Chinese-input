using System.Security.Permissions;
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
	[BepInPlugin(MOD_ID, "早上好雨甸,我有中文输入", "1.2.0")]
	public class Plugin : BaseUnityPlugin
	{
		private static Plugin instance;
		public static Menu.RemixMenu menu = new Menu.RemixMenu();
		private bool initialized;

		private const string MOD_ID = "goodmorningrainmeadow.chineseinput";


		public void OnEnable()
		{
			instance = this;

			// 初始化调试处理器
			DebugHandler.Initialize(Logger);

			On.RainWorld.OnModsInit += RainWorld_OnModsInit;
		}

		public void OnDisable()
		{
			// 清理ChatHud Hook
			ChatHudHook.Cleanup();
			// 清理ChatLogManager Hook
			ChatLogManagerHook.Cleanup();
			// 清理IMEActivator Hook
			// IMEActivatorHook.Cleanup();
		}

		private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
		{
			orig(self);
			if (initialized)
				return;
			initialized = true;
			// 初始化ChatHud Hook
			// ChatHudHook.Initialize(Logger);//暂时感觉没用,先放着

			// 初始化ChatLogManager Hook
			//hook消息管理,使其转发一份消息到我们自定义的ChatLogManager
			ChatLogManagerHook.Initialize();
			
			// 初始化IMEActivator Hook，用于在原版输入框激活时启用IME
			// IMEActivatorHook.Initialize(Logger);

			// 添加输入相关的Hook
			On.Player.checkInput += Player_checkInput;
			On.RWInput.PlayerUIInput_int += PlayerUIInput_int;

			// 初始化菜单
			MachineConnector.SetRegisteredOI(MOD_ID, menu);

			//初始化hud内容
			On.RainWorldGame.ctor += RainWorldGame_ctor;
			//防止打开devhud
			On.MainLoopProcess.RawUpdate += MainLoopProcess_RawUpdate;

		}

		private void MainLoopProcess_RawUpdate(On.MainLoopProcess.orig_RawUpdate orig, MainLoopProcess self, float dt)
		{
			orig(self, dt);
			if (GhostPlayer.GHud.GHUD.Instance != null)
			{
				// 检查是否打开了输入框
				if (GhostPlayer.GHud.GHUD.Instance.LockInput)
				{
					// 如果打开了输入框，关闭devToolsActive
					if (self is RainWorldGame game && game.devToolsActive)
					{
						game.devToolsActive = false;
						game.devToolsLabel.isVisible = false;
                        DebugHandler.Log("[雨甸中文输入] 检测到输入框打开，已关闭开发者工具");
					}

					// 如果打开了输入框，关闭devUI
					if (self is RainWorldGame rwGame && rwGame.devUI != null)
					{
                        Cursor.visible = !Custom.rainWorld.options.fullScreen;
						rwGame.devUI.ClearSprites();
						rwGame.devUI = null;
                        DebugHandler.Log("[雨甸中文输入] 检测到输入框打开，已关闭开发者UI");
					}
				}
			}
		}


		private InputPackage PlayerUIInput_int(On.RWInput.orig_PlayerUIInput_int orig, int playerNumber)
		{
			bool shouldLockInput = GhostPlayer.GHud.GHUD.Instance != null && GhostPlayer.GHud.GHUD.Instance.LockInput;
			if (shouldLockInput)
			{
				// 清空输入
				return new InputPackage(
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
			}
			return orig(playerNumber);
		}

		private void Player_checkInput(On.Player.orig_checkInput orig, Player self)
		{
			orig(self);
			bool shouldLockInput = GhostPlayer.GHud.GHUD.Instance != null && GhostPlayer.GHud.GHUD.Instance.LockInput;
			if (shouldLockInput)
			{
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
			}
		}
		private void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
		{
			orig(self, manager);

			// try
			// {
			// 	var imeObject = new GameObject("IMEHandler");
			// 	var imeHandler = imeObject.AddComponent<雨甸中文输入.src.IMEHandler>();
			// 	UnityEngine.Object.DontDestroyOnLoad(imeObject);
			// 	DebugHandler.Log("已创建IMEHandler实例，中文输入已启用");
			// }
			// catch (Exception ex)
			// {
			// 	DebugHandler.LogError("创建IMEHandler实例失败", ex);
			// }

			// 创建GHUDTest实例，用于初始化GHUD
			
			try
			{
				var testObject = new GameObject("GHUDTest");
				var test = testObject.AddComponent<GhostPlayer.GHUD>();
				UnityEngine.Object.DontDestroyOnLoad(testObject);
				DebugHandler.Log("已创建GHUDTest实例，中文输入已启用");
			}
			catch (Exception ex)
			{
				DebugHandler.LogError("创建GHUDTest实例失败", ex);
			}
		}
	}
}