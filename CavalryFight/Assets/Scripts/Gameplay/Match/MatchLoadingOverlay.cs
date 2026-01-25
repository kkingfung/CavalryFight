#nullable enable

using System;
using CavalryFight.Services.Match;
using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.Gameplay.Match
{
    /// <summary>
    /// マッチシーン内のローディングオーバーレイ
    /// </summary>
    /// <remarks>
    /// ASMのローディング画面が閉じた後も、すべてのエンティティが
    /// スポーンされるまで黒い画面を表示し続けます。
    /// MatchManagerのAllEntitiesReadyイベントを待ってからフェードアウトします。
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class MatchLoadingOverlay : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Settings")]
        [Tooltip("フェードアウト時間（秒）")]
        [SerializeField] private float _fadeOutDuration = 0.5f;

        [Tooltip("エンティティ準備完了前に表示するメッセージ")]
        [SerializeField] private string _loadingMessage = "Preparing match...";

        [Tooltip("準備完了時のメッセージ")]
        [SerializeField] private string _readyMessage = "Ready!";

        #endregion

        #region Private Fields

        private UIDocument? _uiDocument;
        private VisualElement? _overlay;
        private Label? _messageLabel;
        private Label? _progressLabel;
        private bool _isReady;
        private float _fadeStartTime;
        private IMatchReadinessProvider? _matchReadinessProvider;
        private int _retryCount = 0;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            Debug.Log("[MatchLoadingOverlay] Awake - Overlay initialized");
        }

        private void Start()
        {
            SetupUI();
            SubscribeToMatchManager();
        }

        private void Update()
        {
            if (_isReady && _overlay != null)
            {
                // フェードアウト処理
                float elapsed = Time.time - _fadeStartTime;
                float alpha = 1f - (elapsed / _fadeOutDuration);

                if (alpha <= 0)
                {
                    _overlay.style.display = DisplayStyle.None;
                    enabled = false; // Updateを停止
                    Debug.Log("[MatchLoadingOverlay] Fade out complete, overlay hidden");
                }
                else
                {
                    _overlay.style.opacity = alpha;
                }
            }
            else if (!_isReady && _matchReadinessProvider != null)
            {
                // 進捗を更新
                UpdateProgress();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromMatchManager();
        }

        #endregion

        #region Private Methods

        private void SetupUI()
        {
            if (_uiDocument == null)
            {
                return;
            }

            var root = _uiDocument.rootVisualElement;

            // 既存のオーバーレイを探すか、新規作成
            _overlay = root.Q<VisualElement>("MatchLoadingOverlay");

            if (_overlay == null)
            {
                // UIが設定されていない場合、プログラムで作成
                _overlay = new VisualElement
                {
                    name = "MatchLoadingOverlay",
                    style =
                    {
                        position = Position.Absolute,
                        left = 0,
                        top = 0,
                        right = 0,
                        bottom = 0,
                        backgroundColor = Color.black,
                        alignItems = Align.Center,
                        justifyContent = Justify.Center
                    }
                };

                _messageLabel = new Label(_loadingMessage)
                {
                    name = "MessageLabel",
                    style =
                    {
                        color = Color.white,
                        fontSize = 32,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        marginBottom = 20
                    }
                };

                _progressLabel = new Label("")
                {
                    name = "ProgressLabel",
                    style =
                    {
                        color = new Color(0.7f, 0.7f, 0.7f),
                        fontSize = 18,
                        unityTextAlign = TextAnchor.MiddleCenter
                    }
                };

                _overlay.Add(_messageLabel);
                _overlay.Add(_progressLabel);
                root.Add(_overlay);

                Debug.Log("[MatchLoadingOverlay] Created overlay UI programmatically");
            }
            else
            {
                _messageLabel = _overlay.Q<Label>("MessageLabel");
                _progressLabel = _overlay.Q<Label>("ProgressLabel");
            }

            // 初期状態：表示
            _overlay.style.display = DisplayStyle.Flex;
            _overlay.style.opacity = 1f;

            if (_messageLabel != null)
            {
                _messageLabel.text = _loadingMessage;
            }
        }

        private void SubscribeToMatchManager()
        {
            // MatchManagerを探す
            if (MatchManager.Instance != null && MatchManager.Instance is IMatchReadinessProvider provider)
            {
                _matchReadinessProvider = provider;
                _matchReadinessProvider.AllEntitiesReady += OnAllEntitiesReady;
                Debug.Log("[MatchLoadingOverlay] Subscribed to MatchManager.AllEntitiesReady");

                // 既に準備完了している場合
                if (_matchReadinessProvider.AreAllEntitiesReady)
                {
                    Debug.Log("[MatchLoadingOverlay] Entities already ready!");
                    OnAllEntitiesReady();
                }
            }
            else
            {
                Debug.LogWarning("[MatchLoadingOverlay] MatchManager not found! Will retry...");
                // 遅延して再試行
                Invoke(nameof(RetrySubscribe), 0.5f);
            }
        }

        private void RetrySubscribe()
        {
            if (_matchReadinessProvider != null)
            {
                return; // 既に購読済み
            }

            if (MatchManager.Instance != null && MatchManager.Instance is IMatchReadinessProvider provider)
            {
                _matchReadinessProvider = provider;
                _matchReadinessProvider.AllEntitiesReady += OnAllEntitiesReady;
                Debug.Log("[MatchLoadingOverlay] Subscribed to MatchManager.AllEntitiesReady (retry)");

                if (_matchReadinessProvider.AreAllEntitiesReady)
                {
                    OnAllEntitiesReady();
                }
            }
            else
            {
                // さらに再試行（最大5回）
                if (_retryCount < 5)
                {
                    _retryCount++;
                    Invoke(nameof(RetrySubscribe), 0.5f);
                }
                else
                {
                    Debug.LogError("[MatchLoadingOverlay] Failed to find MatchManager after 5 retries. Hiding overlay.");
                    OnAllEntitiesReady(); // フォールバック：オーバーレイを非表示
                }
            }
        }

        private void UnsubscribeFromMatchManager()
        {
            if (_matchReadinessProvider != null)
            {
                _matchReadinessProvider.AllEntitiesReady -= OnAllEntitiesReady;
                _matchReadinessProvider = null;
            }
        }

        private void UpdateProgress()
        {
            if (_matchReadinessProvider == null || _progressLabel == null)
            {
                return;
            }

            string statusMessage = _matchReadinessProvider.LoadStatusMessage;
            float progress = _matchReadinessProvider.EntityLoadProgress;

            _progressLabel.text = $"{statusMessage} ({progress:P0})";

            if (_messageLabel != null)
            {
                _messageLabel.text = _loadingMessage;
            }
        }

        private void OnAllEntitiesReady()
        {
            if (_isReady)
            {
                return; // 既に処理済み
            }

            _isReady = true;
            _fadeStartTime = Time.time;

            Debug.Log("[MatchLoadingOverlay] All entities ready! Starting fade out...");

            if (_messageLabel != null)
            {
                _messageLabel.text = _readyMessage;
            }

            if (_progressLabel != null)
            {
                _progressLabel.text = "";
            }
        }

        #endregion
    }
}
