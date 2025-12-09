using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models.Interfaces;
using AdvancedSceneManager.Models.Internal;
using AdvancedSceneManager.Utility;

namespace AdvancedSceneManager.Models
{

    /// <summary>
    /// Serves as the abstract base class for models that can be opened, closed, and preloaded.
    /// </summary>
    public abstract class ASMModel : ASMModelBase, IOpenable, IPreloadable
    {

        #region IOpenable

        /// <inheritdoc/>
        public abstract bool isOpen { get; }

        /// <inheritdoc/>
        public abstract bool isQueued { get; }

        /// <inheritdoc/>
        public abstract SceneOperation Open();

        /// <inheritdoc/>
        public abstract SceneOperation Reopen();

        /// <inheritdoc/>
        public abstract SceneOperation ToggleOpen();

        /// <inheritdoc/>
        public abstract SceneOperation Close();

        /// <inheritdoc/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public virtual void _Open() => SpamCheck.EventMethods.Execute(() => Open());

        /// <inheritdoc/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public virtual void _Reopen() => SpamCheck.EventMethods.Execute(() => Reopen());

        /// <inheritdoc/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public virtual void _ToggleOpen() => SpamCheck.EventMethods.Execute(() => ToggleOpen());

        /// <inheritdoc/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public virtual void _Close() => SpamCheck.EventMethods.Execute(() => Close());

        #endregion
        #region IPreloadable

        /// <inheritdoc/>
        public abstract bool isPreloaded { get; }

        /// <inheritdoc/>
        public abstract SceneOperation Preload();

        /// <inheritdoc/>
        public virtual SceneOperation FinishPreload() => SceneManager.runtime.FinishPreload();

        /// <inheritdoc/>
        public virtual SceneOperation CancelPreload() => SceneManager.runtime.CancelPreload();

        /// <inheritdoc/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public virtual void _Preload() => SpamCheck.EventMethods.Execute(() => Preload());

        /// <inheritdoc/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public virtual void _FinishPreload() => SpamCheck.EventMethods.Execute(() => FinishPreload());

        /// <inheritdoc/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public virtual void _CancelPreload() => SpamCheck.EventMethods.Execute(() => CancelPreload());

        #endregion

    }

}
