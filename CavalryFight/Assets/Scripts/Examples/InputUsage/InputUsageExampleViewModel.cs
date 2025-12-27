#nullable enable

using System;
using UnityEngine;
using CavalryFight.Core.Services;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Commands;

namespace CavalryFight.Services.Input
{
    /// <summary>
    /// InputServiceの使用例を示すViewModelです。
    /// </summary>
    /// <remarks>
    /// このクラスは、InputServiceをMVVMパターンで使用する方法を示すサンプルコードです。
    /// 実際のゲームロジックでは、このクラスを参考にして各ViewModelで入力処理を実装してください。
    /// </remarks>
    public class InputUsageExampleViewModel : ViewModelBase
    {
        #region Fields
        private const float _maxChargeTime = 3.0f;
        private readonly IInputService _inputService;
        private bool _isPlayerControlEnabled = true;
        private float _currentMovementSensitivity = 1.0f;
        private float _currentCameraSensitivity = 1.0f;
        private Vector2 _movementInput;
        private Vector2 _cameraInput;
        private float _attackChargeTime = 0f;
        private bool _isChargingAttack = false;

        #endregion

        #region Properties

        /// <summary>
        /// プレイヤー操作が有効かどうかを取得または設定します。
        /// </summary>
        public bool IsPlayerControlEnabled
        {
            get => _isPlayerControlEnabled;
            set
            {
                if (SetProperty(ref _isPlayerControlEnabled, value))
                {
                    if (value)
                    {
                        _inputService.EnableInput();
                    }
                    else
                    {
                        _inputService.DisableInput();
                    }
                }
            }
        }

        /// <summary>
        /// 移動感度を取得または設定します（0.0～1.0）
        /// </summary>
        public float MovementSensitivity
        {
            get => _currentMovementSensitivity;
            set
            {
                if (SetProperty(ref _currentMovementSensitivity, value))
                {
                    _inputService.MovementSensitivity = value;
                }
            }
        }

        /// <summary>
        /// カメラ感度を取得または設定します（0.0～1.0）
        /// </summary>
        public float CameraSensitivity
        {
            get => _currentCameraSensitivity;
            set
            {
                if (SetProperty(ref _currentCameraSensitivity, value))
                {
                    _inputService.CameraSensitivity = value;
                }
            }
        }

        /// <summary>
        /// 現在の移動入力ベクトルを取得します。
        /// </summary>
        public Vector2 MovementInput
        {
            get => _movementInput;
            private set => SetProperty(ref _movementInput, value);
        }

        /// <summary>
        /// 現在のカメラ入力ベクトルを取得します。
        /// </summary>
        public Vector2 CameraInput
        {
            get => _cameraInput;
            private set => SetProperty(ref _cameraInput, value);
        }

        /// <summary>
        /// 攻撃のチャージ時間を取得します（秒）
        /// </summary>
        public float AttackChargeTime
        {
            get => _attackChargeTime;
            private set => SetProperty(ref _attackChargeTime, value);
        }

