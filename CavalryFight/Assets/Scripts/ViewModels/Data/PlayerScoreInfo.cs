#nullable enable

namespace CavalryFight.ViewModels.Data
{
    /// <summary>
    /// スコアボード用のプレイヤースコア情報
    /// </summary>
    public class PlayerScoreInfo
    {
        public ulong ClientId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int Score { get; set; }
        public int Hits { get; set; }
        public int Shots { get; set; }
        public int TeamIndex { get; set; }
        public bool IsLocalPlayer { get; set; }

        public float Accuracy => Shots > 0 ? (float)Hits / Shots * 100f : 0f;
        public string AccuracyText => $"{Accuracy:F1}%";
    }
}
