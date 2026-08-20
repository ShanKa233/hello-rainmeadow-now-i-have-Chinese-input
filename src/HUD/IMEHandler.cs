using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

        private const string RootName = "GHUDInputCanvas";
        private const string InputFieldName = "GHUDInputField";

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
        /// 用字符串查类型而非typeof，因为TypingHandler是internal的，
        /// 万一原版改名也只会静默失灵并留下日志，不会抛TypeLoadException。
        /// </summary>
        private static readonly Type TypingHandlerType =
            FindType("Menu.Remix.MixedUI.TypingHandler", "Assembly-CSharp");

        /// <summary>
        /// 雨甸聊天框专用的子类，用来区分当前输入框是不是雨甸的聊天窗口。
        /// 没装雨甸时为null，不影响其它输入框的输入法支持。
        /// </summary>
        private static readonly Type MeadowChatHandlerType =
            FindType("RainMeadow.ButtonTypingHandler", "Rain Meadow");

        /// <summary>
        /// 在已加载的程序集里按名字找类型，优先在指定程序集里找
        /// </summary>
        private static Type FindType(string typeName, string preferredAssembly)
        {
            var type = Type.GetType(typeName + ", " + preferredAssembly);
            if (type != null) return type;

            // 程序集改名了就全局找一遍
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = assembly.GetType(typeName);
                    if (type != null) return type;
                }
                catch (Exception)
                {
                    // 个别程序集反射会抛异常，跳过即可
                }
            }
            return null;
        }

        // UI组件
        public InputField inputField;

        // 缓存存活的输入通道组件。Unity的==null能识别已销毁对象，
        // 所以打字期间这里直接命中缓存，不需要每帧扫描场景。
        private UnityEngine.Object cachedHandler;
        private UnityEngine.Object cachedMeadowHandler;
        private bool activated;

        /// <summary>
        /// 初始化单例
        /// </summary>
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 初始化IMEHandler系统
        /// </summary>
        void Start()
        {
            if (TypingHandlerType == null)
            {
                DebugHandler.LogError("找不到 Menu.Remix.MixedUI.TypingHandler，中文输入将不会生效。原版可能更换了输入处理方式。");
            }
            else if (MeadowChatHandlerType == null)
            {
                // 不影响其它输入框，只是无法单独识别雨甸聊天框
                DebugHandler.Log("未找到 RainMeadow.ButtonTypingHandler，雨甸未安装或已改名");
            }

            // 确保有EventSystem，否则InputField无法获得焦点
            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
                DontDestroyOnLoad(eventSystem);
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

        /// <summary>
        /// 是否存在任何等待键盘输入的输入框。
        /// 原版Remix设置菜单、雨甸聊天框、其它用Remix UI的模组都算在内。
        /// </summary>
        public bool IsTypingActive() => IsHandlerAlive(TypingHandlerType, ref cachedHandler);

        /// <summary>
        /// 当前是否存在雨甸的聊天框。
        /// 这是留给调用方的口子：想只在雨甸聊天时开输入法，
        /// 把InputFieldUpdate里的IsTypingActive()换成这个即可。
        /// </summary>
        public bool IsMeadowChatActive() => IsHandlerAlive(MeadowChatHandlerType, ref cachedMeadowHandler);

        /// <summary>
        /// 场景里是否有该类型的输入通道存活
        /// </summary>
        private static bool IsHandlerAlive(Type handlerType, ref UnityEngine.Object cache)
        {
            if (handlerType == null) return false;

            // 组件还活着说明输入框还在，无需重新扫描
            if (cache != null) return true;

            cache = FindObjectOfType(handlerType);
            return cache != null;
        }

        /// <summary>
        /// 设置输入框
        /// </summary>
        void SetupInputField()
        {
            // 模组重载时旧对象会因为DontDestroyOnLoad残留下来，这里复用或清掉多余的
            var existing = FindObjectsOfType<InputField>()
                .Where(f => f.gameObject.name == InputFieldName)
                .ToArray();

            if (existing.Length > 0)
            {
                for (int i = 1; i < existing.Length; i++)
                {
                    Destroy(existing[i].transform.root.gameObject);
                }

                inputField = existing[0];
                inputField.onEndEdit.RemoveAllListeners();
                inputField.onEndEdit.AddListener(OnEndEdit);
                return;
            }

            // 挪到屏幕外，配合近乎全透明的颜色，玩家看不到它
            var root = new GameObject(RootName);
            root.transform.position = new Vector3(10000f, 10000f, 10000f);
            DontDestroyOnLoad(root);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var inputObj = new GameObject(InputFieldName);
            inputObj.transform.SetParent(root.transform, false);
            inputObj.AddComponent<RectTransform>().sizeDelta = new Vector2(200f, 30f);
            inputObj.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.01f);

            // InputField需要一个文本组件承载内容，必须是子对象
            var textObj = new GameObject("GHUD_Text");
            textObj.transform.SetParent(inputObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(190f, 20f);
            textRect.anchoredPosition = Vector2.zero;

            var text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.color = new Color(1f, 1f, 1f, 0.01f);
            text.alignment = TextAnchor.MiddleLeft;

            inputField = inputObj.AddComponent<InputField>();
            inputField.textComponent = text;
            inputField.caretWidth = 0;
            inputField.caretBlinkRate = 0;
            inputField.selectionColor = new Color(0.2f, 0.6f, 1f, 0.01f);
            inputField.onEndEdit.AddListener(OnEndEdit);
        }

        /// <summary>
        /// 处理输入完成
        /// </summary>
        void OnEndEdit(string value) => ResetInputField();

        /// <summary>
        /// 重置输入框
        /// </summary>
        private void ResetInputField()
        {
            if (inputField == null) return;

            inputField.text = "";
            inputField.DeactivateInputField();
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
                    inputField.ActivateInputField();
                    inputField.Select();
                    activated = true;
                    DebugHandler.Log($"{DescribeActiveInput()}出现，已开启输入法");
                }
                catch (Exception ex)
                {
                    DebugHandler.LogError("激活输入框失败", ex);
                }
                return;
            }

            if (activated && !typingActive)
            {
                ResetInputField();
                DebugHandler.Log("输入框关闭，已关闭输入法");
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
        /// 描述当前是哪种输入框，只用于日志
        /// </summary>
        private string DescribeActiveInput()
        {
            if (cachedHandler == null) return "输入框";
            if (MeadowChatHandlerType != null && MeadowChatHandlerType.IsInstanceOfType(cachedHandler))
            {
                return "雨甸聊天框";
            }
            return $"游戏输入框({cachedHandler.GetType().Name})";
        }
    }
}
