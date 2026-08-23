using System;
using System.Security.Permissions;
using BepInEx;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace GoodMorningRainMeadow
{
	[BepInPlugin(MOD_ID, "早上好现在我有IME", "1.4.0")]
	public class Plugin : BaseUnityPlugin
	{
		private bool initialized;

		private const string MOD_ID = "goodmorningrainmeadow.chineseinput";

		public void OnEnable()
		{
			On.RainWorld.OnModsInit += RainWorld_OnModsInit;
		}

		private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
		{
			orig(self);
			if (initialized)
				return;
			initialized = true;
			// 原版Remix文本框会用正则拒收非ASCII，光开输入法打不出中文，这里放行
			RemixUnicodeHook.Apply();
			// NameYourFriends的命名框有自己的ASCII白名单过滤，同样放行（没装则静默跳过）
			NameYourFriendsHook.Apply();
			//初始化imehandler：直接挂在雨世界主对象上，主对象跨场景永存，不用自己管生命周期
			self.gameObject.AddComponent<IMEHandler>();
		}
	}
}