# ADR-004: 选择 PlayerPrefs + JSON 作为数据持久化方案

**日期**: 2026-07-05  
**状态**: 已实施  
**影响范围**: 数据存储、存档、加密

## 背景

需要存储玩家进度、卡牌收集、金币宝石等数据。

## 选项

1. **PlayerPrefs** — 原始方案，Unity 内置
2. **PlayerPrefs + JSON 文件** — 当前方案（v2.0），AES 加密
3. **SQLite** — 关系型数据库，容量大
4. **Unity Cloud Save** — 需要网络

## 决策

选择 JSON 文件方案（替代纯 PlayerPrefs），理由：
- PlayerPrefs 容量限制 1MB，不支持事务
- JSON + AES 加密安全性优于 PlayerPrefs 明文
- 原子写入 (tmp→backup→primary) 防崩溃数据损坏
- 无需引入第三方库
- 保留 PlayerPrefs 作为回退（容错降级）

## 后果

- ✅ 容量不再受限（文件系统）
- ✅ AES 加密防篡改
- ✅ 原子写入 + 备份恢复
- ⚠️ 仍然是本地存储，不支持云端同步
- ⚠️ JSON 全量读写，大数据量场景需分批
- ⚠️ 后续可平滑迁移到 SQLite（已预留 IPlayerDataRepository 接口）
