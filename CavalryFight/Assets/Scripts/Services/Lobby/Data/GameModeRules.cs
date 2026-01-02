#nullable enable

namespace CavalryFight.Services.Lobby
{
    /// <summary>
    /// ゲームモードごとのルール定義
    /// </summary>
    /// <remarks>
    /// 各ゲームモードで有効/無効な設定を定義します。
    ///
    /// ゲームモードごとの要件:
    /// - Arena: 時間制限必須、無制限の矢、プレイヤーは死なない、勝者は最高スコア
    /// - ScoreMatch: 時間制限必須、矢の制限あり、プレイヤーは死なない、勝者は最高スコア
    /// - TeamFight: 時間制限必須（Arenaと同じだがチーム戦）、無制限の矢、プレイヤーは死なない、勝者はチーム最高スコア
    /// - Deathmatch: 時間制限なし（固定）、無制限の矢、プレイヤーは死ぬ、最後の一人/チームが勝者
    /// - Hunting: チーム対抗、時間制限必須、無制限の矢、ハンター1人+ウルフ複数/チーム、ハンターはスタンのみ（死なない）
    /// </remarks>
    public static class GameModeRules
    {
        /// <summary>
        /// 指定されたゲームモードでタイムリミットが必須かどうかを取得します
        /// </summary>
        /// <param name="mode">ゲームモード</param>
        /// <returns>タイムリミットが必須（"No Limit"を選択できない）場合はtrue</returns>
        public static bool IsTimeLimitRequired(GameMode mode)
        {
            return mode switch
            {
                GameMode.Arena => true,       // 制限時間内でのスコア獲得（必須）
                GameMode.ScoreMatch => true,  // ラウンドごとの時間制限（必須）
                GameMode.TeamFight => true,   // チーム戦での時間制限（必須）
                GameMode.Deathmatch => false, // 最後の一人まで戦う（時間制限なし固定）
                GameMode.Hunting => true,     // 時間制限必須
                _ => false
            };
        }

        /// <summary>
        /// 指定されたゲームモードでタイムリミットが"No Limit"で固定されているかを取得します
        /// </summary>
        /// <param name="mode">ゲームモード</param>
        /// <returns>タイムリミットが"No Limit"で固定（変更不可）の場合はtrue</returns>
        public static bool IsTimeLimitLockedToNoLimit(GameMode mode)
        {
            return mode switch
            {
                GameMode.Deathmatch => true, // Deathmatchは常に時間制限なし
                _ => false
            };
        }

        /// <summary>
        /// 指定されたゲームモードでタイムリミットが編集可能かどうかを取得します
        /// </summary>
        /// <param name="mode">ゲームモード</param>
        /// <returns>タイムリミットが編集可能な場合はtrue</returns>
        public static bool IsTimeLimitEditable(GameMode mode)
        {
            return mode switch
            {
                GameMode.Arena => true,       // 編集可能（ただし"No Limit"は選択不可）
                GameMode.ScoreMatch => true,  // 編集可能（ただし"No Limit"は選択不可）
                GameMode.TeamFight => true,   // 編集可能（ただし"No Limit"は選択不可）
                GameMode.Deathmatch => false, // 固定（常に"No Limit"）
                GameMode.Hunting => true,     // 編集可能（ただし"No Limit"は選択不可）
                _ => false
            };
        }

        /// <summary>
        /// 指定されたゲームモードのデフォルトタイムリミットを取得します（秒）
        /// </summary>
        /// <param name="mode">ゲームモード</param>
        /// <returns>デフォルトタイムリミット（秒）、制限なしの場合は0</returns>
        public static int GetDefaultTimeLimit(GameMode mode)
        {
            return mode switch
            {
                GameMode.Arena => 300,      // 5分
                GameMode.ScoreMatch => 180, // 3分（ラウンドごと）
                GameMode.TeamFight => 600,  // 10分
                GameMode.Deathmatch => 0,   // 制限なし（固定）
                GameMode.Hunting => 600,    // 10分
                _ => 0
            };
        }

        /// <summary>
        /// 指定されたゲームモードで矢の制限が必須かどうかを取得します
        /// </summary>
        /// <param name="mode">ゲームモード</param>
        /// <returns>矢の制限が必須（"No Limit"を選択できない）場合はtrue</returns>
        public static bool IsArrowLimitRequired(GameMode mode)
        {
            return mode switch
            {
                GameMode.Arena => false,      // 無制限の矢
                GameMode.ScoreMatch => true,  // 矢の制限あり（必須）
                GameMode.TeamFight => false,  // 無制限の矢
                GameMode.Deathmatch => false, // 無制限の矢
                GameMode.Hunting => false,    // 無制限の矢
                _ => false
            };
        }

        /// <summary>
        /// 指定されたゲームモードで矢の制限が"No Limit"で固定されているかを取得します
        /// </summary>
        /// <param name="mode">ゲームモード</param>
        /// <returns>矢の制限が"No Limit"で固定（変更不可）の場合はtrue</returns>
        public static bool IsArrowLimitLockedToNoLimit(GameMode mode)
        {
            return mode switch
            {
                GameMode.Arena => true,       // 常に無制限
                GameMode.ScoreMatch => false, // 制限あり（編集可能）
                GameMode.TeamFight => true,   // 常に無制限
                GameMode.Deathmatch => true,  // 常に無制限
                GameMode.Hunting => true,     // 常に無制限
                _ => true
            };
        }

        /// <summary>
        /// 指定されたゲームモードで矢の制限が編集可能かどうかを取得します
        /// </summary>
        /// <param name="mode">ゲームモード</param>
        /// <returns>矢の制限が編集可能な場合はtrue</returns>
        public static bool IsArrowLimitEditable(GameMode mode)
        {
            return mode switch
            {
                GameMode.Arena => false,      // 固定（常に無制限）
                GameMode.ScoreMatch => true,  // 編集可能（ただし"No Limit"は選択不可）
                GameMode.TeamFight => false,  // 固定（常に無制限）
                GameMode.Deathmatch => false, // 固定（常に無制限）
                GameMode.Hunting => false,    // 固定（常に無制限）
                _ => false
            };
        }

        /// <summary>
        /// 指定されたゲームモードのデフォルト矢の制限数を取得します
        /// </summary>
        /// <param name="mode">ゲームモード</param>
        /// <returns>デフォルト矢の制限数、無制限の場合は0</returns>
        public static int GetDefaultArrowLimit(GameMode mode)
        {
            return mode switch
            {
                GameMode.Arena => 0,         // 無制限（固定）
                GameMode.ScoreMatch => 5,    // 5本（デフォルト値）
                GameMode.TeamFight => 0,     // 無制限（固定）
                GameMode.Deathmatch => 0,    // 無制限（固定）
                GameMode.Hunting => 0,       // 無制限（固定）
                _ => 0
            };
        }

        /// <summary>
        /// 指定されたゲームモードでNPCが無効化されているかを取得します
        /// </summary>
        /// <param name="mode">ゲームモード</param>
        /// <returns>NPCが無効（スポーンしない）の場合はtrue</returns>
        public static bool IsNPCDisabled(GameMode mode)
        {
            return mode switch
            {
                GameMode.Arena => false,      // NPCあり
                GameMode.ScoreMatch => false, // NPCあり
                GameMode.TeamFight => false,  // NPCあり
                GameMode.Deathmatch => false, // NPCあり
                GameMode.Hunting => true,     // NPCなし（プレイヤーのみ）
                _ => false
            };
        }
    }
}
