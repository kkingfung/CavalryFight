using AdvancedSceneManager.Loading;
using AdvancedSceneManager.Utility;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedSceneManager.Defaults
{
    /// <summary>A default splash screen that fades the background in and out during startup.</summary>
    [ExecuteAlways]
    [AddComponentMenu("")]
    public class SplashFade : SplashScreen
    {
        /// <summary>Canvas group used to control the fade effect of the background.</summary>
        public CanvasGroup groupBackground;

        /// <summary>Background image displayed during the splash sequence.</summary>
        public Image background;

        /// <inheritdoc />
        protected override void Start()
        {
            base.Start();

            // Use same color as Unity splash screen, if enabled; defaults to black otherwise
            background.color = SceneManager.app.startupProps?.effectiveFadeColor ?? Color.black;
        }

        /// <inheritdoc />
        public override IEnumerator OnOpen()
        {
            yield return groupBackground.Fade(1, 1).StartCoroutine();
        }

        /// <inheritdoc />
        public override IEnumerator OnClose()
        {
            yield return groupBackground.Fade(0, 1f).StartCoroutine();
        }
    }
}
