using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    class PickNamePopup : ViewModel, IPopup
    {

        public record Params(Action<string> onContinue, string value, Action onCancel);

        public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.pickName;
        public override bool remainOpenAsPopupAfterDomainReload => false;

        string value = string.Empty;

        bool isOpen => ASMWindow.IsPopupOpen<PickNamePopup>();

        Params promptContext;

        protected override void OnAdded()
        {

            promptContext = context.OfType<Params>() ?? throw new ArgumentException($"Context.customParam must be of type '{typeof(Params).FullName}'.");

            value = promptContext.value;

            SetupButtons();
            SetupTextField();

        }

        protected override void OnRemoved()
        {
            promptContext = null;
            value = string.Empty;
        }

        Button buttonContinue = null!;
        void SetupButtons()
        {

            buttonContinue = view.Q<Button>("button-continue");
            buttonContinue.clickable = new(Continue);
            buttonContinue.SetEnabled(false);

            view.Q<Button>("button-cancel").clickable = new(Cancel);

        }

        void SetupTextField()
        {

            var textBox = view.Q<TextField>("text-name");

#if UNITY_2022_1_OR_NEWER
            textBox.selectAllOnMouseUp = false;
            textBox.selectAllOnFocus = false;
#endif

            textBox.RegisterValueChangedCallback(e => value = e.newValue);
            textBox.RegisterValueChangedCallback(e => buttonContinue.SetEnabled(Validate()));

            textBox.value = value;
            textBox.Focus();

            if (!string.IsNullOrEmpty(textBox.value))
                textBox.SelectRange(textBox.value?.Length ?? 0, textBox.value?.Length ?? 0);

            //Register enter callback, it must only run onContinue once
            view.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode is KeyCode.KeypadEnter or KeyCode.Return)
                    if (Validate())
                    {

                        e.StopPropagation();
                        e.StopImmediatePropagation();

                        Continue();

                    }
            }, TrickleDown.TrickleDown);

        }

        bool Validate() =>
            !string.IsNullOrWhiteSpace(value) &&
            !value.StartsWith(' ') &&
            !value.EndsWith(' ') &&
            value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

        void Cancel() =>
           promptContext?.onCancel?.Invoke();

        void Continue() =>
            promptContext?.onContinue?.Invoke(value);

    }

}
