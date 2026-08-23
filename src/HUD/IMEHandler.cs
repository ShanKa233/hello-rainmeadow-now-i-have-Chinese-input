using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Menu.Remix.MixedUI;

namespace GoodMorningRainMeadow
{
    /// <summary>
    /// IMEHandler类：中文输入模组的核心
    /// 游戏里出现输入框时激活一个隐藏的InputField，借此打开系统输入法通道。
    /// 输入法提交的字符会进入Input.inputString，由输入框自己读走，本模组不搬运文本。
    ///
    /// 覆盖范围是所有走Remix输入体系的输入框：原版Remix设置菜单的OpTextBox / OpComboBox、
    /// 雨甸的聊天框，以及其它用Remix UI做输入框的模组。
    /// 需要单独识别雨甸聊天框时用IsMeadowChatActive()。
    /// </summary>
    public class IMEHandler : MonoBehaviour
    {
        // 单例模式
        public static IMEHandler Instance { get; private set; }

        private const string RootName = "GHUDInputField";

        /// <summary>
        /// 原版Remix输入体系的输入通道，游戏里所有文本框接收键盘输入都要经过它。
        /// 使用方调用CanBeTypedExt.Assign时按需挂到一个专属GameObject("RemixTyping")上，
        /// 而它在自己的_assigned清空后会自毁，
        /// 所以"它是否存活"等价于"此刻是否存在等待键盘输入的输入框"。
        ///
        /// 雨甸的RainMeadow.ButtonTypingHandler继承自它，
        /// FindObjectOfType会连子类一起匹配，所以雨甸聊天框同样覆盖。
        ///
        /// 这样判断不碰任何菜单页面层级、HUD归属或具体输入框类型，
        /// 雨甸再挪聊天框的位置（0.1.15就把ChatHud从cameras[0].hud搬到了RMOverlayHUD）也不受影响，
        /// 将来新增输入框同样自动覆盖。
        // 输入通道类型。项目引用的游戏DLL是BepInEx publicizer处理的
        // PUBLIC-Assembly-CSharp.dll，internal类型已公开，可以直接typeof。
        // FindObjectOfType会连子类一起匹配，雨甸的ButtonTypingHandler同样覆盖。
        private static readonly Type TypingHandlerType = typeof(TypingHandler);

        /// <summary>
        /// 按实例类型缓存_focused字段的FieldInfo。
        /// 雨甸的ButtonTypingHandler没有复用基类的字段，而是自己声明了一份
        /// 同名_focused并由自己的Update维护，所以必须按运行时类型取字段，
        /// 才能拿到"正在被维护"的那一份。
        /// </summary>
        private static readonly Dictionary<Type, FieldInfo> FocusedFieldCache =
            new Dictionary<Type, FieldInfo>();

