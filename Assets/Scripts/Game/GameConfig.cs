using UnityEngine;
namespace KingdomWar.Game
{
[CreateAssetMenu(fileName = "GameConfig", menuName = "Config/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("网络配置")]
    [Tooltip("TCP服务器IP")]
    public string serverIP = "127.0.0.1";
    [Tooltip("TCP Server Port")]
    public int serverPort = 6066;
    [Tooltip("默认公钥")]
    public string publicKey = "abc123";

    [Header("Hot Update Config")]
    [Tooltip("热更新服务器地址")]
    public string hotUpdateBaseUrl = "http://localhost:8080/";

    [Header("玩家默认数据")]
    [Tooltip("默认初始金币")]
    public int defaultGold = 1000;
    [Tooltip("默认初始宝石")]
    public int defaultGems = 100;
    [Tooltip("默认初始卡牌碎片")]
    public int defaultFragments = 0;

    private static GameConfig instance;
    public static GameConfig Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<GameConfig>("Config/GameConfig");
                if (instance == null)
                {
                    instance = CreateInstance<GameConfig>();
                    Debug.LogWarning("GameConfig not found, using defaults!");
                }
            }
            return instance;
        }
    }
}

}
