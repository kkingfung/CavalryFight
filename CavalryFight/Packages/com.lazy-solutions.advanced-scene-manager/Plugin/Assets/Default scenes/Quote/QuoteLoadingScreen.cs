using AdvancedSceneManager.Utility;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedSceneManager.Defaults
{

    /// <summary>A default loading screen that displays random quotes while loading.</summary>
    public class QuoteLoadingScreen : ProgressBarLoadingScreen
    {

        /// <summary>Quotes to display during the loading screen.</summary>
        public Quotes quotes;

        /// <summary>UI text element used to display the current quote.</summary>
        public Text QuoteText;

        /// <summary>UI text element showing the current quote index and total count.</summary>
        public Text QuoteCountText;

        /// <summary>UI text element for the "Press any key to continue" message.</summary>
        public Text pressAnyKeyToContinueText;

        /// <summary>Transform containing the entire quote content area.</summary>
        public RectTransform Content;

        /// <summary>Transform containing the text elements for fading transitions.</summary>
        public RectTransform Text;

        /// <summary>Time to wait before showing the next quote in seconds.</summary>
        public float slideshowDelay = 4f;

        int index = -1;

        /// <inheritdoc />
        public override IEnumerator OnOpen()
        {
            Next();
            yield return FadeIn();
            StartCoroutine(Slideshow());
        }

        /// <inheritdoc />
        public override IEnumerator OnClose()
        {
            pressAnyKeyToContinueText.CrossFadeAlpha(1, 0.5f, true);
            yield return WaitForAnyKey();

            yield return Content.Fade(0, 0.5f, true);
            yield return FadeOut();
        }

        IEnumerator Slideshow()
        {
            while (this)
            {
                yield return new WaitForSecondsRealtime(slideshowDelay);
                yield return Text.Fade(0, 0.5f, true);

                yield return new WaitForSeconds(1);
                Next();

                yield return Text.Fade(1, 0.5f, true);
            }
        }

        void Next()
        {
            index = (int)Mathf.Repeat(index + 1, quotes.quoteList.Count);
            var quote = quotes.quoteList[index];
            QuoteText.text = quote;

            QuoteCountText.text = $"{index + 1} / {quotes.quoteList.Count}";
        }

    }

}
