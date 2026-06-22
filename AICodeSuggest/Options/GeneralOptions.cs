using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.VisualStudio.Shell;

namespace AICodeSuggest.Options
{
    [ComVisible(true)]
    public class GeneralOptions : UIElementDialogPage
    {
        private GeneralOptionsControl _child;

        [Category("触发设置")]
        [DisplayName("启用内联建议")]
        [Description("开启或关闭 AI 代码内联建议功能")]
        public bool EnableSuggestions { get; set; } = true;

        [Category("触发设置")]
        [DisplayName("触发延迟(毫秒)")]
        [Description("用户停止输入后等待多长时间触发建议，范围 100-1000ms")]
        [DefaultValue(300)]
        public int TriggerDelayMs { get; set; } = 300;

        [Category("上下文")]
        [DisplayName("光标前上下文行数")]
        [Description("光标位置之前收集的代码行数")]
        [DefaultValue(50)]
        public int ContextLinesBefore { get; set; } = 50;

        [Category("上下文")]
        [DisplayName("光标后上下文行数")]
        [Description("光标位置之后收集的代码行数")]
        [DefaultValue(10)]
        public int MaxLinesAfter { get; set; } = 10;

        [Category("上下文")]
        [DisplayName("智能结构检测")]
        [Description("自动检测代码结构（命名空间、类、方法、using 语句等），为 AI 提供更丰富的上下文")]
        [DefaultValue(true)]
        public bool EnableSmartContext { get; set; } = true;

        [Category("上下文")]
        [DisplayName("最大上下文 Token 数")]
        [Description("控制发送给 AI 的上下文大小上限（估算值），0 表示不限制。减小可降低 API 费用")]
        [DefaultValue(2000)]
        public int MaxContextTokens { get; set; } = 2000;

        [Category("建议")]
        [DisplayName("建议最大长度(字符数)")]
        [Description("单次内联建议的最大字符数")]
        [DefaultValue(500)]
        public int SuggestionMaxLength { get; set; } = 500;

        [Category("建议")]
        [DisplayName("最大显示行数")]
        [Description("多行建议在编辑器中最多显示的行数，超出部分将被截断")]
        [DefaultValue(5)]
        public int MaxSuggestionDisplayLines { get; set; } = 5;

        protected override UIElement Child
        {
            get
            {
                if (_child == null)
                {
                    _child = new GeneralOptionsControl();
                    _child.DataContext = this;
                }
                return _child;
            }
        }

        public override void SaveSettingsToStorage()
        {
            if (TriggerDelayMs < 100) TriggerDelayMs = 100;
            if (TriggerDelayMs > 1000) TriggerDelayMs = 1000;
            if (ContextLinesBefore < 5) ContextLinesBefore = 5;
            if (ContextLinesBefore > 200) ContextLinesBefore = 200;
            if (MaxLinesAfter < 0) MaxLinesAfter = 0;
            if (MaxLinesAfter > 50) MaxLinesAfter = 50;
            if (MaxContextTokens < 0) MaxContextTokens = 0;
            if (MaxContextTokens > 8192) MaxContextTokens = 8192;
            if (SuggestionMaxLength < 50) SuggestionMaxLength = 50;
            if (SuggestionMaxLength > 2000) SuggestionMaxLength = 2000;
            if (MaxSuggestionDisplayLines < 1) MaxSuggestionDisplayLines = 1;
            if (MaxSuggestionDisplayLines > 20) MaxSuggestionDisplayLines = 20;

            base.SaveSettingsToStorage();
        }
    }
}
