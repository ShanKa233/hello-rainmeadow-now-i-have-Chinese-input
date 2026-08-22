using System;
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
        /// 用字符串查类型而非typeof，因为TypingHandler是internal的，
        /// 万一原版改名也只会静默失灵并留下日志，不会抛TypeLoadException。Menu.Remix.MixedUI.TypingHandle
        /// 雨甸聊天框专用的子类，用来区分当前输入框是不是雨甸的聊天窗口。
        /// 没装雨甸时为null，不影响其它输入框的输入法支持。
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
            // 结构直接挂在本组件所在的雨世界主对象下：主对象跨场景永存，
            // 子对象随之永存，不需要DontDestroyOnLoad，也不怕场景切换被销毁
            var root = new GameObject(RootName);
            root.transform.SetParent(transform, false);

            // Canvas会自动补上RectTransform：Text生成网格时要用它，没有会报错
            root.AddComponent<Canvas>();

            // textComponent是InputField的硬性要求：
            // ActivateInputField()开头检查它为null时直接return，输入法永不打开
            var text = root.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            // text.color = new Color(1f, 1f, 1f, 0f); // 全透明，玩家看不见

            inputField = root.AddComponent<InputField>();
            inputField.textComponent = text;
            // 关掉输入框自带的闪烁光标，否则聚焦时屏幕上会闪出一个光标
            // inputField.caretWidth = 0;
            // inputField.caretBlinkRate = 0;
            inputField.onEndEdit.AddListener(OnEndEdit);

            // 平时整个辅助对象保持关闭，游戏里出现输入框时才临时打开
            root.SetActive(false);
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
            // 连同整个辅助对象一起关掉：失活后Unity完全跳过它（Update、事件、渲染全停），
            // Selectable的OnDisable还会自动清掉EventSystem的选中状态
            inputField.gameObject.SetActive(false);
            activated = false;
        }

        /// <summary>
        /// 跟随输入框的出现与消失，开关输入法通道
        /// </summary>
        void InputFieldUpdate()
        {
            if (inputField == null) return;


            if (!activated )
            {
                try
                {
                    inputField.gameObject.SetActive(true);
                    inputField.ActivateInputField();
                    inputField.Select();
                    activated = true;
                }
                catch (Exception ex)
                {
                    DebugHandler.LogError("激活输入框失败", ex);
                }
                return;
            }


            // 文本由输入框自己从Input.inputString读取，这里的内容没有用处，
            // 长时间打字会一直堆积，攒够一批就清掉
            if (activated && inputField.text.Length > 64)
            {
                inputField.text = "";
            }
        }
    }
}
