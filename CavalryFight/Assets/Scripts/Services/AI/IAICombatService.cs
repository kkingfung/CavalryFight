#nullable enable

using System;
using System.Collections.Generic;
using CavalryFight.Core.Services;
using CavalryFight.Services.Lobby;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CavalryFight.Services.AI
{
    /// <summary>
    /// AI戦闘システムを管理するサービスのインターフェース
    /// </summary>
    /// <remarks>
    /// BlazeAIを使用してAIプレイヤーの戦闘行動を制御します。
    /// マッチ開始時にAIをスポーンし、ゲームモードに応じた行動を設定します。
    /// </remarks>
    public interface IAICombatService : IService
    {
        #region Properties

        /// <summary>
        /// アクティブなAIプレイヤーの数
        /// </summary>
        int ActiveAICount { get; }

        /// <summary>
        /// AIが有効かどうか
        /// </summary>
        bool IsEnabled { get; }

        #endregion

        #region AI Lifecycle

        /// <summary>
        /// AIシステムを初期化します
        /// </summary>
        /// <param name="gameMode">ゲームモード</param>
        /// <param name="difficulty">AI難易度</param>
        void Initialize(GameMode gameMode, AIDifficulty difficulty);

        /// <summary>
        /// AIをスポーンするターゲットシーンを設定します
        /// </summary>
        /// <remarks>
        /// ローディング画面表示中にAIをスポーンする場合、AIオブジェクトがローディングシーンに
        /// 作成されてしまう問題を回避するため、事前にターゲットシーンを設定する必要があります。
        /// </remarks>
        /// <param name="scene">AIをスポーンするシーン</param>
        void SetTargetScene(Scene scene);

        /// <summary>
        /// AIプレイヤーをスポーンします
        /// </summary>
        /// <param name="spawnPoint">スポーン位置</param>
        /// <param name="teamIndex">チームインデックス（-1の場合はチームなし）</param>
        /// <param name="aiId">AIのユニークID</param>
        /// <returns>スポーンしたAIプレイヤーのGameObject</returns>
        GameObject? SpawnAIPlayer(Vector3 spawnPoint, Quaternion rotation, int teamIndex, ulong aiId);

        /// <summary>
        /// 指定したAIプレイヤーを削除します
        /// </summary>
        /// <param name="aiId">AIのユニークID</param>
        void DespawnAIPlayer(ulong aiId);

        /// <summary>
        /// すべてのAIプレイヤーを削除します
        /// </summary>
        void DespawnAllAIPlayers();

        /// <summary>
        /// AIシステムをクリーンアップします
        /// </summary>
        void Cleanup();

        #endregion

        #region AI Control

        /// <summary>
        /// すべてのAIの行動を有効化します
        /// </summary>
        void EnableAllAI();

        /// <summary>
        /// すべてのAIの行動を無効化します
        /// </summary>
        void DisableAllAI();

        /// <summary>
        /// 指定したAIのターゲットを設定します
        /// </summary>
        /// <param name="aiId">AIのユニークID</param>
        /// <param name="target">ターゲットのGameObject</param>
        void SetAITarget(ulong aiId, GameObject target);

        /// <summary>
        /// 指定したAIを攻撃状態に遷移させます
        /// </summary>
        /// <param name="aiId">AIのユニークID</param>
        void TriggerAIAttack(ulong aiId);

        /// <summary>
        /// 指定したAIにダメージを与えます
        /// </summary>
        /// <param name="aiId">AIのユニークID</param>
        /// <param name="damage">ダメージ量</param>
        /// <param name="attacker">攻撃者のGameObject</param>
        void DamageAI(ulong aiId, int damage, GameObject? attacker);

        /// <summary>
        /// 指定したAIを死亡させます
        /// </summary>
        /// <param name="aiId">AIのユニークID</param>
        /// <param name="killer">キルしたプレイヤーのGameObject</param>
        void KillAI(ulong aiId, GameObject? killer);

        #endregion

        #region AI State

        /// <summary>
        /// 指定したAIが生存しているかどうかを取得します
        /// </summary>
        /// <param name="aiId">AIのユニークID</param>
        /// <returns>生存している場合はtrue</returns>
        bool IsAIAlive(ulong aiId);

        /// <summary>
        /// 指定したAIのGameObjectを取得します
        /// </summary>
        /// <param name="aiId">AIのユニークID</param>
        /// <returns>AIのGameObject（存在しない場合はnull）</returns>
        GameObject? GetAIGameObject(ulong aiId);

        /// <summary>
        /// すべてのAI IDを取得します
        /// </summary>
        /// <returns>AI IDのコレクション</returns>
        IReadOnlyCollection<ulong> GetAllAIIds();

        /// <summary>
        /// 指定したチームのAI IDを取得します
        /// </summary>
        /// <param name="teamIndex">チームインデックス</param>
        /// <returns>AI IDのコレクション</returns>
        IReadOnlyCollection<ulong> GetAIIdsByTeam(int teamIndex);

        #endregion

        #region Events

        /// <summary>
        /// AIがスポーンした時に発生します
        /// </summary>
        event Action<ulong, GameObject>? AISpawned;

        /// <summary>
        /// AIが死亡した時に発生します
        /// </summary>
        event Action<ulong, ulong>? AIDied;

        /// <summary>
        /// AIがスコアを獲得した時に発生します
        /// </summary>
        event Action<ulong, int>? AIScored;

        /// <summary>
        /// AIが矢を発射した時に発生します
        /// </summary>
        event Action<ulong>? AIFiredArrow;

        #endregion
    }
}
