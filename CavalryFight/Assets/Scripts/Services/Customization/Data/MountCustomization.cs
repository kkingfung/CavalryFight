#nullable enable

using System;
using UnityEngine;

namespace CavalryFight.Services.Customization
{
    /// <summary>
    /// 騎乗動物のカスタマイズデータ
    /// </summary>
    /// <remarks>
    /// Malbers Horse AnimSet Proアセットのカスタマイズオプションを表します。
    /// 馬の外見、装備の設定を含みます。
    /// </remarks>
    [Serializable]
    public class MountCustomization
    {
        #region Basic Information

        /// <summary>
        /// 騎乗動物のタイプ
        /// </summary>
        public MountType MountType = MountType.HorseRealistic;

        #endregion

        #region Appearance

        /// <summary>
        /// 毛色
        /// </summary>
        public HorseColor CoatColor = HorseColor.Brown;

        /// <summary>
        /// たてがみのスタイル
        /// </summary>
        public ManeStyle ManeStyle = ManeStyle.Short;

        /// <summary>
        /// たてがみの色
        /// </summary>
        public ManeColor ManeColor = ManeColor.Brown;

        /// <summary>
        /// 角のメッシュタイプ（どの角を表示するか）
        /// </summary>
        public HornType HornType = HornType.None;

        /// <summary>
        /// 角のマテリアル（角の色/スタイル）
        /// </summary>
        public HornMaterial HornMaterial = HornMaterial.BlackPA;

        #endregion

        #region Equipment

        /// <summary>
        /// 馬鎧ID（0 = なし、1-3）
        /// </summary>
        public int ArmorId = 0;

        /// <summary>
        /// 鞍を装備するか
        /// </summary>
        public bool HasSaddle = true;

        /// <summary>
        /// 手綱を装備するか
        /// </summary>
        public bool HasReins = true;

        #endregion

        #region Constructors

        /// <summary>
        /// MountCustomizationの新しいインスタンスを初期化します
        /// </summary>
        public MountCustomization()
        {
        }

        /// <summary>
        /// MountCustomizationの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="mountType">騎乗動物のタイプ</param>
        public MountCustomization(MountType mountType)
        {
            MountType = mountType;
        }

        #endregion

        #region Methods

        /// <summary>
        /// このカスタマイズのコピーを作成します
        /// </summary>
        /// <returns>コピーされたカスタマイズ</returns>
        public MountCustomization Clone()
        {
            return new MountCustomization
            {
                MountType = this.MountType,
                CoatColor = this.CoatColor,
                ManeStyle = this.ManeStyle,
                ManeColor = this.ManeColor,
                HornType = this.HornType,
                HornMaterial = this.HornMaterial,
                ArmorId = this.ArmorId,
                HasSaddle = this.HasSaddle,
                HasReins = this.HasReins
            };
        }

        /// <summary>
        /// JSONに変換します
        /// </summary>
        /// <returns>JSON文字列</returns>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        /// <summary>
        /// JSONから読み込みを試みます
        /// </summary>
        /// <param name="json">JSON文字列</param>
        /// <param name="customization">読み込まれたカスタマイズデータ</param>
        /// <returns>読み込みに成功した場合はtrue</returns>
        public static bool TryFromJson(string json, out MountCustomization? customization)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("[MountCustomization] JSON string is null or empty.");
                customization = null;
                return false;
            }

            try
            {
                customization = JsonUtility.FromJson<MountCustomization>(json);

                if (customization == null)
                {
                    Debug.LogError("[MountCustomization] JsonUtility returned null.");
                    return false;
                }

                return true;
            }
            catch (ArgumentException ex)
            {
                Debug.LogError($"[MountCustomization] Invalid JSON format: {ex.Message}");
                customization = null;
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MountCustomization] Unexpected error parsing JSON: {ex.Message}\nStack trace: {ex.StackTrace}");
                customization = null;
                return false;
            }
        }

        /// <summary>
        /// JSONから読み込みます（後方互換性のため保持）
        /// </summary>
        /// <param name="json">JSON文字列</param>
        /// <returns>カスタマイズデータ（失敗時はnull）</returns>
        public static MountCustomization? FromJson(string json)
        {
            TryFromJson(json, out var customization);
            return customization;
        }

        #endregion
    }
}
