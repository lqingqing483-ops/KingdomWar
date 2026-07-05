# CI/CD 使用说明

---

## 使用方法

项目根目录下运行：
```
powershell scripts\ci.ps1
```

CI 会自动启动 Unity 命令行模式，跑全部 EditMode 测试，输出结果。

如果测试全部通过，退出码为 0。如果有失败，会列出失败项并退出码非 0。

---

## 注意事项

1. **Unity Editor 不能同时打开**。命令行模式和编辑器模式不能共用同一个项目，跑 CI 前必须关掉 Unity Editor。

2. **Unity 进程可能没完全退出**。关掉编辑器窗口后，后台可能还有 Unity 进程残留，确认杀干净再跑。用任务管理器检查 Unity.exe。

3. **首次运行较慢**。Unity 第一次启动要导入资源、编译脚本，耗时 1-3 分钟都是正常的。第二次开始会快很多。

4. **测试结果文件**。跑完后会生成两个文件：
   - `test-results.xml` — 测试结果详情（XML 格式）
   - `unity-ci-log.txt` — Unity 运行日志（调试用）

---

## 要修复的问题

### 测试分类错误（21 个失败测试）

部分测试依赖运行时功能，但被放在了 EditMode 目录下。需要把以下测试从 `Assets/Tests/EditMode/` 移到 `Assets/Tests/PlayMode/`：

| 测试文件 | 失败原因 | 处理方式 |
|---------|---------|---------|
| AudioManagerTests | 用到了 DontDestroyOnLoad，只能在 PlayMode 运行 | 移到 PlayMode |
| UnitHealthBarTests | 创建了 GameObject，需要场景环境 | 移到 PlayMode |
| UIManagerTests | 依赖 UIManager.Instance 运行时初始化 | 移到 PlayMode |
| LotterySystemTests | 依赖 LotterySystem 运行时状态 | 移到 PlayMode |
| ShopManagerTests | 依赖运行时数据 | 移到 PlayMode |

### CardDatabaseTests 部分失败

`CardDatabase_LoadsAllCardsFromResources`、`ElixirCollector_IsLoaded_AsBuilding_6Elixir`、`PEKKA_IsLoaded_AsUnit_7Elixir` 这三个测试失败，可能是 Resources 路径问题或卡片数据本身变了，需要确认数据是否存在。

### 修复后要做的事

修完后重新跑一遍 CI：
```
powershell scripts\ci.ps1
```

目标是全部 45 个测试通过。后续 AI 每次加新功能，都需要保证 CI 全绿才能合并。
