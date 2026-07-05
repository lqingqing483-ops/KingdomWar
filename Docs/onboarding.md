# 新人上手指南 — AI 工作流

> 预计阅读时间：15 分钟  
> 面向对象：新加入项目的开发者

---

## 一、这个工作流是什么

这是一个 **AI 辅助开发的工作流系统**。核心理念：

```
你提需求 → AI 拆任务 → AI 实现 → AI 自测 → 人工/自动审查 → 合并
                         ↕
              每一步都有门禁卡住质量问题
```

**不是什么**：不是 AI 全自动写代码。每一步都有人工参与的质量门禁。

---

## 二、你需要安装什么

### 必需

| 工具 | 用途 | 验证安装 |
|------|------|---------|
| [OpenCode](https://opencode.ai) | AI 编码助手 | `opencode --version` |
| Git | 版本控制 | `git --version` |
| Unity | 项目引擎 | （已有） |

### 推荐

| 工具 | 用途 | 验证安装 |
|------|------|---------|
| `luacheck` | Lua 语法检查 | `luacheck --version` |
| `busted` | Lua 测试框架 | `busted --version` |
| Rust / cargo | Rust 编译 | `cargo --version` |
| GitHub CLI | PR 操作 | `gh --version` |

> **如果某个工具没装，不影响核心流程**。对应的检查脚本会自动跳过并提示。

---

## 三、项目结构速览

```
项目根目录/
├── opencode.json          ← OpenCode 配置（入口）
├── AGENTS.md              ← AI 工作流规则（核心）
│
├── .opencode/
│   ├── agents/            ← AI 代理定义
│   │   ├── orchestrator.md  主编排器（拆任务、派发、验收）
│   │   ├── build.md         构建代理（实现代码）
│   │   └── review.md        审查代理（审查代码）
│   ├── prompts/           ← 标准 Prompt 模板
│   ├── changelog/         ← 操作变更记录
│   ├── metrics/           ← 数据度量
│   ├── tech-debt/         ← 技术债务跟踪
│   └── heartbeat.ps1      ← 子代理心跳检测
│
├── scripts/
│   ├── compile-check.ps1  ← C# 编译检查
│   ├── lint-lua.ps1       ← Lua 语法检查
│   ├── build-rust.ps1     ← Rust 编译
│   ├── ci.ps1             ← 全量测试
│   ├── emergency/         ← 紧急恢复工具
│   └── ...
│
├── skills/                ← 24 个 AI 工作技能
└── Docs/                  ← 文档
```

---

## 四、日常工作流程

### 场景 1：实现一个新功能

```
你： "实现用户登录功能"
                             ← 你在 OpenCode 里输入
AI Orchestrator：
  1. 拆任务：数据模型 → 业务逻辑 → UI → 测试
  2. 为每个子任务派发 Build Agent
  3. 每个 Build Agent：
     a. 实现代码
     b. 运行编译检查（C# + Lua + Rust）
     c. 运行测试
     d. 记录 changelog
  4. 派发 Review Agent 做最终审查
  5. 合并到 master
```

**你的角色**：描述需求 + 验收最终结果  
**AI 的角色**：拆任务 + 写代码 + 自测 + 记录

### 场景 2：修 Bug

```
你： "修复登录崩溃的 Bug"
AI Orchestrator：
  1. 启动 Bug 修复流程（见 prompts/bug-fix.md）
  2. 重现 Bug
  3. 写复现测试（RED）
  4. 修复代码（GREEN）
  5. 验证测试通过
  6. 运行全量测试
  7. One-Bug-One-Test — 必须保留回归测试
```

### 场景 3：代码审查

```
你： "审查这个 PR"
AI Review Agent：
  1. 确定审查等级（L0-L4）
     L0 = 测试/文档 → 直接合并
     L1 = 工具函数 → 自动通过
     L2 = 业务逻辑 → 默认，主 Agent Review
     L3 = 核心算法 → +人工抽查
     L4 = 安全/加密 → Senior 逐行审查
  2. 执行对应级别的审查清单
  3. 返回 PASS / FAIL / 修改建议
```

---

## 五、紧急情况处理

### AI 卡住了怎么办

```
现象：AI 超过 60 秒没有响应
处理：OpenCode 的心跳检测会自动发现 → kill 卡死的子任务 → 重新派发
你不需要做任何事。
```

### AI 把代码改坏了怎么办

```powershell
# 1. 紧急冷冻 — 停止所有 AI 操作
.\scripts\emergency\freeze-ai.ps1

# 2. 回退到最近稳定版本
.\scripts\emergency\reset-to-latest-stable.ps1

# 3. 验证环境健康
.\scripts\emergency\verify-health.ps1

# 4. 如果只是配置文件坏了
.\scripts\emergency\restore-config.ps1
```

**总耗时：不超过 5 分钟。**

---

## 六、质量门禁（必须遵守）

每次合并前必须通过以下门禁：

```
□ 编译检查通过（C# + Lua + Rust）
□ 全部测试通过
□ 审查通过（按 L0-L4 分级标准）
□ Bug 修复必须附带回归测试（One-Bug-One-Test）
□ Changelog 已记录本次变更
□ 文档已同步（如果涉及功能变更）
```

**任何门禁不通过 → 不允许合并。**

---

## 七、如何贡献

### 改进 Prompt 模板

文件在 `.opencode/prompts/`，改完提交 PR。

### 添加新脚本

放在 `scripts/` 目录下，确保：
- 有超时保护（防止挂死）
- 工具不存在时优雅跳过
- 有详细的 help 注释

### 改进技能

文件在 `skills/<技能名>/SKILL.md`，遵循技能模板格式。

### 报告流程问题

发现问题后：
1. 记录场景（你在做什么、AI 怎么反应的）
2. 分析根因（是 prompt 不够好？权限不够？脚本有 bug？）
3. 提交 Issue 或 PR 修复

---

## 八、常见问题

### Q: AI 写出来的代码质量可靠吗？

**A**: 有 4 层保障：
1. 编译检查 → 语法正确
2. 测试 → 功能正确
3. 代码审查 → 逻辑正确
4. One-Bug-One-Test → 不回退

### Q: 我需要一直盯着 AI 吗？

**A**: 不需要。Orchestrator 会自动管理子任务，你只需要在关键节点验收。

### Q: AI 在等我的许可怎么办？

**A**: 正常情况下不会。权限预声明机制会在派发任务前一次性问完。如果遇到等许可的情况，说明有未覆盖的命令，记下来告诉流程维护者。

### Q: LSP 开着电脑变慢了怎么办？

**A**: 在 `opencode.json` 里把 `"lsp": true` 改成 `"lsp": false`，重启 OpenCode。

---

## 九、参考链接

| 资源 | 位置 |
|------|------|
| 工作流核心规则 | `AGENTS.md` |
| Prompt 模板 | `.opencode/prompts/` |
| 技能库 | `skills/` |
| 检查清单 | `references/` |
| 紧急恢复工具 | `scripts/emergency/` |
| 变更记录 | `.opencode/changelog/` |
| 度量报告 | `.opencode/metrics/` |
