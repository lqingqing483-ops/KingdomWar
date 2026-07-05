using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using HybridCLR;
using System.Reflection;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using KingdomWar.Game;
using KingdomWar.UI;
namespace KingdomWar.HotUpdate
{
/// <summary>
/// 热更新管理器
/// 负责加载HotUpdate.dll并执行热更新
/// </summary>
public class HotUpdateManager : MonoBehaviour
{
    #region 单例模式
    
    private static HotUpdateManager instance;
    
    public static HotUpdateManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("HotUpdateManager");
                instance = obj.AddComponent<HotUpdateManager>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }
    
    #endregion

    #region 公共属�?    
    public bool IsUpdated { get; private set; } = false;
    public string CurrentVersion { get; private set; } = "1.0.0";
    public float DownloadProgress { get; private set; } = 0f;
    public string DownloadStatus { get; private set; } = "Preparing...";
    
    #endregion

    #region 私有字段
    
    private Assembly hotUpdateAssembly;
    private string BaseUrl => GameConfig.Instance.hotUpdateBaseUrl;
    private const int Timeout = 30;
    private string localAssetPath;
    private bool useRemoteServer = true;
    
    #endregion

    #region 事件定义
    
    public event Action OnHotUpdateComplete;
    public event Action<string> OnHotUpdateError;
    public event Action<float> OnDownloadProgress;
    
    #endregion

    #region 生命周期方法
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    #endregion

    #region 初始化方�?    
    public void Initialize()
    {
        LoadHotUpdateAssembly();
        Debug.Log("HotUpdateManager initialized");
    }

    private void LoadHotUpdateAssembly()
    {
#if !UNITY_EDITOR
        try
        {
            string dllPath = Path.Combine(Application.streamingAssetsPath, "HotUpdate.dll.bytes");
            if (File.Exists(dllPath))
            {
                hotUpdateAssembly = Assembly.Load(File.ReadAllBytes(dllPath));
                Debug.Log("HotUpdate assembly loaded from file");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load HotUpdate assembly: {e.Message}");
        }
#else
        hotUpdateAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "HotUpdate");
#endif
    }
    
    #endregion

