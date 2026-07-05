using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

namespace KingdomWar.Game.Battle
{
    public static class NetworkUtils
    {
        #region 网络状态检查

        /// <summary>
        /// 检查是否已连接到Photon
        /// </summary>
        /// <returns>是否已连接</returns>
        public static bool IsConnected()
        {
            return PhotonNetwork.IsConnected;
        }

        /// <summary>
        /// 检查是否在房间中
        /// </summary>
        /// <returns>是否在房间中</returns>
        public static bool IsInRoom()
        {
            return PhotonNetwork.IsConnected && PhotonNetwork.InRoom;
        }

        /// <summary>
        /// 检查是否是本地玩家
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否是本地玩家</returns>
        public static bool IsLocalPlayer(int playerId)
        {
            return IsConnected() && PhotonNetwork.LocalPlayer.ActorNumber == playerId;
        }

        /// <summary>
        /// 检查是否是Master Client
        /// </summary>
        /// <returns>是否是Master Client</returns>
        public static bool IsMasterClient()
        {
            return IsInRoom() && PhotonNetwork.IsMasterClient;
        }

        /// <summary>
        /// 获取本地玩家ID
        /// </summary>
        /// <returns>本地玩家ID</returns>
        public static int GetLocalPlayerId()
        {
            if (IsConnected())
            {
                return PhotonNetwork.LocalPlayer.ActorNumber;
            }
            return -1;
        }

        /// <summary>
        /// 获取房间中的玩家数量
        /// </summary>
        /// <returns>玩家数量</returns>
        public static int GetPlayerCount()
        {
            if (IsInRoom())
            {
                return PhotonNetwork.PlayerList.Length;
            }
            return 0;
        }

        #endregion

        #region 同步辅助方法

        /// <summary>
        /// 生成唯一ID
        /// </summary>
        /// <returns>唯一ID</returns>
        public static int GenerateUniqueId()
        {
            return Random.Range(1000, 9999);
        }

        /// <summary>
        /// 平滑同步位置
        /// </summary>
        /// <param name="currentPos">当前位置</param>
        /// <param name="targetPos">目标位置</param>
        /// <param name="smoothFactor">平滑因子</param>
        /// <returns>平滑后的位置</returns>
        public static Vector3 SmoothSyncPosition(Vector3 currentPos, Vector3 targetPos, float smoothFactor = 10f)
        {
            return Vector3.Lerp(currentPos, targetPos, Time.deltaTime * smoothFactor);
        }

        /// <summary>
        /// 平滑同步旋转
        /// </summary>
        /// <param name="currentRot">当前旋转</param>
        /// <param name="targetRot">目标旋转</param>
        /// <param name="smoothFactor">平滑因子</param>
        /// <returns>平滑后的旋转</returns>
        public static Quaternion SmoothSyncRotation(Quaternion currentRot, Quaternion targetRot, float smoothFactor = 10f)
        {
            return Quaternion.Lerp(currentRot, targetRot, Time.deltaTime * smoothFactor);
        }

        /// <summary>
        /// 检查位置是否需要同步
        /// </summary>
        /// <param name="currentPos">当前位置</param>
        /// <param name="targetPos">目标位置</param>
        /// <param name="threshold">阈值</param>
        /// <returns>是否需要同步</returns>
        public static bool NeedSyncPosition(Vector3 currentPos, Vector3 targetPos, float threshold = 0.1f)
        {
            return Vector3.Distance(currentPos, targetPos) > threshold;
        }

        /// <summary>
        /// 检查旋转是否需要同步
        /// </summary>
        /// <param name="currentRot">当前旋转</param>
        /// <param name="targetRot">目标旋转</param>
        /// <param name="threshold">阈值</param>
        /// <returns>是否需要同步</returns>
        public static bool NeedSyncRotation(Quaternion currentRot, Quaternion targetRot, float threshold = 0.05f)
        {
            return Quaternion.Angle(currentRot, targetRot) > threshold;
        }

        #endregion

        #region 错误处理

        /// <summary>
        /// 处理网络错误
        /// </summary>
        /// <param name="errorCode">错误代码</param>
        /// <param name="errorMessage">错误信息</param>
        public static void HandleNetworkError(short errorCode, string errorMessage)
        {
            Debug.LogError($"网络错误: {errorCode}, {errorMessage}");
        }

        /// <summary>
        /// 处理断开连接
        /// </summary>
        /// <param name="cause">断开原因</param>
        public static void HandleDisconnection(DisconnectCause cause)
        {
            Debug.LogFormat("网络断开: {0}", cause);
        }

        #endregion

        #region 网络对象管理

        /// <summary>
        /// 根据ID查找网络单位
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>网络单位</returns>
        public static NetworkUnit FindNetworkUnitById(int unitId)
        {
            // 这里需要根据实际的单位管理逻辑来实现
            // 暂时返回null
            return null;
        }

        /// <summary>
        /// 获取所有网络单位
        /// </summary>
        /// <returns>网络单位列表</returns>
        public static List<NetworkUnit> GetAllNetworkUnits()
        {
            // 这里需要根据实际的单位管理逻辑来实现
            // 暂时返回空列表
            return new List<NetworkUnit>();
        }

        #endregion

        #region 同步配置

        /// <summary>
        /// 默认同步间隔（秒）
        /// </summary>
        public static float DefaultSyncInterval = 0.1f;

        /// <summary>
        /// 默认位置同步阈值
        /// </summary>
        public static float DefaultPositionSyncThreshold = 0.1f;

        /// <summary>
        /// 默认旋转同步阈值
        /// </summary>
        public static float DefaultRotationSyncThreshold = 0.05f;

        #endregion

        #region 调试工具

        /// <summary>
        /// 打印网络状态
        /// </summary>
        public static void PrintNetworkStatus()
        {
            Debug.LogFormat("网络状态: 连接={0}, 在房间={1}, 玩家数量={2}", 
                IsConnected(), IsInRoom(), GetPlayerCount());
        }

        /// <summary>
        /// 打印同步统计信息
        /// </summary>
        /// <param name="syncCount">同步次数</param>
        /// <param name="syncTime">同步时间</param>
        public static void PrintSyncStats(int syncCount, float syncTime)
        {
            Debug.LogFormat("同步统计: 次数={0}, 时间={1:F3}ms", syncCount, syncTime * 1000);
        }

        #endregion
    }
}