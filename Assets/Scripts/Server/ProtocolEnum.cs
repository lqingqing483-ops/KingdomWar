using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace KingdomWar.Server
{
public enum ProtocolEnum
{
    None = 0,
    MsgSecret = 1,
    MsgPing = 2,
    MsgTest=3,
    MsgRegister=4,
    MsgLogin=5,
    MsgSaveBagData = 6,
    MsgGetBagData = 7,
    MsgSavePlayerStatus = 8,
    MsgGetPlayerStatus = 9,
    MsgSaveEquipData = 10,
    MsgGetEquipData = 11
}

public enum RegisterResult
{
    Success,
    Failed,
    AlreadyExist,
    WrongCode
}
public enum LoginResult
{
    Success,
    Failed,
    PwdWrong,
    UserNotExist
}
public enum SaveBagDataResult
{
    Success,
    Failed,
    UserNotExist
}

public enum GetBagDataResult
{
    Success,
    Failed,
    UserNotExist
}
public enum MessageResult
{
    Success,
    Failed,
    UserNotExist
}

public class LoginResponse
{
    public LoginResult Result { get; set; }
    public int UserId { get; set; }

    public static LoginResponse Success(int userId)
    {
        return new LoginResponse() { Result = LoginResult.Success, UserId = userId };
    }

    public static LoginResponse Failed(LoginResult result)
    {
        return new LoginResponse() { Result = result, UserId = 0 };
    }
}
public class GetBagDataResponse
{
    public GetBagDataResult Result { get; set; }
    public List<ItemData> items{ get; set; }

    public static GetBagDataResponse Success(List<ItemData> resitems)
    {
        return new GetBagDataResponse() { Result = GetBagDataResult.Success, items = resitems };
    }

    public static GetBagDataResponse Failed(GetBagDataResult getBagDataResult)
    {
        return new GetBagDataResponse() { Result = getBagDataResult, items=new List<ItemData>() };
    }
}
//获取玩家状态
public class GetStatusDataResponse
{
    public MessageResult Result { get; set; }
    public StatusData StatusData{ get; set; }

    public static GetStatusDataResponse Success(StatusData resitems)
    {
        return new GetStatusDataResponse() { Result = MessageResult.Success, StatusData = resitems };
    }

    public static GetStatusDataResponse Failed(MessageResult messageResult)
    {
        return new GetStatusDataResponse() { Result = messageResult, StatusData=new StatusData() };
    }
}
//获取装备数据
public class GetEquipDataResponse
{
    public MessageResult Result { get; set; }
    public List<int> items{ get; set; }

    public static GetEquipDataResponse Success(List<int> resitems)
    {
        return new GetEquipDataResponse() { Result = MessageResult.Success, items = resitems };
    }

    public static GetEquipDataResponse Failed(MessageResult messageResult)
    {
        return new GetEquipDataResponse() { Result = messageResult, items=new List<int>() };
    }
}

}
