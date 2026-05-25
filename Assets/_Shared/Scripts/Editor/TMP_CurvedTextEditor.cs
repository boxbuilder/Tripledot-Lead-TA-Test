using TMPro.EditorUtilities;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;


[CustomEditor(typeof(TMP_CurvedText))]
public class TMP_CurvedTextEditor : TMP_EditorPanelUI
{
    private TMP_CurvedText _component;
    private bool   _isAnimating;     // also drives the subscription state of EditorApplication.update
    private float  _elapsedTime;
    private double _lastEditorTime;

    protected override void OnEnable()
    {
        base.OnEnable();
        _component = (TMP_CurvedText)target;
        // Don't reset to idle in play mode — the runtime coroutine owns AnimationTime there
        // and we'd cause a one-frame glitch by stomping on it.
        if (!EditorApplication.isPlaying)
            ResetToIdle();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        // Make sure we don't leave a dangling EditorApplication.update subscription
        // if the inspector is closed while a preview was running.
        UnsubscribeIfNeeded();
    }

    public override void OnInspectorGUI()
    {
        if (_component == null)
            _component = (TMP_CurvedText)target;

        serializedObject.Update();
        EditorGUI.BeginChangeCheck();

        base.OnInspectorGUI();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Curved Text properties", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("vertexCurve"),       new GUIContent("Text curvature"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("curveScale"),        new GUIContent("Text curvature scale"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("curveBoundingBox"),  new GUIContent("Curve BoundingBox"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("isAnimated"),        new GUIContent("Is Animated"));

        if (_component.isAnimated)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animationCurve"),         new GUIContent("Animation curve"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animDurationInSeconds"),  new GUIContent("Animation Duration in sec."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("offsetDelayInSeconds"),   new GUIContent("Delay Duration in sec."));

            if (!EditorApplication.isPlaying)
            {
                if (GUILayout.Button(_isAnimating ? "Stop" : "Test Animation"))
                {
                    if (_isAnimating) StopTestAnimation();
                    else              StartTestAnimation();
                }
            }
        }

        bool changed = EditorGUI.EndChangeCheck();
        if (changed)
        {
            serializedObject.ApplyModifiedProperties();
            // Re-warp with the new properties without disturbing whether we're animating or idle.
            _component.RefreshMesh();
        }
    }

    private void StartTestAnimation()
    {
        _elapsedTime = 0f;
        _isAnimating = true;
        _lastEditorTime = EditorApplication.timeSinceStartup;
        // Subscribe to the editor tick only for the duration of the preview — that way play mode
        // (and any other inspector idle state) carries zero per-frame cost from this script.
        EditorApplication.update += OnEditorUpdate;

        // Ensure textInfo is populated so the setter actually invokes GenerateTextMesh
        // and TotalDuration sees the right character count.
        _component.ForceMeshUpdate();
        _component.AnimationTime = 0f;
        EditorApplication.QueuePlayerLoopUpdate();
        InternalEditorUtility.RepaintAllViews();
    }

    private void StopTestAnimation()
    {
        UnsubscribeIfNeeded();
        ResetToIdle();
        // Same Game View / player-loop dance as Start: without this the view stays frozen
        // on the last animated frame even though _animationTime is back to -1.
        EditorApplication.QueuePlayerLoopUpdate();
        InternalEditorUtility.RepaintAllViews();
    }

    private void UnsubscribeIfNeeded()
    {
        if (!_isAnimating) return;
        EditorApplication.update -= OnEditorUpdate;
        _isAnimating = false;
    }

    private void ResetToIdle()
    {
        if (_component != null)
            _component.AnimationTime = -1f;
    }

    private void OnEditorUpdate()
    {
        if (!_isAnimating || _component == null) return;

        double now = EditorApplication.timeSinceStartup;
        float  dt  = (float)(now - _lastEditorTime);
        _lastEditorTime = now;
        _elapsedTime   += dt;

        float total = _component.TotalDuration;
        _component.AnimationTime = _elapsedTime;

        if (_elapsedTime >= total)
        {
            _component.AnimationTime = total;
            UnsubscribeIfNeeded(); // animation finished — release the editor tick subscription
        }

        // Push the mesh we just rebuilt through the canvas pipeline now.
        Canvas.ForceUpdateCanvases();
        // RepaintAllViews wakes up the Scene View but the Game View in edit mode is driven by
        // the player loop, not by editor repaint events. Without QueuePlayerLoopUpdate the Game
        // View stays frozen until something else (a selection change, window focus) forces it.
        EditorApplication.QueuePlayerLoopUpdate();
        InternalEditorUtility.RepaintAllViews();
    }
}