        private static FieldInfo GetFocusedField(Type handlerType)
        {
            if (!FocusedFieldCache.TryGetValue(handlerType, out var field))
            {
                // 原版TypingHandler._focused是private，雨甸ButtonTypingHandler的
                // 同名new字段是public，两种可见性都要搜，否则雨甸拿不到字段
                // 会退回"存活即算"导致聊天框打开期间IME关不掉
                field = handlerType.GetField("_focused",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                FocusedFieldCache[handlerType] = field;
            }
            return field;
        }

        // UI组件
        public InputField inputField;

        // 缓存的输入通道组件。原版TypingHandler和雨甸ButtonTypingHandler
        // 可能同时存活，所以要复数查找、逐个检查。
        // Unity的==null能识别已销毁对象；但聊天框打开是"新增handler"，
        // 缓存里看不出，所以每0.25秒强制重扫一次。
        private UnityEngine.Object[] cachedHandlers;
        private float handlerRescanAt;
        private bool activated;

        /// <summary>
        /// 初始化单例
        /// </summary>
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // 本组件现在挂在雨世界主对象上，绝不能销毁gameObject，只拆掉多余的组件
                Destroy(this);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 初始化IMEHandler系统
        /// </summary>
        void Start()
        {

            // 确保有EventSystem，否则InputField无法获得焦点
            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
                eventSystem.transform.SetParent(transform, false); // 挂在主对象下，随它跨场景永存
            }

            SetupInputField();
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            if (inputField != null)
            {
                inputField.onEndEdit.RemoveAllListeners();
            }
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        void Update() => InputFieldUpdate();

        void SetupInputField()
        {
            var root = new GameObject(RootName);
            root.transform.SetParent(transform, false);
            root.AddComponent<Canvas>();
            var text = root.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            inputField = root.AddComponent<InputField>();
            inputField.textComponent = text;
            inputField.onEndEdit.AddListener(OnEndEdit);

            // 平时整个辅助对象保持关闭，游戏里出现输入框时才临时打开
            root.SetActive(false);
        }

        /// <summary>
        /// 处理输入完成
        /// </summary>
        void OnEndEdit(string value)
        {
            ModLog.Log("[关闭] onEndEdit触发（uGUI自己结束编辑）");
            ResetInputField();
        }

        /// <summary>
        /// 重置输入框
        /// </summary>
        private void ResetInputField()
        {
            if (inputField == null) return;

            inputField.text = "";
            inputField.DeactivateInputField();
            // 连同整个辅助对象一起关掉：失活后Unity完全跳过它（Update、事件、渲染全停），
            // Selectable的OnDisable还会自动清掉EventSystem的选中状态
            inputField.gameObject.SetActive(false);
            // 硬关IME必须放在SetActive之后：InputField的OnDisable内部会调
            // DeactivateInputField把imeCompositionMode设回Auto，先失活再设Off，
            // Off才是最终状态，候选窗才会完全消失
            Input.imeCompositionMode = IMECompositionMode.Off;
            activated = false;
        }

        /// <summary>
        /// 跟随输入框的出现与消失，开关输入法通道
        /// </summary>
        void InputFieldUpdate()
        {
            if (inputField == null) return;

            bool typingActive = IsTypingActive();

            if (!activated && typingActive)
            {
                try
                {
                    inputField.gameObject.SetActive(true);
                    inputField.ActivateInputField();
                    inputField.Select();
                    activated = true;
                    ModLog.Log($"[开启] 输入法已打开 (输入通道={DescribeHandler()})");
                }
                catch (Exception ex)
                {
                    ModLog.LogError($"激活输入框失败\n{ex}");
                }
                return;
            }

            if (activated && !typingActive)
            {
                ModLog.Log($"[关闭] 检测到输入框失焦/关闭 (输入通道={DescribeHandler()})");
                ResetInputField();
                return;
            }

            // 文本由输入框自己从Input.inputString读取，这里的内容没有用处，
            // 长时间打字会一直堆积，攒够一批就清掉
            if (activated && inputField.text.Length > 64)
            {
                inputField.text = "";
            }
        }

        /// <summary>
        /// 是否有输入框正在接收键盘输入。
        /// 遍历全部存活的输入通道组件逐个判断——原版TypingHandler和雨甸
        /// ButtonTypingHandler可能同时在场，只看一个会漏。
        /// 原版TypingHandler._focused由自己的Update维护：输入框失焦置null、
        /// 聚焦重新设置，非空即代表"打字真的会进输入框"。
        /// 雨甸ButtonTypingHandler则把_focused每帧强制设为第一个注册框，
        /// 非空只代表"聊天框打开"，聚焦与否见IsFocusedTypable。
        /// </summary>
        public bool IsTypingActive()
        {
            if (TypingHandlerType == null) return false;

            RefreshHandlerCache();
            if (cachedHandlers == null) return false;

            foreach (var handler in cachedHandlers)
            {
                if (handler == null) continue; // Unity假null能被==识别，直接跳过

                // 按实例类型取_focused（雨甸自己声明并维护了同名字段），
                // 拿不到字段（原版改名）时退回"存活即算"的旧行为
                var focusedField = GetFocusedField(handler.GetType());
                if (focusedField == null)
                {
                    LogTypingSource("字段缺失→存活即算");
                    return true;
                }

                var focused = focusedField.GetValue(handler);
                if (focused == null) continue;

                // Unity假null：输入框组件已销毁但引用还没清，同样视为未聚焦。
                // C#的!= null认不出这种状态，检测会永远为true导致IME关不掉
                if (focused is UnityEngine.Object uo && uo == null) continue;

                // 原版TypingHandler：_focused只指向真正聚焦(或按住)的输入框，非空即算
                if (handler.GetType() == TypingHandlerType)
                {
                    LogTypingSource($"原版TypingHandler(_focused={DescribeTypable(focused)})");
                    return true;
                }

                // TypingHandler的子类（雨甸ButtonTypingHandler）：语义不同，见IsFocusedTypable
                if (IsFocusedTypable(focused))
                {
                    LogTypingSource($"子类{handler.GetType().Name}(_focused={DescribeTypable(focused)})");
                    return true;
                }
            }
            LogTypingSource("无");
            return false;
        }

        /// <summary>
        /// 打字激活来源变化时打日志（值不变不打，避免振荡时刷屏）
        /// </summary>
        private string lastTypingSource = "";
        private void LogTypingSource(string source)
        {
            if (lastTypingSource != source)
            {
                lastTypingSource = source;
                ModLog.Log($"[判定] 打字激活来源={source}");
            }
        }

        /// <summary>
        /// 输入目标的名字，仅用于排查日志
        /// </summary>
        private static string DescribeTypable(object typable)
        {
            if (typable == null) return "null";
            return typable.GetType().Name;
        }

        /// <summary>
        /// 每隔0.25秒重扫一次场景里的输入通道组件。
        /// 聊天框打开会"新增"handler，缓存无法察觉，只能定时重扫；
        /// handler销毁靠Unity的==null识别，重扫后自然剔除。
        /// </summary>
        private void RefreshHandlerCache()
        {
            if (cachedHandlers != null && Time.unscaledTime < handlerRescanAt) return;
            cachedHandlers = FindObjectsOfType(TypingHandlerType);
            handlerRescanAt = Time.unscaledTime + 0.25f;
        }

        /// <summary>
        /// 运行时雨甸是否在场。本模组成品不强制依赖雨甸（modinfo.json 的
        /// requirements 为空），但编译期引用了雨甸项目，所以所有触碰雨甸
        /// 类型的方法必须在确认雨甸已加载后才执行——雨甸缺席时JIT一旦
        /// 解析雨甸类型，整个模组会抛TypeLoadException直接加载失败。
        /// 按类型全名探测，不依赖雨甸的DLL文件名。
        /// </summary>
        private static readonly bool MeadowLoaded =
            AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetType("RainMeadow.ChatTextBox") != null);

