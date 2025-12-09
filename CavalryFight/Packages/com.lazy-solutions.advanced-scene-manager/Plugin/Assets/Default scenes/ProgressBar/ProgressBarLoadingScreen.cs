using AdvancedSceneManager.Loading;
using AdvancedSceneManager.Utility;
using System.Collections;
using UnityEngine.UI;

namespace AdvancedSceneManager.Defaults
{

    /// <summary>A default loading screen script. Displays progress with a progress bar.</summary>
    public class ProgressBarLoadingScreen : FadeLoadingScreen
    {

        /// <summary>Specifies the slider to use as progress bar.</summary>
        public Slider slider;

        /// <inheritdoc />
        public override IEnumerator OnOpen() =>
            FadeIn();

        /// <inheritdoc />
        public override IEnumerator OnClose()
        {

            //Hide slider before fade, since it is brighter than background and will 
            //appear to stay on screen for longer than background which looks bad
            yield return slider.Fade(0, 0.5f, true);

            yield return FadeOut();

        }

        /// <inheritdoc />
        public override void OnProgressChanged(ILoadProgressData progress)
        {
            if (slider)
                slider.value = progress.value;
        }

    }

}
