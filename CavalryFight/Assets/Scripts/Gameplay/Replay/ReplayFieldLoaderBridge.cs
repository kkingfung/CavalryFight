#nullable enable

using UnityEngine;
using CavalryFight.Services.Lobby;
using CavalryFight.Services.Replay;

namespace CavalryFight.Gameplay.Replay
{
    /// <summary>
    /// ReplayPlaybackManagerとFieldLoaderを接続するブリッジコンポーネント
    /// </summary>
    /// <remarks>
    /// ReplayPlaybackManagerはServicesアセンブリにあり、FieldLoaderはGameplayにあるため、
    /// 直接参照できません。このコンポーネントがイベントを購読してフィールドロードを実行します。
    /// Replayシーンに配置してください。
    ///
    /// 重要: Awakeでイベントを購読することで、ReplayPlaybackManager.Start()が
    /// FieldLoadRequestedを発火する前に確実に購読されます。
    /// </remarks>
    public class ReplayFieldLoaderBridge : MonoBehaviour
    {
        private void Awake()
        {
            // Awakeで購読することで、Start()より前に確実に購読される
            ReplayPlaybackManager.FieldLoadRequested += OnFieldLoadRequested;
            Debug.Log("[ReplayFieldLoaderBridge] Awake - FieldLoadRequested イベントを購読しました。");
        }

        private void OnDestroy()
        {
            ReplayPlaybackManager.FieldLoadRequested -= OnFieldLoadRequested;
        }

        /// <summary>
        /// フィールドロード要求を処理します
        /// </summary>
        /// <param name="mapName">マップ名</param>
        private void OnFieldLoadRequested(MapName mapName)
        {
            if (FieldLoader.Instance != null)
            {
                bool success = FieldLoader.Instance.LoadField(mapName);
                Debug.Log($"[ReplayFieldLoaderBridge] フィールドロード: {mapName} - {(success ? "成功" : "失敗")}");
            }
            else
            {
                Debug.LogWarning("[ReplayFieldLoaderBridge] FieldLoader.Instance が見つかりません。");
            }
        }
    }
}
