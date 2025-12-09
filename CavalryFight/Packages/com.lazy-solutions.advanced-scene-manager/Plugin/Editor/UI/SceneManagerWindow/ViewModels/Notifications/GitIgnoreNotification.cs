namespace AdvancedSceneManager.Editor.UI.Notifications
{

    class GitIgnoreNotification : CollapsablePersistentNotification
    {

        public override NotificationImportance importance => NotificationImportance.Priority;

        protected override bool? hideSetting
        {
            get => SceneManager.settings.user.m_hideGitIgnoreNotification;
            set => SceneManager.settings.user.m_hideGitIgnoreNotification = value ?? false;
        }

        protected override Notification GenerateNotification() =>
            new("<b>Quick reminder for public repositories</b> <i>Click for more info.</i>")
            {
                kind = NotificationKind.Info
            };

        protected override Notification GenerateExpandedNotification() =>
            new("<b>Quick reminder for public repositories</b>\n\n" +
                "Just a heads-up to help you out! Advanced Scene Manager (ASM) is a paid asset, like many others on the Unity Asset Store, and shouldn’t be included in public repositories (such as on GitHub or similar platforms) to comply with Unity’s Asset Store End User License Agreement (EULA).\n\n" +
                "To make it easy, just add the following to your .gitignore file:\n" +
                "<b>**/Packages/com.lazy-solutions.advanced-scene-manager/</b>\n\n" +
                "This small step helps keep your project in line with licensing guidelines. Thanks for your attention!\n\n" +
                "<i>(you may dismiss notification in the upper right corner once notification expanded).</i>")
            {
                kind = NotificationKind.Info,
                allowTextClippingIntoMenuButton = true,
            };

    }

}
