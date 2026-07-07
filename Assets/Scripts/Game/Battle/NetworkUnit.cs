using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace KingdomWar.Game.Battle
{
    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(PhotonTransformView))]
    [RequireComponent(typeof(PhotonAnimatorView))]
    public class NetworkUnit : MonoBehaviourPunCallbacks, IPunObservable
    {
        [Header("网络设置")]
        public int unitId;
        public int ownerId;

        [Header("单位状态")]
        public float health;
        public float maxHealth;
        public float moveSpeed;
        public bool isMoving;
        public bool isAttacking;
        public bool isDead;

        private Unit unit;
        private PhotonView photonViewComponent;
        private PhotonTransformView photonTransformView;
        private PhotonAnimatorView photonAnimatorView;
        private bool hasReceivedNetworkData;

        private void Awake()
        {
            photonViewComponent = GetComponent<PhotonView>();
            if (photonViewComponent == null)
            {
                photonViewComponent = gameObject.AddComponent<PhotonView>();
            }

            photonTransformView = GetComponent<PhotonTransformView>();
            if (photonTransformView == null)
            {
                photonTransformView = gameObject.AddComponent<PhotonTransformView>();
            }

            photonAnimatorView = GetComponent<PhotonAnimatorView>();
            if (photonAnimatorView == null)
            {
                photonAnimatorView = gameObject.AddComponent<PhotonAnimatorView>();
            }

            if (PhotonNetwork.IsConnected && photonViewComponent.ViewID == 0)
            {
                photonViewComponent.ViewID = PhotonNetwork.AllocateViewID(0);
            }

            ConfigurePhotonTransformView();

            try
            {
                if (photonViewComponent.ObservedComponents == null)
                {
                    photonViewComponent.ObservedComponents = new System.Collections.Generic.List<Component>();
                }
                
                if (photonTransformView != null && !photonViewComponent.ObservedComponents.Contains(photonTransformView))
                {
                    photonViewComponent.ObservedComponents.Add(photonTransformView);
                }
                
                if (photonAnimatorView != null && !photonViewComponent.ObservedComponents.Contains(photonAnimatorView))
                {
                    photonViewComponent.ObservedComponents.Add(photonAnimatorView);
                }
                
                if (!photonViewComponent.ObservedComponents.Contains(this))
                {
                    photonViewComponent.ObservedComponents.Add(this);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[NetworkUnit] ObservedComponents setup warning: {e.Message}");
            }

            unit = GetComponent<Unit>();
            unitId = Random.Range(1000, 9999);
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
            if (unit != null)
            {
                ownerId = unit.ownerId;
                health = unit.health;
                maxHealth = unit.maxHealth;
                moveSpeed = unit.moveSpeed;
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

        private void Update()
        {
            if (photonViewComponent == null) return;

            bool isLocalUnit = photonViewComponent.IsMine;
            bool shouldBeNetworkControlled = !isLocalUnit;
            int localTeam = 0;
            if (BattleManager.Instance != null)
            {
                localTeam = BattleManager.Instance.GetLocalPlayerTeam();
            }

            // 特殊处理：非网络对战时（如AI对战），所有单位都不进行网络控制
            if (!PhotonNetwork.IsConnected)
            {
                shouldBeNetworkControlled = false;
            }
            // 特殊处理：如果是本地玩家阵营创建的单位，即使所有权还没转移，也应该由本地控制
            else if (unit != null && localTeam != 0 && ownerId == localTeam)
            {
                shouldBeNetworkControlled = false;
                isLocalUnit = true;
            }

            if (unit != null && unit.isNetworkControlled != shouldBeNetworkControlled)
            {
                unit.isNetworkControlled = shouldBeNetworkControlled;
                Debug.Log($"[NetworkUnit] {unit?.unitName} isNetworkControlled set to {shouldBeNetworkControlled}, IsMine: {isLocalUnit}, Owner: {photonViewComponent.Owner?.ActorNumber}, LocalPlayer: {PhotonNetwork.LocalPlayer?.ActorNumber}, ownerId: {ownerId}, localTeam: {localTeam}");
            }

            if (isLocalUnit)
            {
                if (unit != null)
                {
                    health = unit.health;
                    maxHealth = unit.maxHealth;
                    moveSpeed = unit.moveSpeed;
                    isMoving = unit.state == UnitState.Moving;
                    isAttacking = unit.state == UnitState.Attacking;
                }
            }
            else
            {
                if (unit != null && !isDead && hasReceivedNetworkData)
                {
                    if (Mathf.Abs(health - unit.health) > 1f)
                    {
                        unit.health = (int)health;
                    }

                    unit.state = isMoving ? UnitState.Moving : (isAttacking ? UnitState.Attacking : UnitState.Idle);

                    unit.isMoving = isMoving;
                    unit.isAttacking = isAttacking;
                    unit.isDead = isDead;
                }
            }
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
                    Debug.Log($"[NetworkUnit] TransferOwnership: {unit?.unitName} from {photonViewComponent.Owner?.ActorNumber} to {ownerPlayer.ActorNumber}, ownerId={ownerId}");
                    photonViewComponent.TransferOwnership(ownerPlayer);
                }
                else
                {
                    Debug.LogWarning($"[NetworkUnit] No owner player found for {unit?.unitName}, ownerId={ownerId}");
                }
            }
            else
            {
                Debug.Log($"[NetworkUnit] Skip TransferOwnership: Not Master Client, LocalPlayer: {PhotonNetwork.LocalPlayer?.ActorNumber}");
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

            Debug.LogWarning($"[NetworkUnit] No player matches owner team {ownerId}");
            return null;
        }

        public void TakeDamage(float damage)
        {
            if (photonViewComponent == null)
            {
                Debug.LogError($"[NetworkUnit] photonViewComponent is null! Unit: {unit?.unitName}");
                return;
            }

            if (photonViewComponent.ViewID == 0)
            {
                // AI对战或无网络时，直接本地伤害
                if (!PhotonNetwork.IsConnected && unit != null)
                {
                    unit.TakeDamage((int)damage);
                    return;
                }
                Debug.LogError($"[NetworkUnit] ViewID is 0! Unit: {unit?.unitName}");
                return;
            }

            int localTeam = BattleManager.Instance != null ? BattleManager.Instance.GetLocalPlayerTeam() : 0;
            if (PhotonNetwork.IsConnected && localTeam != 0 && ownerId == localTeam)
            {
                Debug.Log($"[NetworkUnit] Ignore self damage sync on local-owned unit {unit?.unitName}, ownerId={ownerId}, localTeam={localTeam}");
                return;
            }

            Debug.Log($"[NetworkUnit] TakeDamage called on {unit?.unitName}, ViewID: {photonViewComponent.ViewID}, IsMine: {photonViewComponent.IsMine}, Damage: {damage}");
            photonViewComponent.RPC("RPC_TakeDamage", RpcTarget.All, damage);
        }

        public void Die()
        {
            if (photonViewComponent != null)
            {
                photonViewComponent.RPC("RPC_Die", RpcTarget.All);
            }
        }

        #region RPC Methods

        [PunRPC]
        private void RPC_TakeDamage(float damage)
        {
            if (isDead) return;
            
            health -= damage;
            
            if (unit != null)
            {
                unit.health = (int)health;
            }
            
            Debug.Log($"[NetworkUnit] {unit?.unitName} took {damage} damage, health now: {health}");
            
            if (health <= 0)
            {
                isDead = true;
                health = 0;
                isMoving = false;
                isAttacking = false;

                if (unit != null)
                {
                    unit.state = UnitState.Dead;
                    unit.health = 0;
                    
                    if (BattleManager.Instance != null)
                    {
                        BattleManager.Instance.Units.Remove(unit);
                    }
                }

                Debug.Log($"[NetworkUnit] {unit?.unitName} died!");
                
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
        private void RPC_Die()
        {
            if (isDead) return;
            
            isDead = true;
            health = 0;
            isMoving = false;
            isAttacking = false;

            if (unit != null)
            {
                unit.state = UnitState.Dead;
                unit.health = 0;
                
                if (BattleManager.Instance != null)
                {
                    BattleManager.Instance.Units.Remove(unit);
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

        #endregion

        #region IPunObservable Implementation

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(health);
                stream.SendNext(maxHealth);
                stream.SendNext(moveSpeed);
                stream.SendNext(isMoving);
                stream.SendNext(isAttacking);
                stream.SendNext(ownerId);
                stream.SendNext(isDead);
            }
            else
            {
                health = (float)stream.ReceiveNext();
                maxHealth = (float)stream.ReceiveNext();
                moveSpeed = (float)stream.ReceiveNext();
                isMoving = (bool)stream.ReceiveNext();
                isAttacking = (bool)stream.ReceiveNext();
                ownerId = (int)stream.ReceiveNext();
                bool receivedIsDead = (bool)stream.ReceiveNext();
                
                if (receivedIsDead && !isDead)
                {
                    isDead = true;
                    if (unit != null)
                    {
                        unit.state = UnitState.Dead;
                        unit.health = 0;
                        
                        if (BattleManager.Instance != null)
                        {
                            BattleManager.Instance.Units.Remove(unit);
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
                Die();
            }
        }

        #endregion
    }
}
