using AdvancedSceneManager.Editor.UI.Views.Popups;

namespace AdvancedSceneManager.Editor.UI.Notifications
{

    class WelcomeNotification : PersistentNotification
    {

        public override NotificationImportance importance => NotificationImportance.Priority;

        protected override bool? hideSetting
        {
            get => SceneManager.settings.user.m_hideDocsNotification;
            set => SceneManager.settings.user.m_hideDocsNotification = value ?? false;
        }

        protected override Notification GenerateNotification() =>
            new("Welcome to Advanced Scene Manager, it is strongly recommended to check out quick start guides before getting started. Click here to do so.")
            {
                dismissOnClick = false,
                kind = NotificationKind.Link,
                iconInfo = "ASMMenu:Docs",
            };

        protected override void OnClick()
        {
            ASMWindow.OpenPopup<MenuPopup>(new(customParam: MenuPopup.flashDocsSection));
        }

    }

}
