#nullable enable

using CavalryFight.Core.Services;
using UnityEngine;

namespace CavalryFight.Examples.PlayerHealth
{
    /// <summary>
    /// プレイヤー体力を管理するサービス
    /// </summary>
    /// <remarks>
    /// Serviceは複数のViewModelから共有されるビジネスロジックを提供します。
    /// この例では、グローバルなプレイヤー体力状態を管理します。
    /// </remarks>
    public class PlayerHealthService : IService
    {
        #region Fields

        private PlayerHealthModel? _playerHealth;

        #endregion

        #region Properties

        /// <summary>
        /// 現在のプレイヤー体力モデルを取得します。
        /// </summary>
        public PlayerHealthModel? CurrentPlayerHealth => _playerHealth;

        #endregion

        #region IService Implementation

        /// <summary>
        /// サービスを初期化します。
        /// </summary>
        public void Initialize()
        {
            Debug.Log("[PlayerHealthService] Initializing...");

            // デフォルトのプレイヤー体力を作成
            _playerHealth = new PlayerHealthModel(maxHealth: 100);

            Debug.Log("[PlayerHealthService] Initialized.");
        }

        /// <summary>
        /// サービスを破棄します。
        /// </summary>
        public void Dispose()
        {
            Debug.Log("[PlayerHealthService] Disposing...");
            _playerHealth = null;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 新しいプレイヤー体力を作成します。
        /// </summary>
        /// <param name="maxHealth">最大体力</param>
        public void CreateNewPlayer(int maxHealth = 100)
        {
            _playerHealth = new PlayerHealthModel(maxHealth);
            Debug.Log($"[PlayerHealthService] Created new player with {maxHealth} max health.");
        }

        /// <summary>
        /// プレイヤーの体力状態をリセットします。
        /// </summary>
        public void ResetHealth()
        {
            _playerHealth?.RestoreToFull();
            Debug.Log("[PlayerHealthService] Player health reset.");
        }

        #endregion
    }
}
