#nullable enable

using System;
using UnityEngine;
using CavalryFight.Services.Match;

namespace CavalryFight.ViewModels.Data
{
    /// <summary>
    /// スコア獲得イベント引数
    /// </summary>
    public class ScoreGainedEventArgs : EventArgs
    {
        public ulong ClientId { get; }
        public int Score { get; }
        public HitLocation HitLocation { get; }
        /// <summary>
        /// ヒット位置（ワールド座標）
        /// </summary>
        public Vector3 HitPosition { get; }

        public ScoreGainedEventArgs(ulong clientId, int score, HitLocation hitLocation, Vector3 hitPosition)
        {
            ClientId = clientId;
            Score = score;
            HitLocation = hitLocation;
            HitPosition = hitPosition;
        }
    }
}
