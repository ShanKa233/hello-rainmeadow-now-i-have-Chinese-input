using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GhostPlayer.GHud
{
    /// <summary>
    /// GInputBox类：输入框的视觉实现
    /// 负责显示用户正在输入的文本和光标
    /// 继承自GHUDPart基类
    /// </summary>
    internal class GInputBox : GHUDPart
    {
        /// <summary>
        /// 输入框的大小
        /// </summary>
        public readonly Vector2 size;
        
        /// <summary>
        /// 光标闪烁计数器的最大值
        /// </summary>
        public readonly int cursorBlinkCounter = 20;

        /// <summary>
        /// 是否在下一次更新时更新光标位置
        /// </summary>
        bool updateCursorNextUpdate;

        /// <summary>
        /// 当前显示状态（0-1）
        /// </summary>
        float show;
        
        /// <summary>
        /// 目标显示状态
        /// </summary>
        float setShow;
        
        /// <summary>
        /// 上一帧的显示状态
        /// </summary>
        float lastShow;

        /// <summary>
        /// 目标光标索引位置
        /// </summary>
        float setCursorIndex;
        
        /// <summary>
        /// 平滑插值后的光标索引位置
        /// </summary>
        float smoothCursorIndex;
        
        /// <summary>
        /// 上一帧的光标索引位置
        /// </summary>
        float lastCusorIndex;

        /// <summary>
        /// 光标是否显示（闪烁状态）
        /// </summary>
        bool cursorBlink;
        
        /// <summary>
        /// 闪烁计数器
        /// </summary>
        int blinkCounter;

        /// <summary>
        /// 文本标签
        /// </summary>
        FLabel label;
        
        /// <summary>
        /// 用于计算光标位置的辅助标签
        /// </summary>
        FLabel cursorCaculateLabel;
        
        /// <summary>
        /// 背景精灵
        /// </summary>
        FSprite background;
        
        /// <summary>
        /// 光标精灵
        /// </summary>
        FSprite cursor;

        /// <summary>
        /// 上一次输入的文本
        /// </summary>
        string lastInputText;
        
        /// <summary>
        /// 当前文本内容
        /// </summary>
        string Text
        {
            get => label.text;
            set => label.text = value;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="hud">所属的GHUD实例</param>
        public GInputBox(GHUD hud) : base(hud)
        {
            // 计算输入框大小
            size = new Vector2(Custom.rainWorld.options.ScreenSize.x - 80f - 80f, 30f);

            // 订阅输入框事件
            hud.OnInputFieldFocus += Hud_OnInputFieldFocus;
            hud.OnInputFieldChanged += Hud_OnInputFieldChanged;

            hud.OnInputFieldSubmit += Hud_OnInputFieldClose;
            hud.OnInputFieldCancel += Hud_OnInputFieldClose;

            // 创建背景精灵
            background = new FSprite("pixel", true)
            {
                anchorX = 0,
                anchorY = 1,

                scaleY = size.y,

                color = GHUDStatic.GHUDgrey,
                isVisible = true,
                alpha = 0f,
            };

            // 创建文本标签
            label = new FLabel(Custom.GetFont(), "")
            {
                anchorX = 0,
                anchorY = 1,

                scale = 1.3f,
                color = GHUDStatic.GHUDwhite,
                isVisible = true,
                alpha = 0f,
            };
            
            // 创建用于计算光标位置的辅助标签
            cursorCaculateLabel = new FLabel(Custom.GetFont(), "")
            {
                anchorX = 0,
                anchorY = 1,

                scale = 1.3f,
                color = GHUDStatic.GHUDwhite,
                isVisible = true,
                alpha = 0f,
            };

            // 创建光标精灵
            cursor = new FSprite("pixel", true)
            {
                anchorX = 0f,
                anchorY = 0.5f,

                scaleY = size.y * 1.1f,
                scaleX = 2f,

                color = GHUDStatic.GHUDwhite,
                isVisible = true,
                alpha = 0f,
            };

            // 添加所有元素到容器
            hud.container.AddChild(background);
            hud.container.AddChild(label);
            hud.container.AddChild(cursorCaculateLabel);
            hud.container.AddChild(cursor);
        }

        /// <summary>
        /// 更新方法，每帧调用
        /// </summary>
        public override void Update()
        {
            // 平滑显示状态变化
            lastShow = show;
            show = Mathf.Lerp(lastShow, setShow, 0.15f);

            // 更新光标位置计算用的文本
            string newText = Text.Substring(0, Mathf.Min(Text.Length, hud.inputField.caretPosition));
            if (newText.Length != cursorCaculateLabel.text.Length)
                UpdateCursor();
            cursorCaculateLabel.text = newText;

            // 处理光标闪烁
            if (blinkCounter < cursorBlinkCounter)
                blinkCounter++;
            else
            {
                blinkCounter = 0;
                cursorBlink = !cursorBlink;
            }

            // 平滑光标位置变化
            setCursorIndex = cursorCaculateLabel.textRect.width * 1.3f;
            lastCusorIndex = smoothCursorIndex;
            smoothCursorIndex = Mathf.Lerp(lastCusorIndex, setCursorIndex, 0.40f);
        }

        /// <summary>
        /// 绘制方法，每帧调用
        /// </summary>
        /// <param name="timeStacker">时间插值器，用于平滑动画</param>
        public override void Draw(float timeStacker)
        {
            // 计算插值后的显示状态和光标位置
            float s_show = Mathf.Lerp(lastShow, show, timeStacker);
            float s_cursor = Mathf.Lerp(lastCusorIndex, smoothCursorIndex, timeStacker);
            
            // 设置背景大小和透明度
            background.scaleX = size.x * s_show;
            background.alpha = s_show * 0.7f;
            background.SetPosition(new Vector2(80f, 105f));

            // 设置文本标签透明度和位置
            label.alpha = s_show;
            label.SetPosition(new Vector2(85f, 100f));

            // 设置光标位置和透明度
            cursor.SetPosition(new Vector2(85f + s_cursor, 105f - size.y / 2f));
            cursor.alpha = s_show * (cursorBlink ? 1f : 0f);

            // 处理输入框关闭时的文本淡出效果
            if (label.alpha > 0.01f && setShow == 0f)
            {
                label.text = lastInputText.Substring(0, Mathf.FloorToInt(s_show * lastInputText.Length));
            }
        }

        /// <summary>
        /// 处理输入框获得焦点事件
        /// </summary>
        /// <param name="value">当前文本</param>
        /// <param name="caretPos">光标位置</param>
        private void Hud_OnInputFieldFocus(string value, int caretPos)
        {
            // 显示输入框
            setShow = 1f;
        }

        /// <summary>
        /// 处理输入框文本变化事件
        /// </summary>
        /// <param name="value">新的文本</param>
        /// <param name="caretPos">光标位置</param>
        private void Hud_OnInputFieldChanged(string value, int caretPos)
        {
            // 更新文本
            Text = value;
            // 更新光标位置
            UpdateCursor();
        }

        /// <summary>
        /// 处理输入框关闭事件（提交或取消）
        /// </summary>
        /// <param name="value">最终文本</param>
        /// <param name="caretPos">光标位置</param>
        private void Hud_OnInputFieldClose(string value, int caretPos)
        {
            // 保存最后的输入文本
            lastInputText = value;
            // 隐藏输入框
            setShow = 0f;
            // show = 0f;
        }

        /// <summary>
        /// 更新光标位置和状态
        /// </summary>
        private void UpdateCursor()
        {
            // 标记需要更新光标
            updateCursorNextUpdate = true;
            // 重置光标闪烁状态
            cursorBlink = true;
            blinkCounter = 0;
        }
    }
}
