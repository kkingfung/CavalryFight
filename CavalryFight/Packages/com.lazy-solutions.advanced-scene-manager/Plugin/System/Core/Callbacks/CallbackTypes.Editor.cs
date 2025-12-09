#if UNITY_EDITOR

using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Interfaces;
using AdvancedSceneManager.Models.Internal;
using System.Collections.Generic;
using UnityEditor;

namespace AdvancedSceneManager.Callbacks.Events.Editor
{

    /// <summary>Occurs when a profile is added to ASM.</summary>
    /// <remarks>Does not support <see cref="EventCallbackBase.WaitFor(System.Collections.IEnumerator)"/> or any of its overloads.</remarks>
    public record ProfileAddedEvent(Profile profile) : EventCallbackBase;

    /// <summary>Occurs when a profile is removed from ASM.</summary>
    /// <remarks>Does not support <see cref="EventCallbackBase.WaitFor(System.Collections.IEnumerator)"/> or any of its overloads.</remarks>
    public record ProfileRemovedEvent(Profile profile) : EventCallbackBase;

    /// <summary>Occurs when a collection is added to a profile.</summary>
    public record CollectionAddedEvent(ISceneCollection collection) : EventCallbackBase;

    /// <summary>Occurs when a collection is removed from a profile.</summary>
    /// <remarks>Soft delete, still recoverable. Triggers undo period.</remarks>
    public record CollectionRemovedEvent(ISceneCollection collection) : EventCallbackBase;

    /// <summary>Occurs when a collection is deleted from a profile.</summary>
    /// <remarks>Hard delete, not recoverable. Happens after undo period.</remarks>
    public record CollectionDeletedEvent(ISceneCollection collection) : EventCallbackBase;

    /// <summary>Occurs when a collection is restored after remove, before hard delete.</summary>
    public record CollectionRestoredEvent(ISceneCollection collection) : EventCallbackBase;

    /// <summary>Occurs when a scene is imported into ASM.</summary>
    public record SceneImportedEvent(Scene scene) : EventCallbackBase;

    /// <summary>Occurs when a scene is unimported from ASM.</summary>
    public record SceneUnimportedEvent(Scene scene) : EventCallbackBase;

    /// <summary>Occurs when selection changes in the collection view, of the ASM window.</summary>
    public record CollectionViewSelectionChangedEvent(IEnumerable<ISceneCollection> selectedCollections, IEnumerable<CollectionScenePair> selectedScenes) : EventCallbackBase;

    /// <summary>Occurs when the active profile is changed.</summary>
    public record ProfileChangedEvent(Profile profile) : EventCallbackBase;

    /// <summary>Occurs when either <see cref="AdvancedSceneManager.Editor.Utility.SceneImportUtility.unimportedScenes"/>, <see cref="AdvancedSceneManager.Editor.Utility.SceneImportUtility.importedScenes"/>, or <see cref="AdvancedSceneManager.Editor.Utility.SceneImportUtility.invalidScenes"/> has changed.</summary>
    public record ScenesAvailableForImportChangedEvent : EventCallbackBase;

    /// <summary>Occurs when an ASM model property changes. This is the same as <see cref="System.ComponentModel.INotifyPropertyChanged"/>.</summary>
    /// <param name="model">The model had a property changed.</param>
    /// <param name="propertyName">The name of the property that changed.</param>
    /// <remarks><see cref="string.Empty"/> will be used when <see cref="System.ComponentModel.INotifyPropertyChanged"/> is called from <see langword="OnValidate"/>.</remarks>
    public record ModelPropertyChangedEvent(ASMModelBase model, string propertyName) : EventCallbackBase;

    /// <summary>Occurs when the unity play mode state changes.</summary>
    /// <remarks>Wrapper for <see cref="UnityEditor.EditorApplication.playModeStateChanged"/>.</remarks>
    public record PlayModeChangedEvent(PlayModeStateChange state) : EventCallbackBase;

    /// <summary>Occurs before ASM enters play mode when ASM play button is used.</summary>
    public record BeforeASMPlayModeEvent(App.StartupProps props) : EventCallbackBase;

    /// <summary>Occurs when a setting in ASM changes.</summary>
    public record ASMSettingsChangedEvent : EventCallbackBase;

    /// <summary>Occurs when a notification is added or removed.</summary>
    internal record NotificationsChangedEvent : EventCallbackBase;

    /// <summary>Occurs when a undo item is added or removed.</summary>
    internal record UndoItemsChangedEvent : EventCallbackBase;

    /// <summary>Occurs after ASM has checked for updates.</summary>
    public record UpdateCheckedEvent : EventCallbackBase;

    /// <summary>Occurs when the ASM editor window is opened.</summary>
    public record ASMWindowOpenEvent : EventCallbackBase;

    /// <summary>Occurs when the ASM editor window is closed.</summary>
    public record ASMWindowCloseEvent : EventCallbackBase;

    /// <summary>Occurs when the ASM editor window is enabled.</summary>
    public record OnWindowEnableEvent : EventCallbackBase;

    /// <summary>Occurs when the ASM editor window is disabled.</summary>
    public record OnWindowDisableEvent : EventCallbackBase;

    /// <summary>Occurs when the ASM editor window gains focus.</summary>
    public record OnWindowFocusEvent : EventCallbackBase;

    /// <summary>Occurs when the ASM editor window loses focus.</summary>
    public record OnWindowLostFocusEvent : EventCallbackBase;

    /// <summary>Occurs when an ASM model is renamed.</summary>
    /// <param name="model">The model that was renamed.</param>
    /// <param name="oldName">The previous name of the model.</param>
    /// <param name="newName">The new name of the model.</param>
    public record ASMModelRenamedEvent(ASMModelBase model, string oldName, string newName) : EventCallbackBase;

}

#endif