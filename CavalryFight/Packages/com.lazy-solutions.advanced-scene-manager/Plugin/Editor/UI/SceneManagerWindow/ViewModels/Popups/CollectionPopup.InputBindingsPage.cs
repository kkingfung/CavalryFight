#if INPUTSYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.UIElements;
#endif

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class CollectionPopup
    {

        public class InputBindingsPage : SubPage
        {

#if INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            public override string title => context.collection.title + " - Input bindings";
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.collection.inputBindings;

            protected override void OnAdded()
            {

                base.OnAdded();


                view.Q<InputBindingField>().Bind(new(context.collection));

                var list = view.Q<ListView>("list-input-bindings-ignore");
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
#endif

        }

    }

}
