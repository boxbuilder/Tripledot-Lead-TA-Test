using System.Threading;
using TMPro;
using UnityEngine;

namespace Tripledot.Shared
{
    /// <summary>
    /// Animates a TMP_Text component's text from <see cref="initialValue"/> to <see cref="finalValue"/>
    /// over <see cref="transitionTime"/> seconds.
    ///
    /// The displayed integer is sampled once per frame (Lerp of the elapsed/duration ratio): when the
    /// range is large compared to the number of frames available, intermediate values are naturally
    /// skipped — which is exactly what we want, since trying to render every integer would either be
    /// impossible (huge ranges) or look like a strobe.
    /// </summary>
    public class AnimatedCounter : MonoBehaviour
    {
        [Header("Range")]
        [SerializeField] private int initialValue = 0;
        [SerializeField] private int finalValue = 100;
        [SerializeField] private float transitionTime = 1.5f;

        [Header("Polish")]
        [Tooltip("Easing applied to the elapsed/duration ratio. Linear by default produces a constant counting rate; EaseOut feels punchier on score reveals.")]
        [SerializeField] private AnimationCurve easing = AnimationCurve.Linear(0, 0, 1, 1);
        [Tooltip("string.Format pattern: \"{0}\" plain, \"{0:N0}\" with thousands separator, \"${0}\" prefixed, etc.")]
        [SerializeField] private string format = "{0}";
        [SerializeField] private bool playOnEnable = true;

        // Internal
        private TMP_Text _label;
        private CancellationTokenSource _cts;

        public int InitialValue     { get => initialValue;   set => initialValue = value; }
        public int FinalValue       { get => finalValue;     set => finalValue = value; }
        public float TransitionTime { get => transitionTime; set => transitionTime = value; }

        void Awake()
        {
            _label = GetComponent<TMP_Text>();
            if (_label == null)
                Debug.LogError($"{nameof(AnimatedCounter)} requires a TMP_Text component on the same GameObject.", this);
        }

        void OnEnable()
        {
            if (playOnEnable) Play();
        }

        void OnDisable()
        {
            CancelInternal();
        }

        /// <summary>Restart the count using the current Initial/Final/TransitionTime values.</summary>
        public async void Play()
        {
            CancelInternal();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            try { await CountAsync(_cts.Token); }
            catch (System.OperationCanceledException) { }
        }

        /// <summary>Cancel any running animation. The text stays on whatever value it last showed.</summary>
        public void Stop()
        {
            CancelInternal();
        }

        /// <summary>Cancel any running animation and snap the text to <see cref="finalValue"/>.</summary>
        public void SkipToFinal()
        {
            CancelInternal();
            SetValue(finalValue);
        }

        private async Awaitable CountAsync(CancellationToken token)
        {
            if (transitionTime <= 0f)
            {
                SetValue(finalValue);
                return;
            }

            SetValue(initialValue);
            float elapsed = 0f;

            while (elapsed < transitionTime)
            {
                await Awaitable.NextFrameAsync(token);
                elapsed += Time.deltaTime;
                float t      = Mathf.Clamp01(elapsed / transitionTime);
                float curveT = easing.Evaluate(t);
                int   v      = Mathf.RoundToInt(Mathf.Lerp(initialValue, finalValue, curveT));
                SetValue(v);
            }

            // Guarantee the final value lands exactly on finalValue regardless of rounding
            // or curve overshoot.
            SetValue(finalValue);
        }

        private void CancelInternal()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private void SetValue(int v)
        {
            if (_label != null)
                _label.text = string.Format(format, v);
        }
    }
}
