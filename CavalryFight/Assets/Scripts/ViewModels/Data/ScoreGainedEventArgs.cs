#nullable enable

using System;
using CavalryFight.Gameplay.Match;

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

        public ScoreGainedEventArgs(ulong clientId, int score, HitLocation hitLocation)
        {
            ClientId = clientId;
            Score = score;
            HitLocation = hitLocation;
        }
    }
}
