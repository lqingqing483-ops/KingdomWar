# Changelog

## [2.0.0] - 2026-07-05

### Phase 1 — 数据安全与工程自动化
- JSON 文件存储替代 PlayerPrefs: `IPlayerDataRepository` + `JsonPlayerDataRepository`
- AES 加密 + 原子写入 (tmp→backup→primary) + 自动迁移
- Code Coverage 包引入 + CI 覆盖率门禁 (50% 阈值)
- `run-coverage.ps1`: 自动收集覆盖率并对比阈值
- 冒烟测试: 5个 PlayMode 测试加载所有场景

### Phase 2 — 解耦与基础设施
- `ServiceLocator`: 单例→DI 的过渡桥接
- `IBattleManager` 接口 + BattleManager 实现
- `IGameService` / `IDisposableService` 生命周期接口
- CI: 新增 build 阶段 + Android 构建
- ADR-001: 网络框架决策记录

### Phase 3 — 性能与本地化
- `run-perf.ps1`: 性能回归门禁 (CI perf 阶段)
- `AnalyticsService`: 本地事件追踪框架
- `LocalizationManager`: CSV 中英双语本地化
- `translations.csv`: 11 个 UI 词条

## [1.5.0] - 2026-07-04
- PlayerDataManager: 新增昵称/头像/胜负平统计 (RecordBattleResult)
- BattleManager: 战斗结束自动记录胜负统计
- ProfilePanel: 新UI面板显示玩家资料 + 竞技场 + 胜率
- mainPanel: 新增资料Toggle入口
- 13个EditMode测试: API契约验证

## [1.4.0] - 2026-07-04

### Economy Balance System
- 新增 `EconomyBalanceSO` ScriptableObject — 集中管理所有经济数值
- ShopManager: 商品定价从硬编码改为读取配置 (basePrice + index * multiplier)
- ChestManager: 宝箱奖励/冷却/加速消耗全部从配置读取
- 24个EditMode测试: 配置加载, 边界值验证, 数值关系检查

## [1.3.0] - 2026-07-04

### Spell Effect System
- 新增 `SpellEffectResolver` — 6种法术效果: Damage/Heal/Freeze/Stun/Boost/Clone
- 新增 `Unit` buff系统: isFrozen/isStunned/damageMultiplier/speedMultiplier
- `Spell.Heal()` 治疗法术支持 (不超过最大HP, 对死亡单位无效)
- `Spell.cs` 重构为委托`SpellEffectResolver`处理效果
- 18个EditMode测试: enum验证, buff应用, 治疗逻辑
- `NetworkManager.cs`: 修复3个预存的括号bug

## [1.2.0] - 2026-07-04

### AI Opponent System
- 新增 `AIDifficulty` 枚举 (Easy/Medium/Hard/Expert) + `AIPlayStyle`
- 新增 `AIDeckData` + `AIOpponentConfig` — 4套预组卡组 + 难度调参
- 新增 `AIOpponentManager` — 手牌管理→选牌→选位→部署完整AI引擎
- BattleManager: 本地对战时自动创建AI对手, 每帧驱动AI决策
- AI同步player2圣水, 通过NetworkEntityManager部署单位

### Matchmaking Service
- 新增 `MatchmakingService` — 奖杯匹配引擎
- 初始奖杯范围 +/-100, 每10s扩展100, 最长60s超时
- 超时后提供 bot match 选项
- NetworkManager: 房间创建时同步房主奖杯信息
- searchPanel: 显示匹配范围+已用时间+bot按钮

## [1.1.1] - 2026-07-04

### Heartbeat & Stuck Prevention
- 新增 **可执行心跳脚本** `.opencode/heartbeat.ps1`
  - 写入 `task-state.json` + 自动检测上次心跳是否超 35s
  - 退出码 0=正常, 2=卡住检测 → 触发恢复流程
  - 每次 Agent dispatch 前、执行中(~10s)、完成后必须调用
- `orchestrator.md`: 心跳协议硬化，强制每次 dispatch 前执行 heartbeat
- `AGENTS.md`: 心跳从"自觉规范"改为"可执行脚本"，退出码驱动恢复
- 三层超时检测：bash命令15s、子任务30s、单步操作60s
- 自动重试策略：首次Kill重试（30s超时），二次失败报告不阻塞
- `scripts/compile-check.ps1`: Unity等待180s→30s，超时自动Kill进程
- `CompileChecker.cs`: 编译超时120s→20s
- `PlayerDataManager.SaveData()`: private→public (hotfix)

## [1.1.0] - 2026-07-04

### Arena/Trophy System
- 新增 `ArenaSystem` — 8个竞技场等级（训练营→传奇），奖杯阈值表
- 新增 `TrophyManager` — 奖杯计算引擎，支持对战胜负/平局/奖杯差调整
- 新增 `SeasonManager` — 28天赛季循环，赛季结算奖励
- 奖杯门机制：不掉段保护（不能低于当前竞技场最低奖杯）
- 战斗结算面板显示奖杯变化和竞技场信息
- 主菜单显示当前奖杯数和竞技场名称
- 16个EditMode测试用例（ArenaConfig + TrophyManager计算逻辑）
- 批量保存优化：`PlayerDataManager` 支持 `saveImmediately` 参数
- 事件安全：`SeasonManager.OnDestroy()` 清理，`mainPanel` 缓存引用

## [1.0.0] - 2026-07-02

### Code Quality
- 重命名 `basePanel` → `BasePanel` 遵循 C# PascalCase 规范
- `SceneNames` 修复: `Battle` 改为 `"battleScene"` (原为 `"Main"`)
- `CardData` / `UnitData` / `SpellData` / `BuildingData` 字段改为属性
- `FrameEvent` / `PlayerData` / `PoolConfig` 字段改为属性
- `BattleManager.buildings` 改为私有字段+属性
- `NetworkManager` 字段改为 `[SerializeField] private` + 属性
- `LotteryResult` 移入 `KingdomWar.HotUpdate` 命名空间
- 创建 `.editorconfig` 统一代码风格

### Performance
- 创建 `LogUtil.cs` 条件编译日志类 (`[Conditional("DEBUG")]`)
- `Unit.cs` / `NetworkManager.cs` / `BattleManager.cs` / `BattleSyncService.cs` / `UIManager.cs` / `ObjectPoolManager.cs` 中的 `Debug.Log` 全部替换为 `LogUtil`
- `UIManager.cs` 移除空 `Update()` 方法
- `ObjectPoolManager.cs` 使用 `TryGetValue` 替代 `ContainsKey` + 索引器

### Object Pool
- 创建 `IPoolable` 接口，支持 `OnGet()` / `OnRelease()` 回调
- `ObjectPool` 修复最大容量检查（`totalCreated` 计数器）
- `NetworkEntityManager` 特效使用对象池
- `BattleManager.CreateBuilding` 接入对象池

### Addressables
- `UIManager.Initialize` 改为 `InitializeAsync`，`WaitForCompletion` 全部改为 `await handle.Task`
- `UIManager` 移除 `Resources.Load` 回退代码
- `CardDatabase.LoadCardData` 改为 `LoadCardDataAsync`，使用 `Addressables.LoadAssetsAsync`
- 创建 `TaskExtensions.Forget()` 扩展方法

### Documentation
- `03-技术架构.md` 同步为实际实现（Photon PUN2、PlayerPrefs、自定义A*）
