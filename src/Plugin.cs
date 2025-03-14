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

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace GoodMorningRainMeadow
{
	[BepInPlugin("goodmorainmeadow", "早上好雨甸,我有中文输入", "1.0.0")]
	public class Plugin : BaseUnityPlugin
	{
		private Harmony harmony;
		internal static ManualLogSource logger;
		private static Plugin instance;
		private RainMeadowAdapter rainMeadowAdapter;

		public void OnEnable()
		{
			instance = this;
			logger = Logger;
			
			// 初始化调试处理器
			DebugHandler.Initialize(Logger);
			
			// 初始化配置管理器
			ConfigManager.Initialize(Config);
			
			harmony = new Harmony("goodmorainmeadow");
			
			// 不使用PatchAll，而是手动应用补丁
			// harmony.PatchAll(Assembly.GetExecutingAssembly());
			
			DebugHandler.Log("雨甸中文输入补丁已加载");
			
			On.RainWorld.OnModsInit += RainWorld_OnModsInit;
		}
		
		private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
		{
			orig(self);
			
			// 创建RainMeadowAdapter
			if (rainMeadowAdapter == null)
			{
				// 创建一个新的GameObject并添加RainMeadowAdapter组件
				var adapterObject = new GameObject("RainMeadowAdapter");
				rainMeadowAdapter = adapterObject.AddComponent<RainMeadowAdapter>();
				UnityEngine.Object.DontDestroyOnLoad(adapterObject);
				DebugHandler.Log("已创建RainMeadowAdapter");
			}
			
			// 只有在配置中启用了禁用Meadow聊天框时才执行
			if (ConfigManager.DisableMeadowChat.Value)
			{
				// 尝试直接Hook掉Meadow的ChatHud
				try
				{
					// 获取Rain Meadow程序集
					var rainMeadowAssembly = AppDomain.CurrentDomain.GetAssemblies()
						.FirstOrDefault(a => a.GetName().Name == "Rain Meadow");
					
					if (rainMeadowAssembly != null)
					{
						DebugHandler.Log("找到Rain Meadow程序集");
						
						// 打印所有类型，帮助调试
						DebugHandler.LogVerbose("开始列出Rain Meadow中的聊天相关类型");
						foreach (var type in rainMeadowAssembly.GetTypes())
						{
							if (type.Name.Contains("Chat") || type.Name.Contains("Text") || type.Name.Contains("Hud"))
							{
								DebugHandler.LogVerbose($"找到类型: {type.FullName}");
							}
						}
						
						// 直接Hook掉ChatHud的Draw方法
						var types = rainMeadowAssembly.GetTypes();
						foreach (var type in types)
						{
							if (type.Name == "ChatHud")
							{
								DebugHandler.Log($"找到ChatHud类型: {type.FullName}");
								
								// 尝试Hook掉Draw方法
								var drawMethod = type.GetMethod("Draw", new Type[] { typeof(float) });
								if (drawMethod != null)
								{
									DebugHandler.Log("找到ChatHud.Draw方法，准备Hook");
									try
									{
										harmony.Patch(
											drawMethod,
											prefix: new HarmonyMethod(typeof(MeadowChatHudPatch).GetMethod("Prefix"))
										);
										DebugHandler.Log("成功Hook掉ChatHud.Draw方法");
									}
									catch (Exception ex)
									{
										DebugHandler.LogError("Hook ChatHud.Draw方法失败", ex);
									}
								}
							}
						}
						
						// 尝试Hook掉HUD.AddPart方法
						var hudType = typeof(HUD.HUD);
						var addPartMethod = hudType.GetMethod("AddPart", BindingFlags.Public | BindingFlags.Instance);
						if (addPartMethod != null)
						{
							DebugHandler.Log("找到HUD.AddPart方法，准备Hook");
							try
							{
								harmony.Patch(
									addPartMethod,
									prefix: new HarmonyMethod(typeof(HudAddPartPatch).GetMethod("Prefix"))
								);
								DebugHandler.Log("成功Hook掉HUD.AddPart方法");
							}
							catch (Exception ex)
							{
								DebugHandler.LogError("Hook HUD.AddPart方法失败", ex);
							}
						}
					}
					else
					{
						DebugHandler.LogWarning("未找到Rain Meadow程序集，可能未安装Rain Meadow模组");
					}
				}
				catch (Exception ex)
				{
					DebugHandler.LogError("Hook失败", ex);
				}
				
				// 注册游戏启动事件，用于查找并禁用已存在的ChatHud
				On.RainWorldGame.ctor += RainWorldGame_ctor;
				
				// 注册Update事件，用于持续检测并禁用ChatHud
				On.RainWorld.Update += RainWorld_Update;
			}
			else
			{
				DebugHandler.Log("根据配置，不禁用Rain Meadow的聊天框");
				
				// 即使不禁用聊天框，也初始化RainMeadowAdapter
				if (rainMeadowAdapter != null)
				{
					rainMeadowAdapter.Initialize();
					DebugHandler.Log("已初始化RainMeadowAdapter");
				}
			}
			
			// 注册按键事件
			On.RainWorldGame.Update += RainWorldGame_Update;
			
			DebugHandler.Log("雨甸中文输入已初始化");
		}
		
		
		private void RainWorld_Update(On.RainWorld.orig_Update orig, RainWorld self)
		{
			orig(self);
			
			// 只有在配置中启用了禁用Meadow聊天框时才执行
			if (ConfigManager.DisableMeadowChat.Value)
			{
				// 每隔一段时间检测并禁用ChatHud
				if (Time.frameCount % 60 == 0) // 每60帧检测一次
				{
					DisableChatHudDirect();
				}
			}
		}
		
		private void DisableChatHudDirect()
		{
			try
			{
				// 查找所有MonoBehaviour
				var allMonoBehaviours = UnityEngine.Object.FindObjectsOfType<UnityEngine.MonoBehaviour>();
				
				// 查找所有可能的ChatHud实例
				foreach (var mb in allMonoBehaviours)
				{
					// 只处理ChatHud类型，更精确地匹配
					if (mb.GetType().FullName != null && mb.GetType().FullName.Contains("RainMeadow.ChatHud"))
					{
						DebugHandler.LogVerbose($"找到ChatHud实例: {mb.GetType().FullName}");
						
						// 尝试调用ShutDownChatLog和ShutDownChatInput方法
						try
						{
							// 关闭聊天日志
							var shutDownChatLogMethod = mb.GetType().GetMethod("ShutDownChatLog", BindingFlags.Public | BindingFlags.Instance);
							if (shutDownChatLogMethod != null)
							{
								shutDownChatLogMethod.Invoke(mb, null);
								DebugHandler.LogVerbose("成功调用ShutDownChatLog方法");
							}
							
							// 关闭聊天输入
							var shutDownChatInputMethod = mb.GetType().GetMethod("ShutDownChatInput", BindingFlags.Public | BindingFlags.Instance);
							if (shutDownChatInputMethod != null)
							{
								shutDownChatInputMethod.Invoke(mb, null);
								DebugHandler.LogVerbose("成功调用ShutDownChatInput方法");
							}
							
							// 设置showChatLog为false
							var showChatLogField = mb.GetType().GetField("showChatLog", BindingFlags.NonPublic | BindingFlags.Instance);
							if (showChatLogField != null)
							{
								showChatLogField.SetValue(mb, false);
								DebugHandler.LogVerbose("成功设置showChatLog为false");
							}
							
							// 设置isLogToggled为false
							var isLogToggledProperty = mb.GetType().GetProperty("isLogToggled", BindingFlags.Public | BindingFlags.Static);
							if (isLogToggledProperty != null)
							{
								isLogToggledProperty.SetValue(null, false, null);
								DebugHandler.LogVerbose("成功设置isLogToggled为false");
							}
							
							DebugHandler.Log("成功隐藏ChatHud显示");
						}
						catch (Exception ex)
						{
							DebugHandler.LogError("隐藏ChatHud显示失败", ex);
						}
					}
				}
				
				// 隐藏聊天相关的GameObject，但不禁用它们
				var allGameObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
				foreach (var go in allGameObjects)
				{
					if (go.name == "ChatLogOverlay" || go.name == "ChatInputOverlay")
					{
						try
						{
							// 获取CanvasGroup组件
							var canvasGroup = go.GetComponent<UnityEngine.CanvasGroup>();
							if (canvasGroup != null)
							{
								// 设置透明度为0，使其不可见
								canvasGroup.alpha = 0f;
								DebugHandler.LogVerbose($"设置{go.name}的透明度为0");
							}
							else
							{
								// 如果没有CanvasGroup组件，尝试隐藏其子对象
								foreach (Transform child in go.transform)
								{
									child.gameObject.SetActive(false);
								}
								DebugHandler.LogVerbose($"隐藏{go.name}的所有子对象");
							}
						}
						catch (Exception ex)
						{
							DebugHandler.LogError($"隐藏{go.name}失败", ex);
						}
					}
				}
			}
			catch (Exception ex)
			{
				DebugHandler.LogError("隐藏ChatHud失败", ex);
			}
		}
		
		private void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
		{
			orig(self, manager);
			
			// 只有在配置中启用了禁用Meadow聊天框时才执行
			if (ConfigManager.DisableMeadowChat.Value)
			{
				// 启动协程来禁用ChatHud
				try
				{
					// 使用反射获取RainWorldGame的协程启动方法
					var startCoroutineMethod = typeof(RainWorldGame).GetMethod("StartCoroutine", 
						BindingFlags.Public | BindingFlags.Instance, 
						null, 
						new Type[] { typeof(IEnumerator) }, 
						null);
					
					if (startCoroutineMethod != null)
					{
						DebugHandler.Log("找到StartCoroutine方法，准备启动协程");
						startCoroutineMethod.Invoke(self, new object[] { DisableChatHudCoroutine() });
						DebugHandler.Log("成功启动协程");
					}
					else
					{
						DebugHandler.LogWarning("未找到StartCoroutine方法，无法启动协程");
					}
				}
				catch (Exception ex)
				{
					DebugHandler.LogError("启动协程失败", ex);
				}
			}
			else
			{
				DebugHandler.Log("根据配置，不禁用Rain Meadow的聊天框，保留原版聊天功能");
			}
			
			// 创建GHUDTest实例，用于初始化GHUD
			try
			{
				// 检查是否启用了中文输入
				if (ConfigManager.EnableChineseInput.Value)
				{
					// 创建一个新的GameObject并添加GHUDTest组件
					var testObject = new GameObject("GHUDTest");
					var test = testObject.AddComponent<GhostPlayer.GHUDTest>();
					UnityEngine.Object.DontDestroyOnLoad(testObject);
					DebugHandler.Log("已创建GHUDTest实例");
				}
				else
				{
					DebugHandler.Log("根据配置，不启用中文输入");
				}
			}
			catch (Exception ex)
			{
				DebugHandler.LogError("创建GHUDTest实例失败", ex);
			}
		}
		
		private IEnumerator DisableChatHudCoroutine()
		{
			// 等待几帧，确保所有HUD组件都已加载
			for (int i = 0; i < 10; i++)
				yield return null;
			
			try
			{
				// 查找所有MonoBehaviour
				var allMonoBehaviours = UnityEngine.Object.FindObjectsOfType<UnityEngine.MonoBehaviour>();
				DebugHandler.LogVerbose($"找到 {allMonoBehaviours.Length} 个MonoBehaviour");
				
				// 查找所有可能的ChatHud实例
				foreach (var mb in allMonoBehaviours)
				{
					// 只处理ChatHud类型，更精确地匹配
					if (mb.GetType().FullName != null && mb.GetType().FullName.Contains("RainMeadow.ChatHud"))
					{
						DebugHandler.Log($"找到ChatHud实例: {mb.GetType().FullName}，尝试隐藏");
						
						// 尝试调用ShutDownChatLog和ShutDownChatInput方法
						try
						{
							// 关闭聊天日志
							var shutDownChatLogMethod = mb.GetType().GetMethod("ShutDownChatLog", BindingFlags.Public | BindingFlags.Instance);
							if (shutDownChatLogMethod != null)
							{
								shutDownChatLogMethod.Invoke(mb, null);
								DebugHandler.Log("成功调用ShutDownChatLog方法");
							}
							
							// 关闭聊天输入
							var shutDownChatInputMethod = mb.GetType().GetMethod("ShutDownChatInput", BindingFlags.Public | BindingFlags.Instance);
							if (shutDownChatInputMethod != null)
							{
								shutDownChatInputMethod.Invoke(mb, null);
								DebugHandler.Log("成功调用ShutDownChatInput方法");
							}
							
							// 设置showChatLog为false
							var showChatLogField = mb.GetType().GetField("showChatLog", BindingFlags.NonPublic | BindingFlags.Instance);
							if (showChatLogField != null)
							{
								showChatLogField.SetValue(mb, false);
								DebugHandler.Log("成功设置showChatLog为false");
							}
							
							// 设置isLogToggled为false
							var isLogToggledProperty = mb.GetType().GetProperty("isLogToggled", BindingFlags.Public | BindingFlags.Static);
							if (isLogToggledProperty != null)
							{
								isLogToggledProperty.SetValue(null, false, null);
								DebugHandler.Log("成功设置isLogToggled为false");
							}
							
							DebugHandler.Log("成功隐藏ChatHud显示");
						}
						catch (Exception ex)
						{
							DebugHandler.LogError("隐藏ChatHud显示失败", ex);
						}
					}
				}
				
				// 隐藏聊天相关的GameObject，但不禁用它们
				var allGameObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
				foreach (var go in allGameObjects)
				{
					if (go.name == "ChatLogOverlay" || go.name == "ChatInputOverlay")
					{
						try
						{
							// 获取CanvasGroup组件
							var canvasGroup = go.GetComponent<UnityEngine.CanvasGroup>();
							if (canvasGroup != null)
							{
								// 设置透明度为0，使其不可见
								canvasGroup.alpha = 0f;
								DebugHandler.LogVerbose($"设置{go.name}的透明度为0");
							}
							else
							{
								// 如果没有CanvasGroup组件，尝试隐藏其子对象
								foreach (Transform child in go.transform)
								{
									child.gameObject.SetActive(false);
								}
								DebugHandler.LogVerbose($"隐藏{go.name}的所有子对象");
							}
						}
						catch (Exception ex)
						{
							DebugHandler.LogError($"隐藏{go.name}失败", ex);
						}
					}
				}
			}
			catch (Exception ex)
			{
				DebugHandler.LogError("隐藏ChatHud失败", ex);
			}
		}
		
		private void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
		{
			orig(self);
			
			// 我们不再需要在这里处理输入框的激活，因为GHUDTest类会处理这个逻辑
			// GHUDTest会检查ConfigManager.EnableChineseInput.Value和ConfigManager.ToggleInputBoxKey.Value
		}
	}
	
	/// <summary>
	/// 禁用HUD.AddPart方法添加ChatHud
	/// </summary>
	public static class HudAddPartPatch
	{
		public static bool Prefix(HudPart part)
		{
			// 只有在配置中启用了禁用Meadow聊天框时才执行
			if (!ConfigManager.DisableMeadowChat.Value)
				return true;
				
			// 检查是否是ChatHud类型
			if (part != null && (part.GetType().Name.Contains("ChatHud") || part.GetType().Name.Contains("Chat")))
			{
				DebugHandler.Log($"阻止添加ChatHud: {part.GetType().FullName}");
				return false; // 阻止添加ChatHud
			}
			return true; // 允许添加其他HudPart
		}
	}
	
	/// <summary>
	/// 禁用Meadow的ChatHud绘制
	/// </summary>
	public static class MeadowChatHudPatch
	{
		public static bool Prefix()
		{
			// 只有在配置中启用了禁用Meadow聊天框时才阻止原始方法执行
			if (!ConfigManager.DisableMeadowChat.Value)
				return true;
				
			// 返回false阻止原始方法执行
			return false;
		}
	}
	
	/// <summary>
	/// 禁用T键的原始功能
	/// </summary>
	public static class TKeyPatch
	{
		public static bool Prefix(object __instance)
		{
			try
			{
				// 只有在配置中启用了禁用Meadow聊天框时才拦截按键
				if (!ConfigManager.DisableMeadowChat.Value)
					return true;
					
				// 检查是否按下了配置的按键
				int keyCode = ConfigManager.GetToggleInputBoxKeyCode();
				if (ReInput.players.GetPlayer(0).GetButtonDown(keyCode))
				{
					// 如果按下了配置的按键，阻止原始方法执行
					DebugHandler.LogVerbose("拦截按键按下事件");
					return false;
				}
			}
			catch (Exception ex)
			{
				// 忽略错误，继续执行原始方法
			}
			
			return true; // 允许原始方法执行
		}
	}
}