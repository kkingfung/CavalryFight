#nullable enable

using UnityEngine;

namespace CavalryFight.Bootstrap
{
    /// <summary>
    /// NetworkManagerをシーン間で永続化します
    /// </summary>
    /// <remarks>
    /// NetworkManagerをDontDestroyOnLoadに設定し、重複インスタンスを防ぎます。
    /// </remarks>
    public class NetworkManagerPersistence : MonoBehaviour
    {
        /// <summary>
        /// 既にインスタンスが存在するか
        /// </summary>
        private static bool _instanceExists = false;

        /// <summary>
        /// Awake時の処理
        /// </summary>
        private void Awake()
        {
            if (_instanceExists)
            {
                // 既にインスタンスが存在する場合は破棄
                Debug.LogWarning("[NetworkManagerPersistence] NetworkManager already exists. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            // シーン間で永続化
            DontDestroyOnLoad(gameObject);
            _instanceExists = true;

            Debug.Log("[NetworkManagerPersistence] NetworkManager marked as DontDestroyOnLoad.");
        }

        /// <summary>
        /// 破棄時の処理
        /// </summary>
        private void OnDestroy()
        {
            if (_instanceExists && gameObject != null)
            {
                _instanceExists = false;
            }
        }
    }
}
