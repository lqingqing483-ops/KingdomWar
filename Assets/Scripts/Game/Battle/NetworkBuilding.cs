using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace KingdomWar.Game.Battle
{
    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(PhotonTransformView))]
    public class NetworkBuilding : MonoBehaviourPunCallbacks, IPunObservable
    {
        [Header("网络设置")]
        public int buildingId;
        public int ownerId;

        [Header("建筑状态")]
        public float health;
        public float maxHealth;
        public BuildingType buildingType;
        public bool isDead;

        private Building building;
        private PhotonView photonView;
        private PhotonTransformView photonTransformView;
        private bool hasReceivedNetworkData;

        private void Awake()
        {
            photonView = GetComponent<PhotonView>();
            if (photonView == null)
            {
                photonView = gameObject.AddComponent<PhotonView>();
            }

            photonTransformView = GetComponent<PhotonTransformView>();
            if (photonTransformView == null)
            {
                photonTransformView = gameObject.AddComponent<PhotonTransformView>();
            }

            if (PhotonNetwork.IsConnected && photonView.ViewID == 0)
            {
                photonView.ViewID = PhotonNetwork.AllocateViewID(0);
            }

            ConfigurePhotonTransformView();

            try
            {
                if (photonView.ObservedComponents == null)
                {
                    photonView.ObservedComponents = new System.Collections.Generic.List<Component>();
                }
                
                if (photonTransformView != null && !photonView.ObservedComponents.Contains(photonTransformView))
                {
                    photonView.ObservedComponents.Add(photonTransformView);
                }
                
                if (!photonView.ObservedComponents.Contains(this))
                {
                    photonView.ObservedComponents.Add(this);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[NetworkBuilding] ObservedComponents setup warning: {e.Message}");
            }

            building = GetComponent<Building>();
            buildingId = Random.Range(1000, 9999);
        }

        private void ConfigurePhotonTransformView()
        {
            if (photonTransformView != null)
            {
                photonTransformView.m_SynchronizePosition = true;
                photonTransformView.m_SynchronizeRotation = true;
                photonTransformView.m_SynchronizeScale = false;
                photonTransformView.m_UseLocal = false;
            }
        }

        private void Start()
        {
            if (building != null)
            {
                ownerId = building.ownerId;
                health = building.health;
                maxHealth = building.maxHealth;
                buildingType = building.buildingType;
            }

            UpdateOwnership();
        }

        private int GetPlayerTeam(Player player)
        {
            if (player == null)
            {
                return 0;
            }

            if (player.CustomProperties.TryGetValue("team", out object teamObj))
            {
                return (byte)teamObj == 1 ? 1 : 2;
            }

            return 0;
        }

        private void UpdateOwnership()
        {
            if (!PhotonNetwork.IsConnected) return;

            // 只有 Master Client 可以转移所有权
            if (PhotonNetwork.IsMasterClient)
            {
                Player ownerPlayer = GetOwnerPlayer();
                if (ownerPlayer != null)
                {
                    photonView.TransferOwnership(ownerPlayer);
                }
            }
        }

        private Player GetOwnerPlayer()
        {
            if (!PhotonNetwork.IsConnected) return null;

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                int playerTeam = GetPlayerTeam(player);
                if (playerTeam == ownerId)
                {
                    return player;
                }
            }

            Debug.LogWarning($"[NetworkBuilding] No player matches owner team {ownerId}");
            return null;
        }

        private void Update()
        {
            if (isDead) return;

            int localTeam = 0;
            if (BattleManager.Instance != null)
            {
                localTeam = BattleManager.Instance.GetLocalPlayerTeam();
            }

            bool isLocalBuilding;
            if (PhotonNetwork.IsConnected && building != null && localTeam != 0 && (ownerId == 1 || ownerId == 2))
            {
                isLocalBuilding = ownerId == localTeam;
            }
            else
            {
                isLocalBuilding = photonView.IsMine;
            }

            if (isLocalBuilding)
            {
                if (building != null)
                {
                    health = building.health;
                    maxHealth = building.maxHealth;
                }
            }
            else
            {
                if (building != null && hasReceivedNetworkData && !isDead)
                {
                    if (Mathf.Abs(health - building.health) > 1f)
                    {
                        building.health = (int)health;
                    }
                    if (Mathf.Abs(maxHealth - building.maxHealth) > 1f)
                    {
                        building.maxHealth = (int)maxHealth;
                    }
                }
            }
        }

        public void TakeDamage(float damage)
        {
            if (photonView == null)
            {
                Debug.LogError($"[NetworkBuilding] photonView is null! Building: {building?.buildingName}");
                return;
            }

            if (photonView.ViewID == 0)
            {
                Debug.LogError($"[NetworkBuilding] ViewID is 0! Building: {building?.buildingName}");
                return;
            }

            int localTeam = BattleManager.Instance != null ? BattleManager.Instance.GetLocalPlayerTeam() : 0;
            if (PhotonNetwork.IsConnected && localTeam != 0 && ownerId == localTeam)
            {
                Debug.Log($"[NetworkBuilding] Ignore self damage sync on local-owned building {building?.buildingName}, ownerId={ownerId}, localTeam={localTeam}");
                return;
            }

            Debug.Log($"[NetworkBuilding] TakeDamage called on {building?.buildingName}, ViewID: {photonView.ViewID}, IsMine: {photonView.IsMine}, Damage: {damage}");
            photonView.RPC("RPC_TakeDamage", RpcTarget.All, damage);
        }

        public void DestroyBuilding()
        {
            if (photonView != null)
            {
                photonView.RPC("RPC_DestroyBuilding", RpcTarget.All);
            }
        }

        #region RPC Methods

        [PunRPC]
        private void RPC_TakeDamage(float damage)
        {
            if (isDead) return;
            
            health -= damage;

            if (building != null)
            {
                building.health = (int)health;
            }
            
            Debug.Log($"[NetworkBuilding] {building?.buildingName} took {damage} damage, health now: {health}");

            if (health <= 0)
            {
                isDead = true;
                health = 0;

                if (building != null)
                {
                    building.health = 0;
                    building.state = BuildingState.Dead;
                }

                if (BattleManager.Instance != null)
                {
                    BattleManager.Instance.buildings.Remove(building);
                }

                Debug.Log($"[NetworkBuilding] {building?.buildingName} destroyed!");
                
                // 播放死亡动画后销毁
                Animator animator = GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetTrigger("Die");
                    Destroy(gameObject, 1f);
                }
                else
                {
                    Destroy(gameObject, 0.5f);
                }
            }
        }

        [PunRPC]
        private void RPC_DestroyBuilding()
        {
            if (isDead) return;
            
            isDead = true;
            health = 0;

            if (building != null)
            {
                building.health = 0;
                building.state = BuildingState.Dead;
            }

            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.buildings.Remove(building);
            }

            Animator animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Die");
                Destroy(gameObject, 1f);
            }
            else
            {
                Destroy(gameObject, 0.5f);
            }
        }

        #endregion

        #region IPunObservable Implementation

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(health);
                stream.SendNext(maxHealth);
                stream.SendNext(ownerId);
                stream.SendNext(isDead);
            }
            else
            {
                health = (float)stream.ReceiveNext();
                maxHealth = (float)stream.ReceiveNext();
                ownerId = (int)stream.ReceiveNext();
                bool receivedIsDead = (bool)stream.ReceiveNext();
                
                if (receivedIsDead && !isDead)
                {
                    isDead = true;
                    if (building != null)
                    {
                        building.health = 0;
                        building.state = BuildingState.Dead;
                        
                        if (BattleManager.Instance != null)
                        {
                            BattleManager.Instance.buildings.Remove(building);
                        }
                    }
                    
                    Animator animator = GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.SetTrigger("Die");
                        Destroy(gameObject, 1f);
                    }
                    else
                    {
                        Destroy(gameObject, 0.5f);
                    }
                }
                
                hasReceivedNetworkData = true;
            }
        }

        #endregion

        #region Photon Callbacks

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            base.OnPlayerLeftRoom(otherPlayer);

            if (GetPlayerTeam(otherPlayer) == ownerId)
            {
                DestroyBuilding();
            }
        }

        #endregion
    }
}
