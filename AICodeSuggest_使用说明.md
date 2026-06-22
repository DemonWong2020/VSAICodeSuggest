# AICodeSuggest 使用说明

> Visual Studio 2022 扩展，提供 AI 驱动的行内代码补全建议，类似 GitHub Copilot。支持 OpenAI 兼容 API 和 Ollama 本地模型。

---

## 1. 系统要求

| 项目 | 要求 |
|------|------|
| Visual Studio | 2022（17.0 ~ 18.0），Community / Professional / Enterprise |
| 操作系统 | Windows x64 |
| .NET 运行时 | .NET Framework 4.8（VS 2022 自带） |
| AI 后端（选一） | 有网络访问的 OpenAI 兼容 API，或本地 Ollama |

---

## 2. 安装

### 方式一：双击安装

1. 关闭所有 Visual Studio 窗口
2. 双击 `AICodeSuggest.vsix`
3. 在 VSIX Installer 中选择你的 VS 2022 版本，点击"安装"
4. 等待"安装完成"提示后，启动 Visual Studio

### 方式二：命令行安装

```powershell
VSIXInstaller.exe /quiet /a AICodeSuggest.vsix
```

---

## 3. 配置

安装后，在 Visual Studio 中打开 **工具 → 选项**，搜索 "AI Code Suggest"，会看到两个设置页。

### 3.1 通用设置

| 设置项 | 默认值 | 范围 | 说明 |
|--------|--------|------|------|
| **启用代码建议** | 开启 | - | 总开关，关闭后不再触发任何建议 |
| **触发延迟** | 300 ms | 100-1000 | 停止输入后等待多久再请求建议 |
| **光标前上下文行数** | 50 | 5-200 | 收集光标前方多少行代码作为上下文 |
| **光标后上下文行数** | 10 | 0-50 | 收集光标后方多少行代码作为上下文 |
| **启用智能上下文分析** | 开启 | - | 自动提取 using/import、类/方法签名等结构化信息 |
| **最大上下文令牌数** | 2000 | 0-8192 | 发送给 AI 的上下文 token 上限，0 表示不限制 |
| **建议最大长度** | 500 | 50-2000 | AI 返回建议的字符数上限 |
| **最大显示行数** | 5 | 1-20 | Ghost Text 可见行数上限 |

### 3.2 AI 模型设置

| 设置项 | 默认值 | 说明 |
|--------|--------|------|
| **提供商类型** | OpenAI兼容 | 可选：`OpenAI兼容`、`Ollama 本地`、`自定义` |
| **API 端点** | `https://api.openai.com/v1` | OpenAI 兼容 API 地址 |
| **API Key** | （空） | 加密存储于 Windows DPAPI，仅当前用户可解密 |
| **模型名称** | `gpt-4o` | 模型 ID（如 `gpt-4o`, `deepseek-coder`, `codellama` 等） |
| **Temperature** | 0.2 | 0-2.0，越低越确定，越高越随机 |
| **Top P** | 0.95 | 0-1.0 核采样参数 |

#### 关于"自定义"类型

选"自定义"时，仍然使用 OpenAI 兼容协议。这意味着你可以连接任何 OpenAI 兼容的服务（DeepSeek、vLLM、LM Studio 等），只需修改 API 端点和模型名称即可。

#### "测试连接"按钮

在 AI 模型设置页点击 **测试连接**：
- OpenAI 兼容模式 → 请求 `/models` 端点验证密钥和连通性
- Ollama 模式 → 请求 `/api/tags` 验证本地服务是否运行
- 成功显示绿色提示，失败显示红色错误信息

---

## 4. 使用方式

### 4.1 工作流程

```
开始输入代码
     ↓
停止输入 (超过触发延迟)
     ↓
Ghost Text 灰色斜体建议出现
     ↓
操作建议
```

### 4.2 键盘操作

| 按键 | 行为 |
|------|------|
| **Tab** | 按词接受：逐个单词接受建议（含前后空白和缩进） |
| **Shift + Tab** 或 **Ctrl + Enter** | 全量接受：一次性接受全部剩余建议 |
| **Esc** | 关闭当前建议 |
| 方向键 / Home / End / PgUp / PgDn | 保留建议（不关闭） |
| Ctrl / Alt / Shift 组合键 | 保留建议（不关闭） |
| **其他任意按键** | 关闭建议并正常输入 |

### 4.3 操作提示

Ghost Text 下方会显示一行低透明度提示：
> Tab / Shift+Tab - 逐词/全部接受 | Esc 关闭

---

## 5. 支持的编程语言

智能上下文分析（自动提取代码结构）支持以下语言：

| 语言 | 可提取的结构信息 |
|------|-----------------|
| C# | using, namespace, class, interface, method, property, field |
| JavaScript / TypeScript | import, class, function, const/let/var, export |
| Python | import, from import, class, def, assignment |
| C / C++ | #include, namespace, class, function, variable |
| 其他语言 | 通用行级上下文（前后 N 行） |

---

## 6. 日志调试

如遇问题，可在 Visual Studio 中查看日志：

1. 菜单 **视图 → 输出**
2. 在输出窗口顶部下拉菜单中选择 **"AI Code Suggest"**
3. 日志包含时间戳、请求信息、API 响应状态和错误详情

---

## 7. 安全说明

- **API Key 加密存储**：使用 Windows DPAPI (`DataProtectionScope.CurrentUser`)，密钥仅当前 Windows 用户可解密
- **API Key 在内存中**：传输时通过 HTTPS，UI 上显示为密码掩码
- **代码上下文**：仅采集编辑器文本和项目/方案名称，不会采集文件系统路径或敏感个人信息

---

## 8. 常见模型配置示例

### OpenAI 官方

| 设置 | 值 |
|------|---|
| 提供商类型 | OpenAI兼容 |
| API 端点 | `https://api.openai.com/v1` |
| 模型名称 | `gpt-4o` 或 `gpt-4o-mini` |

### DeepSeek

| 设置 | 值 |
|------|---|
| 提供商类型 | OpenAI兼容（或自定义） |
| API 端点 | `https://api.deepseek.com/v1` |
| 模型名称 | `deepseek-chat` |

### Ollama 本地

| 设置 | 值 |
|------|---|
| 提供商类型 | Ollama 本地 |
| API 端点 | `http://localhost:11434` |
| 模型名称 | `codellama` 或 `deepseek-coder` |

### LM Studio 本地

| 设置 | 值 |
|------|---|
| 提供商类型 | 自定义 |
| API 端点 | `http://localhost:1234/v1` |
| 模型名称 | 按 LM Studio 加载的模型 |

---

## 9. 从源码构建

```powershell
# 要求：Visual Studio 2022 + .NET SDK

# 方式一：MSBuild（生成完整 VSIX）
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe" `
  AICodeSuggest\AICodeSuggest.csproj -t:Build -p:Configuration=Release

# 方式二：dotnet build（仅编译 DLL，不生成 VSIX）
dotnet build AICodeSuggest\AICodeSuggest.csproj -c Release

# VSIX 产物位于：AICodeSuggest\.vsix 或项目根目录 AICodeSuggest.vsix
```

---

## 10. 卸载

Visual Studio → 扩展 → 管理扩展 → 已安装 → 搜索 "AICodeSuggest" → 卸载 → 重启 VS。

---

## 11. 许可证

MIT License，详见 `LICENSE.txt`。
