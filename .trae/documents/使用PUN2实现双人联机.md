# PUN2双人联机实现方案（实际版）

## 1. 系统架构

### 1.1 核心网络系统

* **NetworkManager**：网络连接和房间管理
  * PUN2初始化和连接管理
  * 自动匹配和房间创建
  * 战斗场景加载

* **NetworkBattleManager**：战斗同步核心
  * 战斗状态管理
  * RPC调用和玩家同步
  * 战斗流程控制

* **NetworkUtils**：网络工具类
  * 网络状态检查
  * 同步辅助方法
  * 错误处理工具

### 1.2 游戏核心系统

* **BattleManager**：战斗主管理器
  * 游戏状态管理
  * 单位、建筑、法术逻辑
  * 网络战斗集成

* **UnitSystem**：单位系统
  * 单位AI和战斗逻辑
  * 目标选择和移动
  * 状态管理

* **BuildingSystem**：建筑系统
  * 建筑类型和功能
  * 放置验证和管理
  * 自动攻击逻辑

* **SpellSystem**：法术系统
  * 法术效果和目标选择
  * 特效管理
  * 伤害计算

* **ElixirSystem**：圣水系统
  * 独立的圣水恢复
  * 圣水上限和消耗
  * 网络同步

## 2. 网络同步实现

### 2.1 PUN2核心概念

* **PhotonView**：网络对象同步
* **RPC调用**：远程方法调用
* **Custom Properties**：状态同步
* **IPunObservable**：自定义同步接口

### 2.2 同步策略

* **状态同步**：使用Custom Properties同步游戏状态
* **RPC调用**：使用[PunRPC]标记实现远程操作
* **Master Client**：负责权威状态管理
* **平滑插值**：减少网络延迟影响

### 2.3 战斗流程同步

* **匹配流程**：battlePanel → searchPanel → 创建/加入房间 → 加载战斗场景
* **倒计时系统**：房间满员后自动开始3秒倒计时
* **战斗状态**：等待 → 倒计时 → 战斗中 → 暂停 → 结束
* **玩家离开处理**：自动判定胜负

## 3. 技术实现细节

### 3.1 网络连接

```csharp
// 初始化Photon网络
PhotonNetwork.GameVersion = gameVersion;
PhotonNetwork.ConnectUsingSettings();

// 开始匹配
PhotonNetwork.JoinRandomRoom();

// 创建房间
RoomOptions roomOptions = new RoomOptions();
roomOptions.MaxPlayers = 2;
PhotonNetwork.CreateRoom(null, roomOptions, null);
```

### 3.2 战斗同步

```csharp
// 同步玩家圣水
Hashtable playerProps = new Hashtable();
playerProps["Elixir"] = currentElixir;
PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);

// 同步战斗状态
Hashtable roomProps = new Hashtable();
roomProps["BattleStatus"] = (int)battleStatus;
PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

// 广播单位放置
photonView.RPC("RPC_PlaceUnit", RpcTarget.All, unitType, position, playerId);
```

### 3.3 网络工具

```csharp
// 检查网络状态
public static bool IsConnected() {
    return PhotonNetwork.IsConnected;
}

// 平滑同步位置
public static Vector3 SmoothSyncPosition(Vector3 currentPos, Vector3 targetPos, float smoothFactor = 10f) {
    return Vector3.Lerp(currentPos, targetPos, Time.deltaTime * smoothFactor);
}

// 处理网络错误
public static void HandleNetworkError(short errorCode, string errorMessage) {
    Debug.LogError($"网络错误: {errorCode}, {errorMessage}");
}
```

## 4. 错误处理与优化

### 4.1 常见错误解决

* **"Unsupported Plugin"错误**：简化房间创建配置
* **连接失败**：检查PhotonServerSettings配置
* **同步错误**：确保RPC调用参数一致

### 4.2 性能优化

* **带宽优化**：只同步必要的状态数据
* **同步频率**：固定同步间隔减少网络开销
* **对象池**：减少Instantiate/Destroy操作

### 4.3 用户体验优化

* **匹配动画**：搜索面板的加载动画
* **状态提示**：详细的网络连接状态
* **错误反馈**：友好的网络错误提示

## 5. 实现步骤

### 5.1 基础搭建

1. 集成PUN2 SDK
2. 配置PhotonServerSettings
3. 创建NetworkManager

### 5.2 网络系统实现

1. 实现连接和房间管理
2. 开发战斗同步核心
3. 创建网络工具类

### 5.3 游戏系统集成

1. 修改BattleManager支持网络战斗
2. 实现单位、建筑、法术同步
3. 完善圣水系统同步

### 5.4 测试与优化

1. 网络连接测试
2. 战斗同步测试
3. 性能优化

## 6. 技术要点

* **PUN2集成**：正确配置和使用PUN2功能
* **状态管理**：使用Custom Properties实现高效状态同步
* **RPC调用**：合理设计远程方法调用
* **错误处理**：健壮的网络错误处理机制
* **性能优化**：减少网络带宽使用

## 7. 测试计划

* **网络连接测试**：不同网络条件下的连接稳定性
* **战斗同步测试**：单位、建筑、法术同步准确性
* **延迟测试**：网络延迟对游戏体验的影响
* **错误恢复测试**：网络断开后的重连和状态恢复
* **性能测试**：高负载下的系统表现

## 8. 未来扩展

* **多人对战**：支持更多玩家的战斗模式
* **排名系统**：集成Photon的排行榜功能
* **观战模式**：实现游戏观战功能
* **语音聊天**：添加实时语音通信

此方案基于PUN2的状态同步机制，提供了完整的双人联机战斗系统，确保了游戏的流畅性和稳定性。

