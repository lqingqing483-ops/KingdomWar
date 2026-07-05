using UnityEngine;
using System.Collections;
using KingdomWar.Game.Battle;
using KingdomWar.Tools;
using Photon.Pun;

namespace KingdomWar.Game.Cards
{
    public class CardEffectManager : MonoBehaviour
{
    public static CardEffectManager Instance { get; private set; }
    
    [Header("特效设置")]
    public GameObject awaitPrefab;
    public GameObject unitSpawnEffect;
    public GameObject spellEffect;
    public GameObject buildingSpawnEffect;

    [Header("网络模式设置")]
    public bool useNetworkMode = true;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    public void UseCardEffect(CardData cardData, Vector3 position)
    {
        if (useNetworkMode && PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            UseCardEffectNetwork(cardData, position);
        }
        else
        {
            UseCardEffectLocal(cardData, position);
        }
    }

    private void UseCardEffectNetwork(CardData cardData, Vector3 position)
    {
        if (NetworkEntityManager.Instance == null)
        {
            Debug.LogError("[CardEffectManager] NetworkEntityManager not found!");
            UseCardEffectLocal(cardData, position);
            return;
        }

        switch (cardData.cardType)
        {
            case CardType.Unit:
                NetworkEntityManager.Instance.SpawnUnitFromCard(cardData, position);
                break;
            case CardType.Spell:
                NetworkEntityManager.Instance.CastSpellFromCard(cardData, position);
                break;
            case CardType.Building:
                NetworkEntityManager.Instance.SpawnBuildingFromCard(cardData, position);
                break;
        }
    }

    private void UseCardEffectLocal(CardData cardData, Vector3 position)
    {
        switch (cardData.cardType)
        {
            case CardType.Unit:
                StartCoroutine(SpawnUnit(cardData, position));
                break;
            case CardType.Spell:
                CastSpell(cardData, position);
                break;
            case CardType.Building:
                StartCoroutine(SpawnBuilding(cardData, position));
                break;
        }
    }
    
    /// <summary>
    /// 生成单位
    /// </summary>
    /// <param name="cardData">卡片数据</param>
    /// <param name="position">生成位置</param>
    private IEnumerator SpawnUnit(CardData cardData, Vector3 position)
    {
        // 显示等待特效
        if (awaitPrefab != null && ObjectPoolManager.Instance != null)
        {
            GameObject awaitEffect = ObjectPoolManager.Instance.GetObject(ObjectPoolManager.AWAIT_EFFECT_POOL, position, Quaternion.identity);
            if (awaitEffect != null)
            {
                // 等待特效播放完成后返回对象池
                StartCoroutine(ReturnToPoolAfterDelay(ObjectPoolManager.AWAIT_EFFECT_POOL, awaitEffect, cardData.unitData.deployTime));
            }
            else
            {
                Debug.Log("等待特效为空, 直接实例化");
                // 备用方案：直接实例化
                GameObject awaitEffectBackup = Instantiate(awaitPrefab, position, Quaternion.identity);
                Destroy(awaitEffectBackup, cardData.unitData.deployTime);
            }
        }
        
        // 等待部署时间
        yield return new WaitForSeconds(cardData.unitData.deployTime);
        
        // 显示生成特效
        if (unitSpawnEffect != null && ObjectPoolManager.Instance != null)
        {
            GameObject spawnEffect = ObjectPoolManager.Instance.GetObject(ObjectPoolManager.UNIT_SPAWN_EFFECT_POOL, position, Quaternion.identity);
            if (spawnEffect != null)
            {
                // 特效播放完成后返回对象池
                StartCoroutine(ReturnToPoolAfterDelay(ObjectPoolManager.UNIT_SPAWN_EFFECT_POOL, spawnEffect, 2f));
            }
            else
            {
                // 备用方案：直接实例化
                spawnEffect = Instantiate(unitSpawnEffect, position, Quaternion.identity);
                Destroy(spawnEffect, 2f);   
            }
        }
        
        // 根据卡片名称生成对应的单位
        //GameObject unitPrefab = GetUnitPrefab(cardData.cardName);
        GameObject unitPrefab = cardData.cardPrefab;
        if (unitPrefab != null)
            {
                GameObject unit;
                if (ObjectPoolManager.Instance != null)
                {
                    unit = ObjectPoolManager.Instance.GetObject(ObjectPoolManager.UNIT_POOL, position, Quaternion.identity);
                    if (unit == null)
                    {
                        // 备用方案：直接实例化
                        unit = Instantiate(unitPrefab, position, Quaternion.identity);
                    }
                }
                else
                {
                    // 备用方案：直接实例化
                    unit = Instantiate(unitPrefab, position, Quaternion.identity);
                }
                
                Unit unitComponent = unit.GetComponent<Unit>();
                if (unitComponent == null)
                {
                    unitComponent = unit.AddComponent<Unit>();
                }

                NetworkUnit networkUnit = unit.GetComponent<NetworkUnit>();
                if (networkUnit == null)
                {
                    networkUnit = unit.AddComponent<NetworkUnit>();
                }
                
                if (unitComponent != null)
                {
                    int ownerId = 0;
                    if (KingdomWar.Game.Battle.BattleManager.Instance != null)
                    {
                        byte team = KingdomWar.Game.Battle.BattleManager.Instance.GetLocalPlayerTeam();
                        ownerId = team == 1 ? 1 : 2;
                        Debug.LogFormat("从BattleManager获取到阵营: {0}，设置ownerId为: {1}", team == 1 ? "蓝方" : "红方", ownerId);
                    }
                    unitComponent.Initialize(ownerId, cardData);
                    networkUnit.ownerId = ownerId;
                    networkUnit.unitId = unit.GetInstanceID();
                    
                    if (KingdomWar.Game.Battle.BattleManager.Instance != null)
                    {
                        KingdomWar.Game.Battle.BattleManager.Instance.AddUnit(unitComponent);
                    }
                }
                Debug.Log($"生成单位: {cardData.cardName} 在位置: {position}");
            }
        else
        {
            Debug.LogWarning($"未找到单位预设: {cardData.cardName}");
        }
    }
    
