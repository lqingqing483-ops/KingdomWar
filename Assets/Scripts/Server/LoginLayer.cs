using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using KingdomWar.Load;
using KingdomWar.UI;
namespace KingdomWar.Server
{
public class LoginLayer : MonoBehaviour
{
    private Text accountText;
    private InputField accountInput;
    private Text passwordText;
    private InputField passwordInput;
    private Button loginButton;
    private Button registerButton;
    private string account;
    private string password;
    protected RectTransform panel;
    public AsyncSceneLoader asyncSceneLoader;
    void Awake()
    {
        accountText = transform.Find("accountText").GetComponent<Text>();
        accountInput = transform.Find("accountText/acounntIput").GetComponent<InputField>();
        passwordText = transform.Find("posswordText").GetComponent<Text>();
        passwordInput = transform.Find("posswordText/passwordIput").GetComponent<InputField>();
        loginButton = transform.Find("LoginBtn").GetComponent<Button>();
        registerButton = transform.Find("RegisterBtn").GetComponent<Button>();

        accountInput.onEndEdit.AddListener(OnAccountSubmitted);
        passwordInput.onEndEdit.AddListener(OnPasswordSubmitted);
        loginButton.onClick.AddListener(onClickLogin);
        registerButton.onClick.AddListener(onClickRegister);

        //连接服务�?
        NetManager.Instance.Connect("127.0.0.1", 6066);
    }
    void Start()
    {
        panel = this.GetComponent<RectTransform>();
        Tweener tweener = panel.DOLocalMove(Vector3.zero, 1);
        //asyncSceneLoader = GameObject.Find("SceneLoader").GetComponent<AsyncSceneLoader>();
        //Tween tween = this.transform.DOLocalMove(Vector3.zero, 1f);
        //tween.SetAutoKill(false);
        //tween.Pause();
    }

    // 登录
    private void onClickRegister()
    {
        ProtoManager.Instance.CSRegister(account, password, RegisterCallback);
    }

    // 注册
    private void onClickLogin()
    {
        ProtoManager.Instance.CSLogin(account, password, LoginCallback);
    }

    private void OnPasswordSubmitted(string arg0)
    {
        password = arg0;
    }

    private void OnAccountSubmitted(string arg0)
    {
        account = arg0;
    }

    void Update()
    {
        // 消息处理    
        NetManager.Instance.MsgUpdate();
    }

    void RegisterCallback(RegisterResult result)
    {
        switch (result)
        {
            case RegisterResult.Success:
                CreatepromptMessage("Registration successful");
                break;
            case RegisterResult.Failed:
                CreatepromptMessage("Registration failed!");
                break;
            case RegisterResult.AlreadyExist:
                CreatepromptMessage("用户已存在！");
                break;
            case RegisterResult.WrongCode:
                CreatepromptMessage("验证码错误！");
                break;
        }
    }
    void LoginCallback(LoginResult result, int userId)
    {
        switch (result)
        {
            case LoginResult.Success:
                CreatepromptMessage("Login successful");
                //PlayerStatus.Instance.Id = userId;
                CreatepromptMessage("User ID: " + userId);
                //跳转场景
                //SceneManager.LoadScene("mainScene");
                this.transform.gameObject.SetActive(false);
                asyncSceneLoader.transform.gameObject.SetActive(true);

                break;
            case LoginResult.Failed:
                CreatepromptMessage("Login failed");
                break;
            case LoginResult.UserNotExist:
                CreatepromptMessage("用户不存在！");
                break;
            case LoginResult.PwdWrong:
                CreatepromptMessage("Wrong password");
                break;
        }
    }
    public void CreatepromptMessage(string msg, Transform parent = null)
    {
        UIManager.Instance.CreatepromptMessage(msg, parent);
    }
}

}
