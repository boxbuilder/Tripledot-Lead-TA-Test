using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tripledot.Shared
{
    public class NavigationController : MonoBehaviour
    {
        [Header("Fade overlay")]
        [SerializeField] private Image fadeOverlay;
        [SerializeField] private float fadeDuration = 0.3f;

        async void Start()
        {
            // Coming in from a previous scene? The overlay was authored at alpha = 1,
            // so fade it out to reveal the new scene without a hard cut.
            if (fadeOverlay != null && fadeOverlay.color.a > 0f)
            {
                try { await FadeAsync(fadeOverlay.color.a, 0f, destroyCancellationToken); }
                catch (System.OperationCanceledException) { }
            }
        }

        // Wired to UI buttons (LevelCompletedButton, BackToMenuButton, ...).
        public async void LoadScene(string sceneName)
        {
            try { await TransitionToAsync(sceneName, destroyCancellationToken); }
            catch (System.OperationCanceledException) { }
        }

        private async Awaitable TransitionToAsync(string sceneName, CancellationToken token)
        {
            // Block input for the entire transition.
            if (fadeOverlay != null)
                fadeOverlay.raycastTarget = true;

            // Start the load straight away but hold activation: the heavy single-frame
            // work that LoadScene used to do is now hidden behind the fade-in.
            var op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            if (fadeOverlay != null)
                await FadeAsync(fadeOverlay.color.a, 1f, token);

            // Unity stalls at 0.9 when allowSceneActivation == false — that's our "ready" signal.
            while (op.progress < 0.9f)
                await Awaitable.NextFrameAsync(token);

            // Hand over. This GameObject is about to be destroyed; the next scene's
            // NavigationController will pick up its own overlay (authored at alpha 1) and fade out.
            op.allowSceneActivation = true;
        }

        private async Awaitable FadeAsync(float from, float to, CancellationToken token)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                await Awaitable.NextFrameAsync(token);
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                SetAlpha(Mathf.Lerp(from, to, t));
            }
            SetAlpha(to);

            // Once fully transparent, stop swallowing clicks.
            if (Mathf.Approximately(to, 0f))
                fadeOverlay.raycastTarget = false;
        }

        private void SetAlpha(float a)
        {
            var c = fadeOverlay.color;
            c.a = a;
            fadeOverlay.color = c;
        }
    }
}
