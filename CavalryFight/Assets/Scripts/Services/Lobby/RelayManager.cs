#nullable enable

using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace CavalryFight.Services.Lobby
{
    /// <summary>
    /// Relayマネージャー
    /// </summary>
    /// <remarks>
    /// Unity Relay Serviceとの統合を管理します。
    /// ホストの割り当て作成とゲストの参加を処理します。
    /// </remarks>
    public class RelayManager
    {
        #region Constants

        /// <summary>
        /// 最大接続数
        /// </summary>
        private const int MAX_CONNECTIONS = 7; // ホスト + 7ゲスト = 8プレイヤー

        #endregion

        #region Fields

        /// <summary>
        /// 初期化済みフラグ
        /// </summary>
        private bool _initialized = false;

        /// <summary>
        /// 現在のジョインコード
        /// </summary>
        private string? _currentJoinCode;

        #endregion

        #region Properties

        /// <summary>
        /// 現在のジョインコードを取得します
        /// </summary>
        public string? CurrentJoinCode => _currentJoinCode;

        #endregion

        #region Initialization

        /// <summary>
        /// Unity Servicesを初期化します
        /// </summary>
        /// <returns>初期化に成功した場合はtrue</returns>
        public async Task<bool> InitializeAsync()
        {
            if (_initialized)
            {
                return true;
            }

            try
            {
                await UnityServices.InitializeAsync();
                _initialized = true;
                Debug.Log("[RelayManager] Unity Services initialized successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RelayManager] Failed to initialize Unity Services: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Host Methods

        /// <summary>
        /// ホストとしてRelayを開始します
        /// </summary>
        /// <returns>ジョインコード</returns>
        public async Task<string?> StartHostAsync()
        {
            if (!_initialized)
            {
                Debug.LogError("[RelayManager] RelayManager not initialized. Call InitializeAsync() first.");
                return null;
            }

            try
            {
                // Relay割り当てを作成
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MAX_CONNECTIONS);

                // ジョインコードを取得
                _currentJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                // Unity TransportにRelay情報を設定
                var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                unityTransport.SetHostRelayData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData
                );

                Debug.Log($"[RelayManager] Host started with join code: {_currentJoinCode}");
                return _currentJoinCode;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RelayManager] Failed to start host: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Client Methods

        /// <summary>
        /// クライアントとしてRelayに参加します
        /// </summary>
        /// <param name="joinCode">ジョインコード</param>
        /// <returns>成功した場合はtrue</returns>
        public async Task<bool> JoinRelayAsync(string joinCode)
        {
            if (!_initialized)
            {
                Debug.LogError("[RelayManager] RelayManager not initialized. Call InitializeAsync() first.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(joinCode))
            {
                Debug.LogError("[RelayManager] Join code is empty.");
                return false;
            }

            try
            {
                // ジョインコードを使用してRelay割り当てに参加
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                // Unity TransportにRelay情報を設定
                var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                unityTransport.SetClientRelayData(
                    joinAllocation.RelayServer.IpV4,
                    (ushort)joinAllocation.RelayServer.Port,
                    joinAllocation.AllocationIdBytes,
                    joinAllocation.Key,
                    joinAllocation.ConnectionData,
                    joinAllocation.HostConnectionData
                );

                _currentJoinCode = joinCode;
                Debug.Log($"[RelayManager] Joined relay with code: {joinCode}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RelayManager] Failed to join relay: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Relayをクリーンアップします
        /// </summary>
        public void Cleanup()
        {
            _currentJoinCode = null;
            Debug.Log("[RelayManager] Relay cleaned up.");
        }

        #endregion
    }
}
