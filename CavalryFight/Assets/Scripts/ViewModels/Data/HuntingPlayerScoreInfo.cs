#nullable enable

namespace CavalryFight.ViewModels.Data
{
    /// <summary>
    /// ハンティングモードのプレイヤースコア情報
    /// </summary>
    public class HuntingPlayerScoreInfo
    {
        public ulong ClientId { get; set; }
        public string PlayerName { get; set; } = "";
        public int Score { get; set; }
        public int Hits { get; set; }
        public int Shots { get; set; }
        public int TeamIndex { get; set; }
        public bool IsHunter { get; set; }
        public bool IsLocalPlayer { get; set; }

        public string AccuracyText
        {
            get
            {
                if (Shots == 0)
                {
                    return "0.0%";
                }
                float accuracy = (float)Hits / Shots * 100f;
                return $"{accuracy:F1}%";
            }
        }

        public string RoleText => IsHunter ? "Hunter" : "Wolf";
    }
}
