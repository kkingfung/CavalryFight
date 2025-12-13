#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// リプレイフレーム（特定時刻のゲーム状態スナップショット）
    /// </summary>
    /// <remarks>
    /// リプレイの1フレーム分のデータを保持します。
    /// すべてのエンティティの状態とゲーム状態を含みます。
    /// </remarks>
    [Serializable]
    public class ReplayFrame
    {
        /// <summary>
        /// フレームのタイムスタンプ（秒）
        /// </summary>
        /// <remarks>
        /// マッチ開始からの経過時間です。
        /// </remarks>
        public float Timestamp = 0f;

        /// <summary>
        /// エンティティのスナップショットリスト
        /// </summary>
        public List<EntitySnapshot> Entities = new List<EntitySnapshot>();

        /// <summary>
        /// プレイヤースコア
        /// </summary>
        public int PlayerScore = 0;

        /// <summary>
        /// 敵スコア
        /// </summary>
        public int EnemyScore = 0;

        /// <summary>
        /// 残り時間（秒）
        /// </summary>
        /// <remarks>
        /// -1の場合は時間制限なし
        /// </remarks>
        public float TimeRemaining = -1f;

        /// <summary>
        /// ゲーム状態（Playing, Paused, Finished等）
        /// </summary>
        public string GameState = "Playing";

        /// <summary>
        /// ReplayFrameの新しいインスタンスを初期化します
        /// </summary>
        public ReplayFrame()
        {
        }

        /// <summary>
        /// ReplayFrameの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="timestamp">タイムスタンプ（秒）</param>
        public ReplayFrame(float timestamp)
        {
            Timestamp = timestamp;
        }

        /// <summary>
        /// エンティティスナップショットを追加します
        /// </summary>
        /// <param name="snapshot">追加するスナップショット</param>
        public void AddEntity(EntitySnapshot snapshot)
        {
            Entities.Add(snapshot);
        }

        /// <summary>
        /// 指定されたIDのエンティティスナップショットを取得します
        /// </summary>
        /// <param name="entityId">エンティティID</param>
        /// <returns>見つかった場合はEntitySnapshot、見つからない場合はnull</returns>
        public EntitySnapshot? GetEntity(string entityId)
        {
            foreach (var entity in Entities)
            {
                if (entity.EntityId == entityId)
                {
                    return entity;
                }
            }
            return null;
        }

        /// <summary>
        /// 指定されたタイプのエンティティスナップショットをすべて取得します
        /// </summary>
        /// <param name="entityType">エンティティタイプ</param>
        /// <returns>該当するスナップショットのリスト</returns>
        public List<EntitySnapshot> GetEntitiesByType(EntityType entityType)
        {
            var result = new List<EntitySnapshot>();
            foreach (var entity in Entities)
            {
                if (entity.EntityType == entityType)
                {
                    result.Add(entity);
                }
            }
            return result;
        }

        /// <summary>
        /// 2つのフレーム間を補間します
        /// </summary>
        /// <param name="from">開始フレーム</param>
        /// <param name="to">終了フレーム</param>
        /// <param name="t">補間係数（0.0～1.0）</param>
        /// <returns>補間されたフレーム</returns>
        public static ReplayFrame Lerp(ReplayFrame from, ReplayFrame to, float t)
        {
            var interpolated = new ReplayFrame
            {
                Timestamp = Mathf.Lerp(from.Timestamp, to.Timestamp, t),
                PlayerScore = t < 0.5f ? from.PlayerScore : to.PlayerScore,
                EnemyScore = t < 0.5f ? from.EnemyScore : to.EnemyScore,
                TimeRemaining = Mathf.Lerp(from.TimeRemaining, to.TimeRemaining, t),
                GameState = to.GameState
            };

            // エンティティの補間
            // 両方のフレームに存在するエンティティのみ補間
            foreach (var fromEntity in from.Entities)
            {
                var toEntity = to.GetEntity(fromEntity.EntityId);
                if (toEntity != null)
                {
                    interpolated.AddEntity(EntitySnapshot.Lerp(fromEntity, toEntity, t));
                }
                else
                {
                    // toフレームに存在しない場合はfromをそのまま使用
                    interpolated.AddEntity(fromEntity.Clone());
                }
            }

            // toにのみ存在するエンティティを追加（新規出現）
            foreach (var toEntity in to.Entities)
            {
                if (from.GetEntity(toEntity.EntityId) == null)
                {
                    interpolated.AddEntity(toEntity.Clone());
                }
            }

            return interpolated;
        }

        /// <summary>
        /// フレームのコピーを作成します
        /// </summary>
        /// <returns>コピーされたフレーム</returns>
        public ReplayFrame Clone()
        {
            var cloned = new ReplayFrame
            {
                Timestamp = this.Timestamp,
                PlayerScore = this.PlayerScore,
                EnemyScore = this.EnemyScore,
                TimeRemaining = this.TimeRemaining,
                GameState = this.GameState
            };

            foreach (var entity in Entities)
            {
                cloned.AddEntity(entity.Clone());
            }

            return cloned;
        }
    }
}
