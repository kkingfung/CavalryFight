using AdvancedSceneManager.Core;

namespace AdvancedSceneManager.Models.Interfaces
{

    /// <summary>Defines members for assets that support preloading.</summary>
    public interface IPreloadable
    {

        /// <summary>Gets whether this asset is currently preloaded.</summary>
        bool isPreloaded { get; }

        /// <summary>Preloads this asset.</summary>
        SceneOperation Preload();

        /// <inheritdoc cref="Preload"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        void _Preload();

        /// <summary>Finishes all active preloads.</summary>
        /// <remarks>Global operation that affects all preloaded assets, not just this instance.</remarks>
        SceneOperation FinishPreload();

        /// <inheritdoc cref="FinishPreload"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        void _FinishPreload();

        /// <summary>Cancels all active preloads.</summary>
        /// <remarks>Global operation that affects all preloaded assets, not just this instance.</remarks>
        SceneOperation CancelPreload();

        /// <inheritdoc cref="CancelPreload"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        void _CancelPreload();

    }

}
