using System;
using System.Collections.Generic;
using UnityEngine;

namespace HotfixLogic
{
    /// <summary>
    /// 抽奖系统热修复类
    /// 用于修复抽奖系统的 bug
    /// 
    /// 使用说明：
    /// 使用 HybridCLR.HotfixAssembly 方法进行方法替换
    /// 需要创建 HotfixManifest 配置文件来指定要修复的方法
    /// 
    /// 注意：HotfixManifest 和 RuntimeApi 只在 HybridCLR 构建环境中存在
    /// 在 Unity 编辑器中会显示为错误，这是正常的
    /// </summary>
    public static class LotterySystemHotfix
    {
        /// <summary>
        /// 热修复配置文件（XML 格式）
        /// </summary>
        private const string HOTFIX_MANIFEST_XML = @"
<manifest>
    <assembly fullname=""Assembly-CSharp"">
        <type fullname=""LotterySystem"">
            <method name=""SpendGold"" />
            <method name=""CanDraw"" />
        </type>
    </assembly>
</manifest>";

        /// <summary>
        /// 应用热修复
        /// 在游戏启动时调用此方法来注册所有热修复
        /// </summary>
        public static void ApplyHotfix()
        {
            Debug.Log("[Hotfix] 开始应用抽奖系统热修复...");
            
#if ENABLE_HYBRIDCLR
            try
            {
                byte[] hotfixDllBytes = LoadHotfixDll();
                if (hotfixDllBytes == null || hotfixDllBytes.Length == 0)
                {
                    Debug.LogWarning("[Hotfix] 未找到热修复 DLL，跳过修复");
                    return;
                }
                
                var manifest = HotfixManifest.LoadFrom(HOTFIX_MANIFEST_XML, assName => null);
                
                HybridCLR.RuntimeApi.HotfixAssembly("Assembly-CSharp", hotfixDllBytes, manifest);
                
                Debug.Log("[Hotfix] 抽奖系统热修复应用成功");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Hotfix] 应用热修复失败：{e.Message}\n{e.StackTrace}");
            }
#else
            Debug.LogWarning("[Hotfix] HybridCLR 未启用，跳过热修复");
            Debug.LogWarning("[Hotfix] 如果需要热修复功能，请在 HybridCLR 构建环境中运行");
#endif
        }
        
        /// <summary>
        /// 加载热修复 DLL
        /// 从本地路径加载热修复 DLL
        /// </summary>
        private static byte[] LoadHotfixDll()
        {
            string[] possiblePaths = new string[]
            {
                $"{Application.persistentDataPath}/HotUpdate/Hotfix.dll.bytes",
                $"{Application.persistentDataPath}/HotUpdate/Hotfix.dll",
                $"{Application.streamingAssetsPath}/HotUpdate/Hotfix.dll.bytes",
                $"{Application.streamingAssetsPath}/HotUpdate/Hotfix.dll"
            };
            
            foreach (string path in possiblePaths)
            {
                try
                {
                    if (System.IO.File.Exists(path))
                    {
                        byte[] dllBytes = System.IO.File.ReadAllBytes(path);
                        if (dllBytes != null && dllBytes.Length > 0)
                        {
                            Debug.Log($"[Hotfix] 从本地加载热修复 DLL 成功: {path}, 大小: {dllBytes.Length} bytes");
                            return dllBytes;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Hotfix] 尝试加载 {path} 失败: {e.Message}");
                }
            }
            
            Debug.LogWarning("[Hotfix] 未找到热修复 DLL 文件");
            return null;
        }
    }
    
    /// <summary>
    /// 热修复方法实现类
    /// 这些方法会被 HotfixManifest 引用
    /// 
    /// 注意：修复方法的参数使用 object 类型，避免直接引用主程序集的类型
    /// 在运行时，HybridCLR 会自动将 object 参数转换为目标类型
    /// </summary>
    public static class LotterySystemFixMethods
    {
        /// <summary>
        /// 修复后的 SpendGold 方法
        /// 抽奖时不在消耗金币
        /// 
        /// 修复内容：
        /// - 成功抽奖
        /// - 不消耗金币
        /// </summary>
        public static void SpendGold(object instance)
        {
            Debug.Log("[热更拦截] 捕获到 SpendGold 调用");
            
            // 修复：不消耗金币，直接返回
            // 原方法会调用 PlayerDataManager.Instance.SpendGold(drawCost)
            // 现在跳过这个调用
            
            Debug.Log("[热更执行] 玩家获得：免费抽奖（不消耗金币）");
        }
        
        /// <summary>
        /// 修复后的 CanDraw 方法
        /// 抽奖时总是允许抽奖
        /// 
        /// 
        /// 修复内容：
        /// - 总是返回 true，允许抽奖
        /// </summary>
        public static bool CanDraw(object self)
        {
            Debug.Log("[热更拦截] 捕获到 CanDraw 调用");
            // 修复：总是返回 true，允许抽奖
            return true;
        }
    }
}