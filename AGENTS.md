# AGENTS.md

This file provides guidance to AI coding agents (Claude Code, Cursor, Copilot, Antigravity, etc.) when working with code in this repository.

## Repository Overview

A collection of skills for Claude.ai and Claude Code for senior software engineers. Skills are packaged instructions and scripts that extend Claude and your coding agents capabilities.

## OpenCode Integration

OpenCode uses a **skill-driven execution model** powered by the `skill` tool and this repository's `/skills` directory.

### Core Rules

- If a task matches a skill, you MUST invoke it
- Skills are located in `skills/<skill-name>/SKILL.md`
- Never implement directly if a skill applies
- Always follow the skill instructions exactly (do not partially apply them)

### Reasoning Effort (深度推理模式)

根据任务复杂度切换推理深度：

**深度推理（必须开启）** — 适用于以下场景：
- 修编译错误、调试 Bug
- 架构设计、系统设计
- Code Review
- 涉及数据一致性、并发、安全的问题
- 任何你不确定后果的修改

在以上场景，必须：彻底拆解问题、追溯根因、检查所有边界情况、显式列出假设并验证、考虑替代方案。

**浅层推理（跳过）** — 适用于以下场景：
- 简单的变量/方法重命名
- 机械操作（建目录、删文件、格式化）
- 已知模式的重复代码
- 纯信息查询（"这个类的作用是什么"）

判断标准：如果你不确定是否要深度推理，就开启深度推理。宁可多花 tokens，不可漏掉根因。

### Intent → Skill Mapping

The agent should automatically map user intent to skills:

- Feature / new functionality → `spec-driven-development`, then `incremental-implementation`, `test-driven-development`
- Planning / breakdown → `planning-and-task-breakdown`
- Bug / failure / unexpected behavior → `debugging-and-error-recovery`
- Code review → `code-review-and-quality`
- Refactoring / simplification → `code-simplification`
- API or interface design → `api-and-interface-design`
- UI work → `frontend-ui-engineering`

### Lifecycle Mapping (Implicit Commands)

OpenCode does not support slash commands like `/spec` or `/plan`.

Instead, the agent must internally follow this lifecycle:

- DEFINE → `spec-driven-development`
- PLAN → `planning-and-task-breakdown`
- BUILD → `incremental-implementation` + `test-driven-development`
- VERIFY → `debugging-and-error-recovery`
- REVIEW → `code-review-and-quality`
- SHIP → `shipping-and-launch`

### Execution Model

For every request:

1. Determine if any skill applies (even 1% chance)
2. Invoke the appropriate skill using the `skill` tool
3. Follow the skill workflow strictly
4. Only proceed to implementation after required steps (spec, plan, etc.) are complete

### 🔴 Permission Pre-Declaration (Mandatory — prevents mid-task stalling)

**Never let a build agent discover it needs permission mid-execution.** All permissions
must be declared and granted BEFORE the agent starts working.

**Workflow:**

```
Orchestrator 拆解子任务
    │
    ├── 分析需要的权限：
    │     ├── 编辑哪些文件？
    │     ├── 运行哪些命令？
    │     └── 调用哪些外部工具？
    │
    ├── 检查是否在 build agent 白名单内：
    │     ├── ✅ 全部在白名单 → 直接派发，无需停顿
    │     └── ❌ 有不在白名单的 → 启动时问人类"这个任务需要权限 X，同意吗？"
    │
    ├── 派发 build agent（prompt 包含 PERMISSION DECLARATION）
    │
    └── build agent 执行：
          ├── 所有命令都在白名单 → 从不暂停
          └── 意外卡住 → orchestrator 心跳 60s 检测到 → kill + 重启
```

