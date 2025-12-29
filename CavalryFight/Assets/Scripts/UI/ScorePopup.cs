#nullable enable

using UnityEngine;
using TMPro;

namespace CavalryFight.UI
{
    /// <summary>
    /// スコアポップアップ表示
    /// </summary>
    /// <remarks>
    /// 3D空間にスコアをポップアップ表示します。
    /// Febucci Text Animatorと統合可能です。
    /// </remarks>
    public class ScorePopup : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private TextMeshPro? _textMesh;

        [Header("Animation Settings")]
        [SerializeField] private float _lifetime = 2f;
        [SerializeField] private float _floatSpeed = 2f;
        [SerializeField] private float _fadeStartTime = 1f;
        [SerializeField] private AnimationCurve _scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Text Animation")]
        [SerializeField] private bool _useTextAnimator = true;
        [SerializeField] private string _animationTag = "<wiggle>";

        #endregion

        #region Private Fields

        private float _spawnTime;
        private Vector3 _startPosition;
        private Color _startColor;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_textMesh == null)
            {
                _textMesh = GetComponentInChildren<TextMeshPro>();
            }

            if (_textMesh != null)
            {
                _startColor = _textMesh.color;
            }
        }

        private void Start()
        {
            _spawnTime = Time.time;
            _startPosition = transform.position;

            // カメラの方を向く
            Camera? mainCam = Camera.main;
            if (mainCam != null)
            {
                transform.LookAt(transform.position + mainCam.transform.rotation * Vector3.forward,
                    mainCam.transform.rotation * Vector3.up);
            }
        }

        private void Update()
        {
            float elapsed = Time.time - _spawnTime;

            // 寿命チェック
            if (elapsed >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            // 上に浮く
            transform.position = _startPosition + Vector3.up * (_floatSpeed * elapsed);

            // スケールアニメーション
            float scaleT = Mathf.Clamp01(elapsed / 0.3f);
            float scale = _scaleCurve.Evaluate(scaleT);
            transform.localScale = Vector3.one * scale;

            // フェードアウト
            if (elapsed >= _fadeStartTime && _textMesh != null)
            {
                float fadeT = (elapsed - _fadeStartTime) / (_lifetime - _fadeStartTime);
                Color color = _startColor;
                color.a = Mathf.Lerp(1f, 0f, fadeT);
                _textMesh.color = color;
            }

            // カメラの方を向き続ける
            Camera? mainCam = Camera.main;
            if (mainCam != null)
            {
                transform.LookAt(transform.position + mainCam.transform.rotation * Vector3.forward,
                    mainCam.transform.rotation * Vector3.up);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// スコアテキストを設定します
        /// </summary>
        /// <param name="score">表示するスコア</param>
        public void SetScore(int score)
        {
            if (_textMesh == null)
            {
                return;
            }

            // Febucci Text Animatorを使用する場合
            if (_useTextAnimator && !string.IsNullOrEmpty(_animationTag))
            {
                _textMesh.text = $"{_animationTag}+{score}</{_animationTag.TrimStart('<').TrimEnd('>')}";
            }
            else
            {
                _textMesh.text = $"+{score}";
            }
        }

        /// <summary>
        /// スコアと色を設定します
        /// </summary>
        /// <param name="score">表示するスコア</param>
        /// <param name="color">テキストの色</param>
        public void SetScore(int score, Color color)
        {
            SetScore(score);

            if (_textMesh != null)
            {
                _textMesh.color = color;
                _startColor = color;
            }
        }

        #endregion
    }
}
