#nullable enable

namespace CavalryFight.Examples.PlayerHealth
{
    /// <summary>
    /// プレイヤーの体力データモデル
    /// </summary>
    /// <remarks>
    /// Modelはデータとビジネスロジックのみを持ち、UIに依存しません。
    /// </remarks>
    public class PlayerHealthModel
    {
        #region Constants

        private const int DEFAULT_MAX_HEALTH = 100;
        private const int MIN_HEALTH = 0;

        #endregion

        #region Fields

        private int _currentHealth;
        private int _maxHealth;

        #endregion

        #region Properties

        /// <summary>
        /// 現在の体力を取得または設定します。
        /// </summary>
        public int CurrentHealth
        {
            get => _currentHealth;
            set => _currentHealth = System.Math.Clamp(value, MIN_HEALTH, _maxHealth);
        }

        /// <summary>
        /// 最大体力を取得または設定します。
        /// </summary>
        public int MaxHealth
        {
            get => _maxHealth;
            set
            {
                _maxHealth = System.Math.Max(1, value);
                if (_currentHealth > _maxHealth)
                {
                    _currentHealth = _maxHealth;
                }
            }
        }

        /// <summary>
        /// プレイヤーが生存しているかどうかを取得します。
        /// </summary>
        public bool IsAlive => _currentHealth > MIN_HEALTH;

        /// <summary>
        /// 体力の割合を取得します（0.0～1.0）
        /// </summary>
        public float HealthRatio => _maxHealth > 0 ? (float)_currentHealth / _maxHealth : 0f;

        #endregion

        #region Constructor

        /// <summary>
        /// PlayerHealthModelの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="maxHealth">最大体力（省略時は100）</param>
        public PlayerHealthModel(int maxHealth = DEFAULT_MAX_HEALTH)
        {
            _maxHealth = System.Math.Max(1, maxHealth);
            _currentHealth = _maxHealth;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// ダメージを受けます。
        /// </summary>
        /// <param name="damage">ダメージ量</param>
        /// <returns>実際に減少した体力</returns>
        public int TakeDamage(int damage)
        {
            if (damage < 0)
            {
                return 0;
            }

            int previousHealth = _currentHealth;
            _currentHealth = System.Math.Max(MIN_HEALTH, _currentHealth - damage);
            return previousHealth - _currentHealth;
        }

        /// <summary>
        /// 体力を回復します。
        /// </summary>
        /// <param name="amount">回復量</param>
        /// <returns>実際に回復した体力</returns>
        public int Heal(int amount)
        {
            if (amount < 0)
            {
                return 0;
            }

            int previousHealth = _currentHealth;
            _currentHealth = System.Math.Min(_maxHealth, _currentHealth + amount);
            return _currentHealth - previousHealth;
        }

        /// <summary>
        /// 体力を完全に回復します。
        /// </summary>
        public void RestoreToFull()
        {
            _currentHealth = _maxHealth;
        }

        #endregion
    }
}
