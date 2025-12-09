using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Editor.UI.Views.Popups;
using AdvancedSceneManager.Services;

namespace AdvancedSceneManager.Editor.UI.Notifications
{

    class UpdateNotification : PersistentNotification
    {

        [Inject] private readonly IUpdateService updateService;

        public override NotificationImportance importance => NotificationImportance.Default;

        protected override void OnAdded() =>
            RegisterEvent<UpdateCheckedEvent>(e => ReloadNotification());

        protected override bool DisplayNotification() =>
            updateService.isUpdateAvailable;

        protected override Notification GenerateNotification() =>
            new($"ASM {updateService.latestVersion} is available for download")
            {
                dismissOnClick = false,
                kind = NotificationKind.Info
            };

        protected override void OnClick() =>
            ASMWindow.OpenPopup<UpdatePopup>();

        protected override void OnDismiss() =>
            updateService.MarkNotified();

    }

}
