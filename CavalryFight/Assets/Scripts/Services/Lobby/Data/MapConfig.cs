#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavalryFight.Services.Lobby
{
    /// <summary>
    /// マップエントリー（マップ名とプレハブのマッピング）
    /// </summary>
    [Serializable]
    public struct MapEntry
    {
        /// <summary>
        /// マップ名
        /// </summary>
        [Tooltip("マップ名")]
        public MapName MapName;

        /// <summary>
        /// 表示名
        /// </summary>
        [Tooltip("UI表示用の名前")]
        public string DisplayName;

        /// <summary>
        /// フィールドプレハブ
        /// </summary>
        [Tooltip("フィールドプレハブ")]
        public GameObject? Prefab;

        /// <summary>
        /// サムネイル画像
        /// </summary>
        [Tooltip("マップ選択画面用のサムネイル")]
        public Sprite? Thumbnail;

        /// <summary>
        /// 説明
        /// </summary>
        [TextArea(2, 4)]
        [Tooltip("マップの説明")]
        public string Description;

        /// <summary>
        /// 対応するゲームモード
        /// </summary>
        [Tooltip("このマップで利用可能なゲームモード")]
        public GameMode[] SupportedGameModes;
    }

    /// <summary>
    /// マップ設定のScriptableObject
    /// </summary>
    /// <remarks>
    /// すべてのマップ情報を一元管理します。
    /// FieldLoader、MatchLobby、MatchRoomなどから参照されます。
    /// </remarks>
    [CreateAssetMenu(fileName = "MapConfig", menuName = "CavalryFight/Config/Map Config")]
    public class MapConfig : ScriptableObject
    {
        #region Serialized Fields

        [Header("Map Entries")]
        [Tooltip("利用可能なマップのリスト")]
        [SerializeField] private List<MapEntry> _maps = new List<MapEntry>();

        [Header("Defaults")]
        [Tooltip("デフォルトマップ")]
        [SerializeField] private MapName _defaultMap = MapName.Arena;

        #endregion

        #region Properties

        /// <summary>
        /// 利用可能なマップのリスト
        /// </summary>
        public IReadOnlyList<MapEntry> Maps => _maps;

        /// <summary>
        /// デフォルトマップ
        /// </summary>
        public MapName DefaultMap => _defaultMap;

        /// <summary>
        /// マップ数
        /// </summary>
        public int Count => _maps.Count;

        #endregion

        #region Public Methods

        /// <summary>
        /// マップ名からエントリーを取得します
        /// </summary>
        /// <param name="mapName">マップ名</param>
        /// <returns>マップエントリー、見つからない場合はnull</returns>
        public MapEntry? GetEntry(MapName mapName)
        {
            foreach (var entry in _maps)
            {
                if (entry.MapName == mapName)
                {
                    return entry;
                }
            }
            return null;
        }

        /// <summary>
        /// マップ名からプレハブを取得します
        /// </summary>
        /// <param name="mapName">マップ名</param>
        /// <returns>フィールドプレハブ、見つからない場合はnull</returns>
        public GameObject? GetPrefab(MapName mapName)
        {
            return GetEntry(mapName)?.Prefab;
        }

        /// <summary>
        /// マップ名から表示名を取得します
        /// </summary>
        /// <param name="mapName">マップ名</param>
        /// <returns>表示名、見つからない場合はenum名</returns>
        public string GetDisplayName(MapName mapName)
        {
            var entry = GetEntry(mapName);
            if (entry.HasValue && !string.IsNullOrEmpty(entry.Value.DisplayName))
            {
                return entry.Value.DisplayName;
            }
            return mapName.ToString();
        }

        /// <summary>
        /// 指定したゲームモードで利用可能なマップのリストを取得します
        /// </summary>
        /// <param name="gameMode">ゲームモード</param>
        /// <returns>対応するマップ名のリスト</returns>
        public List<MapName> GetMapsForGameMode(GameMode gameMode)
        {
            var result = new List<MapName>();
            foreach (var entry in _maps)
            {
                if (entry.SupportedGameModes == null || entry.SupportedGameModes.Length == 0)
                {
                    // ゲームモード指定がない場合は全モードで利用可能
                    result.Add(entry.MapName);
                }
                else
                {
                    foreach (var mode in entry.SupportedGameModes)
                    {
                        if (mode == gameMode)
                        {
                            result.Add(entry.MapName);
                            break;
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// すべてのマップ名を取得します
        /// </summary>
        /// <returns>マップ名のリスト</returns>
        public List<MapName> GetAllMapNames()
        {
            var result = new List<MapName>();
            foreach (var entry in _maps)
            {
                result.Add(entry.MapName);
            }
            return result;
        }

        /// <summary>
        /// すべての表示名を取得します
        /// </summary>
        /// <returns>表示名のリスト</returns>
        public List<string> GetAllDisplayNames()
        {
            var result = new List<string>();
            foreach (var entry in _maps)
            {
                result.Add(!string.IsNullOrEmpty(entry.DisplayName) ? entry.DisplayName : entry.MapName.ToString());
            }
            return result;
        }

        #endregion

        #region Validation

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 重複チェック
            var seen = new HashSet<MapName>();
            foreach (var entry in _maps)
            {
                if (seen.Contains(entry.MapName))
                {
                    Debug.LogWarning($"[MapConfig] 重複したマップ名があります: {entry.MapName}");
                }
                seen.Add(entry.MapName);
            }
        }
#endif

        #endregion
    }
}
