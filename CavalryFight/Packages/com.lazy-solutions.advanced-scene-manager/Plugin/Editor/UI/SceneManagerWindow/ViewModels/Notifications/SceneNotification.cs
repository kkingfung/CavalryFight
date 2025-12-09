using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace AdvancedSceneManager.Editor.UI.Notifications
{

    public class CheckableItem<T>
    {
        public T value;
        public bool isChecked;
    }

    abstract class SceneNotification<T, TPopup> : PersistentNotification where TPopup : ViewModel, new()
    {

        protected override void OnAdded() => EditorApplication.projectChanged += EditorApplication_projectChanged;

        protected override void OnRemoved() => EditorApplication.projectChanged -= EditorApplication_projectChanged;

        private void EditorApplication_projectChanged()
        {
            EditorApplication.delayCall -= ReloadNotification;
            EditorApplication.delayCall += ReloadNotification;
        }

        public virtual NotificationKind kind { get; protected set; } = NotificationKind.Scene;
        public virtual string icon { get; protected set; } = null;
        public virtual string iconFont { get; protected set; } = null;
        public virtual string iconInfo { get; protected set; } = "Scene import";

        public abstract string GetNotificationText(int count);

        public CheckableItem<T>[] items { get; private set; }

        public abstract IEnumerable<T> GetItems();

        protected override void OnClick()
        {
            ASMWindow.OpenPopup<TPopup>();
        }

        public override void ReloadNotification()
        {

            ClearNotification();

            if (!ShouldDisplayNotification())
                return;

            if (!SceneManager.profile)
                return;

            var items = GetItems();
            var count = items.Count();
            var hasItems = count > 0;

            if (!hasItems)
                return;

            Display(new(GetNotificationText(count)) { canDismiss = false, dismissOnClick = false, kind = kind, fontAwesomeIcon = icon, iconFont = iconFont, iconInfo = iconInfo });

        }

        protected override Notification GenerateNotification()
        {
            return null;
        }

    }

}
