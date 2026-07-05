using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace KingdomWar.Game.Battle
{
    // 法术类型枚举
    public enum SpellType
    {
        Fireball,
        Lightning,
        Zap,
        Heal,
        Rage,
        Freeze
    }

    public class NetworkSpell : MonoBehaviourPunCallbacks, IPunObservable
    {
        [Header("网络设置")]
        public bool isNetworkSpell = false; // 是否是网络同步法术
        public int spellId; // 法术ID
        public int casterId; // 施法者ID

        [Header("法术状态")]
        public Vector3 networkPosition; // 网络位置
        public Vector3 networkDirection; // 网络方向
        public float damage; // 伤害值
        public float radius; // 范围半径
        public float duration; // 持续时间

        [Header("同步设置")]
        public float positionSyncThreshold = 0.1f; // 位置同步阈值
        public float syncInterval = 0.1f; // 同步间隔

        private Spell spell; // 关联的Spell组件
        private float lastSyncTime = 0f; // 上次同步时间
        private PhotonView photonView; // PhotonView组件
        private float remainingDuration; // 剩余持续时间

        private void Awake()
        {
            photonView = GetComponent<PhotonView>();
            if (photonView == null)
            {
                photonView = gameObject.AddComponent<PhotonView>();
            }

            if (PhotonNetwork.IsConnected && photonView.ViewID == 0)
            {
                photonView.ViewID = PhotonNetwork.AllocateViewID(0);
            }

            spell = GetComponent<Spell>();
            if (spell != null)
            {
                casterId = spell.ownerId;
                damage = spell.damage;
                radius = spell.radius;
                duration = spell.duration;
            }

            spellId = Random.Range(1000, 9999);
            networkPosition = transform.position;
            networkDirection = transform.forward;
            remainingDuration = duration;
        }

        private void Start()
        {
            bool isLocalSpell = !PhotonNetwork.IsConnected || 
                               (photonView != null && photonView.IsMine);

            if (isLocalSpell)
            {
                isNetworkSpell = true;
            }
            else
            {
                isNetworkSpell = true;
            }
        }

        private void Update()
        {
            // 处理网络同步
            if (isNetworkSpell)
            {
                if (photonView != null && photonView.IsMine)
                {
                    // 本地法术，发送同步信息
                    SyncSpellState();
                    
                    // 更新持续时间
                    UpdateDuration();
                }
                else
                {
                    // 远程法术，接收同步信息
                    ApplyNetworkState();
                }
            }

            // 同步Spell组件状态
            if (spell != null)
            {
                spell.ownerId = casterId;
                spell.damage = (int)damage;
                spell.radius = radius;
                spell.duration = duration;
            }
        }

        /// <summary>
        /// 同步法术状态
        /// </summary>
        private void SyncSpellState()
        {
            if (Time.time - lastSyncTime < syncInterval)
                return;

            lastSyncTime = Time.time;

            // 检查位置是否需要同步
            if (Vector3.Distance(transform.position, networkPosition) > positionSyncThreshold)
            {
                // 更新网络位置
                networkPosition = transform.position;
                networkDirection = transform.forward;

                // 发送同步信息
                photonView.RPC("RPC_SyncSpellState", RpcTarget.Others, 
                    networkPosition, networkDirection, remainingDuration);
            }
        }

        /// <summary>
        /// 应用网络状态
        /// </summary>
        private void ApplyNetworkState()
        {
            // 平滑同步位置
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10f);
            transform.forward = Vector3.Lerp(transform.forward, networkDirection, Time.deltaTime * 10f);

            // 更新Spell组件状态
            if (spell != null)
            {
                // 使用状态来判断是否激活
            }
        }

        /// <summary>
        /// 更新法术持续时间
        /// </summary>
        private void UpdateDuration()
        {
            if (remainingDuration > 0)
            {
                remainingDuration -= Time.deltaTime;
                if (remainingDuration <= 0)
                {
                    DeactivateSpell();
                }
            }
        }

        /// <summary>
        /// 激活法术（网络同步）
        /// </summary>
        public void ActivateSpell()
        {
            if (photonView != null && photonView.IsMine)
            {
                remainingDuration = duration;
                photonView.RPC("RPC_ActivateSpell", RpcTarget.Others);
            }
        }

        /// <summary>
        /// 停用法术（网络同步）
        /// </summary>
        public void DeactivateSpell()
        {
            if (photonView != null)
            {
                if (photonView.IsMine)
                {
                    photonView.RPC("RPC_DeactivateSpell", RpcTarget.Others);
                }
                
                // 延迟销毁法术
                Destroy(gameObject, 1f);
            }
        }

        /// <summary>
        /// 法术命中（网络同步）
        /// </summary>
        /// <param name="targetId">目标ID</param>
        public void HitTarget(int targetId)
        {
            if (photonView != null)
            {
                photonView.RPC("RPC_HitTarget", RpcTarget.All, targetId);
            }
        }

        #region RPC Methods

        [PunRPC]
        private void RPC_SyncSpellState(Vector3 position, Vector3 direction, float currentDuration)
        {
            // 更新网络状态
            networkPosition = position;
            networkDirection = direction;
            remainingDuration = currentDuration;
        }

        [PunRPC]
        private void RPC_ActivateSpell()
        {
            remainingDuration = duration;
            if (spell != null)
            {
                spell.state = SpellState.Active;
            }
        }

        [PunRPC]
        private void RPC_DeactivateSpell()
        {
            if (spell != null)
            {
                spell.state = SpellState.Ended;
            }
        }

        [PunRPC]
        private void RPC_HitTarget(int targetId)
        {
            // 处理法术命中逻辑
            Debug.Log($"法术命中目标: ID={targetId}");
            
            if (spell != null)
            {
                // 法术效果已经在ApplySpellEffect中处理
            }
        }

        #endregion

        #region IPunObservable Implementation

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // 发送数据
                stream.SendNext(transform.position);
                stream.SendNext(transform.forward);
                stream.SendNext(remainingDuration);
            }
            else
            {
                // 接收数据
                networkPosition = (Vector3)stream.ReceiveNext();
                networkDirection = (Vector3)stream.ReceiveNext();
                remainingDuration = (float)stream.ReceiveNext();
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 根据ID查找法术
        /// </summary>
        /// <param name="id">法术ID</param>
        /// <returns>找到的法术</returns>
        private NetworkSpell FindSpellById(int id)
        {
            foreach (NetworkSpell networkSpell in FindObjectsOfType<NetworkSpell>())
            {
                if (networkSpell.spellId == id)
                {
                    return networkSpell;
                }
            }
            return null;
        }

        /// <summary>
        /// 检查是否是本地玩家的法术
        /// </summary>
        /// <returns>是否是本地玩家的法术</returns>
        public bool IsLocalPlayerSpell()
        {
            return PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer.ActorNumber == casterId;
        }

        #endregion

        #region Photon Callbacks

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            base.OnPlayerLeftRoom(otherPlayer);

            // 如果施法者离开，停用法术
            if (otherPlayer.ActorNumber == casterId)
            {
                DeactivateSpell();
            }
        }

        #endregion
    }
}
