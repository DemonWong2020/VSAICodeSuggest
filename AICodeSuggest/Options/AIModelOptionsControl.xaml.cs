using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AICodeSuggest.Options
{
    public partial class AIModelOptionsControl : UserControl
    {
        private bool _initialized;

        public AIModelOptionsControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_initialized || !(DataContext is AIModelOptions options))
                return;

            _initialized = true;

            try
            {
                var key = options.ApiKey ?? string.Empty;
                // 防御：限制 API Key 长度，防止损坏数据导致 PasswordBox 内存溢出
                if (key.Length > 1000)
                    key = string.Empty;

                ApiKeyPasswordBox.Password = key;
            }
            catch
            {
                ApiKeyPasswordBox.Password = string.Empty;
            }

            foreach (ComboBoxItem item in ProviderTypeCombo.Items)
            {
                if (item.Content?.ToString() == options.ProviderType)
                {
                    ProviderTypeCombo.SelectedItem = item;
                    break;
                }
            }
        }

        private void OnProviderTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_initialized) return;

            if (DataContext is AIModelOptions options && ProviderTypeCombo.SelectedItem is ComboBoxItem item)
            {
                options.ProviderType = item.Content?.ToString() ?? "OpenAI兼容";
            }
        }

        private void OnApiKeyPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is AIModelOptions options)
            {
                options.ApiKey = ApiKeyPasswordBox.Password;
            }
        }

        // WPF 事件处理器需要 async void，异常已在内部捕获处理
#pragma warning disable VSTHRD100
        private async void OnTestConnectionClick(object sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100
        {
            var button = sender as Button;
            button.IsEnabled = false;
            ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Gray;
            ConnectionStatusText.Text = "正在测试连接...";

            try
            {
                var options = DataContext as AIModelOptions;
                if (options == null)
                {
                    ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Red;
                    ConnectionStatusText.Text = "错误：无法获取配置";
                    return;
                }

                var result = await TestConnectionAsync(options);
                ConnectionStatusText.Foreground = result.Success
                    ? System.Windows.Media.Brushes.Green
                    : System.Windows.Media.Brushes.Red;
                ConnectionStatusText.Text = result.Message;
            }
            catch (Exception ex)
            {
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Red;
                ConnectionStatusText.Text = $"连接失败：{ex.Message}";
            }
            finally
            {
                button.IsEnabled = true;
            }
        }

        private async Task<(bool Success, string Message)> TestConnectionAsync(AIModelOptions options)
        {
            var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            try
            {
                var endpoint = options.ApiEndpoint?.TrimEnd('/');
                var apiKey = options.ApiKey;

                // 对于 Ollama，使用 /api/tags 端点检测
                if (options.ProviderType == "Ollama 本地")
                {
                    var response = await httpClient.GetAsync($"{endpoint}/api/tags");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        return (true, $"Ollama 连接成功！\n模型列表：{TruncateForDisplay(content, 100)}");
                    }
                    return (false, $"Ollama 返回错误：{response.StatusCode}");
                }

                // OpenAI 兼容和自定义：使用 /models 端点
                var request = new HttpRequestMessage(HttpMethod.Get, $"{endpoint}/models");

                if (!string.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                }

                var response2 = await httpClient.SendAsync(request);
                if (response2.IsSuccessStatusCode)
                {
                    return (true, "连接成功！API 响应正常。");
                }

                var errorBody = await response2.Content.ReadAsStringAsync();
                return (false, $"API 返回错误 {response2.StatusCode}：{TruncateForDisplay(errorBody, 80)}");
            }
            catch (TaskCanceledException)
            {
                return (false, "连接超时（10秒），请检查 API 地址是否正确。");
            }
            catch (Exception ex)
            {
                return (false, $"连接失败：{ex.Message}");
            }
            finally
            {
                httpClient.Dispose();
            }
        }

        private static string TruncateForDisplay(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }
    }
}
