using AdvancedSceneManager.Editor.UI.Views;
using AdvancedSceneManager.Editor.UI.Views.Popups;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using System.Collections.Generic;

namespace AdvancedSceneManager.Editor.UI.Notifications
{

    class InvalidSceneNotification : SceneNotification<Scene, InvalidScenePopup>
    {

        public override NotificationImportance importance => NotificationImportance.High;

        public override IEnumerable<Scene> GetItems() =>
            SceneImportUtility.invalidScenes;

        public override string GetNotificationText(int count) =>
            $"You have {count} imported scenes that are invalid, they have no associated SceneAsset, click here to unimport now...";

    }

}
