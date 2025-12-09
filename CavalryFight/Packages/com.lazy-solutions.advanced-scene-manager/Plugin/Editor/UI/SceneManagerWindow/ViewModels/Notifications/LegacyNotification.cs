using AdvancedSceneManager.Editor.UI.Views.Popups;
using AdvancedSceneManager.Legacy;

namespace AdvancedSceneManager.Editor.UI.Notifications
{

    class LegacyNotification : PersistentNotification
    {

        public override NotificationImportance importance => NotificationImportance.Low;

        protected override bool DisplayNotification() =>
            LegacyUtility.FindAssets();

        protected override Notification GenerateNotification() =>
            new("Legacy ASM 1.0 assets detected. These are no longer used and can safely be removed. They won't interfere with anything but may cause confusion if left behind.\n" +
                "\n" +
                "Click here for more information.")
            {
                dismissOnClick = false,
                canDismiss = false,
                kind = NotificationKind.Info
            };

        protected override void OnClick() =>
            ASMWindow.OpenPopup<LegacyPopup>();

    }

}
