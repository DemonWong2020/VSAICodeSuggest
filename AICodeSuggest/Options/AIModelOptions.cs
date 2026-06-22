using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.VisualStudio.Shell;

namespace AICodeSuggest.Options
{
    [ComVisible(true)]
    public class AIModelOptions : UIElementDialogPage
    {
        private AIModelOptionsControl _child;

        [Category("Provider")]
        [DisplayName("Provider 类型")]
        [Description("选择 AI 服务提供商的类型")]
        public string ProviderType { get; set; } = "OpenAI兼容";

        [Category("Provider")]
        [DisplayName("API 地址")]
        [Description("AI 服务的 API 端点地址")]
        public string ApiEndpoint { get; set; } = "https://api.openai.com/v1";

        [Category("Provider")]
        [DisplayName("API Key")]
        [Description("API 认证密钥")]
        [PasswordPropertyText(true)]
        public string ApiKey { get; set; } = string.Empty;

        [Category("模型参数")]
        [DisplayName("模型名称")]
        [Description("使用的模型名称，如 gpt-4o, gpt-3.5-turbo, codellama 等")]
        public string ModelName { get; set; } = "gpt-4o";

        [Category("模型参数")]
        [DisplayName("Temperature")]
        [Description("生成温度，控制随机性。0=确定性输出，2.0=最大随机性")]
        [DefaultValue(0.2)]
        public double Temperature { get; set; } = 0.2;

        [Category("模型参数")]
        [DisplayName("Top P")]
        [Description("核采样参数，通常与 Temperature 配合使用")]
        [DefaultValue(0.95)]
        public double TopP { get; set; } = 0.95;

        protected override UIElement Child
        {
            get
            {
                try
                {
                    if (_child == null)
                    {
                        _child = new AIModelOptionsControl();
                        _child.DataContext = this;
                    }
                    return _child;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AICodeSuggest] AIModelOptions.Child error: {ex}");
                    return new System.Windows.Controls.TextBlock
                    {
                        Text = $"无法加载设置页面: {ex.Message}",
                        Margin = new Thickness(12)
                    };
                }
            }
        }

        public override void LoadSettingsFromStorage()
        {
            try
            {
                base.LoadSettingsFromStorage();
            }
            catch
            {
                // 设置存储损坏（通常来自旧版本的废弃数据），强制回退到默认值
                ResetToDefaults();
                try
                {
                    // 立即保存默认值，覆盖注册表中的损坏数据
                    base.SaveSettingsToStorage();
                }
                catch { }
            }
        }

        private void ResetToDefaults()
        {
            ProviderType = "OpenAI兼容";
            ApiEndpoint = "https://api.openai.com/v1";
            ApiKey = string.Empty;
            ModelName = "gpt-4o";
            Temperature = 0.2;
            TopP = 0.95;
        }

        public override void SaveSettingsToStorage()
        {
            if (Temperature < 0) Temperature = 0;
            if (Temperature > 2.0) Temperature = 2.0;
            if (TopP < 0) TopP = 0;
            if (TopP > 1.0) TopP = 1.0;
            if (string.IsNullOrWhiteSpace(ApiEndpoint))
                ApiEndpoint = "https://api.openai.com/v1";

            base.SaveSettingsToStorage();
        }
    }
}
