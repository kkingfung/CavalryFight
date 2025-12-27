#nullable enable

namespace CavalryFight.Services.Customization
{
    /// <summary>
    /// 角のメッシュタイプ
    /// </summary>
    /// <remarks>
    /// どの角メッシュを表示するかを指定します。
    /// 実際のメッシュは「Horns/Horn1」「Horns/Horn2」にあります。
    /// </remarks>
    public enum HornType
    {
        /// <summary>角なし</summary>
        None = 0,

        /// <summary>角1</summary>
        Horn1 = 1,

        /// <summary>角2</summary>
        Horn2 = 2
    }
}
