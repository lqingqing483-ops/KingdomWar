using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KingdomWar.Game;
namespace KingdomWar.Server
{
public class ServerRoot : MonoBehaviour {

	// Use this for initialization
	void Start () {
		var cfg = GameConfig.Instance;
		NetManager.Instance.Connect(cfg.serverIP, cfg.serverPort);
	}
	
	// Update is called once per frame
	void Update ()
	{
		NetManager.Instance.MsgUpdate();


		// if (Input.GetKeyDown(KeyCode.A))
		// {
		// 	//ProtoManager.Instance.CSTest();
		// 	ProtoManager.Instance.CSRegister("123","456",RegisterCallback);
		// }

		// if (Input.GetKeyDown(KeyCode.Q))
		// {
		// 	ProtoManager.Instance.CSLogin("123","45667",LoginCallback);

		// }
	}

	void RegisterCallback(RegisterResult result)
	{
		switch (result)
		{
			case RegisterResult.Success:
				Debug.Log("注册成功！");
				break;
			case RegisterResult.Failed:
				Debug.Log("注册失败！");
				break;
			case RegisterResult.AlreadyExist:
				Debug.Log("用户已存在！");
				break;
			case RegisterResult.WrongCode:
				Debug.Log("验证码错误！");
				break;
		}
	}
	void LoginCallback(LoginResult result)
	{
		switch (result)
		{
			case LoginResult.Success:
				Debug.Log("登录成功！");
				break;
			case LoginResult.Failed:
				Debug.Log("登录失败！");
				break;
			case LoginResult.UserNotExist:
				Debug.Log("用户不存在！");
				break;
			case LoginResult.PwdWrong:
				Debug.Log("密码错误！");
				break;
		}
	}
}

}
