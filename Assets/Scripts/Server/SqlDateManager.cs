using System.Collections.Generic;
using UnityEngine;
namespace KingdomWar.Server
{
public class SqlDateManager : MonoBehaviour
{
    public static SqlDateManager Instance;
    public int Id;

    void Awake()
    {
        Instance = this;
    }
    void Update()
    {
        // message processing
        NetManager.Instance.MsgUpdate();
        //if(Input.GetKeyDown(KeyCode.Z))
        //{
        //  PlayerStatus.Instance?.SavePlayerStatus();
        //}
        //if (Input.GetKeyDown(KeyCode.X))
        //{
        //    GetPlayerStatus();
        //}
        //if(Input.GetKeyDown(KeyCode.C))
        //{
        //  GetEquipData();
        //}
        //if(Input.GetKeyDown(KeyCode.V)){
        //  equipPanel.Instance.SaveEquipData();
        //}
    }

    //private void Start()
    //{
    //    GetData();
    //}
    ////save bag data
    //public void UpdateBagData(List<ItemData> items)
    //{
    //    Id = PlayerStatus.Instance.Id;
    //    ProtoManager.Instance.CSSaveBagData(Id, items, SaveBagDataCallback);
    //}
    ////get bag data
    //public void GetBagData()
    //{
    //    Id = PlayerStatus.Instance.Id;
    //    ProtoManager.Instance.CSGetBagData(Id, GetBagDataCallback);
    //}
    ////save player status
    //public void UpdatePlayerStatus(StatusData status)
    //{
    //    Id = PlayerStatus.Instance.Id;
    //    ProtoManager.Instance.CSSavePlayerStatus(Id, status, SaveDataCallback);
    //}
    ////get player status
    //public void GetPlayerStatus()
    //{
    //    Debug.Log("--------get player status");
    //    Id = PlayerStatus.Instance.Id;
    //    ProtoManager.Instance.CSGetPlayerStatus(Id, GetPlayerStatusCallback);
    //}
    ////save equip data
    //public void UpdateEquipData(List<int> items)
    //{
    //    Id = PlayerStatus.Instance.Id;
    //    ProtoManager.Instance.CSSaveEquipData(Id, items, SaveDataCallback);
    //}
    ////get equip data
    //public void GetEquipData()
    //{
    //    Debug.Log("--------get equip data");
    //    Id = PlayerStatus.Instance.Id;
    //    ProtoManager.Instance.CSGetEquipData(Id, GetEquipDataCallback);
    //}
    //public void GetData()
    //{
    //    GetPlayerStatus();
    //    GetBagData();
    //    GetEquipData();
    //}
    //private void GetBagDataCallback(GetBagDataResult result, List<ItemData> list)
    //{
    //    bagPanel.Instance.Init(list);
    //    switch (result)
    //    {
    //        case GetBagDataResult.Success:
    //            break;
    //        case GetBagDataResult.Failed:
    //            break;
    //        case GetBagDataResult.UserNotExist:
    //            break;
    //    }
    //}
    //private void SaveBagDataCallback(SaveBagDataResult result)
    //{
    //    switch (result)
    //    {
    //        case SaveBagDataResult.Success:
    //            break;
    //        case SaveBagDataResult.Failed:
    //            break;
    //        case SaveBagDataResult.UserNotExist:
    //            break;
    //    }
    //}
    //private void SaveDataCallback(MessageResult result)
    //{
    //    switch (result)
    //    {
    //        case MessageResult.Success:
    //            break;
    //        case MessageResult.Failed:
    //            break;
    //        case MessageResult.UserNotExist:
    //            break;
    //    }
    //}
    //private void GetPlayerStatusCallback(MessageResult result, StatusData status)
    //{
    //    PlayerStatus.Instance.Init(status);
    //    switch (result)
    //    {
    //        case MessageResult.Success:
    //            break;
    //        case MessageResult.Failed:
    //            break;
    //        case MessageResult.UserNotExist:
    //            break;
    //    }
    //}
    //private void GetEquipDataCallback(MessageResult result, List<int> list)
    //{
    //    equipPanel.Instance.Init(list);
    //    switch (result)
    //    {
    //        case MessageResult.Success:
    //            break;
    //        case MessageResult.Failed:
    //            break;
    //        case MessageResult.UserNotExist:
    //            break;
    //    }
    //}
}
}
