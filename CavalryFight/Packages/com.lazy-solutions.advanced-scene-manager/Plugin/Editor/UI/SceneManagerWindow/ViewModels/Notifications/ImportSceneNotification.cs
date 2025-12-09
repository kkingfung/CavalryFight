using AdvancedSceneManager.Editor.UI.Views.Popups;
using AdvancedSceneManager.Editor.Utility;
using System.Collections.Generic;
using System.Linq;

namespace AdvancedSceneManager.Editor.UI.Notifications
{

    class ImportSceneNotification : SceneNotification<string, ImportScenePopup>
    {

        public override NotificationImportance importance => NotificationImportance.High;

        public override NotificationKind kind => NotificationKind.Info;
        public override string icon => notificationService.Icons[NotificationKind.Scene].fontAwesomeIcon;
        public override string iconFont => notificationService.Icons[NotificationKind.Scene].font;
        public override string iconInfo => "Scene import";

        public override IEnumerable<string> GetItems() =>
            SceneImportUtility.unimportedScenes.Except(SceneImportUtility.dynamicScenes);

        public override string GetNotificationText(int count) =>
            $"You have {count} scenes ready to be imported, click here to import them now...";

    }

}
