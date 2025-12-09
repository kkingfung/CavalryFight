#if UNITY_EDITOR

using AdvancedSceneManager.Callbacks.Events;
using System;

namespace AdvancedSceneManager.Editor.UI
{

    /// <summary>Contains APIs relating to the ASM window.</summary>
    /// <remarks>Only available in the editor.</remarks>
    public static class ASMWindow
    {

        static void ValidateType(Type type)
        {

            type = type ?? throw new ArgumentNullException(nameof(type));

            if (!type.IsType<ViewModel>())
                throw new ArgumentException("Type must inherit from ViewModel.");

        }

        #region Notifications

        internal record AddNotificationRequestEvent(Notification notification) : EventCallbackBase;
        internal record RemoveNotificationRequestEvent(Notification notification) : EventCallbackBase;

        /// <summary>Adds a notification to the ASM window, optionally with click and dismiss callbacks, dismiss behavior, visual style, and icon information.</summary>
        /// <remarks>Does nothing if the ASM window is not open.</remarks>
        public static void AddNotification(Notification notification) =>
            SceneManager.events.InvokeCallbackSync(new AddNotificationRequestEvent(notification));

        /// <summary>Removes the notification with the specified id.</summary>
        /// <remarks>Does nothing if the ASM window is not open.</remarks>
        public static void RemoveNotification(Notification notification) =>
            SceneManager.events.InvokeCallbackSync(new RemoveNotificationRequestEvent(notification));

        #endregion
        #region Popups

        internal record OpenPopupRequestEvent(Type type, ViewModelContext? context) : EventCallbackBase;
        internal record ClosePopupRequestEvent() : EventCallbackBase;

        internal static Type currentPopup;

        /// <summary>Determines whether any popup is currently open.</summary>
        public static bool IsPopupOpen() => currentPopup is not null;

        /// <summary>Determines whether a popup of type <typeparamref name="T"/> is currently open.</summary>
        public static bool IsPopupOpen<T>() =>
            currentPopup?.IsAssignableFrom(typeof(T)) ?? false;

        /// <summary>Opens <typeparamref name="T"/> as a popup.</summary>
        /// <remarks>Does nothing if the ASM window is not open.</remarks>
        public static void OpenPopup<T>() where T : ViewModel, new() =>
            OpenPopup(typeof(T));

        /// <summary>Opens <typeparamref name="T"/> as a popup.</summary>
        /// <remarks>Does nothing if the ASM window is not open.</remarks>
        public static void OpenPopup<T>(ViewModelContext? context = null) where T : ViewModel, new() =>
            OpenPopup(typeof(T), context);

        /// <summary>Opens <paramref name="type"/> as a popup.</summary>
        /// <remarks>Does nothing if the ASM window is not open.</remarks>
        public static void OpenPopup(Type type, ViewModelContext? context = null)
        {
            ValidateType(type);
            SceneManager.events.InvokeCallbackSync(new OpenPopupRequestEvent(type, context));
        }

        /// <summary>Closes the currently open popup, if one is open..</summary>
        /// <remarks>Does nothing if the ASM window is not open.</remarks>
        public static void ClosePopup() =>
            SceneManager.events.InvokeCallbackSync(new ClosePopupRequestEvent());

        #endregion
        #region Settings

        internal record OpenSettingsPageRequest(Type type, ViewModelContext? context) : EventCallbackBase;

        /// <summary></summary>
        /// <remarks>Does nothing if the ASM window is not open.</remarks>
        public static void OpenSettings() =>
            OpenSettingsInternal(null, skipViewModelValidation: true);

        /// <summary></summary>
        /// <remarks>Does nothing if the ASM window is not open.</remarks>
        public static void OpenSettings<T>() where T : ViewModel, new() =>
            OpenSettingsInternal(typeof(T));

        /// <summary></summary>
        /// <remarks>Does nothing if the ASM window is not open.</remarks>
        public static void OpenSettings<T>(ViewModelContext context) where T : ViewModel, new() =>
            OpenSettingsInternal(typeof(T), context);

        /// <summary></summary>
        /// <remarks>Does nothing if the ASM window is not open.</remarks>
        public static void OpenSettings(Type type) =>
            OpenSettingsInternal(type);

        /// <summary></summary>
        /// <remarks>Does nothing if the ASM window is not open.</remarks>
        public static void OpenSettings(Type type, ViewModelContext context) =>
            OpenSettingsInternal(type, context);

        static void OpenSettingsInternal(Type type, ViewModelContext? context = null, bool skipViewModelValidation = false)
        {

            if (!skipViewModelValidation)
                ValidateType(type);

            SceneManager.events.InvokeCallbackSync(new OpenSettingsPageRequest(type, context));

        }

        #endregion
        #region Reload collections

        internal record ReloadCollectionViewRequest : EventCallbackBase;

        /// <summary>Reloads collection ui.</summary>
        public static void ReloadCollections()
        {
            SceneManager.events.InvokeCallbackSync<ReloadCollectionViewRequest>();
        }

        #endregion

    }

}
#endif
