using AdvancedSceneManager.Loading;
using AdvancedSceneManager.Utility;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedSceneManager.Defaults
{

    /// <summary>A default loading screen script. Fades screen out, then fades screen in when loading is done. Does not display progress.</summary>
    public class FadeLoadingScreen : LoadingScreen, IFadeLoadingScreen
    {

        /// <summary>The <see cref="CanvasGroup"/> to fade in and out.</summary>
        public CanvasGroup fadeGroup;

        /// <summary>The image of which to set background color.</summary>
        public Image fadeBackground;

        /// <summary>Programmatic override for <see cref="fadeDuration"/>, which is saved in scene file.</summary>
        public float? fadeInDurationOverride;

        /// <summary>The duration to fade in and out for.</summary>
        public float fadeDuration = 0.5f;

        /// <summary>The color of the background.</summary>
        public Color color;

        float IFadeLoadingScreen.fadeDuration
        {
            get => fadeDuration;
            set => fadeInDurationOverride = value;
        }

        Color IFadeLoadingScreen.color
        {
            get => color;
            set => color = value;
        }

        /// <inheritdoc />
        protected override void Start()
        {
            base.Start();
            if (fadeGroup)
                fadeGroup!.alpha = 0;
        }

        /// <inheritdoc />
        public override IEnumerator OnOpen()
        {
            yield return FadeIn();
        }

        /// <inheritdoc />
        public override IEnumerator OnClose()
        {
            yield return FadeOut();
        }

        /// <summary>Fades <see cref="fadeGroup"/> in, for the amount of second specified <see cref="fadeDuration"/>, or <see cref="fadeInDurationOverride"/> if specified.</summary>
        protected IEnumerator FadeIn()
        {

            if (!fadeBackground || !fadeGroup)
            {
                Log.Warning("Could not fade since properties weren't set.");
                yield break;
            }

            fadeBackground!.color = color; //Color can be changed when using FadeUtility methods

            if ((fadeInDurationOverride ?? fadeDuration) > 0)
                yield return fadeGroup!.Fade(1, fadeInDurationOverride ?? fadeDuration);
            else
                fadeGroup!.alpha = 1;

        }

        /// <summary>Fades <see cref="fadeGroup"/> out, for the amount of second specified <see cref="fadeDuration"/>, or <see cref="fadeInDurationOverride"/> if specified.</summary>
        protected IEnumerator FadeOut()
        {
            yield return fadeGroup.Fade(0, fadeDuration);
        }

    }

}