    #region 热更新核心流�?    
    /// <summary>
    /// 执行热更�?    /// 从网络下载HotUpdate.dll并加�?    /// </summary>
    /// <returns>协程迭代�?/returns>
    public IEnumerator CheckAndUpdate()
    {
        Debug.Log("Starting hot update...");
        
        if (!useRemoteServer)
        {
            Debug.Log("Remote server not enabled, using local HotUpdate.dll");
            DownloadStatus = "Using local hot update";
            yield break;
        }
        
        string dllFileName = "HotUpdate.dll.bytes";
        string remoteUrl = BaseUrl + dllFileName;
        string localPath = string.Empty;
        bool downloadSuccess = false;
        UnityWebRequest request = null;
        
        // initialize local resource path
        try
        {
            localAssetPath = Path.Combine(Application.persistentDataPath, "HotUpdate");
            if (!Directory.Exists(localAssetPath))
            {
                Directory.CreateDirectory(localAssetPath);
            }
            
            localPath = Path.Combine(localAssetPath, dllFileName);
            
            DownloadStatus = "Downloading hot update resources...";
            DownloadProgress = 0f;
            OnDownloadProgress?.Invoke(DownloadProgress);
        }
        catch (Exception e)
        {
            string error = "Hot update failed: " + e.Message;
            Debug.LogError(error);
            DownloadStatus = "Hot update failed";
            OnHotUpdateError?.Invoke(error);
            yield break;
        }
        
        try
        {
            request = UnityWebRequest.Get(remoteUrl);
            request.timeout = Timeout;
            
            // 发送请�?            request.SendWebRequest();
        }
        catch (Exception e)
        {
            string error = "Create web request failed: " + e.Message;
            Debug.LogError(error);
            DownloadStatus = "Download failed";
            OnHotUpdateError?.Invoke(error);
            yield break;
        }
        
        // wait for download to complete and update progress
        while (!request.isDone)
        {
            if (request.downloadProgress >= 0)
            {
                try
                {
                    DownloadProgress = request.downloadProgress;
                    OnDownloadProgress?.Invoke(DownloadProgress);
                    DownloadStatus = "Downloading: " + Mathf.Round(DownloadProgress * 100) + "%";
                }
                catch (Exception e)
                {
                    Debug.LogError("Update progress failed: " + e.Message);
                }
            }
            yield return null;
        }
        
        // check download result
        try
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                // 保存下载的文�?                File.WriteAllBytes(localPath, request.downloadHandler.data);
                Debug.Log("HotUpdate.dll download success: " + localPath);
                downloadSuccess = true;
            }
            else
            {
                string error = "Download failed: " + request.error;
                Debug.LogError(error);
                DownloadStatus = "Download failed";
                OnHotUpdateError?.Invoke(error);
                yield break;
            }
        }
        catch (Exception e)
        {
            string error = "Process download result failed: " + e.Message;
            Debug.LogError(error);
            DownloadStatus = "Download failed";
            OnHotUpdateError?.Invoke(error);
            yield break;
        }
        finally
        {
            if (request != null)
            {
                request.Dispose();
            }
        }
        
        // verify file integrity
        bool fileValid = false;
        try
        {
            if (File.Exists(localPath))
            {
                long fileSize = new FileInfo(localPath).Length;
                if (fileSize > 0)
                {
                    Debug.Log($"File verified, size: {fileSize} bytes");
                    DownloadStatus = "File verified";
                    fileValid = true;
                }
                else
                {
                    string error = "Downloaded file is empty";
                    Debug.LogError(error);
                    DownloadStatus = "File verification failed";
                    OnHotUpdateError?.Invoke(error);
                    yield break;
                }
            }
            else
            {
                string error = "Downloaded file not found";
                Debug.LogError(error);
                DownloadStatus = "文件验证失败";
                OnHotUpdateError?.Invoke(error);
                yield break;
            }
        }
        catch (Exception e)
        {
            string error = "File verification failed: " + e.Message;
            Debug.LogError(error);
            DownloadStatus = "文件验证失败";
            OnHotUpdateError?.Invoke(error);
            yield break;
        }
        
        // 下载热修复DLL
        yield return StartCoroutine(DownloadHotfixDll());
        
        // 加载HotUpdate.dll
        if (fileValid)
        {
            try
            {
                hotUpdateAssembly = Assembly.Load(File.ReadAllBytes(localPath));
                Debug.Log("HotUpdate assembly loaded successfully");
                
                ApplyHotfixes();
            }
            catch (Exception e)
            {
                string error = "Failed to load HotUpdate.dll: " + e.Message;
                Debug.LogError(error);
                DownloadStatus = "Load failed";
                OnHotUpdateError?.Invoke(error);
                yield break;
            }
        }
        
        // 检查并更新Addressables资源
        yield return StartCoroutine(UpdateAddressablesResources());
        
        HasUpdateResource();

        // 更新状�?        IsUpdated = true;
        CurrentVersion = "1.0.1";
        DownloadStatus = "Hot update complete";
        DownloadProgress = 1f;
        OnDownloadProgress?.Invoke(DownloadProgress);
        Debug.Log("Hot update complete, version: " + CurrentVersion);
        
        OnHotUpdateComplete?.Invoke();
        
        yield return null;
    }
    
    #endregion

    #region 辅助方法
    
    /// <summary>
    /// 下载热修复DLL
    /// </summary>
    private IEnumerator DownloadHotfixDll()
    {
        string hotfixDllFileName = "Hotfix.dll.bytes";
        string hotfixRemoteUrl = BaseUrl + hotfixDllFileName;
        string hotfixLocalPath = Path.Combine(localAssetPath, hotfixDllFileName);
        
        Debug.Log($"Downloading hotfix DLL: {hotfixRemoteUrl}");
        DownloadStatus = "Downloading hotfix patch...";
        
        UnityWebRequest hotfixRequest = null;
        try
        {
            hotfixRequest = UnityWebRequest.Get(hotfixRemoteUrl);
            hotfixRequest.timeout = Timeout;
            hotfixRequest.SendWebRequest();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to create hotfix DLL download request: {e.Message}");
            yield break;
        }
        
        yield return hotfixRequest;
        
        try
        {
            if (hotfixRequest.result == UnityWebRequest.Result.Success)
            {
                File.WriteAllBytes(hotfixLocalPath, hotfixRequest.downloadHandler.data);
                Debug.Log($"Hotfix DLL downloaded: {hotfixLocalPath}");
            }
            else
            {
                Debug.LogWarning($"Hotfix DLL download failed: {hotfixRequest.error}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to save hotfix DLL: {e.Message}");
        }
        finally
        {
            if (hotfixRequest != null)
            {
                hotfixRequest.Dispose();
            }
        }
    }
    
    /// <summary>
    /// 应用热修复补�?    /// 加载并执行热修复DLL中的修复方法
    /// </summary>
    private void ApplyHotfixes()
    {
        Debug.Log("[Hotfix] Applying hotfix...");
        
#if ENABLE_HYBRIDCLR
        try
        {
            string hotfixDllPath = Path.Combine(localAssetPath, "Hotfix.dll.bytes");
            
            if (!File.Exists(hotfixDllPath))
            {
                hotfixDllPath = Path.Combine(Application.persistentDataPath, "HotUpdate", "Hotfix.dll.bytes");
            }
            
            if (!File.Exists(hotfixDllPath))
            {
                Debug.LogWarning("[Hotfix] Hotfix DLL not found, skipping");
                return;
            }
            
            byte[] hotfixDllBytes = File.ReadAllBytes(hotfixDllPath);
            if (hotfixDllBytes == null || hotfixDllBytes.Length == 0)
            {
                Debug.LogWarning("[Hotfix] Hotfix DLL is empty, skipping");
                return;
            }
            
            Assembly hotfixAssembly = Assembly.Load(hotfixDllBytes);
            if (hotfixAssembly == null)
            {
                Debug.LogError("[Hotfix] Failed to load hotfix DLL");
                return;
            }
            
            Type hotfixType = hotfixAssembly.GetType("HotfixLogic.LotterySystemHotfix");
            if (hotfixType == null)
            {
                Debug.LogWarning("[Hotfix] LotterySystemHotfix type not found");
                return;
            }
            
            MethodInfo applyMethod = hotfixType.GetMethod("ApplyHotfix");
            if (applyMethod == null)
            {
                Debug.LogWarning("[Hotfix] ApplyHotfix method not found");
                return;
            }
            
            applyMethod.Invoke(null, null);
            Debug.Log("[Hotfix] Hotfix applied successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Hotfix] Failed to apply hotfix: {e.Message}\n{e.StackTrace}");
        }
#else
        Debug.LogWarning("[Hotfix] HybridCLR not enabled, skipping hotfix");
#endif
    }
    
    /// <summary>
    /// 检查是否有热更新资�?    /// </summary>
    /// <returns>存在返回true，否则返回false</returns>
    public bool HasUpdateResource()
    {
        string localAssetPath = Path.Combine(Application.persistentDataPath, "HotUpdate");
        string dllPath = Path.Combine(localAssetPath, "HotUpdate.dll.bytes");
        return File.Exists(dllPath);
    }
    
    /// <summary>
    /// 更新Addressables资源
    /// 检查并下载更新的资源包
    /// </summary>
    /// <returns>协程迭代�?/returns>
    private IEnumerator UpdateAddressablesResources()
    {
        DownloadStatus = "Checking resource updates...";
        Debug.Log("Checking Addressables resource updates");
        
        if (!useRemoteServer)
        {
            Debug.Log("Remote server not enabled, using local Addressables resources");
            DownloadStatus = "Using local resources";
            yield break;
        }
        
        var checkForUpdateOperation = Addressables.CheckForCatalogUpdates(false);
        yield return checkForUpdateOperation;
        
        if (checkForUpdateOperation.Status == AsyncOperationStatus.Succeeded)
        {
            var catalogsToUpdate = checkForUpdateOperation.Result;
            if (catalogsToUpdate != null && catalogsToUpdate.Count > 0)
            {
                Debug.Log("Found " + catalogsToUpdate.Count + " catalogs to update");
                DownloadStatus = "Downloading resource updates...";
                OnDownloadProgress?.Invoke(0.9f);
                
                var updateOperation = Addressables.UpdateCatalogs(catalogsToUpdate, false);
                yield return updateOperation;
                
                if (updateOperation.Status == AsyncOperationStatus.Succeeded)
                {
                    Debug.Log("Addressables resources updated");
                    DownloadStatus = "Resource update complete";
                    
                    ClearUICache();
                }
                else
                {
                    Debug.LogError("Addressables资源更新失败: " + updateOperation.OperationException);
                    DownloadStatus = "Resource update failed";
                }
            }
            else
            {
                Debug.Log("No Addressables resource updates found");
                DownloadStatus = "Resources are up to date";
            }
        }
        else
        {
            Debug.LogError("检查Addressables资源更新失败: " + checkForUpdateOperation.OperationException);
            DownloadStatus = "Check resource update failed";
        }
        
        Addressables.Release(checkForUpdateOperation);
    }

    /// <summary>
    /// 清除UI缓存，使下次加载获取最新资�?    /// </summary>
    private void ClearUICache()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ClearPanelCache(UIPanelType.lotteryPanel);
            UIManager.Instance.ClearPanelCache(UIPanelType.mainPanel);
            UIManager.Instance.ClearPanelCache(UIPanelType.battlePanel);
            Debug.Log("UI cache cleared");
        }
    }
    public void UpdateAddressables()
    {
        GameObject obj = GameObject.Find("GameBg");
        Image img = obj.GetComponent<Image>();
        Addressables.LoadAssetAsync<Sprite>("Assets/Epic Toon FX/Demo/Textures/2D Sprites/devsprite.png").Completed += (obj) =>
          {
            if(img != null)
            {
                // 图片
                Sprite sprite = obj.Result;
                img.sprite = sprite;
            }
          };
    }
    #endregion
}

}
