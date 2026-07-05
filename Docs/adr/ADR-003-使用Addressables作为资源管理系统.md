# ADR-003: 使用 Addressables 作为资源管理系统

**日期**: 2026-07-05  
**状态**: 已实施  
**影响范围**: 资源加载、热更新、内存管理

## 背景

需要一套资源管理系统，支持异步加载、依赖管理、热更新和按需卸载。

## 选项

1. **Addressables** — 当前使用，Unity 官方
2. **Resources.Load** — 旧方案，无法热更新
3. **AssetBundle 原生** — 需要自行管理依赖
4. **AssetManager (自制)** — 需要从零开发

## 决策

选择 Addressables，理由：
- Unity 官方维护，持续更新
- 自动处理依赖关系
- 支持异步加载 + 按需下载
- 与 CI/CD 集成简单
- 从 Resources.Load 迁移路径清晰

## 后果

- ✅ 异步加载，不阻塞主线程
- ✅ 自动引用计数 + 内存管理
- ✅ 支持远程热更新
- ⚠️ 需要配置分组策略
- ⚠️ 调试不如 Resources.Load 直观
- ⚠️ 保留 Resources.Load 作为调试回退（UIManager.cs）
