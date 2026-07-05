# ADR-006: 使用 ServiceLocator 作为 DI 过渡方案

**日期**: 2026-07-05  
**状态**: 已实施  
**影响范围**: 架构、依赖注入、可测试性

## 背景

当前项目大量使用静态单例（`XX.Instance`），导致代码耦合度高、难以替换实现、单元测试困难。需要逐步引入依赖注入。

## 选项

1. **纯单例** — 当前方案，简单但耦合
2. **ServiceLocator** — 当前选中的过渡方案
3. **VContainer** — 完整 DI 容器
4. **Zenject/Extenject** — 第三方 DI

## 决策

选择 ServiceLocator 作为第一步 + VContainer 作为最终方向，理由：
- ServiceLocator 实现简单，一行代码即可注册
- 可以零改动逐步迁移：先注册单例，再慢慢换接口
- VContainer 已加入 Packages/manifest.json 但未强制使用
- 避免一次性大规模重构（风险太高）

## 后果

- ✅ 立即降低耦合度
- ✅ 逐步迁移，每步可测试
- ✅ 不阻塞现有功能开发
- ⚠️ ServiceLocator 是反模式（隐藏依赖关系）
- ⚠️ 最终仍需迁移到 VContainer 构造函数注入

## 迁移计划

1. 当前：ServiceLocator 注册已有单例 ✅
2. 下一步：提取接口 + 用 ServiceLocator 替换 Instance 调用
3. 最终：切换到 VContainer 构造函数注入
