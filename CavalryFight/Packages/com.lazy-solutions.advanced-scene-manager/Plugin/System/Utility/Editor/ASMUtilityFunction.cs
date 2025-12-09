using System;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    /// <summary>Base class for ASM utility functions available in the editor UI.</summary>
    public abstract class ASMUtilityFunction
    {

        /// <summary>Gets the display name of the function.</summary>
        public abstract string Name { get; }

        /// <summary>Gets the description of the function.</summary>
        public abstract string Description { get; }

        /// <summary>Gets the group this function belongs to.</summary>
        public abstract string Group { get; }

        /// <summary>Gets the order used for sorting within its group.</summary>
        public virtual int Order { get; }

        internal Action onCloseRequest;

        /// <summary>Called when this function is invoked from the UI.</summary>
        /// <param name="optionsGUI">Use this to provide options in the UI, and include a run button. If <see langword="null"/>, the popup closes immediately as the action is assumed to run without options.</param>
        public virtual void OnInvoke(ref VisualElement optionsGUI) { optionsGUI = null; }

        /// <summary>Closes the popup if options were provided in <see cref="OnInvoke(ref VisualElement)"/>.</summary>
        /// <remarks>If no options are provided, the popup closes automatically when focus is lost.</remarks>
        public void ClosePopup() =>
            onCloseRequest?.Invoke();

        /// <summary>Called when the function is enabled.</summary>
        public virtual void OnEnable() { }

        /// <summary>Called when the function is disabled.</summary>
        public virtual void OnDisable() { }

    }

}
