using UnityEngine;

namespace KingdomWar.Game.Config
{
[CreateAssetMenu(fileName = "EconomyBalance", menuName = "Config/Economy Balance")]
public class EconomyBalanceSO : ScriptableObject
{
    [Header("===== Player Defaults =====")]
    public int defaultGold = 1000;
    public int defaultGems = 100;

    [Header("===== Shop Settings =====")]
    public int shopDailyCardCount = 3;
    public int shopCardBasePrice = 100;
    public int shopCardPriceMultiplier = 100;

    [Header("===== Free Chest =====")]
    public int freeChestCooldownSeconds = 14400;
    public int freeChestGold = 50;
    public int freeChestGems = 0;
    public int freeChestCardCount = 1;
    public int[] freeChestRarityWeights = new int[] { 80, 15, 5, 0 };

    [Header("===== Victory Chest =====")]
    public int victoryChestGold = 200;
    public int victoryChestGems = 10;
    public int victoryChestCardCount = 3;
    public int victoryChestUnlockMinutes = 240;
    public int[] victoryChestRarityWeights = new int[] { 60, 30, 10, 0 };

    [Header("===== Chest SpeedUp =====")]
    public int chestSpeedUpGemCostPerMinute = 1;

    [Header("===== Battle Rewards =====")]
    public int battleWinGold = 50;
    public int battleLoseGold = 10;
    public int battleDrawGold = 20;

    private static EconomyBalanceSO instance;
    public static EconomyBalanceSO Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<EconomyBalanceSO>("Config/EconomyBalance");
                if (instance == null)
                {
                    instance = CreateInstance<EconomyBalanceSO>();
                    Debug.LogWarning("EconomyBalanceSO not found in Resources/Config/, using defaults!");
                }
            }
            return instance;
        }
    }
}
}
