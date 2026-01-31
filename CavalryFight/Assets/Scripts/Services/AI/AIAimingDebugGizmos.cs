#nullable enable

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CavalryFight.Services.AI
{
    /// <summary>
    /// AIの照準システムをビジュアル化するデバッグGizmo
    /// </summary>
    /// <remarks>
    /// AIPlayerControllerと同じGameObjectまたは子にアタッチして使用します。
    /// Scene ViewでAIの照準方向、予測位置、射撃弧などを視覚的に確認できます。
    ///
    /// 表示される要素:
    /// - 赤: ターゲット位置
    /// - 黄色(弧): 射撃可能範囲
    /// - 緑(弧): 理想射撃角度範囲
    /// - シアン: 弓の発射ポイント
    /// - 青: 矢の根元ポイント
    /// - マゼンタ: 2点方式の照準方向
    /// - 黄色(線): ターゲットへの直接照準方向
    /// - 緑: 予測射撃位置と方向
    /// - 白: 最終照準方向（チャージ中のみ）
    /// - オレンジ: 矢のスポーン位置
    /// </remarks>
    public class AIAimingDebugGizmos : MonoBehaviour
    {
        #region Settings

        [Header("表示設定")]
        [Tooltip("Gizmosを有効にする")]
        [SerializeField] private bool _enableGizmos = true;

        [Tooltip("射撃弧を表示する")]
        [SerializeField] private bool _showShootingArc = true;

        [Tooltip("照準方向を表示する")]
        [SerializeField] private bool _showAimDirections = true;

        [Tooltip("予測射撃を表示する")]
        [SerializeField] private bool _showPrediction = true;

        [Tooltip("情報テキストを表示する")]
        [SerializeField] private bool _showInfoText = true;

        [Tooltip("射撃弧の半径")]
        [SerializeField] private float _arcRadius = 8f;

        #endregion

        #region Private Fields

        private AIPlayerController? _aiController;
        private GameObject? _mountObject;
        private GameObject? _currentTarget;
        private Transform? _bowFirePoint;
        private Transform? _arrowRootPoint;
        private bool _isCharging;
        private float _currentCharge;
        private Vector3 _targetVelocity;
        private DifficultySettings _difficultySettings;
        private float _minArrowSpeed;
        private float _maxArrowSpeed;

        // 定数（AIPlayerControllerと同じ値）
        private const float MinHorizontalRotation = -45f;
        private const float MaxHorizontalRotation = 125f;
        private const float IdealShootingAngle = 75f;
        private const float ShootingAngleMargin = 30f;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            // AIPlayerControllerから情報を取得
            if (_aiController == null)
            {
                _aiController = GetComponentInParent<AIPlayerController>();
            }

            if (_aiController == null) return;

            // リフレクションを使用してprivateフィールドにアクセス
            var type = typeof(AIPlayerController);

            _mountObject = GetFieldValue<GameObject>(_aiController, type, "_mountObject");
            _currentTarget = GetFieldValue<GameObject>(_aiController, type, "_currentTarget");
            _bowFirePoint = GetFieldValue<Transform>(_aiController, type, "_bowFirePoint");
            _arrowRootPoint = GetFieldValue<Transform>(_aiController, type, "_arrowRootPoint");
            _isCharging = GetFieldValue<bool>(_aiController, type, "_isCharging");
            _currentCharge = GetFieldValue<float>(_aiController, type, "_currentCharge");
            _targetVelocity = GetFieldValue<Vector3>(_aiController, type, "_targetVelocity");
            _difficultySettings = GetFieldValue<DifficultySettings>(_aiController, type, "_difficultySettings");
            _minArrowSpeed = GetFieldValue<float>(_aiController, type, "_minArrowSpeed");
            _maxArrowSpeed = GetFieldValue<float>(_aiController, type, "_maxArrowSpeed");
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            // 常に何かを表示して、コンポーネントが動作していることを確認
            DrawDebugStatus();

            if (!_enableGizmos)
            {
                return;
            }

            if (!Application.isPlaying)
            {
                DrawNotPlayingWarning();
                return;
            }

            if (_currentTarget == null || _mountObject == null)
            {
                DrawNoTargetWarning();
                return;
            }

            // 各要素を描画
            if (_showShootingArc)
            {
                DrawShootingArc();
            }

            DrawTargetPosition();

            if (_showAimDirections)
            {
                DrawFireAndRootPoints();
                DrawTwoPointAimDirection();
                DrawDirectAimDirection();
                DrawFinalAimDirection();
            }

            if (_showPrediction)
            {
                DrawPrediction();
            }

            if (_showInfoText)
            {
                DrawInfoText();
            }
        }

        /// <summary>
        /// デバッグステータスを常に表示（コンポーネントが動作しているか確認用）
        /// </summary>
        private void DrawDebugStatus()
        {
            // このGameObjectの周りに大きな球を描画して、コンポーネントが動作していることを示す
            Gizmos.color = new Color(0f, 1f, 1f, 0.2f); // 半透明のシアン
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            #if UNITY_EDITOR
            Handles.Label(transform.position + Vector3.up * 1f, "AIAimingDebugGizmos Active", new GUIStyle
            {
                normal = new GUIStyleState { textColor = Color.cyan },
                fontSize = 14,
                fontStyle = FontStyle.Bold
            });
            #endif
        }

        /// <summary>
        /// プレイ中でない場合の警告
        /// </summary>
        private void DrawNotPlayingWarning()
        {
            #if UNITY_EDITOR
            Handles.Label(transform.position + Vector3.up * 2f, "⚠ Not in Play Mode ⚠", new GUIStyle
            {
                normal = new GUIStyleState { textColor = Color.yellow },
                fontSize = 16,
                fontStyle = FontStyle.Bold
            });
            #endif
        }

        /// <summary>
        /// ターゲットがない場合の警告
        /// </summary>
        private void DrawNoTargetWarning()
        {
            #if UNITY_EDITOR
            string message = _mountObject == null ? "⚠ No Mount Object ⚠" : "⚠ No Target ⚠";
            Handles.Label(transform.position + Vector3.up * 2f, message, new GUIStyle
            {
                normal = new GUIStyleState { textColor = Color.red },
                fontSize = 16,
                fontStyle = FontStyle.Bold
            });

            if (_aiController != null)
            {
                var type = typeof(AIPlayerController);
                var currentState = GetFieldValue<object>(_aiController, type, "_currentState");
                Handles.Label(transform.position + Vector3.up * 1.5f, $"AI State: {currentState}", new GUIStyle
                {
                    normal = new GUIStyleState { textColor = Color.white },
                    fontSize = 12
                });
            }
            #endif
        }

        /// <summary>
        /// ターゲット位置を描画
        /// </summary>
        private void DrawTargetPosition()
        {
            if (_currentTarget == null) return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_currentTarget.transform.position, 0.5f);
            Gizmos.DrawLine(transform.position, _currentTarget.transform.position);

            #if UNITY_EDITOR
            Handles.Label(_currentTarget.transform.position + Vector3.up * 0.5f, "TARGET");
            #endif
        }

        /// <summary>
        /// 射撃弧を描画
        /// </summary>
        private void DrawShootingArc()
        {
            if (_mountObject == null) return;

            Vector3 mountPos = _mountObject.transform.position;
            Vector3 mountForward = _mountObject.transform.forward;
            mountForward.y = 0;
            mountForward.Normalize();

            // 最小角度から最大角度まで弧を描画（射撃可能範囲）
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // 半透明の黄色
            Vector3 prevPoint = mountPos + Quaternion.Euler(0, MinHorizontalRotation, 0) * mountForward * _arcRadius;

            for (float angle = MinHorizontalRotation; angle <= MaxHorizontalRotation; angle += 5f)
            {
                Vector3 direction = Quaternion.Euler(0, angle, 0) * mountForward;
                Vector3 point = mountPos + direction * _arcRadius;
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }

            // 弧の端点から中心への線
            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
            Vector3 minDir = Quaternion.Euler(0, MinHorizontalRotation, 0) * mountForward;
            Vector3 maxDir = Quaternion.Euler(0, MaxHorizontalRotation, 0) * mountForward;
            Gizmos.DrawLine(mountPos, mountPos + minDir * _arcRadius);
            Gizmos.DrawLine(mountPos, mountPos + maxDir * _arcRadius);

            // 理想射撃角度範囲を濃い色で表示
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f); // 半透明の緑
            float minIdealAngle = IdealShootingAngle - ShootingAngleMargin;
            float maxIdealAngle = IdealShootingAngle + ShootingAngleMargin;

            Vector3 idealPrevPoint = mountPos + Quaternion.Euler(0, minIdealAngle, 0) * mountForward * _arcRadius;
            for (float angle = minIdealAngle; angle <= maxIdealAngle; angle += 5f)
            {
                Vector3 direction = Quaternion.Euler(0, angle, 0) * mountForward;
                Vector3 point = mountPos + direction * _arcRadius;
                Gizmos.DrawLine(idealPrevPoint, point);
                idealPrevPoint = point;
            }

            #if UNITY_EDITOR
            // ラベル表示
            Handles.Label(mountPos + minDir * _arcRadius, $"Min: {MinHorizontalRotation:F0}°");
            Handles.Label(mountPos + maxDir * _arcRadius, $"Max: {MaxHorizontalRotation:F0}°");
            #endif

            // 現在のターゲット角度
            if (_currentTarget != null)
            {
                Vector3 toTarget = _currentTarget.transform.position - mountPos;
                toTarget.y = 0;
                toTarget.Normalize();
                float currentAngle = Vector3.SignedAngle(mountForward, toTarget, Vector3.up);

                Vector3 currentDir = Quaternion.Euler(0, currentAngle, 0) * mountForward;
                Gizmos.color = Color.red;
                Gizmos.DrawLine(mountPos, mountPos + currentDir * _arcRadius);

                #if UNITY_EDITOR
                Handles.Label(mountPos + currentDir * _arcRadius * 0.5f, $"Current: {currentAngle:F1}°");
                #endif
            }
        }

        /// <summary>
        /// 発射ポイントと根元ポイントを描画
        /// </summary>
        private void DrawFireAndRootPoints()
        {
            if (_bowFirePoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(_bowFirePoint.position, 0.2f);
                #if UNITY_EDITOR
                Handles.Label(_bowFirePoint.position + Vector3.up * 0.3f, "Fire Point");
                #endif
            }

            if (_arrowRootPoint != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(_arrowRootPoint.position, 0.15f);
                #if UNITY_EDITOR
                Handles.Label(_arrowRootPoint.position + Vector3.up * 0.3f, "Root Point");
                #endif
            }
        }

        /// <summary>
        /// 2点方式の照準方向を描画
        /// </summary>
        private void DrawTwoPointAimDirection()
        {
            if (_arrowRootPoint == null || _bowFirePoint == null) return;

            Vector3 twoPointDir = (_bowFirePoint.position - _arrowRootPoint.position).normalized;
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(_arrowRootPoint.position, _arrowRootPoint.position + twoPointDir * 5f);
            Gizmos.DrawWireSphere(_arrowRootPoint.position + twoPointDir * 5f, 0.3f);

            #if UNITY_EDITOR
            Handles.Label(_arrowRootPoint.position + twoPointDir * 2.5f, "2-Point Aim");
            #endif
        }

        /// <summary>
        /// ターゲットへの直接照準方向を描画（フォールバック）
        /// </summary>
        private void DrawDirectAimDirection()
        {
            if (_bowFirePoint == null || _currentTarget == null) return;

            Vector3 targetAimPos = _currentTarget.transform.position + Vector3.up * 1f;
            Vector3 directDir = (targetAimPos - _bowFirePoint.position).normalized;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_bowFirePoint.position, _bowFirePoint.position + directDir * 5f);

            #if UNITY_EDITOR
            Handles.Label(_bowFirePoint.position + directDir * 2.5f, "Direct Aim");
            #endif
        }

        /// <summary>
        /// 予測射撃を描画
        /// </summary>
        private void DrawPrediction()
        {
            if (_currentTarget == null || _bowFirePoint == null) return;

            float leadFactor = _difficultySettings.LeadTargetFactor;
            if (leadFactor > 0f && _targetVelocity.sqrMagnitude > 0.1f)
            {
                float estimatedArrowSpeed = Mathf.Lerp(_minArrowSpeed, _maxArrowSpeed, _currentCharge);
                float distanceToTarget = Vector3.Distance(_bowFirePoint.position, _currentTarget.transform.position);
                float timeToHit = distanceToTarget / estimatedArrowSpeed;

                Vector3 predictedPosition = _currentTarget.transform.position + (_targetVelocity * timeToHit * leadFactor);

                // 予測位置を表示
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(predictedPosition, 0.6f);
                Gizmos.DrawLine(_currentTarget.transform.position, predictedPosition);

                // 予測方向を表示
                Vector3 predictedDir = (predictedPosition + Vector3.up * 1f - _bowFirePoint.position).normalized;
                Gizmos.color = new Color(0f, 1f, 0.5f); // 明るい緑
                Gizmos.DrawLine(_bowFirePoint.position, _bowFirePoint.position + predictedDir * 7f);
                Gizmos.DrawWireSphere(_bowFirePoint.position + predictedDir * 7f, 0.4f);

                #if UNITY_EDITOR
                Handles.Label(predictedPosition + Vector3.up * 0.5f,
                    $"Predicted Pos\nLeadFactor: {leadFactor:F2}\nTimeToHit: {timeToHit:F2}s\nVelocity: {_targetVelocity.magnitude:F1}m/s");
                Handles.Label(_bowFirePoint.position + predictedDir * 3.5f, "Predicted Aim");
                #endif
            }
        }

        /// <summary>
        /// 最終照準方向を描画（チャージ中のみ）
        /// </summary>
        private void DrawFinalAimDirection()
        {
            if (!_isCharging || _bowFirePoint == null || _currentTarget == null) return;

            Vector3 finalDir = CalculateFinalAimDirection();

            Gizmos.color = Color.white;
            Gizmos.DrawLine(_bowFirePoint.position, _bowFirePoint.position + finalDir * 10f);
            Gizmos.DrawWireSphere(_bowFirePoint.position + finalDir * 10f, 0.5f);

            // 矢のスポーン位置
            Vector3 spawnPos = _bowFirePoint.position + finalDir * 0.5f;
            Gizmos.color = new Color(1f, 0.5f, 0f); // オレンジ
            Gizmos.DrawWireSphere(spawnPos, 0.25f);

            #if UNITY_EDITOR
            Handles.Label(_bowFirePoint.position + finalDir * 5f, "★ FINAL AIM ★");
            Handles.Label(spawnPos + Vector3.up * 0.3f, "Spawn Pos");
            #endif
        }

        /// <summary>
        /// 情報テキストを描画
        /// </summary>
        private void DrawInfoText()
        {
            #if UNITY_EDITOR
            if (_aiController == null || _currentTarget == null) return;

            Vector3 labelPos = transform.position + Vector3.up * 3f;
            float distToTarget = Vector3.Distance(transform.position, _currentTarget.transform.position);

            // AIの状態を取得
            var type = typeof(AIPlayerController);
            var currentState = GetFieldValue<object>(_aiController, type, "_currentState");
            var aiId = GetFieldValue<ulong>(_aiController, type, "_aiId");

            string info = $"=== AI {aiId} Aiming Debug ===\n" +
                         $"State: {currentState}\n" +
                         $"Charging: {_isCharging} ({_currentCharge:P0})\n" +
                         $"Distance: {distToTarget:F1}m\n" +
                         $"Target Velocity: {_targetVelocity.magnitude:F1}m/s\n" +
                         $"Lead Factor: {_difficultySettings.LeadTargetFactor:F2}\n" +
                         $"Arrow Speed: {Mathf.Lerp(_minArrowSpeed, _maxArrowSpeed, _currentCharge):F1}m/s\n" +
                         $"Accuracy: {_difficultySettings.AimAccuracy:P0}\n" +
                         $"Miss Chance: {_difficultySettings.MissChance:P0}";

            Handles.Label(labelPos, info, new GUIStyle
            {
                normal = new GUIStyleState { textColor = Color.white },
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            });
            #endif
        }

        /// <summary>
        /// 最終照準方向を計算（FireArrow()と同じロジック）
        /// </summary>
        private Vector3 CalculateFinalAimDirection()
        {
            if (_currentTarget == null || _bowFirePoint == null)
            {
                return transform.forward;
            }

            // 2点方式またはフォールバック
            Vector3 direction;
            if (_arrowRootPoint != null && _bowFirePoint != null)
            {
                direction = (_bowFirePoint.position - _arrowRootPoint.position).normalized;
            }
            else
            {
                Vector3 targetAimPos = _currentTarget.transform.position + Vector3.up * 1f;
                direction = (targetAimPos - _bowFirePoint.position).normalized;
            }

            // 予測射撃を適用
            float leadFactor = _difficultySettings.LeadTargetFactor;
            if (leadFactor > 0f && _targetVelocity.sqrMagnitude > 0.1f)
            {
                float estimatedArrowSpeed = Mathf.Lerp(_minArrowSpeed, _maxArrowSpeed, _currentCharge);
                float distanceToTarget = Vector3.Distance(_bowFirePoint.position, _currentTarget.transform.position);
                float timeToHit = distanceToTarget / estimatedArrowSpeed;

                Vector3 predictedPosition = _currentTarget.transform.position + (_targetVelocity * timeToHit * leadFactor);
                Vector3 predictedDirection = (predictedPosition + Vector3.up * 1f - _bowFirePoint.position).normalized;

                direction = Vector3.Slerp(direction, predictedDirection, leadFactor);
            }

            return direction;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// リフレクションを使用してprivateフィールドの値を取得
        /// </summary>
        private T GetFieldValue<T>(object obj, System.Type type, string fieldName)
        {
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                var value = field.GetValue(obj);
                if (value is T typedValue)
                {
                    return typedValue;
                }
            }
            return default!;
        }

        #endregion
    }
}
