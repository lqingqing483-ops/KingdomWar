using System;
using System.Collections.Generic;
using UnityEngine;
namespace KingdomWar.UI
{
/// <summary>
/// UI面板类型枚举
/// </summary>
public enum UIPanelType
{
    settingPanel,
    bagPanel,
    mainPanel,
    taskPanel,
    shopPanel,
    GetGoodsPanel,
    deckPanel,
    battlePanel,
    settlementPanel,
    searchPanel,
    loadPanel,
    lotteryPanel,
    chestPanel,
    upgradePanel,
    profilePanel,
    seasonPassPanel,
    questPanel
}
[Serializable]
public class UIPanelTypeInfo : ISerializationCallbackReceiver
{
    public UIPanelType panelType;
    public string panelName;
    public string panelPath;

    public void OnBeforeSerialize()
    {
        //序列化之�?
    }
    public void OnAfterDeserialize()
    {
        //序列化之�?
        panelType = (UIPanelType)System.Enum.Parse(typeof(UIPanelType), panelName);
    }

}
[Serializable]
public class UIPanelTypeTypeJson
{
    public List<UIPanelTypeInfo> panelInfoList;

}

}
