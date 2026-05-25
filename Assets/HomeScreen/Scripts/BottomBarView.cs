using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tripledot.HomeScreen
{
    public class BottomBarView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private RectTransform indicator;
        [SerializeField] private ButtonFooterController startSelected;
        [SerializeField] private List<ButtonFooterController> footerButtons;

        [Header("Settings")]
        [SerializeField] private float indicatorMoveDuration  = 0.25f;
        [SerializeField] private float indicatorScaleDuration = 0.1f;
        [SerializeField] private float baseFlexWidth          = 1f;
        [SerializeField] private float selectedFlexWidth      = 1.6f;

        [Header("Events")]
        public UnityEvent<ButtonFooterController> ContentActivated;
        public UnityEvent Closed;

        // Internal
        private ButtonFooterController _buttonSelected;
        private CancellationTokenSource _animCts;
        private LayoutElement[] _layoutElements;
        private RectTransform _layoutRoot;

        void Awake()
        {
            _layoutElements = new LayoutElement[footerButtons.Count];
            for (int i = 0; i < footerButtons.Count; i++)
            {
                var btn = footerButtons[i];
                var le = btn.GetComponent<LayoutElement>();
                if (le == null) le = btn.gameObject.AddComponent<LayoutElement>();
                le.flexibleWidth = baseFlexWidth;
                _layoutElements[i] = le;
            }

            if (footerButtons.Count > 0)
                _layoutRoot = (RectTransform)footerButtons[0].transform.parent;
        }

        void Start()
        {
            if (startSelected != null)
            {
                OnButtonClickedEvent(startSelected);
            }
            else
            {
                indicator.gameObject.SetActive(false);
            }
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

            bool isDeselect        = _buttonSelected == buttonClicked;
            bool wasSelectionEmpty = _buttonSelected == null;

            _buttonSelected = isDeselect ? null : buttonClicked;

            foreach (var btn in footerButtons)
                btn.SetSelect(_buttonSelected == btn);

            if (isDeselect)
                Closed?.Invoke();
            else
                ContentActivated?.Invoke(_buttonSelected);

            AnimateAsync(isDeselect, wasSelectionEmpty);
        }

        private async void AnimateAsync(bool disappearing, bool wasSelectionEmpty)
        {
            _animCts?.Cancel();
            _animCts?.Dispose();
            _animCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            var token = _animCts.Token;

            // Snapshot start / target accordion flex values
            float[] startFlex = new float[footerButtons.Count];
            float[] endFlex   = new float[footerButtons.Count];
            for (int i = 0; i < footerButtons.Count; i++)
            {
                startFlex[i] = _layoutElements[i].flexibleWidth;
                endFlex[i]   = (footerButtons[i] == _buttonSelected) ? selectedFlexWidth : baseFlexWidth;
            }

            bool appearing = wasSelectionEmpty && !disappearing;

            // Scale.x range for this animation
            float startScaleX, endScaleX;
            if (appearing)
            {
                // The indicator is about to be revealed — pin scale.x to 0 BEFORE activating so the
                // first frame doesn't flash at full size, then activate and let the loop scale it up.
                SetIndicatorScaleX(0f);
                indicator.gameObject.SetActive(true);
                startScaleX = 0f;
                endScaleX   = 1f;
            }
            else if (disappearing)
            {
                startScaleX = indicator.localScale.x;
                endScaleX   = 0f;
            }
            else
            {
                // Sliding between buttons. Normally scale.x is already 1, but if the user
                // interrupted a mid-appear we still want to land cleanly at 1.
                startScaleX = indicator.localScale.x;
                endScaleX   = 1f;
            }

            // Position: snap on appear (indicator is materialising on the target), slide otherwise.
            bool  snapPosition    = appearing;
            float startIndicatorX = indicator.position.x;
            float fixedY          = indicator.position.y;
            float fixedZ          = indicator.position.z;

            float maxDuration = Mathf.Max(indicatorMoveDuration, indicatorScaleDuration);
            float elapsed = 0f;

            try
            {
                while (elapsed < maxDuration)
                {
                    await Awaitable.NextFrameAsync(token);
                    elapsed += Time.deltaTime;

                    // Accordion + position progress (smooth-stepped)
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / indicatorMoveDuration));
                    ApplyFlexAtProgress(startFlex, endFlex, t);
                    UpdateIndicatorPosition(snapPosition, startIndicatorX, fixedY, fixedZ, t);

                    // Scale.x progress (linear — it's so short that easing is imperceptible).
                    float st = Mathf.Clamp01(elapsed / indicatorScaleDuration);
                    SetIndicatorScaleX(Mathf.Lerp(startScaleX, endScaleX, st));
                }

                // Settle exact final values to avoid floating-point drift at the very end.
                ApplyFlexAtProgress(startFlex, endFlex, 1f);
                UpdateIndicatorPosition(snapPosition, startIndicatorX, fixedY, fixedZ, 1f);
                SetIndicatorScaleX(endScaleX);

                // Hide the GameObject only after the disappear scale-down has fully played.
                if (disappearing)
                    indicator.gameObject.SetActive(false);
            }
            catch (System.OperationCanceledException)
            {
                // A new selection cancelled this animation — the new call takes over.
            }
        }

        private void ApplyFlexAtProgress(float[] startFlex, float[] endFlex, float t)
        {
            for (int i = 0; i < footerButtons.Count; i++)
                _layoutElements[i].flexibleWidth = Mathf.Lerp(startFlex[i], endFlex[i], t);

            if (_layoutRoot != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_layoutRoot);
        }

        private void UpdateIndicatorPosition(bool snap, float startIndicatorX, float fixedY, float fixedZ, float t)
        {
            if (_buttonSelected == null) return;

            float currentTargetX = ComputeIndicatorWorldXFor(_buttonSelected);
            float newX = snap ? currentTargetX : Mathf.Lerp(startIndicatorX, currentTargetX, t);
            indicator.position = new Vector3(newX, fixedY, fixedZ);
        }

        private float ComputeIndicatorWorldXFor(ButtonFooterController button)
        {
            var target = (RectTransform)button.transform;

            // Align the indicator's visual center with the button's visual center,
            // independent of either rect's pivot or parent hierarchy.
            float buttonCenterWorldX    = target.TransformPoint(target.rect.center).x;
            float indicatorCenterWorldX = indicator.TransformPoint(indicator.rect.center).x;
            return indicator.position.x + (buttonCenterWorldX - indicatorCenterWorldX);
        }

        private void SetIndicatorScaleX(float x)
        {
            // localScale is a struct: copy, mutate, assign back.
            var s = indicator.localScale;
            s.x = x;
            indicator.localScale = s;
        }
    }
}
