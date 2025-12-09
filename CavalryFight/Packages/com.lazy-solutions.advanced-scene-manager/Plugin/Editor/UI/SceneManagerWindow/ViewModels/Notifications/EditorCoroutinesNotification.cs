using AdvancedSceneManager.Services;
using UnityEditor;

namespace AdvancedSceneManager.Editor.UI.Notifications
{

    class EditorCoroutinesNotification : PersistentNotification
    {

        public override NotificationImportance importance => NotificationImportance.Default;

        protected override bool? hideSetting
        {
            get => SessionState.GetBool(nameof(EditorCoroutinesNotification), false);
            set => SessionState.SetBool(nameof(EditorCoroutinesNotification), value ?? false);
        }

        protected override void OnAdded()
        {
            ServiceUtility.Register(this);
        }

        protected override void OnRemoved()
        {
            ServiceUtility.Unregister(this);
        }

#if COROUTINES
        protected override bool DisplayNotification() => false;
#else
        protected override bool DisplayNotification() => true;
#endif

        public void Show()
        {
            hideSetting = false;
            ReloadNotification();
        }

        protected override Notification GenerateNotification() =>
            new("Editor coroutines is not installed, this may cause some features to behave unexpectedly outside of play mode.")
            {
                dismissOnClick = false,
                kind = NotificationKind.Link,
                iconInfo = "PackageManager:com.unity.editorcoroutines"
            };

        protected override void OnClick()
        {
            UnityEditor.PackageManager.UI.Window.Open("com.unity.editorcoroutines");
        }

    }

}
