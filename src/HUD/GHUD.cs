#pragma warning disable 0649
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using RainMeadow;
using System.Collections;

namespace GhostPlayer.GHud
{
    /// <summary>
    /// GHUD类：Ghost Player模组的主要HUD系统
    /// 负责管理所有UI元素，包括聊天框、输入框和通知系统
    /// 继承自MonoBehaviour，可以附加到游戏对象上
    /// </summary>
    public class GHUD : MonoBehaviour
    {
        // 单例模式，确保全局只有一个GHUD实例
        public static GHUD Instance { get; private set; }

        // 输入锁定状态
        bool _lockInput;

        /// <summary>
        /// 控制是否锁定游戏输入
        /// 当UI活跃时阻止游戏接收输入
        /// </summary>
        public bool LockInput
        {
            get => _lockInput;
            set
            {
                if (_lockInput == value)
                    return;
                _lockInput = value;
                Debug.Log($"[雨甸中文输入] 输入锁定状态改变: {value}");
            }
        }

        /// <summary>
        /// 检查是否应该阻止PauseMenu的调用
        /// </summary>
        public bool ShouldBlockPauseMenu()
        {
            return activated && inputField != null;
        }

        /// <summary>
        /// 检查是否可以激活输入框
        /// </summary>
        private bool CanActivateInputField()
        {
            // 检查是否在线
            if (MatchmakingManager.currentInstance == null)
            {
                return false;
            }

            // 获取当前进程
            var currentProcess = Custom.rainWorld?.processManager?.currentMainLoop;

            // 检查是否在游戏内
            if (!(currentProcess is RainWorldGame))
            {
                Debug.Log("[雨甸中文输入] 不在游戏内，无法打开聊天框");
                return false;
            }

            var game = currentProcess as RainWorldGame;

            // 检查是否存在暂停菜单
            if (game.pauseMenu != null)
            {
                return false;
            }

            return true;
        }

        #region RWParam
        // Futile容器，用于添加UI元素
        public FContainer container;
        // 雨世界游戏实例引用
        public RainWorld rainWorld;
        #endregion


        #region FixedUpdate
        // 每秒固定更新的帧数
        static readonly int framePerSec = 40;
        // 时间累加器，用于固定更新
        public float timeStacker;
        #endregion

        #region UnityUI
        // Unity Canvas组件，用于UI渲染
        public Canvas canvas;
        // 输入框组件，处理文本输入
        public InputField inputField;

        // 当前输入的文本内容
        public string currentInputString;
        // 输入框是否激活
        bool activated;
        #endregion

        #region event
        // 输入框获得焦点时触发的事件
        public event InputFieldTextEvent OnInputFieldFocus;
        // 输入框提交内容时触发的事件
        public event InputFieldTextEvent OnInputFieldSubmit;
        // 输入框内容变化时触发的事件
        public event InputFieldTextEvent OnInputFieldChanged;
        // 输入框取消输入时触发的事件
        public event InputFieldTextEvent OnInputFieldCancel;
        #endregion

        // 所有HUD部件的列表
        public List<GHUDPart> parts = new List<GHUDPart>();

