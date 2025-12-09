using System;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class SettingsPopup
    {

        public class ExtendableUIPage : SubPage
        {

            public override string title => "Extendable UI";
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.settings.appearance.extendableUI;
            public override BindTo bindTo => BindTo.UserSettings;

            internal override Type[] priorPages => new[]
            {
                typeof(AppearancePage)
            };

        }

    }

}
