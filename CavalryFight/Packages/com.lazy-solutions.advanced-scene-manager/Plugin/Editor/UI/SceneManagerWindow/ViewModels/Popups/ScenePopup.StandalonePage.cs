using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class ScenePopup
    {

        public class StandalonePage : SubPage
        {

            public override string title => "Standalone scene settings";
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.scene.standalone;

            protected override void OnAdded()
            {

                view.Q<InputBindingField>().Bind(new(context.scene));
                view.Bind(new(context.scene));

                var list = view.Q<ListView>("list-input-bindings-ignore");
                list.makeNoneElement = () => new Label("No scenes added") { name = "text-no-items" };

                list.makeItem += () =>
                {
                    var container = new VisualElement();
                    var field = new SceneField();
                    field.style.paddingTop = 0;
                    field.style.paddingBottom = 0;
                    field.style.paddingLeft = 2;
                    field.style.paddingRight = 2;
                    container.Add(field);
                    return container;
                };

            }

        }

    }

}

