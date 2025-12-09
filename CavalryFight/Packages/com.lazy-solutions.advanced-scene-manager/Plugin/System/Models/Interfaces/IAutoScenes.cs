using System.Collections.Generic;

namespace AdvancedSceneManager.Models.Interfaces
{

    /// <summary>Provides access to members needed for the auto scene API, which is implemented using extension methods.</summary>
    /// <remarks>See also <see cref="AdvancedSceneManager.Utility.AutoSceneUtility"/>.</remarks>
    public interface IAutoScenes<TKey, TOption> : IAutoScenes
    { }

    /// <summary>Provides access to members needed for the auto scene API, which is implemented using extension methods.</summary>
    /// <remarks>See also <see cref="AdvancedSceneManager.Utility.AutoSceneUtility"/>.</remarks>
    public interface IAutoScenes
    {
        /// <summary>Gets the auto scenes.</summary>
        List<AutoSceneEntry> autoScenes { get; }

        /// <summary>Saves the object these auto scenes are attached to.</summary>
        void Save();
    }

}
