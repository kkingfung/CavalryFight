using AdvancedSceneManager.Editor.UI.Views;
using AdvancedSceneManager.Editor.UI.Views.Popups;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using System.Collections.Generic;

namespace AdvancedSceneManager.Editor.UI.Notifications
{

    class BadScenePathNotification : SceneNotification<Scene, BadPathScenePopup>
    {

        public override NotificationImportance importance => NotificationImportance.High;

        public override IEnumerable<Scene> GetItems() =>
            SceneImportUtility.scenesWithBadPath;

        public override string GetNotificationText(int count) =>
            $"You have {count} imported scenes that have been de-referenced, and are recoverable, click here to fix now...";

    }

}
