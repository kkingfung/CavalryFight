#nullable enable

using UnityEngine;
using TMPro;

namespace CavalryFight.Services.Training
{
    /// <summary>
    /// スコアポップアップ表示コンポーネント
    /// </summary>
    /// <remarks>
    /// ターゲット命中時に3D空間にスコアを表示します。
    /// TextMeshProを使用してテキストを表示します。
    /// </remarks>
    [RequireComponent(typeof(TextMeshPro))]
    public class ScorePopup : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Animation")]
        [SerializeField] private float _lifetime = 2f;
        [SerializeField] private float _floatSpeed = 1f;

        #endregion

        #region Private Fields

        private TextMeshPro? _textMeshPro;
        private float _elapsedTime;
        private Vector3 _startPosition;
        private Camera? _mainCamera;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _textMeshPro = GetComponent<TextMeshPro>();
            _startPosition = transform.position;
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            _elapsedTime += Time.deltaTime;

            // 上に浮かぶアニメーション
            transform.position = _startPosition + Vector3.up * (_floatSpeed * _elapsedTime);

            // カメラに向くように回転（ビルボード）
            if (_mainCamera != null)
            {
                transform.rotation = _mainCamera.transform.rotation;
            }

            // フェードアウト
            if (_textMeshPro != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, _elapsedTime / _lifetime);
                Color color = _textMeshPro.color;
                color.a = alpha;
                _textMeshPro.color = color;
            }

            // ライフタイム終了で削除
            if (_elapsedTime >= _lifetime)
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// スコアを設定して表示します
        /// </summary>
        /// <param name="score">表示するスコア</param>
        /// <param name="color">テキストの色</param>
        public void SetScore(int score, Color color)
        {
            if (_textMeshPro == null)
            {
                return;
            }

            _textMeshPro.text = $"+{score}";
            _textMeshPro.color = color;

            Debug.Log($"[ScorePopup] Displaying score: +{score}");
        }

        #endregion
    }
}