        /// <summary>
        /// 攻撃をチャージ中かどうかを取得します。
        /// </summary>
        public bool IsChargingAttack
        {
            get => _isChargingAttack;
            private set => SetProperty(ref _isChargingAttack, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// 入力をリセットするコマンド
        /// </summary>
        public ICommand ResetInputCommand { get; }

        /// <summary>
        /// Y軸を反転するコマンド
        /// </summary>
        public ICommand ToggleInvertYAxisCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// InputUsageExampleViewModelの新しいインスタンスを初期化します。
        /// </summary>
        public InputUsageExampleViewModel()
        {
            // ServiceLocatorからInputServiceを取得
            _inputService = ServiceLocator.Instance.Get<IInputService>();

            // イベントをサブスクライブ
            _inputService.AttackButtonPressed += OnAttackButtonPressed;
            _inputService.AttackButtonReleased += OnAttackButtonReleased;
            _inputService.CancelAttackButtonPressed += OnCancelAttackButtonPressed;
            _inputService.BoostButtonPressed += OnBoostButtonPressed;
            _inputService.MountButtonPressed += OnMountButtonPressed;
            _inputService.JumpButtonPressed += OnJumpButtonPressed;
            _inputService.MenuButtonPressed += OnMenuButtonPressed;

            // コマンドを初期化
            ResetInputCommand = new RelayCommand(
                execute: () => _inputService.ResetInput(),
                canExecute: () => _inputService.InputEnabled
            );

            ToggleInvertYAxisCommand = new RelayCommand(
                execute: () => _inputService.InvertYAxis = !_inputService.InvertYAxis
            );

            // 現在の感度を取得
            _currentMovementSensitivity = _inputService.MovementSensitivity;
            _currentCameraSensitivity = _inputService.CameraSensitivity;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 入力をポーリングします（Updateループから呼び出されることを想定）
        /// </summary>
        /// <remarks>
        /// ViewがUpdateループからこのメソッドを呼び出すことで、
        /// 継続的な入力（移動、カメラ）を取得できます。
        /// </remarks>
        public void PollInput()
        {
            if (!_inputService.InputEnabled)
            {
                return;
            }

            // 移動入力を取得
            MovementInput = _inputService.GetMovementInput();

            // カメラ入力を取得
            CameraInput = _inputService.GetCameraInput();

            // 攻撃チャージ中の場合、チャージ時間を増やす
            if (IsChargingAttack)
            {
                AttackChargeTime += Time.deltaTime;

                // 実際のゲームでは、チャージ時間に応じて攻撃力を変化させます
                float chargePercent = Mathf.Clamp01(AttackChargeTime / _maxChargeTime);

                Debug.Log($"[InputUsageExample] Charging attack: {chargePercent:P0} ({AttackChargeTime:F2}s)");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 攻撃ボタンが押された時の処理
        /// </summary>
        /// <remarks>
        /// チャージ攻撃を開始します。
        /// 実際のチャージ時間の累積はPollInput()で行われます。
        /// </remarks>
        /// <param name="sender">イベント送信元</param>
        /// <param name="e">イベント引数</param>
        private void OnAttackButtonPressed(object? sender, EventArgs e)
        {
            Debug.Log("[InputUsageExample] Attack button pressed! Starting charge...");

            // チャージ開始
            IsChargingAttack = true;
            AttackChargeTime = 0f;

            // チャージ開始時の処理
            // 例: チャージエフェクトの表示開始
        }

        /// <summary>
        /// 攻撃ボタンが離された時の処理
        /// </summary>
        /// <remarks>
        /// チャージされた攻撃を発動します。
        /// </remarks>
        /// <param name="sender">イベント送信元</param>
        /// <param name="e">イベント引数</param>
        private void OnAttackButtonReleased(object? sender, EventArgs e)
        {
            if (!IsChargingAttack)
            {
                return;
            }

            Debug.Log("[InputUsageExample] Attack button released! Executing charged attack...");

            // チャージ攻撃を発動
            ExecuteChargedAttack(AttackChargeTime);

            // チャージ状態をリセット
            IsChargingAttack = false;
            AttackChargeTime = 0f;
        }

        /// <summary>
        /// 攻撃キャンセルボタンが押された時の処理
        /// </summary>
        /// <remarks>
        /// チャージ中の攻撃をキャンセルします。
        /// </remarks>
        /// <param name="sender">イベント送信元</param>
        /// <param name="e">イベント引数</param>
        private void OnCancelAttackButtonPressed(object? sender, EventArgs e)
        {
            if (!IsChargingAttack)
            {
                return;
            }

            Debug.Log("[InputUsageExample] Attack cancelled!");

            // チャージをキャンセル
            IsChargingAttack = false;
            AttackChargeTime = 0f;

            // キャンセル時の処理
            // 例: チャージエフェクトを停止
        }

        /// <summary>
        /// チャージ攻撃を実行します。
        /// </summary>
        /// <param name="chargeTime">チャージされた時間（秒）</param>
        private void ExecuteChargedAttack(float chargeTime)
        {
            // チャージ時間に応じた攻撃力を計算
            float chargePercent = Mathf.Clamp01(chargeTime / _maxChargeTime);
            float attackPower = 1.0f + (chargePercent * 2.0f); // 1.0～3.0倍

            Debug.Log($"[InputUsageExample] Charged attack executed! Power: {attackPower:F1}x (Charge: {chargePercent:P0})");

            // 実際のゲームでは、ここで攻撃判定を実行します
            // 例: PlayerViewModel.ExecuteAttack(attackPower);
        }

        /// <summary>
        /// 騎乗/降馬ボタンが押された時の処理
        /// </summary>
        /// <param name="sender">イベント送信元</param>
        /// <param name="e">イベント引数</param>
        private void OnMountButtonPressed(object? sender, EventArgs e)
        {
            Debug.Log("[InputUsageExample] Mount/Dismount button pressed!");

            // 実際のゲームでは、ここで騎乗/降馬処理を実行します
            // 例: PlayerViewModel.ToggleMount();
        }

        /// <summary>
        /// ジャンプボタンが押された時の処理
        /// </summary>
        /// <param name="sender">イベント送信元</param>
        /// <param name="e">イベント引数</param>
        private void OnJumpButtonPressed(object? sender, EventArgs e)
        {
            Debug.Log("[InputUsageExample] Jump button pressed!");

            // 実際のゲームでは、ここでジャンプ処理を実行します
            // 例: PlayerViewModel.Jump();
        }

        /// <summary>
        /// メニューボタンが押された時の処理
        /// </summary>
        /// <param name="sender">イベント送信元</param>
        /// <param name="e">イベント引数</param>
        private void OnMenuButtonPressed(object? sender, EventArgs e)
        {
            Debug.Log("[InputUsageExample] Menu button pressed! Opening options menu...");

            // 実際のゲームでは、ここでオプションメニューを表示します
            // 例: UIManager.ShowOptionsMenu();
            // オプション例: 設定変更、マッチからのリタイア等
        }

        /// <summary>
        /// ブーストボタンが押された時の処理
        /// </summary>
        /// <param name="sender">イベント送信元</param>
        /// <param name="e">イベント引数</param>
        private void OnBoostButtonPressed(object? sender, EventArgs e)
        {
            Debug.Log("[InputUsageExample] Boost button pressed!");

            // 実際のゲームでは、ここで馬のブーストを発動します
            // 例: HorseController.TriggerBoost();
        }

        #endregion

        #region Dispose

        /// <summary>
        /// リソースを破棄します。
        /// </summary>
        protected override void OnDispose()
        {
            // イベントをアンサブスクライブ
            _inputService.AttackButtonPressed -= OnAttackButtonPressed;
            _inputService.AttackButtonReleased -= OnAttackButtonReleased;
            _inputService.CancelAttackButtonPressed -= OnCancelAttackButtonPressed;
            _inputService.BoostButtonPressed -= OnBoostButtonPressed;
            _inputService.MountButtonPressed -= OnMountButtonPressed;
            _inputService.JumpButtonPressed -= OnJumpButtonPressed;
            _inputService.MenuButtonPressed -= OnMenuButtonPressed;

            base.OnDispose();
        }

        #endregion
    }
}
