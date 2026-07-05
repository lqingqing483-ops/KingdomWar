using System;
using System.Collections.Generic;
using UnityEngine;
using KingdomWar.Tools;
namespace KingdomWar.Server
{
public class ProtoManager : Singleton<ProtoManager>
{
    //请求密钥
    public void CSSecret()
    {
        MsgSecret msgSecret = new MsgSecret();
        NetManager.Instance.Send(msgSecret);
        // NetManager.Instance.AddProtoListener(ProtocolEnum.MsgSecret, (resMsg) =>
        // {
        //     NetManager.Instance.Secret = ((MsgSecret)resMsg).Secret;
        //     Debug.Log("收到密钥回执："+((MsgSecret)resMsg).Secret);
        // });
        NetManager.Instance.AddProtoListener(ProtocolEnum.MsgSecret, NetManager.Instance.SCSecret);
    }
    //测试请求
    public void CSTest()
    {
        MsgTest msg = new MsgTest();
        msg.CSContent = "abcdefg";
        NetManager.Instance.Send(msg);
        NetManager.Instance.AddProtoListener(ProtocolEnum.MsgTest, (resMsg) =>
        {
            Debug.Log("接受到服务器返回的信息：" + (MsgTest)resMsg);
        });
    }
    //注册请求
    public void CSRegister(string userName, string pwd, Action<RegisterResult> callback)
    {
        MsgRegister msgRegister = new MsgRegister();
        msgRegister.Account = userName;
        msgRegister.Password = pwd;
        NetManager.Instance.Send(msgRegister);
        NetManager.Instance.AddProtoListener(ProtocolEnum.MsgRegister, (resMsg) =>
        {
            MsgRegister msg = (MsgRegister)resMsg;
            callback(msg.registerResult);
        });
    }
    //登录请求
    public void CSLogin(string userName, string pwd, Action<LoginResult, int> callback)
    {
        MsgLogin msgLogin = new MsgLogin();
        msgLogin.Account = userName;
        msgLogin.Password = pwd;
        NetManager.Instance.Send(msgLogin);
        NetManager.Instance.AddProtoListener(ProtocolEnum.MsgLogin, (resMsg) =>
        {
            MsgLogin msg = (MsgLogin)resMsg;
            callback(msg.loginResult, msg.UserId);
        });
    }

    //存储背包数据
    public void CSSaveBagData(int userid, List<ItemData> items, Action<SaveBagDataResult> callback)
    {
        MsgSaveBagData bagDate = new MsgSaveBagData();
        bagDate.UserId = userid;
        bagDate.Items = items;
        NetManager.Instance.Send(bagDate);
        NetManager.Instance.AddProtoListener(ProtocolEnum.MsgSaveBagData, (resMsg) =>
        {
            MsgSaveBagData msg = (MsgSaveBagData)resMsg;
            Debug.Log("存储背包数据结果：" + msg.saveBagDataResult);
            callback(msg.saveBagDataResult);
        });
    }
    //获取背包数据
    public void CSGetBagData(int userid, Action<GetBagDataResult, List<ItemData>> callback)
    {

        MsgGetBagData bagDate = new MsgGetBagData();
        bagDate.UserId = userid;
        NetManager.Instance.Send(bagDate);
        Debug.Log("发送获取背包数据请求");
        NetManager.Instance.AddProtoListener(ProtocolEnum.MsgGetBagData, (resMsg) =>
        {
            Debug.Log("收到获取背包数据结果");
            MsgGetBagData msg = (MsgGetBagData)resMsg;
            callback(msg.getBagDataResult, msg.Items);
        });
    }

    //存储状态数据
    public void CSSavePlayerStatus(int userid, StatusData Status, Action<MessageResult> callback)
    {
        MsgSavePlayerStatus playerStatus = new MsgSavePlayerStatus();
        playerStatus.UserId = userid;
        playerStatus.StatusData = Status;
        NetManager.Instance.Send(playerStatus);
        NetManager.Instance.AddProtoListener(ProtocolEnum.MsgSavePlayerStatus, (resMsg) =>
        {
            MsgSavePlayerStatus msg = (MsgSavePlayerStatus)resMsg;
            Debug.Log("存储状态数据结果：" + msg.messageResult);
            callback(msg.messageResult);
        });
    }
    //获取状态数据
    public void CSGetPlayerStatus(int userid, Action<MessageResult, StatusData> callback)
    {

        MsgGetPlayerStatus playerStatus = new MsgGetPlayerStatus();
        playerStatus.UserId = userid;
        NetManager.Instance.Send(playerStatus);
        Debug.Log("发送获取状态数据请求");
        NetManager.Instance.AddProtoListener(ProtocolEnum.MsgGetPlayerStatus, (resMsg) =>
        {
            Debug.Log("收到获取状态数据结果");
            MsgGetPlayerStatus msg = (MsgGetPlayerStatus)resMsg;
            callback(msg.messageResult, msg.StatusData);
        });
    }

    // 存储装备数据
    public void CSSaveEquipData(int userid, List<int> items, Action<MessageResult> callback)
    {
        MsgSaveEquipData euipData = new MsgSaveEquipData();
        euipData.UserId = userid;
        euipData.EquipsId = items;
        NetManager.Instance.Send(euipData);
        NetManager.Instance.AddProtoListener(ProtocolEnum.MsgSaveEquipData, (resMsg) =>
        {
            MsgSaveEquipData msg = (MsgSaveEquipData)resMsg;
            Debug.Log("存储装备数据结果：" + msg.messageResult);
            callback(msg.messageResult);
        });
    }

    // 获取装备数据
    public void CSGetEquipData(int userid, Action<MessageResult, List<int>> callback)
    {
        MsgGetEquipData euipData = new MsgGetEquipData();
        euipData.UserId = userid;
        NetManager.Instance.Send(euipData);
        Debug.Log("发送获取装备数据请求");
        NetManager.Instance.AddProtoListener(ProtocolEnum.MsgGetEquipData, (resMsg) =>
        {
            Debug.Log("收到获取装备数据结果");
            MsgGetEquipData msg = (MsgGetEquipData)resMsg;
            callback(msg.messageResult, msg.EquipsId);
        });
    }
}

}
