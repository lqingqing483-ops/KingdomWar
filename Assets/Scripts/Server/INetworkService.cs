using System;

namespace KingdomWar.Server
{
    public enum NetworkConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        InRoom
    }

    public interface INetworkService
    {
        NetworkConnectionState ConnectionState { get; }
        int LocalPlayerId { get; }
        bool IsMasterClient { get; }

        void Connect();
        void Disconnect();
        void CreateRoom(string roomName, int maxPlayers);
        void JoinRoom(string roomName);
        void LeaveRoom();

        event Action OnConnected;
        event Action OnDisconnected;
        event Action<string> OnRoomJoined;
        event Action<int> OnPlayerJoined;
        event Action<int> OnPlayerLeft;
        event Action OnMasterClientSwitched;

        void SendMessage(byte[] data, int targetPlayerId = -1);
        void ReceiveMessage(byte[] data);
    }
}
