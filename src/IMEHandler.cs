using System;
using UnityEngine;
using UnityEngine.UI;
using RainMeadow;

namespace 雨甸中文输入.src
{
    public class IMEHandler : MonoBehaviour
    {
        // 单例实例
        public static IMEHandler Instance { get; private set; }
        
        // 输入框组件
        public InputField inputField;
        
        // 当前输入的文本内容
        public string currentInputString;
        
        // 输入框是否激活
        private bool activated;

        void Awake()
        {
            // 设置单例实例
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // 创建最简单的InputField
            CreateSimpleInputField();
        }
        
        void Update()
        {
            // 检查游戏状态
            if (OnlineManager.lobby == null && activated)
            {
                DeactivateInput();
            }
            
            // 处理输入激活/取消
            HandleInputActivation();
        }
        
        // 创建最简单的InputField
        private void CreateSimpleInputField()
        {
            try
            {
                // 创建GameObject
                var obj = new GameObject("SimpleInputField");
                obj.transform.SetParent(transform);
                
                // 添加InputField组件
                inputField = obj.AddComponent<InputField>();
                
                // 创建最简单的文本组件
                var textObj = new GameObject("Text");
                textObj.transform.SetParent(obj.transform);
                var text = textObj.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.color = Color.white;
                
                // 设置InputField
                inputField.textComponent = text;
                
                // 隐藏输入框
                obj.SetActive(false);
                
                // 添加监听器
                inputField.onValueChanged.AddListener(OnInputChanged);
                inputField.onEndEdit.AddListener(OnInputEnd);
            }
            catch (Exception ex)
            {
                Debug.LogError("[雨甸中文输入] 创建InputField出错: " + ex.Message);
            }
        }
        
        // 处理输入激活/取消
        private void HandleInputActivation()
        {
            // 按ESC取消输入
            if (Input.GetKeyDown(KeyCode.Escape) && activated)
            {
                DeactivateInput();
                return;
            }
            
            // 按F2激活输入
            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (activated)
                {
                    // 如果已激活，提交输入
                    if (!string.IsNullOrEmpty(currentInputString))
                    {
                        ProcessInput(currentInputString);
                    }
                    DeactivateInput();
                }
                else
                {
                    // 激活输入
                    ActivateInput();
                }
            }
            
            // 按回车提交输入
            if (Input.GetKeyDown(KeyCode.Return) && activated)
            {
                if (!string.IsNullOrEmpty(currentInputString))
                {
                    ProcessInput(currentInputString);
                }
                DeactivateInput();
            }
        }
        
        // 输入变化回调
        private void OnInputChanged(string value)
        {
            currentInputString = value;
        }
        
        // 输入结束回调
        private void OnInputEnd(string value)
        {
            // 如果不是因为按下回车键结束输入，则取消输入
            if (!Input.GetKey(KeyCode.Return))
            {
                DeactivateInput();
            }
        }
        
        // 处理输入内容
        private void ProcessInput(string input)
        {
            Debug.Log("[雨甸中文输入] 输入内容: " + input);
            // 这里添加处理输入的逻辑
        }
        
        // 激活输入
        public void ActivateInput()
        {
            if (inputField == null) return;
            
            // 显示并激活输入框
            inputField.gameObject.SetActive(true);
            inputField.text = "";
            currentInputString = "";
            inputField.ActivateInputField();
            activated = true;
        }
        
        // 取消输入
        public void DeactivateInput()
        {
            if (inputField == null) return;
            
            // 清空并隐藏输入框
            inputField.text = "";
            currentInputString = "";
            inputField.DeactivateInputField();
            inputField.gameObject.SetActive(false);
            activated = false;
        }
    }
}
