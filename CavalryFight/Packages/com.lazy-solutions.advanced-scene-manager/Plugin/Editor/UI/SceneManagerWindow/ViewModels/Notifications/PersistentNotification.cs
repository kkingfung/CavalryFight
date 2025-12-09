using AdvancedSceneManager.Services;

namespace AdvancedSceneManager.Editor.UI
{

    public interface IPersistentNotification
    {
        void ReloadNotification();
    }

    abstract class PersistentNotification : ViewModel, IPersistentNotification
    {

        protected virtual string id => GetType().AssemblyQualifiedName;
        protected virtual bool? hideSetting { get; set; }
        public virtual NotificationImportance importance { get; }

        protected virtual bool DisplayNotification() => true;

        [Inject] protected INotificationService notificationService = null!;
        [Inject] protected IPersistentNotificationService persistentNotificationService = null!;

        public virtual void ReloadNotification()
        {
            Display(GenerateNotification());
        }

        protected void Display(Notification notification)
        {

            ClearNotification();

            if (!ShouldDisplayNotification())
                return;

            if (notification is null)
                return;

            notification.id = id;
            notification.onDismiss = OnDismiss;
            notification.onClick = OnClick;
            notification.importance = importance;

            notificationService.Notify(notification);

        }

        protected bool ShouldDisplayNotification()
        {

            if (persistentNotificationService.forceDisplayAll)
                return true;

            if (hideSetting is true)
                return false;

            return DisplayNotification();

        }

        protected void ClearNotification()
        {
            notificationService.ClearNotification(id);
        }

        protected virtual void OnDismiss()
        {
            if (hideSetting.HasValue)
            {
                hideSetting = true;
                SceneManager.settings.user.Save();
            }
        }

        protected virtual void OnClick()
        { }

        protected abstract Notification GenerateNotification();

    }

}
