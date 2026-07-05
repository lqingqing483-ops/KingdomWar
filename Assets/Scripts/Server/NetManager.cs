using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using KingdomWar.Game;
using KingdomWar.Tools;
namespace KingdomWar.Server
{
public class NetManager : Singleton<NetManager>
{
    public string PublicKey { get; set; }
    public string Secret { get; set; }
    private Socket m_Socket;
    private ByteArray byteArray;
    private List<MsgBase> m_MsgList;
    private List<MsgBase> m_UnityMsgList;
    private int m_MsgCount;
    private Thread m_MsgThread;
    private Thread m_HeartThread;
    private CancellationTokenSource m_Cts;
    private long lastPongTime;
    private long lastPingTime;

    public delegate void ProtoListener(MsgBase msgBase);

    private Dictionary<ProtocolEnum, ProtoListener> m_ProtoDic;
    private Queue<ByteArray> m_WriteQueue;
    private long m_PingInterval = 120;

    public void Connect(string ip, int port)
    {
        if (m_Socket != null && m_Socket.Connected)
        {
            Debug.LogError("链接失败，已经链接上了！");
            return;
        }

        InitState();
        m_Socket.NoDelay = true;
        m_Socket.BeginConnect(ip, port, ConnectCallback, m_Socket);
    }

    void InitState()
    {
        m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        byteArray = new ByteArray();
        m_MsgList = new List<MsgBase>();
        m_UnityMsgList = new List<MsgBase>();
        m_ProtoDic = new Dictionary<ProtocolEnum, ProtoListener>();
        m_WriteQueue = new Queue<ByteArray>();
        m_MsgCount = 0;
        m_Cts = new CancellationTokenSource();
        PublicKey = GameConfig.Instance.publicKey;
        lastPingTime = GetTimeStamp();
        lastPongTime = GetTimeStamp();
    }

