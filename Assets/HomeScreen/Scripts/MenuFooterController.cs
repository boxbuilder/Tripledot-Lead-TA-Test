using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Tripledot.HomeScreen
{
    public class MenuFooterController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private RectTransform indicator;
        [SerializeField] private ButtonFooterController startSelected;
        [SerializeField] private List<ButtonFooterController> footerButtons;

        [Header("Settings")]
        [SerializeField] private float indicatorMoveDuration = 0.25f;

        // Internal
        private ButtonFooterController _buttonSelected;
        private CancellationTokenSource _moveCts;

        void Start()
        {
            if (startSelected != null)
                OnButtonClickedEvent(startSelected);
            else
                indicator.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            foreach (var btn in footerButtons)
                btn.OnButtonClickedEvent.AddListener(OnButtonClickedEvent);
        }

        void OnDisable()
        {
            foreach (var btn in footerButtons)
                btn.OnButtonClickedEvent.RemoveListener(OnButtonClickedEvent);
        }

        private void OnButtonClickedEvent(ButtonFooterController buttonClicked)
        {
            if (!footerButtons.Contains(buttonClicked)) return;

            // Clicking the already-selected button deselects everything
            if (_buttonSelected == buttonClicked)
            {
                _buttonSelected = null;

                foreach (var btn in footerButtons)
                    btn.SetSelect(false);

                indicator.gameObject.SetActive(false);
                return;
            }

            _buttonSelected = buttonClicked;

            foreach (var btn in footerButtons)
                btn.SetSelect(_buttonSelected == btn);

            MoveIndicator();
        }

        private async void MoveIndicator()
        {
            if (_buttonSelected == null) return;

            indicator.gameObject.SetActive(true);

            // Cancel any in-flight animation, link to destroyCancellationToken
            // so the async operation is automatically stopped if this GO is destroyed
            _moveCts?.Cancel();
            _moveCts?.Dispose();
            _moveCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

            var token   = _moveCts.Token;
            var target  = (RectTransform)_buttonSelected.transform;
            float startX  = indicator.anchoredPosition.x;
            float targetX = target.anchoredPosition.x;
            float elapsed = 0f;

            try
            {
                while (elapsed < indicatorMoveDuration)
                {
                    await Awaitable.NextFrameAsync(token);
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / indicatorMoveDuration));
                    indicator.anchoredPosition = new Vector2(
                        Mathf.Lerp(startX, targetX, t),
                        indicator.anchoredPosition.y
                    );
                }

                // Snap to exact target position once animation completes
                indicator.anchoredPosition = new Vector2(targetX, indicator.anchoredPosition.y);
            }
            catch (System.OperationCanceledException)
            {
                // A new button was selected mid-animation — the new call handles the rest
            }
        }
    }
}
