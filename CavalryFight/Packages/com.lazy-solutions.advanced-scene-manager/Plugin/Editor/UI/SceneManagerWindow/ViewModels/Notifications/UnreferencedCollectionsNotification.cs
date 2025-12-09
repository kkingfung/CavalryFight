using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Editor.UI.Views.Popups;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace AdvancedSceneManager.Editor.UI.Notifications
{

    class UnreferencedCollectionsNotification : PersistentNotification
    {

        public override NotificationImportance importance => NotificationImportance.Default;

        Dictionary<Profile, ISceneCollection[]> collections;
        protected override void OnAdded()
        {
            collections = UnreferencedCollectionsPopup.GetUnreferencedCollections();

            RegisterEvent<ScenesAvailableForImportChangedEvent>(e =>
            {
                collections = UnreferencedCollectionsPopup.GetUnreferencedCollections();
                ReloadNotification();
            });
        }

        protected override bool DisplayNotification() =>
            collections.Any();

        protected override Notification GenerateNotification() =>
            new($"You have {collections.Values.Sum(c => c.Length)} collection assets that have remained after deletion from profile. Click here to clean them up.")
            {
                canDismiss = false,
                dismissOnClick = false,
                kind = NotificationKind.Scene
            };

        protected override void OnClick() =>
            ASMWindow.OpenPopup<UnreferencedCollectionsPopup>();

    }

}
