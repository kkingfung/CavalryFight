using System.Collections;
using UnityEngine;

namespace AdvancedSceneManager.Defaults
{

    /// <summary>A default loading screen script. Requires the user to press any key before loading screen closes.</summary>
    public class PressAnyButtonLoadingScreen : FadeLoadingScreen
    {

        /// <inheritdoc />
        public override IEnumerator OnOpen()
        {
            color = Color.white; //Override color since we're displaying a background
            yield return FadeIn();
        }

        /// <inheritdoc />
        public override IEnumerator OnClose()
        {
            yield return WaitForAnyKey();
            yield return FadeOut();
        }

    }

}
