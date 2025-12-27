#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavalryFight.Services.Customization
{
    /// <summary>
    /// キャラクターのカスタマイズデータ
    /// </summary>
    /// <remarks>
    /// P09 Modular Humanoidアセットのカスタマイズオプションを表します。
    /// すべての外見、防具、武器の設定を含みます。
    /// </remarks>
    [Serializable]
    public class CharacterCustomization
    {
        #region Basic Information

        /// <summary>
        /// 性別
        /// </summary>
        public Gender Gender = Gender.Male;

        /// <summary>
        /// 顔のタイプ（1-3、P09は1-based indexing）
        /// </summary>
        public int FaceType = 1;

        #endregion

        #region Appearance

        /// <summary>
        /// 髪型ID（1-14、0 = なし）
        /// </summary>
        public int HairstyleId = 1;

        /// <summary>
        /// 髪の色ID（1-9）
        /// </summary>
        public int HairColorId = 1;

        /// <summary>
        /// 目の色ID（1-5）
        /// </summary>
        public int EyeColorId = 1;

        /// <summary>
        /// 顔のひげID（男性のみ、0-8、0 = なし）
        /// </summary>
        public int FacialHairId = 0;

        /// <summary>
        /// 肌の色ID（1-3）
        /// </summary>
        public int SkinToneId = 1;

        /// <summary>
        /// バストサイズ（女性のみ、1-3）
        /// </summary>
        /// <remarks>
        /// 1 = 小、2 = 中、3 = 大
        /// </remarks>
        public int BustSize = 1;

        #endregion

        #region Armor

        /// <summary>
        /// 頭部防具ID（0 = なし、2-12）
        /// </summary>
        public int HeadArmorId = 0;

        /// <summary>
        /// 胸部防具ID（0 = 素体、1-12）
        /// </summary>
        public int ChestArmorId = 0;

        /// <summary>
        /// 腕部防具ID（0 = 素体、1-12）
        /// </summary>
        public int ArmsArmorId = 0;

        /// <summary>
        /// 腰部防具ID（1-12）
        /// </summary>
        public int WaistArmorId = 1;

        /// <summary>
        /// 脚部防具ID（0 = 素体、1-12）
        /// </summary>
        public int LegsArmorId = 0;

        #endregion

        #region Weapons

        /// <summary>
        /// 弓ID（10-13、P09のWeapon IDs: 1-5=剣、6-9=杖、10-13=弓）
        /// </summary>
        public int BowId = 10;

        #endregion

        #region Constructors

        /// <summary>
        /// CharacterCustomizationの新しいインスタンスを初期化します
        /// </summary>
        public CharacterCustomization()
        {
        }

        /// <summary>
        /// CharacterCustomizationの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="gender">性別</param>
        public CharacterCustomization(Gender gender)
        {
            Gender = gender;
        }

        #endregion

        #region Methods

        /// <summary>
        /// 防具スロットの値を取得します
        /// </summary>
        /// <param name="slot">防具スロット</param>
        /// <returns>防具ID</returns>
        public int GetArmorId(ArmorSlot slot)
        {
            return slot switch
            {
                ArmorSlot.Head => HeadArmorId,
                ArmorSlot.Chest => ChestArmorId,
                ArmorSlot.Arms => ArmsArmorId,
                ArmorSlot.Waist => WaistArmorId,
                ArmorSlot.Legs => LegsArmorId,
                _ => 0
            };
        }

        /// <summary>
        /// 防具スロットの値を設定します
        /// </summary>
        /// <param name="slot">防具スロット</param>
        /// <param name="armorId">防具ID</param>
        public void SetArmorId(ArmorSlot slot, int armorId)
        {
            switch (slot)
            {
                case ArmorSlot.Head:
                    HeadArmorId = armorId;
                    break;
                case ArmorSlot.Chest:
                    ChestArmorId = armorId;
                    break;
                case ArmorSlot.Arms:
                    ArmsArmorId = armorId;
                    break;
                case ArmorSlot.Waist:
                    WaistArmorId = armorId;
                    break;
                case ArmorSlot.Legs:
                    LegsArmorId = armorId;
                    break;
            }
        }

        /// <summary>
        /// このカスタマイズのコピーを作成します
        /// </summary>
        /// <returns>コピーされたカスタマイズ</returns>
        public CharacterCustomization Clone()
        {
            return new CharacterCustomization
            {
                Gender = this.Gender,
                FaceType = this.FaceType,
                HairstyleId = this.HairstyleId,
                HairColorId = this.HairColorId,
                EyeColorId = this.EyeColorId,
                FacialHairId = this.FacialHairId,
                SkinToneId = this.SkinToneId,
                BustSize = this.BustSize,
                HeadArmorId = this.HeadArmorId,
                ChestArmorId = this.ChestArmorId,
                ArmsArmorId = this.ArmsArmorId,
                WaistArmorId = this.WaistArmorId,
                LegsArmorId = this.LegsArmorId,
                BowId = this.BowId
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
        /// JSONから読み込みます
        /// </summary>
        /// <param name="json">JSON文字列</param>
        /// <returns>カスタマイズデータ</returns>
        public static CharacterCustomization? FromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<CharacterCustomization>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CharacterCustomization] Failed to parse JSON: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}