    /// <summary>
    /// 释放法术
    /// </summary>
    /// <param name="cardData">卡片数据</param>
    /// <param name="position">释放位置</param>
    private void CastSpell(CardData cardData, Vector3 position)
    {
        if (spellEffect != null && ObjectPoolManager.Instance != null)
        {
            GameObject effect = ObjectPoolManager.Instance.GetObject(ObjectPoolManager.SPELL_EFFECT_POOL, position, Quaternion.identity);
            if (effect != null)
            {
                StartCoroutine(ReturnToPoolAfterDelay(ObjectPoolManager.SPELL_EFFECT_POOL, effect, 2f));
            }
            else
            {
                GameObject effectBackup = Instantiate(spellEffect, position, Quaternion.identity);
                Destroy(effectBackup, 2f);
            }
        }
        
        GameObject spell;
        GameObject spellPrefab = cardData.cardPrefab;
        if (spellPrefab != null)
        {
            if (ObjectPoolManager.Instance != null)
            {
                spell = ObjectPoolManager.Instance.GetObject(ObjectPoolManager.SPELL_POOL, position, Quaternion.identity);
                if (spell == null)
                {
                    spell = Instantiate(spellPrefab, position, Quaternion.identity);
                }
            }
            else
            {
                spell = Instantiate(spellPrefab, position, Quaternion.identity);
            }
        }
        else
        {
            if (ObjectPoolManager.Instance != null)
            {
                spell = ObjectPoolManager.Instance.GetObject(ObjectPoolManager.SPELL_POOL, position, Quaternion.identity);
                if (spell == null)
                {
                    spell = new GameObject(cardData.cardName);
                    spell.transform.position = position;
                }
            }
            else
            {
                spell = new GameObject(cardData.cardName);
                spell.transform.position = position;
            }
        }
        
        Spell spellComponent = spell.GetComponent<Spell>();
        if (spellComponent == null)
        {
            spellComponent = spell.AddComponent<Spell>();
        }

        NetworkSpell networkSpell = spell.GetComponent<NetworkSpell>();
        if (networkSpell == null)
        {
            networkSpell = spell.AddComponent<NetworkSpell>();
        }
        
        if (spellComponent != null)
        {
            int ownerId = 1;
            if (KingdomWar.Game.Battle.BattleManager.Instance != null)
            {
                byte team = KingdomWar.Game.Battle.BattleManager.Instance.GetLocalPlayerTeam();
                ownerId = team == 1 ? 1 : 2;
                Debug.LogFormat("从BattleManager获取到阵营: {0}，设置ownerId为: {1}", team == 1 ? "蓝方" : "红方", ownerId);
            }
            spellComponent.Initialize(ownerId, cardData, position);
            networkSpell.casterId = ownerId;
            networkSpell.spellId = spell.GetInstanceID();
            networkSpell.damage = cardData.spellData.damage;
            networkSpell.radius = cardData.spellData.radius;
            networkSpell.duration = cardData.spellData.duration;
        }
        
        // 计算法术伤害范围
        float radius = cardData.spellData.radius;
        
        // 查找范围内的敌人（3D环境）
        Collider[] enemies = Physics.OverlapSphere(position, radius);
        foreach (Collider enemy in enemies)
        {
            // 这里可以添加伤害逻辑
            Debug.Log($"法术命中: {enemy.gameObject.name}，伤害: {cardData.spellData.damage}");
        }
        
        Debug.Log($"释放法术: {cardData.cardName} 在位置: {position}，范围: {radius}");
        
        // 添加到BattleManager
        if (KingdomWar.Game.Battle.BattleManager.Instance != null)
        {
            KingdomWar.Game.Battle.BattleManager.Instance.AddSpell(spellComponent);
        }
    }
    
