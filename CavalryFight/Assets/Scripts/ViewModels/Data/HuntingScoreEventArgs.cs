#nullable enable

using System;
using UnityEngine;
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
        /// <summary>
        /// ヒット位置（ワールド座標）
        /// </summary>
        public Vector3 HitPosition { get; set; }
    }
}
