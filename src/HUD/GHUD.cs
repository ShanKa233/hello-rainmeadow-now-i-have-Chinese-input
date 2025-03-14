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
                //if (_lockInput)
                //{
                //    GAnnouncementHud.NewAnnouncement("LockInput", 40, GAnnouncementHud.AnnouncementType.Default);
                //}
                //else
                //    GAnnouncementHud.NewAnnouncement("ReleaseInput", 40, GAnnouncementHud.AnnouncementType.Default);
            }
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
        /// Unity启动函数，初始化GHUD系统
        /// </summary>
        void Start()
        {
            try
            {
                Debug.Log("[雨甸中文输入] GHUD开始初始化");
                
                // 设置单例实例
                Instance = this;
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
                    Debug.Log("[雨甸中文输入] 创建了新的EventSystem");
                }
                else
                {
                    Debug.Log("[雨甸中文输入] 使用现有的EventSystem");
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
                // 固定更新逻辑
                timeStacker += framePerSec * Time.deltaTime;
                if (timeStacker >= 1)
                {
                    timeStacker--;
                    FixUpdate();
                }

                // 绘制逻辑
                Draw();

                // 测试功能和输入框更新
                TestFunc();
                InputFieldUpdate();
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] GHUD更新失败: " + ex.Message);
                Debug.LogException(ex);
            }
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
                // 如果按下了回车键，则提交
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        OnInputFieldSubmit?.Invoke(value, inputField.caretPosition);
                        inputField.text = "";
                        currentInputString = "";
                    }
                    inputField.DeactivateInputField();
                    activated = false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] 处理输入框结束编辑失败: " + ex.Message);
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 更新输入框状态
        /// </summary>
        void InputFieldUpdate()
        {
            try
            {
                bool origInputLock = LockInput;
                LockInput = false;
                
                // 如果输入框失去焦点且之前是激活的，则取消输入
                if (!inputField.isFocused && activated && !Input.GetKey(KeyCode.Return) && !Input.GetKey(KeyCode.KeypadEnter))
                {
                    OnInputFieldCancel?.Invoke(currentInputString, inputField.caretPosition);
                    currentInputString = "";
                    inputField.text = "";
                    activated = false;
                }

                // 处理回车键按下事件
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
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
                            origInputLock = false;
                        }
                        // 如果没有输入内容，则取消
                        else
                        {
                            currentInputString = "";
                            inputField.text = "";
                            inputField.DeactivateInputField();
                            OnInputFieldCancel?.Invoke(currentInputString, inputField.caretPosition);
                            activated = false;
                            origInputLock = false;
                        }
                    }
                    // 如果输入框未激活，则激活
                    else
                    {
                        OnInputFieldFocus?.Invoke(currentInputString, inputField.caretPosition);
                        inputField.ActivateInputField();
                        inputField.Select();
                        activated = true;
                        LockInput = true;
                    }
                }

                LockInput = origInputLock;
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] 更新输入框状态失败: " + ex.Message);
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