    //链接成功时的回调
    void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState;
            socket.EndConnect(ar);
            m_MsgThread = new Thread(MsgThread);
            m_MsgThread.IsBackground = true;
            m_MsgThread.Start();
            m_HeartThread = new Thread(MsgHeartThread);
            m_HeartThread.IsBackground = true;
            m_HeartThread.Start();
            Debug.Log("socket链接成功！");
            ProtoManager.Instance.CSSecret();
            m_Socket.BeginReceive(byteArray.Bytes, byteArray.writeIdx, byteArray.Remain, 0, ReceiveCallback, socket);
        }
        catch (SocketException e)
        {
            Debug.LogError("链接成功回调异常！" + e);
            Close();
        }
    }

    //消息线程回调
    void MsgThread()
    {
        var token = m_Cts.Token;
        while (!token.IsCancellationRequested && m_Socket != null && m_Socket.Connected)
        {
            if (m_MsgList.Count <= 0)
            {
                try
                {
                    Task.Delay(10, token).Wait(token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                continue;
            }

            MsgBase msgBase = null;
            lock (m_MsgList)
            {
                if (m_MsgList.Count > 0)
                {
                    msgBase = m_MsgList[0];
                    m_MsgList.RemoveAt(0);
                }
            }

            if (msgBase != null)
            {
                if (msgBase is MsgPing)
                {
                    lastPongTime = GetTimeStamp();
                    Debug.Log("接受到心跳包！！！");
                    m_MsgCount--;
                }
                else
                {
                    lock (m_UnityMsgList)
                    {
                        m_UnityMsgList.Add(msgBase);
                    }
                }
            }
            else
            {
                Close();
                break;
            }
        }
    }

    //心跳线程回调
    void MsgHeartThread()
    {
        var token = m_Cts.Token;
        while (!token.IsCancellationRequested && m_Socket != null && m_Socket.Connected)
        {
            long timeNow = GetTimeStamp();
            if (timeNow - lastPingTime > m_PingInterval)
            {
                MsgPing msgPing = new MsgPing();
                Send(msgPing);
                lastPingTime = GetTimeStamp();
                Debug.Log("客户端向服务器发送心跳包！");
            }

            if (timeNow - lastPongTime >= m_PingInterval * 4)
            {
                Debug.Log("心跳超时，服务器已经该客户端踢下线！");
                Close();
            }

            try
            {
                Task.Delay(1000, token).Wait(token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    //处理unity中的消息
    public void MsgUpdate()
    {
        if (m_Socket != null && m_Socket.Connected)
        {
            if (m_MsgCount == 0)
            {
                return;
            }

            MsgBase msgBase = null;
            lock (m_UnityMsgList)
            {
                if (m_UnityMsgList.Count > 0)
                {
                    msgBase = m_UnityMsgList[0];
                    //Debug.Log("处理unity消息，类型："+ msgBase.ProtocolType.ToString());
                    m_UnityMsgList.RemoveAt(0);
                    m_MsgCount--;
                }
            }

            if (msgBase != null)
            {
                //Debug.Log("处理消息，类型："+ msgBase.ProtocolType.ToString());
                StartProto(msgBase.ProtocolType, msgBase);
            }
        }
    }

    //接受到数据
    void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState;
            int count = socket.EndReceive(ar);
            if (count <= 0)
            {
                Close();
                return;
            }

            byteArray.writeIdx += count;
            //对接收到的数据进行处理
            OnReceiveData();
            if (byteArray.Remain < 8)
            {
                byteArray.ChecheAndMoveBytes();
                byteArray.Resize(byteArray.Length * 2);
            }

            socket.BeginReceive(byteArray.Bytes, byteArray.writeIdx, byteArray.Remain, 0, ReceiveCallback, socket);
        }
        catch (SocketException e)
        {
            Debug.LogError("接受数据回调异常！" + e);
            Close();
        }
    }

    //对接收到的数据进行处理
    void OnReceiveData()
    {
        if (byteArray.Length <= 4 || byteArray.readIdx < 0)
        {
            return;
        }

        int readIdx = byteArray.readIdx;
        byte[] bytes = byteArray.Bytes;
        int bodyLength = BitConverter.ToInt32(bytes, readIdx); //计算出消息长度
        if (byteArray.Length < bodyLength + 4) //分包处理
        {
            return;
        }

        //对接受到的数据进行处理
        byteArray.readIdx += 4;
        //解析协议名
        int nameCount = 0;
        ProtocolEnum protocol = ProtocolEnum.None;
        try
        {
            protocol = MsgBase.DecodeName(byteArray.Bytes, byteArray.readIdx, out nameCount);
        }
        catch (Exception e)
        {
            Debug.LogError("解析协议名异常！" + e);
            Close();
            return;
        }

        if (protocol == ProtocolEnum.None)
        {
            Debug.LogError("解析协议名失败！");
            Close();
            return;
        }

        //协议名解析完成，解析协议内容
        byteArray.readIdx += nameCount;
        int bodyCount = bodyLength - nameCount;
        MsgBase msgBase = null;
        try
        {
            msgBase = MsgBase.Decode(protocol, byteArray.Bytes, byteArray.readIdx, bodyCount);
        }
        catch (Exception e)
        {
            Debug.LogError("解析协议内容异常！" + e);
            Close();
            return;
        }

        if (msgBase == null)
        {
            Debug.LogError("解析" + protocol + "协议内容错误！");
            Close();
            return;
        }

        //整条协议解析完成，对数据需要处理
        byteArray.readIdx += bodyCount;
        byteArray.ChecheAndMoveBytes();
        Debug.Log("收到" + protocol + "消息，对消息进行处理");
        Debug.Log("信息类型：" + msgBase.ProtocolType.ToString());
        lock (m_MsgList)
        {
            m_MsgList.Add(msgBase);
            m_MsgCount++;
        }

        //一条完整的数据处理完成 粘包
        if (byteArray.Length > 4)
        {
            OnReceiveData();
        }
    }

    //协议监听
    public void AddProtoListener(ProtocolEnum protocolEnum, ProtoListener listener)
    {
        if (!m_ProtoDic.ContainsKey(protocolEnum))
        {
            m_ProtoDic[protocolEnum] = listener;
        }
    }

    //执行协议
    public void StartProto(ProtocolEnum protocolEnum, MsgBase msgBase)
    {
        if (m_ProtoDic.ContainsKey(protocolEnum))
        {
            //Debug.Log("信息类型2：" + msgBase.ProtocolType.ToString());
            m_ProtoDic[protocolEnum](msgBase);
        }
    }

    //发送数据
    //发送消息
    public void Send(MsgBase msgBase)
    {
        if (m_Socket == null || !m_Socket.Connected)
        {
            return;
        }

        try
        {
            byte[] nameBytes = MsgBase.EncodeName(msgBase);
            byte[] bodyBytes = MsgBase.Encode(msgBase);
            int len = nameBytes.Length + bodyBytes.Length;
            byte[] headBytes = BitConverter.GetBytes(len);
            byte[] sendBytes = new byte[len + headBytes.Length];
            Array.Copy(headBytes, 0, sendBytes, 0, headBytes.Length);
            Array.Copy(nameBytes, 0, sendBytes, headBytes.Length, nameBytes.Length);
            Array.Copy(bodyBytes, 0, sendBytes, headBytes.Length + nameBytes.Length, bodyBytes.Length);
            ByteArray ba = new ByteArray(sendBytes);
            int count = 0;
            lock (m_WriteQueue)
            {
                m_WriteQueue.Enqueue(ba);
                count = m_WriteQueue.Count;
            }

            if (count == 1)
            {
                m_Socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, SendCallback, m_Socket);
            }
        }
        catch (SocketException e)
        {
            Debug.LogError("发送数据异常！" + e);
            throw;
        }
    }

    //发送结束回调
    void SendCallback(IAsyncResult ar)
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState;
            if (socket == null || !socket.Connected)
            {
                return;
            }

            int count = socket.EndSend(ar); //向服务端发送了多少数据
            ByteArray ba;
            lock (m_WriteQueue)
            {
                ba = m_WriteQueue.First();
            }

            ba.readIdx += count;
            if (ba.Length == 0) //数据发送完整
            {
                lock (m_WriteQueue)
                {
                    m_WriteQueue.Dequeue();
                    if (m_WriteQueue.Count > 0)
                    {
                        ba = m_WriteQueue.First();
                    }
                    else
                    {
                        ba = null;
                    }
                }
            }

            if (ba != null)
            {
                socket.BeginSend(ba.Bytes, ba.readIdx, ba.Length, 0, SendCallback, socket);
            }
        }
        catch (SocketException e)
        {
            Debug.LogError("发送结束回调异常！" + e);
            Close();
        }
    }

    //关闭链接
    void Close()
    {
        if (m_Socket == null || !m_Socket.Connected)
        {
            return;
        }

        Secret = "";
        m_Socket.Close();

        if (m_Cts != null)
        {
            m_Cts.Cancel();
            if (m_MsgThread != null && m_MsgThread.IsAlive)
            {
                m_MsgThread.Join(3000);
                m_MsgThread = null;
            }
            if (m_HeartThread != null && m_HeartThread.IsAlive)
            {
                m_HeartThread.Join(3000);
                m_HeartThread = null;
            }
            m_Cts.Dispose();
            m_Cts = null;
        }

        Debug.Log("关闭链接！");
    }

    //计算时间戳
    public long GetTimeStamp()
    {
        TimeSpan timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(timeSpan.TotalSeconds);
    }


    //请求密钥回调的函数
    public void SCSecret(MsgBase msgBase)
    {
        Debug.Log("请求密钥回调函数监听成功");
        Secret = ((MsgSecret)msgBase).Secret;
        Debug.Log("收到密钥回执：" + ((MsgSecret)msgBase).Secret);
    }
}

}
