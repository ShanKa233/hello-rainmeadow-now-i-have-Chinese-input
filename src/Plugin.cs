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
using MonoMod.RuntimeDetour;
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
			On.RWInput.PlayerUIInput_int += PlayerUIInput_int;
		}

        private InputPackage PlayerUIInput_int(On.RWInput.orig_PlayerUIInput_int orig, int playerNumber)
        {
        	InputPackage inputPackage = orig(playerNumber);
			if(IMEHandler.Instance!=null&&IMEHandler.Instance.IsChatActive())
			{
				inputPackage.y = 0;
				inputPackage.x = 0;
				inputPackage.thrw=false;
				inputPackage.jmp=false;
				inputPackage.analogueDir=new Vector2(0,0);
				inputPackage.pckp=false;
			}
			return inputPackage;
        }

        public void OnDisable()
		{
		}

		private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
		{
			orig(self);
			if (initialized)
				return;
			initialized = true;
			// 初始化菜单
			//全改了所以暂时不需要这个了(((
			// MachineConnector.SetRegisteredOI(MOD_ID, menu);

			//初始化imehandler
			if (IMEHandler.Instance == null)
			{
				var imeHandlerObject = new GameObject("IMEHandler");
				imeHandlerObject.AddComponent<IMEHandler>();
				DontDestroyOnLoad(imeHandlerObject);
			}
		}
	}
}