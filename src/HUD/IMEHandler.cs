#pragma warning disable 0649
using RWCustom;
using System;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RainMeadow;

namespace GoodMorningRainMeadow
{
    /// <summary>
    /// IMEHandler类：中文输入模组的主要HUD系统
    /// 负责管理UI输入框，处理中文输入
    /// </summary>
    public class IMEHandler : MonoBehaviour
    {
        // 单例模式
        public static IMEHandler Instance { get; private set; }

        // UI组件
        public InputField inputField;
        private bool activated;

        /// <summary>
        /// 检查是否应该阻止PauseMenu的调用
        /// </summary>
        public bool ShouldBlockPauseMenu() => activated && inputField != null;

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
            // 确保有EventSystem
            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
                DontDestroyOnLoad(eventSystem);
            }
            
            // 设置输入框
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
                inputField.onValueChanged.RemoveAllListeners();
                inputField.onEndEdit.RemoveAllListeners();
            }
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        void Update() => InputFieldUpdate();

        /// <summary>
        /// 检查聊天框是否开启
        /// </summary>
        public bool IsChatHudActive()
        {
            RainWorldGame game = Custom.rainWorld?.processManager?.currentMainLoop as RainWorldGame;
            if (game == null || game.cameras == null || game.cameras.Length == 0 || game.cameras[0]?.hud?.parts == null) return false;
            ChatHud chatHud = game.cameras[0].hud.parts.OfType<ChatHud>().FirstOrDefault();
            return chatHud != null && chatHud.chatInputActive;
        }

        /// <summary>
        /// 检查聊天是否激活
        /// </summary>
        public bool IsChatActive()
        {
            if (Custom.rainWorld?.processManager == null) return false;
            
            var processManager = Custom.rainWorld.processManager;
            if (processManager.currentMainLoop is StoryOnlineMenu storyMenu && 
                storyMenu.pages != null && 
                storyMenu.pages.Count > 0 && 
                storyMenu.pages[0]?.subObjects != null)
            {
                return storyMenu.pages[0].subObjects.Any(obj => obj is ChatTextBox);
            }
            return IsChatHudActive();
        }

        /// <summary>
        /// 设置输入框
        /// </summary>
        void SetupInputField()
        {
            var existingInputFields = FindObjectsOfType<InputField>().Where(f => f.gameObject.name == "GHUDInputField").ToArray();

            if (existingInputFields.Length > 1)
            {
                for (int i = 1; i < existingInputFields.Length; i++)
                {
                    Destroy(existingInputFields[i].gameObject);
                }
            }

            if (existingInputFields.Length > 0)
            {
                inputField = existingInputFields[0];
                inputField.onValueChanged.RemoveAllListeners();
                inputField.onEndEdit.RemoveAllListeners();
                inputField.onEndEdit.AddListener(OnEndEdit);
                return;
            }

            // 创建一个简单的GameObject作为容器
            var obj = new GameObject("GHUDInputField");
            obj.transform.position = new Vector3(10000f, 10000f, 10000f);
            
            // 添加Canvas组件确保正确渲染
            var canvas = obj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            obj.AddComponent<CanvasScaler>();
            obj.AddComponent<GraphicRaycaster>();

            // 添加必要的UI组件
            var inputObj = new GameObject("InputFieldObj");
            inputObj.transform.SetParent(obj.transform, false);
            
            var rectTransform = inputObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 30);
            
            var image = inputObj.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.01f);

            // 创建文本组件
            var text = new GameObject("GHUD_Text");
            text.transform.SetParent(inputObj.transform, false);
            var textRect = text.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(190, 20);
            textRect.anchoredPosition = Vector2.zero;
            var inputText = text.AddComponent<Text>();
            inputText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            inputText.color = new Color(1f, 1f, 1f, 0.01f);
            inputText.alignment = TextAnchor.MiddleLeft;

            // 创建占位符组件
            var placeHolder = new GameObject("GHUD_PlaceHolder");
            placeHolder.transform.SetParent(inputObj.transform, false);
            var placeHolderRect = placeHolder.AddComponent<RectTransform>();
            placeHolderRect.sizeDelta = new Vector2(190, 20);
            placeHolderRect.anchoredPosition = Vector2.zero;
            var placeholderText = placeHolder.AddComponent<Text>();
            placeholderText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.01f);
            placeholderText.text = "";
            placeholderText.alignment = TextAnchor.MiddleLeft;

            // 添加InputField组件
            inputField = inputObj.AddComponent<InputField>();
            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;
            inputField.caretWidth = 0;
            inputField.caretBlinkRate = 0;
            inputField.selectionColor = new Color(0.2f, 0.6f, 1f, 0.01f);

            inputField.onEndEdit.AddListener(OnEndEdit);
            
            // 确保不被销毁
            DontDestroyOnLoad(obj);
        }

        /// <summary>
        /// 处理输入完成
        /// </summary>
        void OnEndEdit(string value)
        {
            ResetInputField();
        }

        /// <summary>
        /// 重置输入框
        /// </summary>
        private void ResetInputField()
        {
            if (inputField != null)
            {
                inputField.text = "";
                inputField.DeactivateInputField();
                activated = false;
            }
        }

        /// <summary>
        /// 更新输入框状态
        /// </summary>
        void InputFieldUpdate()
        {
            if (inputField == null) return;
            
            if (!activated && IsChatActive())
            {
                try
                {
                    inputField.ActivateInputField();
                    inputField.Select();
                    activated = true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"激活输入框失败: {ex.Message}");
                }
                return;
            }
            if (activated && !IsChatActive())
            {
                ResetInputField();
                return;
            }
        }

        /// <summary>
        /// 初始化方法（外部调用）
        /// </summary>
        internal void Initialize() { }
    }

    /// <summary>
    /// 输入框文本事件委托
    /// </summary>
    public delegate void InputFieldTextEvent(string text, int caretPos);
}

