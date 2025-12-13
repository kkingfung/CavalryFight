#nullable enable

using Unity.Netcode;
using UnityEngine;

namespace CavalryFight.Services.Match
{
    /// <summary>
    /// プレイヤーネットワーク識別子
    /// </summary>
    /// <remarks>
    /// プレイヤーのルートオブジェクトに取り付けて、
    /// 子オブジェクトのコライダーからプレイヤーのClientIdを取得できるようにします。
    /// </remarks>
    [RequireComponent(typeof(NetworkObject))]
    public class PlayerNetworkIdentity : MonoBehaviour
    {
        #region Fields

        /// <summary>
        /// NetworkObjectへの参照（キャッシュ）
        /// </summary>
        private NetworkObject? _networkObject;

        #endregion

        #region Properties

        /// <summary>
        /// このプレイヤーのクライアントIDを取得します
        /// </summary>
        public ulong ClientId
        {
            get
            {
                if (_networkObject == null)
                {
                    _networkObject = GetComponent<NetworkObject>();
                }

                if (_networkObject != null && _networkObject.IsSpawned)
                {
                    return _networkObject.OwnerClientId;
                }

                Debug.LogWarning($"[PlayerNetworkIdentity] {gameObject.name} is not spawned or has no NetworkObject!", this);
                return 0;
            }
        }

        /// <summary>
        /// このプレイヤーがスポーン済みかどうかを取得します
        /// </summary>
        public bool IsSpawned
        {
            get
            {
                if (_networkObject == null)
                {
                    _networkObject = GetComponent<NetworkObject>();
                }

                return _networkObject != null && _networkObject.IsSpawned;
            }
        }

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Awake時の初期化
        /// </summary>
        private void Awake()
        {
            _networkObject = GetComponent<NetworkObject>();
        }

        /// <summary>
        /// インスペクターで値が変更された時に呼ばれます
        /// </summary>
        private void OnValidate()
        {
            // NetworkObjectが存在することを確認
            if (GetComponent<NetworkObject>() == null)
            {
                Debug.LogWarning($"[PlayerNetworkIdentity] {gameObject.name} has PlayerNetworkIdentity but no NetworkObject!", this);
            }
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// コライダーからプレイヤーのClientIdを取得します
        /// </summary>
        /// <param name="hitCollider">命中したコライダー</param>
        /// <returns>プレイヤーのClientId（0 = プレイヤーではない）</returns>
        public static ulong GetClientIdFromCollider(Collider hitCollider)
        {
            // 親階層を遡ってPlayerNetworkIdentityを探す
            var identity = hitCollider.GetComponentInParent<PlayerNetworkIdentity>();

            if (identity != null && identity.IsSpawned)
            {
                return identity.ClientId;
            }

            return 0;
        }

        /// <summary>
        /// コライダーからヒットボックス情報を取得します
        /// </summary>
        /// <param name="hitCollider">命中したコライダー</param>
        /// <returns>ヒットボックスコンポーネント（null = ヒットボックスなし）</returns>
        public static HitboxComponent? GetHitboxFromCollider(Collider hitCollider)
        {
            return hitCollider.GetComponent<HitboxComponent>();
        }

        #endregion
    }
}
