using AdvancedSceneManager.Editor.UI.Utility;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    /// <summary>A simple animated progress spinner, optionally with text.</summary>
    [UxmlElement]
    public partial class ProgressSpinner : VisualElement
    {
        [UxmlAttribute]
        public float RotationsPerMinute
        {
            get => m_rpm;
            set { m_rpm = value; UpdateAnimation(); }
        }

        readonly VisualElement spinner;
        float m_rpm = 60;

        public ProgressSpinner()
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;
            pickingMode = PickingMode.Ignore;

            spinner = new Label
            {
                text = "\uf1ce",
                pickingMode = PickingMode.Ignore,
                parseEscapeSequences = true
            };

            spinner.AddToClassList("fontAwesome");
            spinner.style.width = 32;
            spinner.style.height = 32;
            spinner.style.marginLeft = 0;
            spinner.style.marginTop = 0;
            spinner.style.marginRight = 0;
            spinner.style.marginBottom = 0;
            spinner.style.paddingLeft = 0;
            spinner.style.paddingTop = 0;
            spinner.style.paddingRight = 0;
            spinner.style.paddingBottom = 0;
            spinner.style.alignSelf = Align.Center;
            spinner.style.position = Position.Absolute;
            spinner.style.display = DisplayStyle.Flex;

            spinner.style.unityTextAlign = TextAnchor.MiddleCenter;

            Add(spinner);

            UpdateAnimation();

            RegisterCallback<GeometryChangedEvent>(e =>
            {
                // Use the smallest dimension to ensure the spinner fits in both directions
                float size = Mathf.Min(resolvedStyle.width, resolvedStyle.height);

                // Optionally scale slightly smaller to avoid clipping at edges
                spinner.style.fontSize = new Length(size * 0.85f, LengthUnit.Pixel);
            });

        }

        IVisualElementScheduledItem animation;
        void UpdateAnimation()
        {
            animation?.Pause();

            var msPerRotation = 60_000f / RotationsPerMinute;
            var tickInterval = 10f; // ms per frame
            var stepPerTick = 360f / (msPerRotation / tickInterval);

            animation = spinner.Rotate((long)tickInterval, (int)stepPerTick);
        }

        public void Start()
        {
            UpdateAnimation();
        }

        public void Stop()
        {
            animation?.Pause();
        }

    }

}
