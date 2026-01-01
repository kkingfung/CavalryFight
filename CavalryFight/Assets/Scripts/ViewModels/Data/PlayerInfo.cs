#nullable enable

using System.ComponentModel;

namespace CavalryFight.ViewModels.Data
{
    /// <summary>
    /// プレイヤー情報
    /// </summary>
    /// <remarks>
    /// マッチルームでのプレイヤースロット情報を保持します。
    /// INotifyPropertyChangedを実装し、UIバインディングに対応しています。
    /// </remarks>
    public class PlayerInfo : INotifyPropertyChanged
    {
        private int _slotIndex = 0;
        private string _playerId = "";
        private string _playerName = "";
        private bool _isHost = false;
        private bool _isReady = false;
        private PlayerTeam _team = PlayerTeam.None;
        private int _fps = 0;
        private bool _isNPC = false;
        private string _difficulty = "Normal";

        /// <summary>
        /// スロットインデックス
        /// </summary>
        public int SlotIndex
        {
            get => _slotIndex;
            set
            {
                _slotIndex = value;
                OnPropertyChanged(nameof(SlotIndex));
            }
        }

        /// <summary>
        /// プレイヤーID
        /// </summary>
        public string PlayerId
        {
            get => _playerId;
            set
            {
                _playerId = value;
                OnPropertyChanged(nameof(PlayerId));
            }
        }

        /// <summary>
        /// プレイヤー名
        /// </summary>
        public string PlayerName
        {
            get => _playerName;
            set
            {
                _playerName = value;
                OnPropertyChanged(nameof(PlayerName));
            }
        }

        /// <summary>
        /// ホストかどうか
        /// </summary>
        public bool IsHost
        {
            get => _isHost;
            set
            {
                _isHost = value;
                OnPropertyChanged(nameof(IsHost));
            }
        }

        /// <summary>
        /// 準備完了かどうか
        /// </summary>
        public bool IsReady
        {
            get => _isReady;
            set
            {
                _isReady = value;
                OnPropertyChanged(nameof(IsReady));
            }
        }

        /// <summary>
        /// チーム
        /// </summary>
        public PlayerTeam Team
        {
            get => _team;
            set
            {
                _team = value;
                OnPropertyChanged(nameof(Team));
            }
        }

        /// <summary>
        /// FPS
        /// </summary>
        public int Fps
        {
            get => _fps;
            set
            {
                _fps = value;
                OnPropertyChanged(nameof(Fps));
            }
        }

        /// <summary>
        /// NPCかどうか
        /// </summary>
        public bool IsNPC
        {
            get => _isNPC;
            set
            {
                _isNPC = value;
                OnPropertyChanged(nameof(IsNPC));
            }
        }

        /// <summary>
        /// NPC難易度（Easy, Normal, Hard, Expert）
        /// </summary>
        public string Difficulty
        {
            get => _difficulty;
            set
            {
                _difficulty = value;
                OnPropertyChanged(nameof(Difficulty));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
