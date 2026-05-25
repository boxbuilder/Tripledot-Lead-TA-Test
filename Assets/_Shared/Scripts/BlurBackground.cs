using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace Tripledot.Shared
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(RawImage))]
    public class BlurBackground : MonoBehaviour
    {
        private enum BlurQuality : ushort
        {
            Fast   = 1,
            Normal = 2,
            Best   = 3
        }

        [Header("Blur")]
        [Tooltip("Material that performs the blur blit pass (e.g. a two-pass Gaussian).")]
        [SerializeField] private Material blitMaterial;
        [SerializeField] private BlurQuality blurQuality = BlurQuality.Normal;
        [SerializeField] private int targetWidth  = 256;
        [SerializeField] private int targetHeight = 128;

        [Header("Fade")]
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("Camera filter")]
        [Tooltip("Cameras tagged this way are skipped when capturing the blur source (e.g. a UI overlay camera). Leave empty to capture every enabled camera.")]
        [SerializeField] private string ignoreCameraTag = "";

        // Internal
        private RawImage      _rawImage;
        private RenderTexture _cameraTexture;
        private RenderTexture _downsizedTexture;
        private CancellationTokenSource _animCts;

        void Awake()
        {
            _rawImage = GetComponent<RawImage>();
        }

        async void OnEnable()
        {
            // Each time the popup re-opens we recapture: the world behind it may have changed.
            SetAlpha(0f);
            ReleaseTextures();

            _animCts?.Cancel();
            _animCts?.Dispose();
            _animCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            var token = _animCts.Token;

            try
            {
                // Wait for the regular frame to finish rendering before we manually re-render each
                // camera into our RT — otherwise we'd capture a half-drawn frame.
                await Awaitable.EndOfFrameAsync(token);
                CaptureAndBlur();
                await FadeAsync(0f, 1f, token);
            }
            catch (System.OperationCanceledException)
            {
                // Disabled mid-show — OnDisable already handled cleanup.
            }
        }

        void OnDisable()
        {
            _animCts?.Cancel();
            _animCts?.Dispose();
            _animCts = null;
            ReleaseTextures();
        }

        void OnDestroy()
        {
            // OnDisable already fires on destroy, but be defensive in case of weird shutdown order.
            _animCts?.Cancel();
            _animCts?.Dispose();
            _animCts = null;
            ReleaseTextures();
        }

        /// <summary>
        /// Fade the blur out. Await this from the popup controller before deactivating the
        /// GameObject so the fade is allowed to complete instead of being cut off by OnDisable.
        /// </summary>
        public async Awaitable HideAsync()
        {
            if (_rawImage == null) return;

            _animCts?.Cancel();
            _animCts?.Dispose();
            _animCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            var token = _animCts.Token;

            try { await FadeAsync(_rawImage.color.a, 0f, token); }
            catch (System.OperationCanceledException) { }
        }

        private void CaptureAndBlur()
        {
            // 1) Render every enabled camera (minus the filtered tag) into a single RenderTexture.
            _cameraTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.RGB565);
            foreach (var cam in Camera.allCameras)
            {
                if (!cam.enabled) continue;
                if (!string.IsNullOrEmpty(ignoreCameraTag) && cam.CompareTag(ignoreCameraTag)) continue;
                cam.targetTexture = _cameraTexture;
                cam.Render();
                cam.targetTexture = null;
            }

            // 2) Downsample into the working texture.
            _downsizedTexture = RenderTexture.GetTemporary(targetWidth, targetHeight);
            Graphics.Blit(_cameraTexture, _downsizedTexture, blitMaterial);
            DestroyTexture(_cameraTexture);
            _cameraTexture = null;

            // 3) Ping-pong blit through a temp buffer to apply N blur passes.
            var tempBuffer = RenderTexture.GetTemporary(targetWidth, targetHeight);
            for (int i = 0; i < (int)blurQuality; i++)
            {
                Graphics.Blit(_downsizedTexture, tempBuffer,         blitMaterial);
                Graphics.Blit(tempBuffer,         _downsizedTexture, blitMaterial);
            }
            RenderTexture.ReleaseTemporary(tempBuffer);

            _rawImage.texture = _downsizedTexture;
        }

        private async Awaitable FadeAsync(float from, float to, CancellationToken token)
        {
            if (fadeDuration <= 0f || Mathf.Approximately(from, to))
            {
                SetAlpha(to);
                return;
            }

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                await Awaitable.NextFrameAsync(token);
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                SetAlpha(Mathf.Lerp(from, to, t));
            }
            SetAlpha(to);
        }

        private void SetAlpha(float a)
        {
            var c = _rawImage.color;
            c.a = a;
            _rawImage.color = c;
        }

        private void ReleaseTextures()
        {
            if (_downsizedTexture != null)
            {
                RenderTexture.ReleaseTemporary(_downsizedTexture);
                _downsizedTexture = null;
            }
            DestroyTexture(_cameraTexture);
            _cameraTexture = null;
        }

        private static void DestroyTexture(RenderTexture texture)
        {
            if (!texture) return;
            texture.Release();
            texture.DiscardContents();
            Destroy(texture);
        }
    }
}
