namespace AdvancedSceneManager.Editor.UI.Notifications
{

    abstract class CollapsablePersistentNotification : PersistentNotification
    {

        bool showFull;
        public override void ReloadNotification() =>
            Display(GetNotification());

        Notification GetNotification()
        {
            var notification = showFull ? GenerateExpandedNotification() : GenerateNotification();

            if (notification is not null)
            {
                notification.id = id;
                notification.isExpanded = showFull;
                notification.dismissOnClick = false;
                notification.onDismiss = OnDismiss;
                notification.onClick = OnClick;
                notification.canMute = false;
            }

            return notification;
        }

        protected override void OnClick()
        {
            showFull = !showFull;
            ReloadNotification();
        }

        protected abstract Notification GenerateExpandedNotification();

    }

}
