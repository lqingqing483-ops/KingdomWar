using UnityEngine;
using HotfixLogic;

public class HotUpdateInit
{
    public static void Initialize()
    {
        Debug.Log("HotUpdateInit.Initialize() called");
        
        // 应用热修复
        LotterySystemHotfix.ApplyHotfix();
        
        Debug.Log("HotUpdate initialized successfully");
    }
}