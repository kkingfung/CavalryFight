using UnityEngine;

namespace AdvancedSceneManager.Models
{

    partial class Profile
    {

        [SerializeField] private bool m_notify;
        [SerializeField] private string m_notifyMessage;

        /// <summary>Specifies whatever this profile should trigger a notification when imported.</summary>
        public bool notify
        {
            get => m_notify;
            set { m_notify = value; Save(); }
        }

        /// <summary>Specifies the notification messasge, when <see cref="notify"/> is <see langword="true"/>.</summary>
        /// <remarks>If empty, then a fallback message will be used.</remarks>
        public string notifyMessage
        {
            get => m_notifyMessage;
            set { m_notifyMessage = value; Save(); }
        }

    }

}
