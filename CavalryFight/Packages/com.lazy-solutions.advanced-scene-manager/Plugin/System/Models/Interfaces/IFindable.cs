namespace AdvancedSceneManager.Models.Interfaces
{

    /// <summary>Defines methods for finding assets.</summary>
    /// <remarks>See also: <see cref="AdvancedSceneManager.Utility.AssetSearchUtility"/>.</remarks>
    public interface IFindable
    {

        /// <summary>Matches this <see cref="IFindable"/> against the query string.</summary>
        public bool IsMatch(string q);

    }

}
