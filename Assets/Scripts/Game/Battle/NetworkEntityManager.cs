using UnityEngine;
using Photon.Pun;
using System.Collections;
using KingdomWar.Game.Cards;

namespace KingdomWar.Game.Battle
{
    public class NetworkEntityManager : MonoBehaviourPunCallbacks
    {
        public static NetworkEntityManager Instance { get; private set; }

        [Header("生成特效")]
        public GameObject unitSpawnEffect;
        public GameObject buildingSpawnEffect;
        public GameObject awaitEffect;

        private PhotonView photonViewComponent;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                EnsurePhotonView();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void EnsurePhotonView()
        {
            photonViewComponent = GetComponent<PhotonView>();
            if (photonViewComponent == null)
            {
                photonViewComponent = gameObject.AddComponent<PhotonView>();
            }
            
            if (PhotonNetwork.IsConnected && photonViewComponent.ViewID == 0)
            {
                photonViewComponent.ViewID = PhotonNetwork.AllocateViewID(0);
                Debug.Log($"[NetworkEntityManager] Allocated ViewID: {photonViewComponent.ViewID}");
            }
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            
            if (photonViewComponent != null && photonViewComponent.ViewID == 0)
            {
                photonViewComponent.ViewID = PhotonNetwork.AllocateViewID(0);
                Debug.Log($"[NetworkEntityManager] Allocated ViewID on join: {photonViewComponent.ViewID}");
            }
        }

        #region Unit Spawning

        public void SpawnUnitFromCard(CardData cardData, Vector3 position)
        {
            if (cardData == null || cardData.cardPrefab == null)
            {
                Debug.LogError("[NetworkEntityManager] Invalid card data or prefab");
                return;
            }

            int ownerId = GetLocalOwnerId();

            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                int prefabIndex = GetPrefabIndex(cardData);
                photonViewComponent.RPC("RPC_SpawnUnit", RpcTarget.All, prefabIndex, position, ownerId, cardData.cardName);
            }
            else
            {
                CreateUnitLocal(cardData.cardPrefab, position, ownerId, cardData);
            }
        }

        [PunRPC]
        private void RPC_SpawnUnit(int prefabIndex, Vector3 position, int ownerId, string cardName)
        {
            CardData cardData = FindCardDataByIndex(prefabIndex, CardType.Unit);
            if (cardData == null)
            {
                Debug.LogError($"[NetworkEntityManager] CardData not found for index: {prefabIndex}");
                return;
            }
            StartCoroutine(SpawnUnitCoroutine(cardData.cardPrefab, position, ownerId, cardData));
        }

        private IEnumerator SpawnUnitCoroutine(GameObject prefab, Vector3 position, int ownerId, CardData cardData)
        {
            if (awaitEffect != null)
            {
                GameObject effect = Instantiate(awaitEffect, position, Quaternion.identity);
                Destroy(effect, 1f);
            }

            yield return new WaitForSeconds(0.5f);

            CreateUnitLocal(prefab, position, ownerId, cardData);

            if (unitSpawnEffect != null)
            {
                GameObject effect = Instantiate(unitSpawnEffect, position, Quaternion.identity);
                Destroy(effect, 2f);
            }
        }

        private void CreateUnitLocal(GameObject prefab, Vector3 position, int ownerId, CardData cardData)
        {
            if (prefab == null)
            {
                Debug.LogError("[NetworkEntityManager] Unit prefab is null");
                return;
            }

            GameObject unitObj = Instantiate(prefab, position, Quaternion.identity);

            Unit unit = unitObj.GetComponent<Unit>();
            if (unit == null)
            {
                unit = unitObj.AddComponent<Unit>();
            }

            NetworkUnit networkUnit = unitObj.GetComponent<NetworkUnit>();
            if (networkUnit == null)
            {
                networkUnit = unitObj.AddComponent<NetworkUnit>();
            }

            var unitData = cardData.unitData;
            if (unitData != null)
            {
                unit.Initialize(ownerId, cardData.cardName, unitData.health, unitData.damage, unitData.attackSpeed, unitData.moveSpeed, unitData.attackRange);
            }
            else
            {
                unit.Initialize(ownerId, cardData.cardName, 100, 10, 1f, 1f, 3f);
            }
            networkUnit.ownerId = ownerId;
            networkUnit.unitId = unitObj.GetInstanceID();

            Debug.Log($"[NetworkEntityManager] Unit created: {cardData.cardName}, Owner={ownerId}, Position={position}");

            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.AddUnit(unit);
            }