    /// <summary>
    /// 生成建筑
    /// </summary>
    /// <param name="cardData">卡片数据</param>
    /// <param name="position">生成位置</param>
    private IEnumerator SpawnBuilding(CardData cardData, Vector3 position)
    {
        // 显示等待特效
        if (awaitPrefab == null || ObjectPoolManager.Instance == null)
        {
            Debug.LogWarning("等待特效预设或对象池未初始化");
            yield break;
        }
        if (awaitPrefab != null && ObjectPoolManager.Instance != null)
        {
            Debug.Log("生成建筑");
            GameObject awaitEffect = ObjectPoolManager.Instance.GetObject(ObjectPoolManager.AWAIT_EFFECT_POOL, position, Quaternion.identity);
            if (awaitEffect != null)
            {
                Debug.Log("播放建筑等待特效");
                // 等待特效播放完成后返回对象池
                StartCoroutine(ReturnToPoolAfterDelay(ObjectPoolManager.AWAIT_EFFECT_POOL, awaitEffect, cardData.unitData.deployTime));
            }
            else
            {
                Debug.Log("等待特效为空, 直接实例化");
                // 备用方案：直接实例化
                GameObject awaitEffectBackup = Instantiate(awaitPrefab, position, Quaternion.identity);
                awaitEffectBackup.name = "--AwaitEffectBackup--";
                Destroy(awaitEffectBackup, Mathf.Max(cardData.unitData.deployTime, 1.5f));
            }
        }
        // 等待部署时间
        yield return new WaitForSeconds(cardData.buildingData.deployTime);
        
        // 显示生成特效
        if (buildingSpawnEffect != null && ObjectPoolManager.Instance != null)
        {
            GameObject effect = ObjectPoolManager.Instance.GetObject(ObjectPoolManager.BUILDING_SPAWN_EFFECT_POOL, position, Quaternion.identity);
            if (effect != null)
            {
                // 特效播放完成后返回对象池
                StartCoroutine(ReturnToPoolAfterDelay(ObjectPoolManager.BUILDING_SPAWN_EFFECT_POOL, effect, 2f));
            }
            else
            {
                // 备用方案：直接实例化
                effect = Instantiate(buildingSpawnEffect, position, Quaternion.identity);
                Destroy(effect, 2f);
            }
        }
        
        // 根据卡片名称生成对应的建筑
        GameObject buildingPrefab = cardData.cardPrefab;
        if (buildingPrefab != null)
            {
                GameObject building;
                if (ObjectPoolManager.Instance != null)
                {
                    building = ObjectPoolManager.Instance.GetObject(ObjectPoolManager.BUILDING_POOL, position, Quaternion.identity);
                    if (building == null)
                    {
                        // 备用方案：直接实例化
                        building = Instantiate(buildingPrefab, position, Quaternion.identity);
                    }
                }
                else
                {
                    // 备用方案：直接实例化
                    building = Instantiate(buildingPrefab, position, Quaternion.identity);
                }
                
                Building buildingComponent = building.GetComponent<Building>();
                if (buildingComponent == null)
                {
                    buildingComponent = building.AddComponent<Building>();
                }

                NetworkBuilding networkBuilding = building.GetComponent<NetworkBuilding>();
                if (networkBuilding == null)
                {
                    networkBuilding = building.AddComponent<NetworkBuilding>();
                }
                
                if (buildingComponent != null)
                {
                    int ownerId = 1;
                    if (KingdomWar.Game.Battle.BattleManager.Instance != null)
                    {
                        byte team = KingdomWar.Game.Battle.BattleManager.Instance.GetLocalPlayerTeam();
                        ownerId = team == 1 ? 1 : 2;
                        Debug.LogFormat("从BattleManager获取到阵营: {0}，设置ownerId为: {1}", team == 1 ? "蓝方" : "红方", ownerId);
                    }
                    buildingComponent.Initialize(ownerId, cardData);
                    networkBuilding.ownerId = ownerId;
                    networkBuilding.buildingId = building.GetInstanceID();
                }
                Debug.Log($"生成建筑: {cardData.cardName} 在位置: {position}");
            }
        else
        {
            Debug.LogWarning($"未找到建筑预设: {cardData.cardName}");
        }
    }
    
    /// <summary>
    /// 延迟后将对象返回对象池
    /// </summary>
    /// <param name="poolName">池名称</param>
    /// <param name="obj">要返回的对象</param>
    /// <param name="delay">延迟时间</param>
    /// <returns></returns>
    private IEnumerator ReturnToPoolAfterDelay(string poolName, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null && ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnObject(poolName, obj);
        }
    }
    }
}