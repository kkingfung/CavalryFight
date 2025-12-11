#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using CavalryFight.Core.Services;

namespace CavalryFight.Services.AI
{
    /// <summary>
    /// Blaze AI管理サービスのインターフェース
    /// </summary>
    /// <remarks>
    /// Blaze AIエンジンをラップし、MVVMパターンでAI敵の管理を簡素化します。
    /// AI敵のスポーン、ターゲット設定、状態管理などの機能を提供します。
    /// </remarks>
    public interface IBlazeAIService : IService
    {
        #region Events

        /// <summary>
        /// AI敵がスポーンされた時に発生します。
        /// </summary>
        event EventHandler<AISpawnedEventArgs>? AISpawned;

        /// <summary>
        /// AI敵が死亡した時に発生します。
        /// </summary>
        event EventHandler<AIDeathEventArgs>? AIDied;

        /// <summary>
        /// AI敵のターゲットが変更された時に発生します。
        /// </summary>
        event EventHandler<AITargetChangedEventArgs>? AITargetChanged;

        /// <summary>
        /// AI敵の状態が変更された時に発生します。
        /// </summary>
        event EventHandler<AIStateChangedEventArgs>? AIStateChanged;

        #endregion

        #region Properties

        /// <summary>
        /// 現在アクティブなAI敵のリストを取得します。
        /// </summary>
        IReadOnlyList<BlazeAI> ActiveAIs { get; }

        /// <summary>
        /// アクティブなAI敵の数を取得します。
        /// </summary>
        int ActiveAICount { get; }

        #endregion

        #region Spawn Management

        /// <summary>
        /// AI敵をスポーンします。
        /// </summary>
        /// <param name="prefab">スポーンするAI敵のPrefab</param>
        /// <param name="position">スポーン位置</param>
        /// <param name="rotation">スポーン回転</param>
        /// <returns>スポーンされたAI敵</returns>
        BlazeAI? SpawnAI(GameObject prefab, Vector3 position, Quaternion rotation);

        /// <summary>
        /// 指定されたAI敵を削除します。
        /// </summary>
        /// <param name="ai">削除するAI敵</param>
        void RemoveAI(BlazeAI ai);

        /// <summary>
        /// すべてのAI敵を削除します。
        /// </summary>
        void RemoveAllAIs();

        #endregion

        #region Target Management

        /// <summary>
        /// 指定されたAI敵のターゲットを設定します。
        /// </summary>
        /// <param name="ai">AI敵</param>
        /// <param name="target">ターゲット</param>
        /// <param name="turnToAttackState">攻撃状態に遷移するか</param>
        void SetAITarget(BlazeAI ai, GameObject target, bool turnToAttackState = true);

        /// <summary>
        /// すべてのAI敵に同じターゲットを設定します。
        /// </summary>
        /// <param name="target">ターゲット</param>
        /// <param name="turnToAttackState">攻撃状態に遷移するか</param>
        void SetAllAIsTarget(GameObject target, bool turnToAttackState = true);

        /// <summary>
        /// 指定されたAI敵のターゲットをクリアします。
        /// </summary>
        /// <param name="ai">AI敵</param>
        void ClearAITarget(BlazeAI ai);

        #endregion

        #region AI Control

        /// <summary>
        /// 指定されたAI敵をフレンドリーモードに設定します。
        /// </summary>
        /// <param name="ai">AI敵</param>
        /// <param name="isFriendly">フレンドリーかどうか</param>
        void SetAIFriendly(BlazeAI ai, bool isFriendly);

        /// <summary>
        /// 指定されたAI敵を特定の位置に移動させます。
        /// </summary>
        /// <param name="ai">AI敵</param>
        /// <param name="position">目標位置</param>
        void MoveAITo(BlazeAI ai, Vector3 position);

        /// <summary>
        /// 指定されたAI敵を特定の方向に回転させます。
        /// </summary>
        /// <param name="ai">AI敵</param>
        /// <param name="direction">回転方向</param>
        /// <param name="speed">回転速度</param>
        void RotateAI(BlazeAI ai, Vector3 direction, float speed);

        #endregion

        #region Audio Control

        /// <summary>
        /// 指定されたAI敵のパトロールオーディオを再生します。
        /// </summary>
        /// <param name="ai">AI敵</param>
        void PlayAIPatrolAudio(BlazeAI ai);

        /// <summary>
        /// 指定されたAI敵のオーディオを停止します。
        /// </summary>
        /// <param name="ai">AI敵</param>
        void StopAIAudio(BlazeAI ai);

        #endregion

        #region Query Methods

        /// <summary>
        /// 指定された範囲内のAI敵を取得します。
        /// </summary>
        /// <param name="position">中心位置</param>
        /// <param name="radius">半径</param>
        /// <returns>範囲内のAI敵のリスト</returns>
        List<BlazeAI> GetAIsInRadius(Vector3 position, float radius);

        /// <summary>
        /// プレイヤーに最も近いAI敵を取得します。
        /// </summary>
        /// <param name="playerPosition">プレイヤーの位置</param>
        /// <returns>最も近いAI敵。存在しない場合はnull</returns>
        BlazeAI? GetClosestAI(Vector3 playerPosition);

        #endregion
    }
}
