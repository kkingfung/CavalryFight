#nullable enable

using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.UI
{
    /// <summary>
    /// RenderTextureをUI Toolkit VisualElementに表示するヘルパー
    /// </summary>
    /// <remarks>
    /// UI ToolkitのVisualElementにRenderTextureを表示します。
    /// カスタマイズプレビューなど、3DコンテンツをUIに埋め込む際に使用します。
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class RenderTextureDisplay : MonoBehaviour
    {
        #region Inspector Fields

        /// <summary>
        /// 表示するRenderTexture
        /// </summary>
        [Tooltip("表示するRenderTexture")]
        [SerializeField] private RenderTexture? _renderTexture;

        /// <summary>
        /// RenderTextureを表示するVisualElementの名前
        /// </summary>
        [Tooltip("RenderTextureを表示するVisualElementの名前")]
        [SerializeField] private string _visualElementName = "PreviewContainer";

        /// <summary>
        /// ScaleModeの設定
        /// </summary>
        [Tooltip("ScaleModeの設定")]
        [SerializeField] private ScaleMode _scaleMode = ScaleMode.ScaleToFit;

        #endregion

        #region Fields

        /// <summary>
        /// UIDocument
        /// </summary>
        private UIDocument? _uiDocument;

        /// <summary>
        /// 表示先のVisualElement
        /// </summary>
        private VisualElement? _targetElement;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (_uiDocument != null && _uiDocument.rootVisualElement != null)
            {
                SetupRenderTexture();
            }
        }

        private void Start()
        {
            // Awake/OnEnableで設定できなかった場合のフォールバック
            if (_targetElement == null && _uiDocument != null)
            {
                SetupRenderTexture();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// RenderTextureを設定します
        /// </summary>
        /// <param name="renderTexture">設定するRenderTexture</param>
        public void SetRenderTexture(RenderTexture? renderTexture)
        {
            _renderTexture = renderTexture;
            UpdateDisplay();
        }

        /// <summary>
        /// ScaleModeを設定します
        /// </summary>
        /// <param name="scaleMode">設定するScaleMode</param>
        public void SetScaleMode(ScaleMode scaleMode)
        {
            _scaleMode = scaleMode;
            UpdateDisplay();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// RenderTextureの表示を設定します
        /// </summary>
        private void SetupRenderTexture()
        {
            if (_uiDocument == null || _uiDocument.rootVisualElement == null)
            {
                Debug.LogWarning("[RenderTextureDisplay] UIDocument or root visual element is null.");
                return;
            }

            _targetElement = _uiDocument.rootVisualElement.Q<VisualElement>(_visualElementName);

            if (_targetElement == null)
            {
                Debug.LogError($"[RenderTextureDisplay] Could not find VisualElement with name: {_visualElementName}");
                return;
            }

            UpdateDisplay();
        }

        /// <summary>
        /// 表示を更新します
        /// </summary>
        private void UpdateDisplay()
        {
            if (_targetElement == null)
            {
                return;
            }

            if (_renderTexture != null)
            {
                // RenderTextureを背景として設定
                // UI ToolkitではBackgroundプロパティを使用してRenderTextureを設定
                _targetElement.style.backgroundImage = Background.FromRenderTexture(_renderTexture);

                // ScaleModeに応じて背景サイズを設定（modern API使用）
                ApplyScaleMode(_targetElement, _scaleMode);

                Debug.Log($"[RenderTextureDisplay] RenderTexture set to element: {_visualElementName}");
            }
            else
            {
                // 既存の背景をクリア
                _targetElement.style.backgroundImage = StyleKeyword.Null;
                Debug.LogWarning("[RenderTextureDisplay] RenderTexture is null.");
            }
        }

        /// <summary>
        /// ScaleModeに応じて背景のスタイルを設定します
        /// </summary>
        /// <param name="element">設定対象のVisualElement</param>
        /// <param name="scaleMode">スケールモード</param>
        private void ApplyScaleMode(VisualElement element, ScaleMode scaleMode)
        {
            switch (scaleMode)
            {
                case ScaleMode.StretchToFill:
                    // 要素のサイズに合わせて引き伸ばす
                    element.style.backgroundSize = new BackgroundSize(new Length(100, LengthUnit.Percent), new Length(100, LengthUnit.Percent));
                    element.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
                    break;

                case ScaleMode.ScaleAndCrop:
                    // アスペクト比を維持して要素を覆うようにスケール（はみ出た部分はクロップ）
                    element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                    element.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
                    break;

                case ScaleMode.ScaleToFit:
                default:
                    // アスペクト比を維持して要素内に収まるようにスケール
                    element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                    element.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
                    break;
            }
        }

        #endregion
    }
}
