using System;
using System.Collections;
using TMPro;
using UnityEngine;


public class TMP_CurvedText : TextMeshProUGUI
{
    [Tooltip("Vertical curvature of the baseline (X = 0..1 along the text width)")]
    public AnimationCurve vertexCurve = new(
        new Keyframe(0, 0),
        new Keyframe(0.25f, 2.0f),
        new Keyframe(0.5f, 0),
        new Keyframe(0.75f, 2.0f),
        new Keyframe(1, 0f));

    [Tooltip("Multiplier on the curvature amplitude")]
    public float curveScale = 1.0f;

    [Tooltip("If true the curve is sampled over the RectTransform width; if false over the actual text bounding box")]
    public bool curveBoundingBox = true;

    [Tooltip("Whether the scale-pop animation plays on enable")]
    public bool isAnimated = true;

    [Tooltip("Per-character scale curve (X = 0..1 over one character's animation lifetime)")]
    public AnimationCurve animationCurve = new(
        new Keyframe(0, 0),
        new Keyframe(0.25f, 2.0f),
        new Keyframe(0.5f, 0),
        new Keyframe(0.75f, 2.0f),
        new Keyframe(1, 1f));

    [Tooltip("Duration of a single character's animation, in seconds")]
    public float animDurationInSeconds = 1.0f;

    [Tooltip("Stagger between consecutive characters, in seconds")]
    public float offsetDelayInSeconds = 0.2f;

    // < 0 == idle (no animation in progress). >= 0 == elapsed time since the routine started.
    private float _animationTime = -1f;
    private Coroutine _animationCoroutine;

    /// <summary>Elapsed time of the running animation. Set externally (e.g. by the editor preview) to scrub.</summary>
    public float AnimationTime
    {
        get => _animationTime;
        set
        {
            _animationTime = value;
            // Always invoke the regen chain: base.GenerateTextMesh() is the path that
            // populates textInfo, so guarding on textInfo HERE creates a chicken-and-egg
            // where the setter exits before TMP has had a chance to lay out the text.
            // The empty-text case is handled inside GenerateTextMesh itself.
            GenerateTextMesh();
        }
    }

    /// <summary>Total time required for every character to complete its animation, including the stagger tail.</summary>
    public float TotalDuration
    {
        get
        {
            int chars = textInfo != null ? textInfo.characterCount : 0;
            return animDurationInSeconds + Mathf.Max(0, chars - 1) * offsetDelayInSeconds;
        }
    }

    public bool IsAnimationCompleted =>
        !isAnimated || _animationTime < 0f || _animationTime >= TotalDuration;

    protected override void Awake()
    {
        base.Awake();
        vertexCurve.preWrapMode = WrapMode.Clamp;
        vertexCurve.postWrapMode = WrapMode.Clamp;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (!Application.isPlaying) return;

        if (isAnimated)
        {
            if (_animationCoroutine != null)
                StopCoroutine(_animationCoroutine);
            // textInfo isn't guaranteed to be populated yet — force a pass so the coroutine
            // can compute TotalDuration against the real characterCount (otherwise the
            // stagger tail gets clipped and the animation appears to end early).
            ForceMeshUpdate();
            _animationCoroutine = StartCoroutine(ExecuteAnimationRoutine());
        }
        else
        {
            AnimationTime = -1f;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }
    }

    private IEnumerator ExecuteAnimationRoutine()
    {
        float elapsed = 0f;
        // Re-read TotalDuration each iteration so a mid-flight text change can't truncate the tail.
        while (elapsed < TotalDuration)
        {
            elapsed += Time.deltaTime;
            AnimationTime = elapsed;
            yield return null;
        }
        AnimationTime = TotalDuration;
        _animationCoroutine = null;
    }

    public void SkipAnimation()
    {
        if (!Application.isPlaying || !isAnimated) return;
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }
        AnimationTime = TotalDuration;
    }

    /// <summary>Public hook for editor preview / external refresh after property changes.</summary>
    public void RefreshMesh()
    {
        if (textInfo == null) return;
        GenerateTextMesh();
    }

    protected override void GenerateTextMesh()
    {
        try
        {
            base.GenerateTextMesh();
            if (textInfo == null || textInfo.characterCount < 1) return;
            WarpText();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void WarpText()
    {
        RectTransform rt = (RectTransform)transform;
        float rectWidth = rt.rect.width;

        // Choose the horizontal axis along which the curve is sampled.
        float axisOrigin, axisWidth;
        if (curveBoundingBox)
        {
            axisOrigin = rt.rect.xMin;
            axisWidth = rectWidth;
        }
        else
        {
            axisOrigin = textInfo.characterInfo[0].bottomLeft.x;
            axisWidth = textInfo.characterInfo[textInfo.characterCount - 1].topRight.x - axisOrigin;
        }

        const float h = 0.0001f;
        float animDenom = animDurationInSeconds > 0f ? animDurationInSeconds : 1f;
        bool isIdle = _animationTime < 0f || !isAnimated;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            int vertexIndex   = textInfo.characterInfo[i].vertexIndex;
            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            Vector3[] verts   = textInfo.meshInfo[materialIndex].vertices;

            if (verts == null || vertexIndex < 0 || vertexIndex + 3 >= verts.Length) continue;

            // Pivot at the mid-baseline of the glyph so rotation/scale are visually correct.
            Vector2 pivot = new Vector2(
                (verts[vertexIndex + 0].x + verts[vertexIndex + 2].x) * 0.5f,
                textInfo.characterInfo[i].baseLine);

            verts[vertexIndex + 0] -= (Vector3)pivot;
            verts[vertexIndex + 1] -= (Vector3)pivot;
            verts[vertexIndex + 2] -= (Vector3)pivot;
            verts[vertexIndex + 3] -= (Vector3)pivot;

            // Sample the curve at the character's normalised X and the angle of its tangent.
            float x0 = (pivot.x - axisOrigin) / axisWidth;
            float y0 = vertexCurve.Evaluate(x0)       * curveScale * 10f;
            float y1 = vertexCurve.Evaluate(x0 + h)   * curveScale * 10f;
            float angleDeg = Mathf.Atan2(y1 - y0, h * axisWidth) * Mathf.Rad2Deg;

            // Per-character scale: idle = 1; otherwise sample the animation curve at this char's local progress.
            Vector3 scale;
            if (isIdle)
            {
                scale = Vector3.one;
            }
            else
            {
                float localElapsed = _animationTime - i * offsetDelayInSeconds;
                float localT = Mathf.Clamp01(localElapsed / animDenom);
                scale = Vector3.one * animationCurve.Evaluate(localT);
            }

            var m = Matrix4x4.TRS(new Vector3(0, y0, 0), Quaternion.Euler(0, 0, angleDeg), scale);

            verts[vertexIndex + 0] = m.MultiplyPoint3x4(verts[vertexIndex + 0]) + (Vector3)pivot;
            verts[vertexIndex + 1] = m.MultiplyPoint3x4(verts[vertexIndex + 1]) + (Vector3)pivot;
            verts[vertexIndex + 2] = m.MultiplyPoint3x4(verts[vertexIndex + 2]) + (Vector3)pivot;
            verts[vertexIndex + 3] = m.MultiplyPoint3x4(verts[vertexIndex + 3]) + (Vector3)pivot;
        }

        UpdateVertexData();
    }
}
