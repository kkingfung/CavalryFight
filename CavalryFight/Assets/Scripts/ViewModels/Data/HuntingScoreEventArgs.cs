#nullable enable

using System;
using CavalryFight.Services.Match;

namespace CavalryFight.ViewModels.Data
{
    /// <summary>
    /// ハンティングモードのスコアイベント引数
    /// </summary>
    public class HuntingScoreEventArgs : EventArgs
    {
        public ulong ClientId { get; set; }
        public int Score { get; set; }
        public HitLocation Location { get; set; }
    }
}
