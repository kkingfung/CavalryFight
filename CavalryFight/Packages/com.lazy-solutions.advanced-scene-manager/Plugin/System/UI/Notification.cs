#if UNITY_EDITOR

using AdvancedSceneManager.Utility;
using System;

namespace AdvancedSceneManager.Editor.UI
{

    /// <summary>Specifies the importance of a notification.</summary>
    public enum NotificationImportance
    {
        /// <summary>Determines that the notification will be displayed in non-overflow view when space allows, otherwise overflow menu.</summary>
        Default,
        /// <summary>Determines that the notification will never be displayed in non-overflow view.</summary>
        Low,
        /// <summary>Determines that the notification will never overflow.</summary>
        High,
        /// <summary>Determines that the notification is to be prioritized. Prioritized notifications hide all others until dismissed or removed.</summary>
        Priority
    }

    /// <summary>Specifies the kind of notification.</summary>
    /// <remarks>Only available in the editor.</remarks>
    public enum NotificationKind
    {
        /// <summary>Does not indicate anything in particular. Displays no icon.</summary>
        Default,
        /// <summary>Indicates informational content. Displays an info icon.</summary>
        Info,
        /// <summary>Indicates a warning or fixable issue. Displays a warning icon.</summary>
        FixUp,
        /// <summary>Indicates something related to scenes. Displays a scene icon.</summary>
        Scene,
        /// <summary>Indicates a link to something. Displays a link icon.</summary>
        Link,
        /// <summary>Indicates something related to profiles. Displays a profile icon.</summary>
        Profile
    }

    /// <summary>Represents a notification to be displayed.</summary>
    public class Notification
    {

        /// <summary>Unique identifier for the notification.</summary>
        public string id;

        /// <summary>Displayed message text.</summary>
        public string message;

        /// <summary>Action invoked when the notification is clicked.</summary>
        public Action onClick;

        /// <summary>Action invoked when the notification is dismissed.</summary>
        public Action onDismiss;

        /// <summary>Whether the notification can be dismissed by the user.</summary>
        public bool canDismiss = true;

        /// <summary>Whether the notification is dismissed when clicked.</summary>
        public bool dismissOnClick = true;

        /// <summary>Specifies the visual kind of the notification.</summary>
        public NotificationKind kind = NotificationKind.Default;

        /// <summary>Specifies an optional icon info identifier.</summary>
        public string iconInfo = null;

        /// <summary>Specifies an optional Font Awesome icon name.</summary>
        public string fontAwesomeIcon = null;

        /// <summary>Specifies an optional font name for the icon.</summary>
        public string iconFont = null;

        /// <summary>Specifies the importance level of the notification.</summary>
        public NotificationImportance importance = NotificationImportance.Default;

        /// <summary>Whether text may overlap the menu button area.</summary>
        public bool allowTextClippingIntoMenuButton;

        /// <summary>Whether the notification is expanded, if applicable.</summary>
        public bool? isExpanded;

        /// <summary>Whether the notification can be muted.</summary>
        public bool canMute = true;

        /// <summary>Creates a new notification.</summary>
        /// <param name="message">Displayed message text.</param>
        /// <param name="onClick">Action invoked when clicked.</param>
        /// <param name="id">Optional identifier. Auto-generated if <see langword="null"/>.</param>
        public Notification(string message, Action onClick = null, string id = null)
        {
            this.id = id ?? GuidReferenceUtility.GenerateID();
            this.message = message;
            this.onClick = onClick;
        }

    }

}
#endif