**Build agent 的职责：**
- 如果 prompt 中有 `PERMISSION DECLARATION` 部分，里面的权限全部预授权，**执行中绝不等待**
- 如果遇到不在白名单的命令，**立即汇报给 orchestrator**，不要沉默等待
- Build agent 的白名单覆盖：git, scripts/*.ps1, cargo, luac/luacheck/busted, 文件操作, 控制台输出

**Orchestrator 的职责：**
- 每次派发前必须完成权限分析
- dispatch prompt 中必须包含 `PERMISSION DECLARATION` 和 `PROGRESS REPORTING` 两个区块
- 如果超时 60 秒 → 先检查是否在等权限 → 是则立即批准 → 否则 kill + 重启

### Compilation & Test Gate (Mandatory)

Every task MUST pass this gate before being marked complete:

```
Implement changes
    │
    ▼
Run compilation check: scripts/compile-check.ps1
    │
    ├── Errors found → Fix errors → Repeat compile check
    │
    ▼
Run all EditMode tests
    │
    ├── Tests fail → Fix tests → Repeat tests
    │
    ▼
Commit to branch
    │
    ▼
Merge to master → Run compile check AGAIN → Run ALL tests AGAIN
    │
    ├── Any failure → Reject merge, document failure reason, fix, repeat
    │
    ▼
Task complete
```

### 🔴 One-Bug-One-Test Rule (Mandatory — Zero Bug Recurrence)

Every bug discovered during development or runtime MUST be accompanied by a
targeted regression test. The same bug must never appear twice.

**Rule:**

```
Bug发现
  │
  ├── 修复代码
  │
  └── 写回归测试!
        └── 这个测试必须能捕获这个bug:
              ├── 如果是NullReferenceException → 验证null检查存在
              ├── 如果是逻辑错误 → 验证正确行为
              └── 如果是性能问题 → 添加性能基准断言
```

**举例:**
| Bug | 回归测试 |
|-----|---------|
| `mainPanel.Start()` NRE `cachedPlayerData` 为 null | `PlayerDataManager_OnTrophiesChanged_CanSubscribe` 验证事件订阅不抛NRE |
| 重复面板实例 | `PlayerDataManager_Instance_IsNotNull` 验证单例初始化 |
| 奖杯计算错误 | `TrophyManager_ApplyTrophyChange_IncreasesTrophiesOnWin` 验证加减正确 |

**验证方式（代码审查时检查）:**
- PR 中修复 bug 的 commit 后面必须有对应的 test commit
- 如果缺少回归测试 → **拒绝合并**
- 同一个 bug 再次出现 → 说明上次的回归测试不够精确，需要补强

**Unity CLI compile check** (MUST use this script — it has timeout protection):
```powershell
.\scripts\compile-check.ps1
```

**Unity Test Runner** (MUST use CI script — it has timeout handling):
```powershell
# Run EditMode tests
.\scripts\ci.ps1 -Mode EditMode
# Run PlayMode tests
.\scripts\ci.ps1 -Mode PlayMode
```

**🚨 NEVER run Unity.exe directly.** Always use the wrapper scripts above.
If you need to run a custom Unity command, use:
```powershell
.\scripts\Invoke-Unity.ps1 -Arguments "-quit -batchmode -projectPath . -executeMethod X.Y.Z" -TimeoutSeconds 30
```

### Sub-Agent Progress Monitoring (Mandatory — for all `task` dispatches)

#### 核心原则

**Orchestrator 绝不自己实现代码。** 所有实现工作必须派发给 build agent。
当 build agent 卡住时，正确的做法是**关闭卡死的子代理、重新派发**，而不是自己上手写。

#### 子任务进度监控流程

```
Orchestrator 拆解任务 → 派发 build agent
    │
    ├── 派发时，prompt 中必须包含进度汇报要求：
    │   "## PROGRESS REPORTING (MANDATORY)
    │    You MUST report your progress at least every 30 seconds.
    │    Tell me: what you've done, what you're working on, any issues."
    │
    ├── 派发后，orchestrator 跟踪耗时：
    │     ├── <30s → 正常等待
    │     ├── 30-60s → 检查 agent 是否有汇报
    │     └── >60s无汇报 → 判定超时！
    │           ├── 关闭卡死的子任务
    │           ├── Kill 残留 Unity 进程
    │           ├── 检查已做完的部分
    │           └── 重新派发剩余工作给新 agent
    │
    └── 子任务回报 → 验证结果 → 继续下一个
```

#### 为何不能自己实现

| 不正确做法 | 正确做法 |
|-----------|---------|
| ❌ "子任务超时了，我自己写代码" | ✅ "子任务超时了，关闭它，重新派发" |
| ❌ "build agent 卡住了，我直接改文件" | ✅ "检查卡住原因，重新派发给新的 build agent" |
| ❌ "task 工具没有超时参数，我自己来" | ✅ "在 prompt 里要求每 30s 汇报，>60s 判定超时" |

#### Progress Reporting Template（每次派发时必须包含）

每次使用 `task` 工具派发 build agent 时，prompt **必须**以这段话结尾：

```
## PROGRESS REPORTING (MANDATORY)
You MUST report your progress at least every 30 seconds.
Tell me: what you've done so far, what you're working on now, any issues.
If I don't hear from you for >60s, I will consider you stuck and restart.
```

#### 心跳脚本（辅助工具）

`.opencode/heartbeat.ps1` 用于检测上次任务是否有残留卡住状态：

```powershell
# 每次派发前检查是否有残留卡住
.opencode/heartbeat.ps1 -Task "功能名称" -Subtask "1/5" -Step "开始" -Status running -Elapsed 0
# 退出码=2 表示有残留卡住 → 先清理再派发
```

### Layered Agent Workflow (复杂任务)

对于超过 30 分钟的复杂任务，使用分层 Agent 模式：

```
主 agent（当前会话）
  │
  ├── 拆任务 → 派发 build agent
  │     ├── build agent: 实现 + 编译检查 + 测试
  │     └── build agent 回报结果
  │
  ├── 验收 → 派发 review agent
  │     ├── review agent: 审查代码
  │     └── review agent 回报结果
  │
  ├── 验收不通过 → 打回 build agent 修改
  │
  └── 验收通过 → 合并分支
```

- 主 agent 负责理解需求、拆任务、验收、合并
- build agent 只负责实现和测试
- review agent 只负责审查（只读权限，不能改代码）
- 子 agent 汇报结果后主 agent 做决策

### Documentation Sync

每个任务完成后，必须同步更新相关文档：
- 新增功能 → 更新 `Docs/需求/` 或创建新文档
- 修改接口 → 更新对应文档中的代码示例
- 目录结构变化 → 更新 `Docs/03-技术架构.md`
- Bug 修复 → 在修复提交信息中注明

### Anti-Rationalization

The following thoughts are incorrect and must be ignored:

- "This is too small for a skill"
- "I can just quickly implement this"
- "I’ll gather context first"

Correct behavior:

- Always check for and use skills first

This ensures OpenCode behaves similarly to Claude Code with full workflow enforcement.

## Orchestration: Personas, Skills, and Commands

This repo has three composable layers. They have different jobs and should not be confused:

- **Skills** (`skills/<name>/SKILL.md`) — workflows with steps and exit criteria. The *how*. Mandatory hops when an intent matches.
- **Personas** (`agents/<role>.md`) — roles with a perspective and an output format. The *who*.
- **Slash commands** (`.claude/commands/*.md`) — user-facing entry points. The *when*. The orchestration layer.

Composition rule: **the user (or a slash command) is the orchestrator. Personas do not invoke other personas.** A persona may invoke skills.

The only multi-persona orchestration pattern this repo endorses is **parallel fan-out with a merge step** — used by `/ship` to run `code-reviewer`, `security-auditor`, and `test-engineer` concurrently and synthesize their reports. Do not build a "router" persona that decides which other persona to call; that's the job of slash commands and intent mapping.

See [docs/agents.md](docs/agents.md) for the decision matrix and [references/orchestration-patterns.md](references/orchestration-patterns.md) for the full pattern catalog.

**Claude Code interop:** the personas in `agents/` work as Claude Code subagents (auto-discovered from this plugin's `agents/` directory) and as Agent Teams teammates (referenced by name when spawning). Two platform constraints align with our rules: subagents cannot spawn other subagents, and teams cannot nest. Plugin agents silently ignore the `hooks`, `mcpServers`, and `permissionMode` frontmatter fields.

## Creating a New Skill

> **Before you start:** run the pre-flight checks in [CONTRIBUTING.md](CONTRIBUTING.md#before-proposing-a-new-skill), search the catalog, check open PRs (`gh pr list --state open`), confirm the idea fits [docs/skill-anatomy.md](docs/skill-anatomy.md), and justify the gap in your PR description. Most new-skill ideas overlap an existing skill or an open PR; prefer extending an existing skill over adding a near-duplicate. CONTRIBUTING.md is the single source of truth for this workflow.

Skills in this repo are markdown-first: each lives at `skills/<kebab-case-name>/SKILL.md` with YAML frontmatter (`name`, `description`) and follows the section anatomy (Overview, When to Use, Process, Common Rationalizations, Red Flags, Verification). Add a `scripts/` directory only when the skill ships runnable helpers; most skills are markdown only, and there are no per-skill zip packages.

For the full format, naming conventions, frontmatter rules, supporting-file thresholds, and writing principles, see [docs/skill-anatomy.md](docs/skill-anatomy.md), the single source of truth for skill structure. Do not restate that guidance here, link to it.

## Unity-Specific Adaptation Notes

### Skill Adaptations

| Skill | Unity Adaptation |
|-------|-----------------|
| **frontend-ui-engineering** | Use UGUI + DOTween patterns. Component architecture maps to UGUI Panel system |
| **api-and-interface-design** | Apply to C# interfaces, Network layer protocols, ScriptableObject data contracts |
| **test-driven-development** | Use Unity Test Framework (NUnit). EditMode tests for logic, PlayMode for integration |
| **performance-optimization** | Use Unity Profiler. Focus on Draw Calls, GC, Addressables loading |
| **ci-cd-and-automation** | Use Unity CLI: `-quit -batchmode -executeMethod`, not npm |
| **git-workflow-and-versioning** | Keep `.meta` files in sync, avoid large binary files in git |

### Project Conventions

- **C# version**: 9.0 (block-scoped namespaces only, no file-scoped)
- **Namespaces**: `KingdomWar.X` scheme
- **Asset loading**: Prefer Addressables async, fallback to Resources.Load
- **Hot update**: HybridCLR for code, Addressables catalog for assets
- **Networking**: Photon PUN2 for real-time, custom TCP (`127.0.0.1:6066`) for data service

### Ponytail (代码精简插件)

已通过 `opencode.json` 的 `plugin` 配置启用 [Ponytail](https://github.com/DietrichGebert/ponytail)。

AI 在实现代码前会执行七层决策梯子，避免过度工程：

```
1. 这东西真的需要存在吗？            → 不需要就跳过 (YAGNI)
2. 项目代码库里已经有现成的？         → 重用，不重写
3. Unity/BCL 标准库能实现？           → 用标准库
4. Unity 引擎原生功能能实现？          → 用引擎原生
   (协程、Animator、Addressables、UGUI 等)
5. 已安装的依赖/框架能实现？          → 用已有依赖
   (DOTween、Photon、HybridCLR 等)
6. 一行能搞定？                      → 就一行
7. 以上都不行 → 写最简实现
```

**重要**：这七层是在理解问题之后运行的，不是替代理解。AI 必须先读代码、理解上下文，再决定走哪一层。

**安全红线**（绝不因为"懒"而砍掉）：
- 输入验证、错误处理、安全性、可访问性始终保留
- Unity MonoBehaviour 生命周期方法（Awake/Start/Update）必须有正确的 null 检查
- Lua 和 Rust 层的 FFI 边界保护不因精简而省略

### LSP 集成 (OpenCode 内置)

已在 `opencode.json` 中启用 `"lsp": true`，OpenCode 自动启动以下语言服务器：

| 语言 | LSP 服务器 | 价值 |
|------|-----------|------|
| **C#** | OpenCode 内置 csharp | 编译诊断、跳转定义、类型信息 |
| **Lua** | OpenCode 内置 lua-ls | 自动补全、引用查找、语法检查 |
| **Rust** | OpenCode 内置 rust-analyzer | 类型推断、borrow check 提示、crates 信息 |

**注意**：LSP 会消耗内存和 CPU，如果发现 AI 变慢，在 `opencode.json` 中改为 `"lsp": false` 可关闭。

### 外部 AI 技能资源 (参考)

这些技能来自 Anthropic 官方插件市场，**仅限 Claude Code**，不能直接在 OpenCode 中使用。
但它们的 SKILL.md 文件遵循 Agent Skills 开放标准，可以作为参考来改进我们自己的技能：

| 技能/插件 | 适用平台 | 参考价值 |
|-----------|---------|---------|
| `csharp-lsp` | Claude Code | 我们已有 OpenCode 内置 C# LSP |
| `lua-lsp` | Claude Code | 我们已有 OpenCode 内置 Lua LSP |
| `rust-analyzer-lsp` | Claude Code | 我们已有 OpenCode 内置 Rust LSP |
| `code-simplifier` | Claude Code | ✅ 可参考其 SKILL.md，融入我们的 `code-simplification` 技能 |
| `skill-creator` | Claude Code | ✅ 可参考其评估方法，用于测试我们自己的 skill |
| `security-guidance` | Claude Code | ✅ 可参考其安全检查清单 |
| `pr-review-toolkit` | Claude Code | ✅ 可参考其审查流程 |
| Claude 内置 `/batch` | Claude Code | 概念可借鉴——大规模并行改造 |
| Claude 内置 `/fewer-permission-prompts` | Claude Code | 概念可借鉴——我们已有权限预声明机制 |
