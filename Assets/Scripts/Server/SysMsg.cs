using ProtoBuf;
using System;
using System.Collections.Generic;
namespace KingdomWar.Server
{
[ProtoContract]
public class MsgSecret : MsgBase
{
    public MsgSecret()
    {
        ProtocolType = ProtocolEnum.MsgSecret;
    }
    [ProtoMember(1)]
    public override ProtocolEnum ProtocolType { get; set; }
    [ProtoMember(2)]
    public string Secret { get; set; }
}
[ProtoContract]
public class MsgPing : MsgBase
{
    public MsgPing()
    {
        ProtocolType = ProtocolEnum.MsgPing;
    }
    [ProtoMember(1)]
    public override ProtocolEnum ProtocolType { get; set; }
}
[ProtoContract]
public class MsgTest : MsgBase
{
    public MsgTest()
    {
        ProtocolType = ProtocolEnum.MsgTest;
    }
    [ProtoMember(1)]
    public override ProtocolEnum ProtocolType { get; set; }
    [ProtoMember(2)]

    public string SCContent { get; set; }
    [ProtoMember(3)]

    public string CSContent { get; set; }
}

[ProtoContract]
public class MsgRegister : MsgBase
{
    public MsgRegister()
    {
        ProtocolType = ProtocolEnum.MsgRegister;
    }
    [ProtoMember(1)]
    public override ProtocolEnum ProtocolType { get; set; }
    [ProtoMember(2)]

    public string Account { get; set; }
    [ProtoMember(3)]

    public string Password { get; set; }
    [ProtoMember(4)]
    public RegisterResult registerResult { get; set; }
}

[ProtoContract]
public class MsgLogin : MsgBase
{
    public MsgLogin()
    {
        ProtocolType = ProtocolEnum.MsgLogin;
    }
    [ProtoMember(1)]
    public override ProtocolEnum ProtocolType { get; set; }
    [ProtoMember(2)]

    public string Account { get; set; }
    [ProtoMember(3)]

    public string Password { get; set; }
    [ProtoMember(4)]
    public LoginResult loginResult { get; set; }
    [ProtoMember(5)]
    public int UserId { get; set; }
}

//保存背包数据
[ProtoContract]
public class MsgSaveBagData : MsgBase
{
    public MsgSaveBagData()
    {
        ProtocolType = ProtocolEnum.MsgSaveBagData;
    }
    [ProtoMember(1)]
    public override ProtocolEnum ProtocolType { get; set; }

    [ProtoMember(2)]
    public int UserId { get; set; }

    [ProtoMember(3)]
    public List<ItemData> Items { get; set; }

    [ProtoMember(4)]
    public SaveBagDataResult saveBagDataResult { get; set; }

}

[ProtoContract]
public class ItemData
{
    public ItemData() { }

    [ProtoMember(1)]
    public int ItemId { get; set; }
    [ProtoMember(2)]
    public int Count { get; set; }
}

//获取背包数据
[ProtoContract]
public class MsgGetBagData : MsgBase
{
    public MsgGetBagData()
    {
        ProtocolType = ProtocolEnum.MsgGetBagData;
    }
    [ProtoMember(1)]
    public override ProtocolEnum ProtocolType { get; set; }

    [ProtoMember(2)]
    public int UserId { get; set; }

    [ProtoMember(3)]
    public List<ItemData> Items { get; set; }

    [ProtoMember(4)]
    public GetBagDataResult getBagDataResult { get; set; }

}
//状态数据
[ProtoContract]
public class StatusData
{
    public StatusData() { }

    [ProtoMember(1)]
    public int Level { get; set; }
    [ProtoMember(2)]
    public int Exp { get; set; }
    [ProtoMember(3)]
    public int Golds { get; set; }
}

//保存状态数据
[ProtoContract]
public class MsgSavePlayerStatus : MsgBase
{
    public MsgSavePlayerStatus()
    {
        ProtocolType = ProtocolEnum.MsgSavePlayerStatus;
    }
    [ProtoMember(1)]
    public override ProtocolEnum ProtocolType { get; set; }

    [ProtoMember(2)]
    public int UserId { get; set; }

    [ProtoMember(3)]
    public StatusData StatusData { get; set; }

    [ProtoMember(4)]
    public MessageResult messageResult { get; set; }

}

//获取状态数据
[ProtoContract]
public class MsgGetPlayerStatus : MsgBase
{
    public MsgGetPlayerStatus()
    {
        ProtocolType = ProtocolEnum.MsgGetPlayerStatus;
    }
    [ProtoMember(1)]
    public override ProtocolEnum ProtocolType { get; set; }

    [ProtoMember(2)]
    public int UserId { get; set; }

    [ProtoMember(3)]
    public StatusData StatusData { get; set; }

    [ProtoMember(4)]
    public MessageResult messageResult { get; set; }

}

//保存装备数据
[ProtoContract]
public class MsgSaveEquipData : MsgBase
{
    public MsgSaveEquipData()
    {
        ProtocolType = ProtocolEnum.MsgSaveEquipData;
    }
    [ProtoMember(1)]
    public override ProtocolEnum ProtocolType { get; set; }

    [ProtoMember(2)]
    public int UserId { get; set; }

    [ProtoMember(3)]
    public List<int> EquipsId { get; set; }

    [ProtoMember(4)]
    public MessageResult messageResult { get; set; }

}

//获取装备数据
[ProtoContract]
public class MsgGetEquipData : MsgBase
{
    public MsgGetEquipData()
    {
        ProtocolType = ProtocolEnum.MsgGetEquipData;
    }
    [ProtoMember(1)]
    public override ProtocolEnum ProtocolType { get; set; }

    [ProtoMember(2)]
    public int UserId { get; set; }

    [ProtoMember(3)]
    public List<int> EquipsId { get; set; }

    [ProtoMember(4)]
    public MessageResult messageResult { get; set; }

}

}