        #region UnityFunc
        /// <summary>
        /// Unity Awake函数，在Start之前调用，用于初始化单例
        /// </summary>
        void Awake()
        {
            try
            {
                // 检查是否已存在GHUD实例
                if (Instance != null && Instance != this)
                {
                    Debug.Log("[雨甸中文输入] 检测到多个GHUD实例，销毁当前实例");
                    Destroy(gameObject);
                    return;
                }

                // 设置单例实例
                Instance = this;
                Debug.Log($"[雨甸中文输入] GHUD实例初始化，实例ID: {GetInstanceID()}");
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] GHUD Awake失败: " + ex.Message);
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Unity启动函数，初始化GHUD系统
        /// </summary>
        void Start()
        {
            try
            {
                Debug.Log("[雨甸中文输入] GHUD开始初始化");

                // 获取雨世界实例
                rainWorld = Custom.rainWorld;

                // 检查必要的程序集是否已加载
                CheckRequiredAssemblies();

                // 创建Unity UI系统组件
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                // 添加事件系统
                if (FindObjectOfType<EventSystem>() == null)
                {
                    var eventSystem = new GameObject("EventSystem");
                    eventSystem.AddComponent<EventSystem>();
                    eventSystem.AddComponent<StandaloneInputModule>();
                    DontDestroyOnLoad(eventSystem);
                }

                // 创建Futile容器
                container = new FContainer();

                // 创建Futile舞台并添加容器
                var stage = new FStage("GHUD");
                Futile.AddStage(stage);
                stage.AddChild(container);

                // 设置输入框和HUD组件
                SetupInputField();
                SetupHUD();

                Debug.Log("[雨甸中文输入] GHUD初始化成功");
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] GHUD初始化失败: " + ex.Message);
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Unity OnDestroy函数，在对象销毁时调用，用于清理资源
        /// </summary>
        void OnDestroy()
        {
            try
            {
                Debug.Log("[雨甸中文输入] GHUD开始销毁");

                // 如果当前实例是单例实例，则重置单例
                if (Instance == this)
                {
                    Instance = null;
                }

                // 清理所有HUD部件
                if (parts != null)
                {
                    foreach (var part in parts)
                    {
                        part.ClearSprites();
                    }
                    parts.Clear();
                }

                // 移除输入框事件监听
                if (inputField != null)
                {
                    inputField.onValueChanged.RemoveAllListeners();
                    inputField.onEndEdit.RemoveAllListeners();
                }

                // 移除Futile容器
                if (container != null)
                {
                    container.RemoveFromContainer();
                }

                Debug.Log("[雨甸中文输入] GHUD销毁完成");
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] GHUD销毁失败: " + ex.Message);
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 检查必要的程序集是否已加载
        /// </summary>
        private void CheckRequiredAssemblies()
        {
            try
            {
                // 检查UI模块
                var uiAssembly = typeof(UnityEngine.UI.InputField).Assembly;
                Debug.Log($"[雨甸中文输入] UI模块已加载: {uiAssembly.FullName}");

                // 检查UIModule
                var uiModuleAssembly = typeof(UnityEngine.Canvas).Assembly;
                Debug.Log($"[雨甸中文输入] UIModule已加载: {uiModuleAssembly.FullName}");

                // 检查EventSystems
                var eventSystemsAssembly = typeof(UnityEngine.EventSystems.EventSystem).Assembly;
                Debug.Log($"[雨甸中文输入] EventSystems已加载: {eventSystemsAssembly.FullName}");
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] 检查程序集失败: " + ex.Message);
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Unity更新函数，每帧调用
        /// </summary>
        void Update()
        {
            try
            {
                // 检查游戏场景是否已切换
                CheckGameState();
                
                // 更新输入框状态
                InputFieldUpdate();

                // 更新时间累加器
                timeStacker += Time.deltaTime * framePerSec;

                // 如果累加器达到1，执行固定更新
                while (timeStacker >= 1f)
                {
                    timeStacker -= 1f;
                    FixUpdate();
                }

                // 绘制逻辑
                Draw();

                // 测试功能和输入框更新
                TestFunc();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[雨甸中文输入] GHUD更新失败: " + ex.Message);
                UnityEngine.Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 检查游戏状态，如果不在游戏场景中则销毁GHUD
        /// </summary>
        private void CheckGameState()
        {
            try
            {
                // 获取当前进程
                var currentProcess = Custom.rainWorld?.processManager?.currentMainLoop;
                
                // 如果不在游戏内且GHUD实例存在，则销毁GHUD
                if (!(currentProcess is RainWorldGame) && Instance == this)
                {
                    Debug.Log("[雨甸中文输入] 检测到游戏场景已切换，销毁GHUD实例");
                    
                    // 清理资源
                    if (inputField != null)
                    {
                        // 确保输入框不再活跃
                        if (activated)
                        {
                            inputField.DeactivateInputField();
                            activated = false;
                            LockInput = false;
                        }
                    }
                    
                    // 延迟销毁，避免在Update中直接销毁对象
                    StartCoroutine(DelayedDestroy());
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] 检查游戏状态失败: " + ex.Message);
                Debug.LogException(ex);
            }
        }
        
        /// <summary>
        /// 延迟销毁GHUD实例
        /// </summary>
        private IEnumerator DelayedDestroy()
        {
            yield return null; // 等待一帧
            Destroy(gameObject);
        }
        #endregion

        /// <summary>
        /// 设置HUD组件，添加各种HUD部件
        /// </summary>
        void SetupHUD()
        {
            try
            {
                // // 添加通知系统
                // parts.Add(new GAnnouncementHud(this));
                // Debug.Log("[雨甸中文输入] 添加了GAnnouncementHud");

                // 添加聊天框
                parts.Add(new GChatHud(this));
                Debug.Log("[雨甸中文输入] 添加了GChatHud");

                // 添加输入框
                parts.Add(new GInputBox(this));
                Debug.Log("[雨甸中文输入] 添加了GInputBox");

                Debug.Log("[雨甸中文输入] HUD组件设置成功");
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] HUD组件设置失败: " + ex.Message);
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 固定更新函数，更新所有HUD部件
        /// </summary>
        void FixUpdate()
        {
            try
            {
                // 从后向前遍历，以便安全移除元素
                for (int i = parts.Count - 1; i >= 0; i--)
                {
                    // 如果部件标记为删除，则清理并移除
                    if (parts[i].slatedForDeletion)
                    {
                        parts[i].ClearSprites();
                        parts.RemoveAt(i);
                    }
                    else
                        // 否则更新部件
                        parts[i].Update();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] FixUpdate失败: " + ex.Message);
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 绘制函数，绘制所有HUD部件
        /// </summary>
        void Draw()
        {
            try
            {
                for (int i = 0; i < this.parts.Count; i++)
                {
                    this.parts[i].Draw(timeStacker);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] Draw失败: " + ex.Message);
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 测试功能，用于开发调试
        /// </summary>
        void TestFunc()
        {
            //if (Input.GetKeyDown(KeyCode.Space))
            //{
            //    string result = "";
            //    for(int i = 0;i < Random.Range(1, 5); i++)
            //    {
            //        result += "this is a test message ";
            //    }

            //    string[] testName = { "[Harvie]", "[wawa screamer]", "[this is a long name which is hugely long as you can see]", "[Joar]" };

            //    GChatHud.NewChatLine(testName[Random.Range(0,testName.Length)], result, (result.Length * testName.Length) * 6, Color.white);
            //    Debug.Log("[雨甸中文输入] 新聊天行已添加");
            //}
        }

        #region InputField
        /// <summary>
        /// 设置输入框，创建Unity UI输入框
        /// </summary>
        void SetupInputField()
        {
            try
            {
                Debug.Log("[雨甸中文输入] 开始设置输入框");

                // 检查是否已存在输入框
                var existingInputFields = FindObjectsOfType<InputField>().Where(f => f.gameObject.name == "GHUDInputField").ToArray();
                
                // 如果存在多个输入框，删除除第一个以外的所有输入框
                if (existingInputFields.Length > 1)
                {
                    Debug.Log($"[雨甸中文输入] 检测到{existingInputFields.Length}个输入框，保留第一个，删除其余的");
                    for (int i = 1; i < existingInputFields.Length; i++)
                    {
                        Destroy(existingInputFields[i].gameObject);
                    }
                }

                // 如果存在至少一个输入框，使用它
                if (existingInputFields.Length > 0)
                {
                    Debug.Log("[雨甸中文输入] 使用现有输入框");
                    inputField = existingInputFields[0];
                    
                    // 重新设置事件监听
                    inputField.onValueChanged.RemoveAllListeners();
                    inputField.onEndEdit.RemoveAllListeners();
                    inputField.onValueChanged.AddListener(ListenChange);
                    inputField.onEndEdit.AddListener(OnEndEdit);
                    return;
                }

                // 创建输入框游戏对象
                var obj = new GameObject("GHUDInputField");
                // 将对象放在视野外（隐藏但仍然活跃）
                obj.transform.position = new Vector3(100000f, 100000f, 100000f);
                obj.transform.SetParent(canvas.transform, false);

                // 添加必要的UI组件
                obj.AddComponent<CanvasRenderer>();
                var rectTransform = obj.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(200, 30);
                var image = obj.AddComponent<Image>();
                image.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

                // 创建占位符对象
                var placeHolder = new GameObject("GHUD_PlaceHolder");
                placeHolder.transform.SetParent(obj.transform, false);
                var placeHolderRect = placeHolder.AddComponent<RectTransform>();
                placeHolderRect.sizeDelta = new Vector2(190, 20);
                placeHolderRect.anchoredPosition = Vector2.zero;
                placeHolder.AddComponent<CanvasRenderer>();
                var placeholderText = placeHolder.AddComponent<Text>();
                placeholderText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                placeholderText.text = "输入聊天内容...";
                placeholderText.alignment = TextAnchor.MiddleLeft;

                // 创建文本对象
                var text = new GameObject("GHUD_Text");
                text.transform.SetParent(obj.transform, false);
                var textRect = text.AddComponent<RectTransform>();
                textRect.sizeDelta = new Vector2(190, 20);
                textRect.anchoredPosition = Vector2.zero;
                text.AddComponent<CanvasRenderer>();
                var inputText = text.AddComponent<Text>();
                inputText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                inputText.color = Color.white;
                inputText.alignment = TextAnchor.MiddleLeft;

                // 设置输入框组件
                inputField = obj.AddComponent<InputField>();
                inputField.textComponent = inputText;
                inputField.placeholder = placeholderText;
                inputField.caretWidth = 2;
                inputField.selectionColor = new Color(0.2f, 0.6f, 1f, 0.4f);

                // 添加值变化监听器
                inputField.onValueChanged.AddListener(ListenChange);
                inputField.onEndEdit.AddListener(OnEndEdit);

                Debug.Log("[雨甸中文输入] 输入框设置成功");
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] 输入框设置失败: " + ex.Message);
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 监听输入框值变化
        /// </summary>
        /// <param name="value">新的输入值</param>
        void ListenChange(string value)
        {
            try
            {
                // 更新当前输入字符串
                currentInputString = value;
                // 触发值变化事件
                OnInputFieldChanged?.Invoke(value, inputField.caretPosition);
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] 处理输入框值变化失败: " + ex.Message);
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 处理输入框结束编辑事件
        /// </summary>
        /// <param name="value">输入的文本</param>
        void OnEndEdit(string value)
        {
            try
            {
                // 如果失去焦点但没有按回车，则取消
                if (!inputField.isFocused && !Input.GetKey(KeyCode.Return) && !Input.GetKey(KeyCode.KeypadEnter))
                {
                    OnInputFieldCancel?.Invoke(value, inputField.caretPosition);
                    inputField.text = "";
                    currentInputString = "";
                    inputField.DeactivateInputField();
                    activated = false;
                    // 添加一个短暂的延迟再解除输入锁定
                    StartCoroutine(DelayedUnlock());
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] 处理输入框结束编辑失败: " + ex.Message);
                Debug.LogException(ex);
            }
        }

        private IEnumerator DelayedUnlock()
        {
            yield return new WaitForSeconds(0.1f);
            LockInput = false;
        }

        // 用于防止重复激活的标志
        private bool isProcessingEnterKey = false;

        /// <summary>
        /// 更新输入框状态
        /// </summary>
        void InputFieldUpdate()
        {
            try
            {
                // 设置输入锁定状态
                LockInput = activated;

                // 处理ESC键
                if (Input.GetKeyDown(KeyCode.Escape) && activated)
                {
                    OnInputFieldCancel?.Invoke(currentInputString, inputField.caretPosition);
                    currentInputString = "";
                    inputField.text = "";
                    inputField.DeactivateInputField();
                    activated = false;
                    StartCoroutine(DelayedUnlock());
                    return;
                }

                // 如果输入框失去焦点且之前是激活的，则取消输入
                if (!inputField.isFocused && activated && !Input.GetKey(KeyCode.Return) && !Input.GetKey(KeyCode.KeypadEnter))
                {
                    OnInputFieldCancel?.Invoke(currentInputString, inputField.caretPosition);
                    currentInputString = "";
                    inputField.text = "";
                    activated = false;
                    StartCoroutine(DelayedUnlock());
                }

                // 处理回车键按下事件
                bool enterKeyDown = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
                bool enterKeyUp = Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.KeypadEnter);

                // 如果正在处理回车键且按键已释放，重置状态
                if (isProcessingEnterKey && enterKeyUp)
                {
                    isProcessingEnterKey = false;
                    return;
                }

                // 只在按下回车键时处理，并设置处理标志
                if (enterKeyDown && !isProcessingEnterKey)
                {
                    isProcessingEnterKey = true;

                    // 如果输入框已激活
                    if (activated)
                    {
                        // 如果有输入内容，则提交
                        if (!string.IsNullOrWhiteSpace(currentInputString))
                        {
                            OnInputFieldSubmit?.Invoke(currentInputString, inputField.caretPosition);
                            inputField.text = "";
                            currentInputString = "";
                            inputField.DeactivateInputField();
                            activated = false;
                            StartCoroutine(DelayedUnlock());
                        }
                        // 如果没有输入内容，则取消
                        else
                        {
                            OnInputFieldCancel?.Invoke(currentInputString, inputField.caretPosition);
                            currentInputString = "";
                            inputField.text = "";
                            activated = false;
                            inputField.DeactivateInputField();
                            StartCoroutine(DelayedUnlock());
                        }
                    }
                    // 如果输入框未激活且可以激活，则激活
                    else if (CanActivateInputField())
                    {
                        OnInputFieldFocus?.Invoke(currentInputString, inputField.caretPosition);
                        inputField.transform.position = new Vector3(80f, 80f, 0f);
                        inputField.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 30f);
                        inputField.ActivateInputField();
                        inputField.Select();
                        activated = true;
                        LockInput = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] 更新输入框状态失败: " + ex.Message);
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 激活输入框
        /// </summary>
        public void ActivateInputField()
        {
            try
            {
                // 检查是否可以激活输入框
                if (!CanActivateInputField())
                {
                    UnityEngine.Debug.Log("[雨甸中文输入] 当前无法打开聊天框");
                    return;
                }

                // 检查输入框是否存在
                if (inputField == null)
                {
                    UnityEngine.Debug.LogError("[雨甸中文输入] 输入框不存在，无法激活");
                    return;
                }

                Debug.Log("[雨甸中文输入] 激活输入框");

                // 如果输入框已经激活，则不处理
                if (activated)
                {
                    Debug.Log("[雨甸中文输入] 输入框已经激活");
                    return;
                }

                // 确保没有其他激活的输入框
                var allInputFields = FindObjectsOfType<InputField>();
                foreach (var field in allInputFields)
                {
                    if (field != inputField && field.isFocused)
                    {
                        field.DeactivateInputField();
                    }
                }

                // 设置输入框位置和大小
                inputField.transform.position = new Vector3(80f, 80f, 0f);
                inputField.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 30f);

                // 激活输入框
                activated = true;
                inputField.ActivateInputField();

                Debug.Log("[雨甸中文输入] 输入框已激活");
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] 激活输入框失败: " + ex.Message);
                Debug.LogException(ex);
            }
        }
        #endregion
    }

    /// <summary>
    /// 输入框文本事件委托，用于处理输入框事件
    /// </summary>
    /// <param name="text">输入的文本</param>
    /// <param name="caretPos">光标位置</param>
    public delegate void InputFieldTextEvent(string text, int caretPos);

    /// <summary>
    /// GHUD部件基类，所有HUD部件的基类
    /// </summary>
    public abstract class GHUDPart
    {
        /// <summary>
        /// 所属的GHUD实例
        /// </summary>
        protected GHUD hud;

        /// <summary>
        /// 是否标记为删除
        /// </summary>
        public bool slatedForDeletion;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="hud">所属的GHUD实例</param>
        protected GHUDPart(GHUD hud)
        {
            this.hud = hud;
        }

        /// <summary>
        /// 更新方法，每帧调用
        /// </summary>
        public abstract void Update();

        /// <summary>
        /// 绘制方法，每帧调用
        /// </summary>
        /// <param name="timeStacker">时间插值器，用于平滑动画</param>
        public abstract void Draw(float timeStacker);

        /// <summary>
        /// 清理精灵资源
        /// </summary>
        public virtual void ClearSprites() { }
    }

    /// <summary>
    /// GHUD静态类，包含常用的颜色定义
    /// </summary>
    public static class GHUDStatic
    {
        // 常用颜色定义
        public static readonly Color GHUDwhite = new Color(0.9f, 0.9f, 0.9f);
        public static readonly Color GHUDgrey = new Color(0.2f, 0.2f, 0.2f);
        public static readonly Color GHUDgreen = new Color(0.2f, 0.9f, 0.2f);
        public static readonly Color GHUDyellow = new Color(0.9f, 0.9f, 0.2f);
        public static readonly Color GHUDred = new Color(0.9f, 0.2f, 0.2f);
    }
}
