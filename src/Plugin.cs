using System.Security.Permissions;
using BepInEx;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace GoodMorningRainMeadow
{
	[BepInPlugin(MOD_ID, "早上好雨甸,我有中文输入", "1.4.0")]
	public class Plugin : BaseUnityPlugin
	{
		private bool initialized;

		private const string MOD_ID = "goodmorningrainmeadow.chineseinput";

		public void OnEnable()
		{
			// 初始化调试处理器
			DebugHandler.Initialize(Logger);

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