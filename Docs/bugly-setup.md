# Bugly 崩溃上报接入指南

## 状态

- ✅ 代码已就绪：`CrashReporter.cs` + `Bootstrapper.cs`
- ✅ App ID 已配置：`6617eaa29d`
- ❌ 缺少 Bugly SDK 二进制文件（需手动导入）

## 接入步骤

### 1. 下载 Bugly Unity SDK

访问：https://bugly.qq.com/docs/user-guide/unity-guide/

点击下载 `BuglyPlugin.unitypackage`

### 2. 导入 Unity 项目

在 Unity Editor 中：
`Assets → Import Package → Custom Package` → 选择下载的 `.unitypackage`

确保导入的文件包含：
- `Plugins/Android/*.aar` 或 `*.jar`
- `Plugins/iOS/*.a`
- `BuglyAgent.cs`

### 3. 验证

运行游戏后可在 Console 看到：
```
[CrashReporter] Bugly initialized successfully
[Bootstrapper] Services initialized
```

### 4. 崩溃测试（可选）

在任意代码中加入以下测试，确认 Bugly 能收到上报：
```csharp
// 触发一个空引用异常，Bugly 应该能在 Dashboard 捕获到
throw new System.NullReferenceException("Test crash from KingdomWar");
```

### 5. 查看崩溃

登录 https://bugly.qq.com/ → 产品列表 → 选择 KingdomWar → 崩溃分析

## 架构说明

```
CrashReporter.Initialize()  (Bootstrapper, 游戏启动时)
  │
  ├── Bugly SDK 存在? → BuglyAgent.InitWithAppId("6617eaa29d")
  │                        └── 所有异常自动上报到 Bugly 后台
  │
  └── Bugly SDK 不存在? → 回退到 Debug.LogError（调试模式）
```