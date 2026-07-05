using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
namespace KingdomWar.Server
{
    public class RoomCreationTest : MonoBehaviourPunCallbacks
    {
        [Header("UI Elements")]
        public Text statusText;
        public Button testButton;
        public Button disconnectButton;

        private bool isTesting = false;

        private void Start()
        {
            // 设置按钮事件
            if (testButton != null)
            {
                testButton.onClick.AddListener(TestRoomCreation);
            }

            if (disconnectButton != null)
            {
                disconnectButton.onClick.AddListener(Disconnect);
            }

            // 初始化状态文�?            UpdateStatusText("就绪，可以开始测�?);
        }

        /// <summary>
        /// 测试房间创建
        /// </summary>
        public void TestRoomCreation()
        {
            if (isTesting)
            {
                UpdateStatusText("测试已在进行中，请等�?..");
                return;
            }

            isTesting = true;
            UpdateStatusText("正在测试房间创建...");

            // 检查是否已连接到Photon
            if (!PhotonNetwork.IsConnected)
            {
                UpdateStatusText("未连接到Photon，正在连�?..");
                PhotonNetwork.GameVersion = "1.0";
                PhotonNetwork.ConnectUsingSettings();
            }
            else
            {
                // 已连接，直接尝试创建房间
                CreateTestRoom();
            }
        }

        /// <summary>
        /// 创建测试房间
        /// </summary>
        private void CreateTestRoom()
        {
            UpdateStatusText("正在创建测试房间...");

            // 使用最基本的RoomOptions配置
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = 2;
            roomOptions.IsVisible = true;
            roomOptions.IsOpen = true;

            // 生成唯一的房间名
            string uniqueRoomName = "TestRoom_" + System.DateTime.Now.Ticks;
            UpdateStatusText($"正在创建房间: {uniqueRoomName}");

            // 创建房间
            PhotonNetwork.CreateRoom(uniqueRoomName, roomOptions, null);
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            if (PhotonNetwork.IsConnected)
            {
                UpdateStatusText("正在断开连接...");
                PhotonNetwork.Disconnect();
            }
            else
            {
                UpdateStatusText("未连接到Photon");
            }
        }

        /// <summary>
        /// 更新状态文�?        /// </summary>
        /// <param name="message">状态消�?/param>
        private void UpdateStatusText(string message)
        {
            Debug.Log(message);
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        #region MonoBehaviourPunCallbacks

        public override void OnConnectedToMaster()
        {
            UpdateStatusText("已连接到Photon主服务器，正在创建房�?..");
            CreateTestRoom();
        }

        public override void OnCreatedRoom()
        {
            UpdateStatusText($"房间创建成功！房间名: {PhotonNetwork.CurrentRoom.Name}");
            isTesting = false;
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            UpdateStatusText($"房间创建失败: {returnCode}, {message}");
            isTesting = false;
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            UpdateStatusText($"已断开连接: {cause}");
            isTesting = false;
        }

        #endregion
    }

}
