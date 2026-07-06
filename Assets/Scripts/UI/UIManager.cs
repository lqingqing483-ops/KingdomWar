using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using KingdomWar.Tools;
namespace KingdomWar.UI
{
public class UIManager
{
    private Dictionary<UIPanelType, string> panelPathDic = new Dictionary<UIPanelType, string>();
    private Dictionary<UIPanelType, basePanel> panelObjDic = new Dictionary<UIPanelType, basePanel>();
    private Stack<basePanel> panelStack = new Stack<basePanel>();

    private Transform Tips;
    private bool initialized;

    #region 单例
    private static UIManager instance;
    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new UIManager();
            }
            return instance;
        }
    }
    private UIManager() { }
    #endregion

    public void Initialize()
    {
        if (initialized) return;
        initialized = true;
        ParseUITextSync();
        LoadCanvasSync();
    }

    public void InitializeAsync(Action<bool> onComplete)
    {
        if (initialized)
        {
            onComplete?.Invoke(true);
            return;
        }
        initialized = true;
        ParseUITextSync();
        LoadCanvasAsync(onComplete);
    }

    private Transform canvas;
    public Transform Canvas
    {
        get
        {
            if (canvas == null)
            {
                Initialize();
            }
            return canvas;
        }
    }

    private void LoadCanvasSync()
    {
        var prefab = Resources.Load<GameObject>("Prefabs/UIPrefab/Canvas");
        if (prefab != null)
        {
            canvas = GameObject.Instantiate(prefab).transform;
            Tips = canvas.Find("Tips");
        }
    }

    private void LoadCanvasAsync(Action<bool> onComplete)
    {
        var loadOperation = Addressables.LoadAssetAsync<GameObject>("Prefabs/UIPrefab/Canvas");
        loadOperation.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null)
            {
                canvas = GameObject.Instantiate(op.Result).transform;
                Tips = canvas.Find("Tips");
                onComplete?.Invoke(true);
            }
            else
            {
                try
                {
                    canvas = GameObject.Instantiate(Resources.Load<Transform>("Prefabs/UIPrefab/Canvas").transform);
                    Tips = canvas.Find("Tips");
                    onComplete?.Invoke(true);
                }
                catch (Exception e)
                {
                    Debug.LogError($"加载Canvas失败: {e.Message}");
                    onComplete?.Invoke(false);
                }
            }
        };
    }

    private void ParseUITextSync()
    {
        TextAsset config = LoadConfigSync();
        if (config != null)
        {
            UIPanelTypeTypeJson uiObjectJson = JsonUtility.FromJson<UIPanelTypeTypeJson>(config.text);
            foreach (UIPanelTypeInfo info in uiObjectJson.panelInfoList)
            {
                panelPathDic.Add(info.panelType, info.panelPath);
            }
        }
    }

    private TextAsset LoadConfigSync()
    {
        try
        {
            return Resources.Load<TextAsset>("Config/UIPanelType");
        }
        catch (Exception e)
        {
            Debug.LogError($"加载配置文件失败: {e.Message}");
            return null;
        }
    }

    private void LoadConfigAsync(Action<TextAsset> onComplete)
    {
        var loadOperation = Addressables.LoadAssetAsync<TextAsset>("Config/UIPanelType");
        loadOperation.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null)
            {
                onComplete?.Invoke(op.Result);
            }
            else
            {
                try
                {
                    onComplete?.Invoke(Resources.Load<TextAsset>("Config/UIPanelType"));
                }
                catch (Exception e)
                {
                    Debug.LogError($"加载配置文件失败: {e.Message}");
                    onComplete?.Invoke(null);
                }
            }
        };
    }
    /// <summary>
    /// 添加并返回UI面板
    /// </summary>
    public basePanel PushPanel(UIPanelType panelType)
    {
        if (panelStack == null)
        {
            return null;
        }

        // pause previous panel
        if (panelStack.Count > 0)
        {
            basePanel topPanel = panelStack.Peek();
            if (topPanel != null)
            {
                topPanel.OnPause();
            }
        }
        //添加面板
        basePanel panel = null;
        panel = GetPanel(panelType);
        if (panel != null)
        {
            panel.OnEnter();
            panelStack.Push(panel);
        }
        return panel;
    }
    /// <summary>
    /// 弹出UI面板
    /// </summary>
    public void PopPanel()
    {
        // stack is empty
        if (panelStack.Count <= 0)
        {
            return;
        }

        basePanel popPanel = panelStack.Pop();
        if (popPanel != null)
        {
            popPanel.OnExit();
        }
        // enable new top panel
        if (panelStack.Count > 0)
        {
            basePanel newpopPanel = panelStack.Peek();
            if (newpopPanel != null)
            {
                newpopPanel.OnResume();
            }
        }

    }
    public basePanel GetPanel(UIPanelType panelType)
    {
        Initialize();

        if (panelObjDic == null)
        {
            return null;
        }

        if (panelObjDic.TryGetValue(panelType, out basePanel panel))
        {
            return panel;
        }

        if (!panelPathDic.TryGetValue(panelType, out string panelPath))
        {
            Debug.Log($"未找到UI面板:{panelType}");
            return null;
        }

        GameObject prefab = LoadPanelSync(panelPath);
        if (prefab == null) return null;

        GameObject instanPanel = GameObject.Instantiate(prefab);
        AddScriptComponent(panelType, instanPanel);
        instanPanel.transform.SetParent(Canvas, false);

        if (!panelObjDic.ContainsKey(panelType))
        {
            panelObjDic.Add(panelType, instanPanel.GetComponent<basePanel>());
        }
        else
        {
            Debug.LogWarning($"面板 {panelType} 已存在于字典中，跳过添加");
        }
        return instanPanel.GetComponent<basePanel>();
    }

    private GameObject LoadPanelSync(string panelPath)
    {
        var loadOperation = Addressables.LoadAssetAsync<GameObject>(panelPath);
        if (loadOperation.IsDone && loadOperation.Status == AsyncOperationStatus.Succeeded && loadOperation.Result != null)
        {
            return loadOperation.Result;
        }

        try
        {
            return Resources.Load<GameObject>(panelPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"加载面板失败:{panelPath}，错误：{e.Message}");
            return null;
        }
    }

    private void LoadPanelAsync(string panelPath, Action<GameObject> onComplete)
    {
        var loadOperation = Addressables.LoadAssetAsync<GameObject>(panelPath);
        loadOperation.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null)
            {
                onComplete?.Invoke(op.Result);
            }
            else
            {
                try
                {
                    onComplete?.Invoke(Resources.Load<GameObject>(panelPath));
                }
                catch (Exception e)
                {
                    Debug.LogError($"加载面板失败:{panelPath}，错误：{e.Message}");
                    onComplete?.Invoke(null);
                }
            }
        };
    }

    private void AddScriptComponent(UIPanelType panelType, GameObject instanPanel)
    {
        string scriptName = System.Enum.GetName(typeof(UIPanelType), panelType);
        Type scriptType = null;
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            scriptType = assembly.GetType(scriptName);
            if (scriptType != null) break;
            scriptType = assembly.GetType("KingdomWar.UI." + scriptName);
            if (scriptType != null) break;
        }
        if (scriptType == null)
        {
            Debug.LogError($"Script type not found: {scriptName}");
            return;
        }
        if (!instanPanel.GetComponent(scriptType))
        {
            instanPanel.AddComponent(scriptType);
        }
    }

    void Update()
    {

    }
    /// <summary>
    /// 清空UI面板
    /// </summary>
    /// <param name="panelType"></param>
    public void ClearPanel()
    {
        panelStack.Clear();
        panelObjDic.Clear();
    }
    
    /// <summary>
    /// 清除指定面板的缓�?    /// </summary>
    /// <param name="panelType">面板类型</param>
    public void ClearPanelCache(UIPanelType panelType)
    {
        if (panelObjDic.ContainsKey(panelType))
        {
            panelObjDic.Remove(panelType);
            Debug.Log($"Cleared {panelType} cache");
        }
    }

    public void Init()
    {
        UIManager.Instance.GetPanelAsync(UIPanelType.deckPanel, null);
        UIManager.Instance.GetPanelAsync(UIPanelType.battlePanel, null);
        UIManager.Instance.GetPanelAsync(UIPanelType.shopPanel, null);
        UIManager.Instance.GetPanelAsync(UIPanelType.mainPanel, null);
    }

    [Obsolete("Use CreatePromptMessageAsync instead")]
    public void CreatepromptMessage(string msg, Transform parent = null)
    {
        CreatePromptMessageAsync(msg, parent);
    }

    public void CreatePromptMessageAsync(string msg, Transform parent = null)
    {
        KingdomWar.Tools.CoroutineRunner.RunCoroutine(LoadPromptMessageCoroutine(msg, parent));
    }

    private IEnumerator LoadPromptMessageCoroutine(string msg, Transform parent)
    {
        var loadOperation = Addressables.LoadAssetAsync<GameObject>("Prefabs/UIPrefab/promptMessage");
        yield return loadOperation;

        GameObject prefab;
        if (loadOperation.Status == AsyncOperationStatus.Succeeded && loadOperation.Result != null)
        {
            prefab = loadOperation.Result;
        }
        else
        {
            ResourceRequest request = Resources.LoadAsync<GameObject>("Prefabs/UIPrefab/promptMessage");
            yield return request;
            prefab = request.asset as GameObject;
            if (prefab == null)
            {
                Debug.LogError("加载promptMessage失败");
                yield break;
            }
        }

        promptMessage promsg = GameObject.Instantiate(prefab).GetComponent<promptMessage>();
        promsg.transform.SetParent(parent == null ? canvas : parent, false);
        promsg.Show(msg);
    }

    public void CreateTipAsync(string msg)
    {
        KingdomWar.Tools.CoroutineRunner.RunCoroutine(LoadTipCoroutine(msg));
    }

    private IEnumerator LoadTipCoroutine(string msg)
    {
        var loadOperation = Addressables.LoadAssetAsync<GameObject>("Prefabs/UIPrefab/Tip");
        yield return loadOperation;

        GameObject prefab;
        if (loadOperation.Status == AsyncOperationStatus.Succeeded && loadOperation.Result != null)
        {
            prefab = loadOperation.Result;
        }
        else
        {
            ResourceRequest request = Resources.LoadAsync<GameObject>("Prefabs/UIPrefab/Tip");
            yield return request;
            prefab = request.asset as GameObject;
            if (prefab == null)
            {
                Debug.LogError("加载Tip失败");
                yield break;
            }
        }

        Tip tip = GameObject.Instantiate(prefab, Tips).GetComponent<Tip>();
        tip.Init(msg);
    }

    #region Addressables热更

    private bool isCatalogUpdating = false;

    public bool IsCatalogUpdating => isCatalogUpdating;

    public void CheckCatalogUpdate(Action<bool> onComplete)
    {
        if (isCatalogUpdating)
        {
            Debug.LogWarning("Catalog is updating, please wait...");
            onComplete?.Invoke(false);
            return;
        }

        isCatalogUpdating = true;
        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        checkHandle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                bool needUpdate = op.Result.Count > 0;
                Debug.Log($"Catalog check complete, needs update: {needUpdate}, count: {op.Result.Count}");
                onComplete?.Invoke(needUpdate);
            }
            else
            {
                Debug.LogError($"Catalog check failed: {op.OperationException}");
                onComplete?.Invoke(false);
            }
            Addressables.Release(checkHandle);
            isCatalogUpdating = false;
        };
    }

    public void UpdateCatalog(Action<bool> onComplete, Action<float> onProgress = null)
    {
        if (isCatalogUpdating)
        {
            Debug.LogWarning("Catalog is updating, please wait...");
            onComplete?.Invoke(false);
            return;
        }

        isCatalogUpdating = true;
        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        checkHandle.Completed += checkOp =>
        {
            if (checkOp.Status == AsyncOperationStatus.Succeeded && checkOp.Result.Count > 0)
            {
                Debug.Log($"Found {checkOp.Result.Count} catalogs need update");
                var updateHandle = Addressables.UpdateCatalogs(checkOp.Result, false);
                
                updateHandle.Completed += updateOp =>
                {
                    if (updateOp.Status == AsyncOperationStatus.Succeeded)
                    {
                        Debug.Log("Catalog updated successfully");
                        foreach (var locator in updateOp.Result)
                        {
                            Debug.Log($"Loaded Catalog: {locator.LocatorId}");
                        }
                        onComplete?.Invoke(true);
                    }
                    else
                    {
                        Debug.LogError($"Catalog update failed: {updateOp.OperationException}");
                        onComplete?.Invoke(false);
                    }
                    Addressables.Release(updateHandle);
                    isCatalogUpdating = false;
                };
            }
            else if (checkOp.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log("Catalog is up to date, no update needed");
                onComplete?.Invoke(true);
                isCatalogUpdating = false;
            }
            else
            {
                Debug.LogError($"Catalog check failed: {checkOp.OperationException}");
                onComplete?.Invoke(false);
                isCatalogUpdating = false;
            }
            Addressables.Release(checkHandle);
        };
    }

    public void ClearAddressablesCache()
    {
        Caching.ClearCache();
        Debug.Log("Addressables cache cleared");
    }

    public void ClearAllCache()
    {
        ClearPanel();
        Caching.ClearCache();
        Debug.Log("All caches cleared");
    }

    public void GetPanelAsync(UIPanelType panelType, Action<basePanel> onComplete)
    {
        Initialize();

        if (panelObjDic.TryGetValue(panelType, out basePanel cachedPanel))
        {
            onComplete?.Invoke(cachedPanel);
            return;
        }

        if (!panelPathDic.TryGetValue(panelType, out string panelPath))
        {
            Debug.LogError($"未找到UI面板路径: {panelType}");
            onComplete?.Invoke(null);
            return;
        }

        // Try Addressables (supports hot update), fallback to Resources
        try
        {
            var loadOperation = Addressables.LoadAssetAsync<GameObject>(panelPath);
            loadOperation.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null)
                {
                    GameObject instanPanel = GameObject.Instantiate(op.Result);
                    AddScriptComponent(panelType, instanPanel);
                    instanPanel.transform.SetParent(Canvas, false);
                    var panel = instanPanel.GetComponent<basePanel>();
                    panelObjDic[panelType] = panel;
                    onComplete?.Invoke(panel);
                }
                else
                {
                    LoadPanelFromResources(panelType, panelPath, onComplete);
                }
            };
        }
        catch (Exception)
        {
            LoadPanelFromResources(panelType, panelPath, onComplete);
        }
    }

    private void LoadPanelFromResources(UIPanelType panelType, string panelPath, Action<basePanel> onComplete)
    {
        try
        {
            GameObject instanPanel = GameObject.Instantiate(Resources.Load<GameObject>(panelPath));
            AddScriptComponent(panelType, instanPanel);
            instanPanel.transform.SetParent(Canvas, false);
            var panel = instanPanel.GetComponent<basePanel>();
            panelObjDic[panelType] = panel;
            onComplete?.Invoke(panel);
        }
        catch (Exception e)
        {
            Debug.LogError($"加载面板失败: {panelPath}, 错误: {e.Message}");
            onComplete?.Invoke(null);
        }
    }

    public IEnumerator CheckAndUpdateCatalog(Action<bool> onComplete)
    {
        if (isCatalogUpdating)
        {
            Debug.LogWarning("Catalog is updating, please wait...");
            onComplete?.Invoke(false);
            yield break;
        }

        isCatalogUpdating = true;

        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        yield return checkHandle;

        if (checkHandle.Status == AsyncOperationStatus.Succeeded && checkHandle.Result.Count > 0)
        {
            Debug.Log($"Found {checkHandle.Result.Count} catalogs need update");
            
            var updateHandle = Addressables.UpdateCatalogs(checkHandle.Result, false);
            yield return updateHandle;

            if (updateHandle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log("Catalog更新成功");
                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogError($"Catalog更新失败: {updateHandle.OperationException}");
                onComplete?.Invoke(false);
            }
            Addressables.Release(updateHandle);
        }
        else if (checkHandle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log("Catalog已是最新，无需更新");
            onComplete?.Invoke(true);
        }
        else
        {
            Debug.LogError($"Catalog check failed: {checkHandle.OperationException}");
            onComplete?.Invoke(false);
        }

        Addressables.Release(checkHandle);
        isCatalogUpdating = false;
    }

    #endregion
}

}