        /// <summary>
        /// 判断TypingHandler子类的输入目标是否真的在接收键盘输入。
        /// 雨甸ButtonTypingHandler把_focused每帧强制设为第一个注册框
        /// （见雨甸Menu/Objects/ButtonTypingHandler.cs），聊天框只要打开就
        /// 非空，聚焦与否要看ChatTextBox自己：
        /// - 大厅聊天框(MultiView=true)：需点击/按键聚焦，Focused=true才接收字符
        /// - 游戏内聊天框(MultiView=false)：打开即接收字符，非空即算
        /// 雨甸类型只出现在IsFocusedMeadowChat的方法体里（类型签名层面零
        /// 雨甸类型），入口先查MeadowLoaded，再try/catch隔离。
        /// </summary>
        private static bool IsFocusedTypable(object typable)
        {
            if (!MeadowLoaded) return true; // 雨甸没装：按原版语义兜底
            try
            {
                return IsFocusedMeadowChat(typable);
            }
            catch (Exception)
            {
                // 雨甸内部结构变了，退回原版语义
                return true;
            }
        }

        /// <summary>
        /// 只有确认雨甸在场后才允许被调用（见IsFocusedTypable）。
        /// 编译期引用的雨甸类型只允许出现在这个方法体内部。
        /// </summary>
        private static bool IsFocusedMeadowChat(object typable)
        {
            if (typable is RainMeadow.ChatTextBox chatBox)
            {
                // 大厅聊天框聚焦才接收字符；游戏内聊天框打开即接收字符
                var result = chatBox.MultiView ? chatBox.Focused : true;
                LogMeadowChatDetail($"is匹配成功 MultiView={chatBox.MultiView} Focused={chatBox.Focused} → {result}");
                return result;
            }
            LogMeadowChatDetail($"is匹配失败({DescribeTypable(typable)}) → 兜底true");
            return true; // 不认识的目标按原版语义兜底
        }

        /// <summary>
        /// 雨甸聊天框判定详情变化时打日志（值不变不打，避免振荡时刷屏）
        /// </summary>
        private static string lastMeadowChatDetail = "";
        private static void LogMeadowChatDetail(string detail)
        {
            if (lastMeadowChatDetail != detail)
            {
                lastMeadowChatDetail = detail;
                ModLog.Log($"[判定] 雨甸聊天框 {detail}");
            }
        }

        /// <summary>
        /// 当前存活的输入通道名字，仅用于排查日志
        /// </summary>
        private string DescribeHandler()
        {
            if (cachedHandlers == null || cachedHandlers.Length == 0) return "无";
            return string.Join(",", cachedHandlers.Where(h => h != null).Select(h => h.GetType().Name).Distinct());
        }
    }
}