            if (BattleEventSystem.Instance != null)
            {
                BattleEventSystem.Instance.EmitUnitSpawned(unit);
            }
        }

        #endregion

        #region Building Spawning

        public void SpawnBuildingFromCard(CardData cardData, Vector3 position)
        {
            if (cardData == null || cardData.cardPrefab == null)
            {
                Debug.LogError("[NetworkEntityManager] Invalid card data or prefab");
                return;
            }

            int ownerId = GetLocalOwnerId();

            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                int prefabIndex = GetPrefabIndex(cardData);
                photonView.RPC("RPC_SpawnBuilding", RpcTarget.All, prefabIndex, position, ownerId, cardData.cardName);
            }
            else
            {
                CreateBuildingLocal(cardData.cardPrefab, position, ownerId, cardData);
            }
        }

        [PunRPC]
        private void RPC_SpawnBuilding(int prefabIndex, Vector3 position, int ownerId, string cardName)
        {
            CardData cardData = FindCardDataByIndex(prefabIndex, CardType.Building);
            if (cardData == null)
            {
                Debug.LogError($"[NetworkEntityManager] CardData not found for index: {prefabIndex}");
                return;
            }
            StartCoroutine(SpawnBuildingCoroutine(cardData.cardPrefab, position, ownerId, cardData));
        }

        private IEnumerator SpawnBuildingCoroutine(GameObject prefab, Vector3 position, int ownerId, CardData cardData)
        {
            if (awaitEffect != null)
            {
                GameObject effect = Instantiate(awaitEffect, position, Quaternion.identity);
                Destroy(effect, 1f);
            }

            yield return new WaitForSeconds(1f);

            CreateBuildingLocal(prefab, position, ownerId, cardData);

            if (buildingSpawnEffect != null)
            {
                GameObject effect = Instantiate(buildingSpawnEffect, position, Quaternion.identity);
                Destroy(effect, 2f);
            }
        }

        private void CreateBuildingLocal(GameObject prefab, Vector3 position, int ownerId, CardData cardData)
        {
            if (prefab == null)
            {
                Debug.LogError("[NetworkEntityManager] Building prefab is null");
                return;
            }

            GameObject buildingObj = Instantiate(prefab, position, Quaternion.identity);

            Building building = buildingObj.GetComponent<Building>();
            if (building == null)
            {
                building = buildingObj.AddComponent<Building>();
            }

            NetworkBuilding networkBuilding = buildingObj.GetComponent<NetworkBuilding>();
            if (networkBuilding == null)
            {
                networkBuilding = buildingObj.AddComponent<NetworkBuilding>();
            }

            var buildingData = cardData.buildingData;
            if (buildingData != null)
            {
                building.Initialize(ownerId, cardData.cardName, buildingData.health, buildingData.damage, buildingData.attackSpeed, buildingData.attackRange, buildingData.duration);
            }
            else
            {
                building.Initialize(ownerId, cardData.cardName, 500, 20, 1.5f, 5f, 0f);
            }
            networkBuilding.ownerId = ownerId;
            networkBuilding.buildingId = buildingObj.GetInstanceID();

            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.AddBuilding(building);
            }

            if (BattleEventSystem.Instance != null)
            {
                BattleEventSystem.Instance.EmitBuildingSpawned(building);
            }

            Debug.Log($"[NetworkEntityManager] Building created: {cardData.cardName}, Owner={ownerId}, Position={position}");
        }

        #endregion

        #region Spell Casting

        public void CastSpellFromCard(CardData cardData, Vector3 position)
        {
            if (cardData == null)
            {
                Debug.LogError("[NetworkEntityManager] CardData is null");
                return;
            }

            int ownerId = GetLocalOwnerId();

            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                int prefabIndex = GetPrefabIndex(cardData);
                photonView.RPC("RPC_CastSpell", RpcTarget.All, prefabIndex, position, ownerId, cardData.cardName);
            }
            else
            {
                CreateSpellLocal(cardData.cardPrefab, position, ownerId, cardData);
            }
        }

        [PunRPC]
        private void RPC_CastSpell(int prefabIndex, Vector3 position, int ownerId, string cardName)
        {
            CardData cardData = FindCardDataByIndex(prefabIndex, CardType.Spell);
            if (cardData == null)
            {
                Debug.LogError($"[NetworkEntityManager] CardData not found for index: {prefabIndex}");
                return;
            }
            CreateSpellLocal(cardData.cardPrefab, position, ownerId, cardData);
        }

        private void CreateSpellLocal(GameObject prefab, Vector3 position, int ownerId, CardData cardData)
        {
            Debug.Log($"[NetworkEntityManager] CreateSpellLocal called: prefab={prefab != null}, position={position}, ownerId={ownerId}, cardData={cardData?.cardName}");
            
            GameObject spellObj;
            
            if (prefab != null)
            {
                spellObj = Instantiate(prefab, position, Quaternion.identity);
                Debug.Log($"[NetworkEntityManager] Spell object instantiated from prefab: {spellObj.name}");
            }
            else
            {
                // 如果没有预制体，创建一个空对象
                spellObj = new GameObject($"Spell_{cardData?.cardName ?? "Unknown"}");
                spellObj.transform.position = position;
                Debug.Log($"[NetworkEntityManager] Spell object created without prefab: {spellObj.name}");
            }
            
            Spell spell = spellObj.GetComponent<Spell>();
            if (spell == null)
            {
                spell = spellObj.AddComponent<Spell>();
            }
            
            if (spell != null)
            {
                spell.Initialize(ownerId, cardData, position);
                Debug.Log($"[NetworkEntityManager] Spell initialized: {spell.spellName}, Damage={spell.damage}, State={spell.state}");
                
                if (BattleManager.Instance != null)
                {
                    BattleManager.Instance.AddSpell(spell);
                    Debug.Log($"[NetworkEntityManager] Spell added to BattleManager");
                }
                else
                {
                    Debug.LogError("[NetworkEntityManager] BattleManager.Instance is null!");
                }
                
                Debug.Log($"[NetworkEntityManager] Spell created and initialized: {cardData.cardName}, Owner={ownerId}, Damage={spell.damage}");
            }
            else
            {
                Debug.LogWarning($"[NetworkEntityManager] Failed to create Spell component: {cardData.cardName}");
            }
        }

        #endregion

        #region Helper Methods

        private int GetLocalOwnerId()
        {
            if (BattleManager.Instance != null)
            {
                byte team = BattleManager.Instance.GetLocalPlayerTeam();
                return team == 1 ? 1 : 2;
            }
            return PhotonNetwork.LocalPlayer.ActorNumber;
        }

        private int GetPrefabIndex(CardData cardData)
        {
            if (CardDatabase.Instance == null || cardData == null) return 0;

            var allCards = CardDatabase.Instance.GetAllCards();
            for (int i = 0; i < allCards.Count; i++)
            {
                if (allCards[i] == cardData)
                    return i;
            }
            return 0;
        }

        private CardData FindCardDataByIndex(int index, CardType type)
        {
            if (CardDatabase.Instance == null) return null;

            var allCards = CardDatabase.Instance.GetAllCards();
            if (index >= 0 && index < allCards.Count)
            {
                return allCards[index];
            }
            return null;
        }

        #endregion
    }
}
