using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Utility.Discoverability;
using System;
using System.Linq;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class SettingsPopup
    {

        public class ExtensionsPage : SubPage
        {

            public override string title => "Extension settings";
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.settings.extensions;

            protected override void OnAdded()
            {

                view.Clear();

                var extensionPages = DiscoverabilityUtility.GetMembers<ASMWindowElementAttribute>().
                    Where(discoverable => ((ASMWindowElementAttribute)discoverable.attribute).location == ElementLocation.Settings).
                    Select(discoverable => discoverable.member as Type).
                    Select(Instantiate).
                    ToList();

                foreach (var viewModel in extensionPages)
                {
                    try
                    {

                        var button = new NavigationButton
                        {
                            iconLeft = viewModel.settingsCategoryIcon,
                            text = viewModel.title
                        };

                        button.Q<Button>().RegisterCallback<ClickEvent>(e => view.GetAncestor<PageStackView>().Push(viewModel.GetType()));

                        view.Add(button);

                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex);
                    }
                }

            }

        }

    }

}
