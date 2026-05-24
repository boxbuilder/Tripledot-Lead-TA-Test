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
        [SerializeField] private float indicatorMoveDuration = 0.25f;
        [SerializeField] private float baseFlexWidth = 1f;
        [SerializeField] private float selectedFlexWidth = 1.6f;

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

            bool isDeselect        = _buttonSelected == buttonClicked;
            bool wasSelectionEmpty = _buttonSelected == null;

            _buttonSelected = isDeselect ? null : buttonClicked;

            foreach (var btn in footerButtons)
                btn.SetSelect(_buttonSelected == btn);

            if (isDeselect)
                indicator.gameObject.SetActive(false);
            else
                indicator.gameObject.SetActive(true);

            // Snap the indicator when it appears from nothing; slide when it was already on a button.
            bool snapIndicator = wasSelectionEmpty;
            AnimateAsync(snapIndicator);

            if (isDeselect)
                Closed?.Invoke();
            else
                ContentActivated?.Invoke(_buttonSelected);
        }

        private async void AnimateAsync(bool snapIndicator)
        {
            _animCts?.Cancel();
            _animCts?.Dispose();
            _animCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            var token = _animCts.Token;

            // Snapshot start / target flex values for the accordion
            float[] startFlex = new float[footerButtons.Count];
            float[] endFlex   = new float[footerButtons.Count];
            for (int i = 0; i < footerButtons.Count; i++)
            {
                startFlex[i] = _layoutElements[i].flexibleWidth;
                endFlex[i]   = (footerButtons[i] == _buttonSelected) ? selectedFlexWidth : baseFlexWidth;
            }

            float startIndicatorX = indicator.position.x;
            float fixedY = indicator.position.y;
            float fixedZ = indicator.position.z;
            float elapsed = 0f;

            try
            {
                while (elapsed < indicatorMoveDuration)
                {
                    await Awaitable.NextFrameAsync(token);
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / indicatorMoveDuration));

                    ApplyFlexAtProgress(startFlex, endFlex, t);
                    UpdateIndicatorPosition(snapIndicator, startIndicatorX, fixedY, fixedZ, t);
                }

                ApplyFlexAtProgress(startFlex, endFlex, 1f);
                UpdateIndicatorPosition(snapIndicator, startIndicatorX, fixedY, fixedZ, 1f);
            }
            catch (System.OperationCanceledException)
            {
                // A new selection cancelled this animation — the new call takes over
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
    }
}
