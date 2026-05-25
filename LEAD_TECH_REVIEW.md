# Lead Technical Artist — Project Review

> Project reviewed against the **Mid-Level Tech Art Brief** (`MidLevelTechArtTestBrief.pdf`)
> Unity `6000.0.40f1` · URP 2D · Mobile Target

---

## Table of Contents

1. [Delivery Issues (Pre-flight)](#delivery-issues-pre-flight)
2. [Holistic Project Critique](#1-holistic-project-critique)
3. [UI & Design System Review](#2-ui--design-system-review)
4. [Code & Architecture Audit](#3-code--architecture-audit)
5. [Performance](#4-performance)
6. [Deep-Dive Analysis — `SineWaveTextAnimation.cs` → replaced by `TMP_CurvedText`](#5-deep-dive-analysis--sinewavtextanimationcs--replaced-by-tmp_curvedtext)
7. [Visual & Artistic Polish](#6-visual--artistic-polish)
8. [Priority Matrix](#priority-matrix)

---

## Delivery Issues (Pre-flight)

Before the project was even opened, several red flags appeared at the submission level.

The delivered package weighs **nearly 1 GB** and includes folders that should never be committed to a repository:

| Folder / File | Why it should not be committed |
|---|---|
| `Library/` | Unity's local import cache — regenerated per machine |
| `Temp/` | Build temporaries — deleted by Unity on close |
| `Obj/` | C# compiler output |
| `.vs/` | Visual Studio local workspace state |
| `Logs/` | Editor logs |
| `UserSettings/` | Per-developer editor preferences |
| `*.sln` / `*.csproj` | Unity-generated solution files — regenerated on open |

These are generated artifacts that are **local to each developer's machine**. They bloat the repository significantly and can actively break the project for anyone who clones it, because Unity regenerates them against its own installation and conflicting cached data causes hard-to-diagnose import errors.

A correct Unity submission includes only: `Assets/`, `Packages/`, `ProjectSettings/`, and optionally `UserSettings/` if intentional. The official [Unity `.gitignore` template](https://github.com/github/gitignore/blob/main/Unity.gitignore) handles this automatically and should always be committed as the very first file in any Unity repository.

Additionally, due to deeply nested folder paths inside `Library/`, Windows throws **Error 0x80010135: Path too long** on extraction — meaning reviewers on Windows cannot even unzip the submission without manual workarounds. This is a hard blocker for any production pipeline.

---

## 1. Holistic Project Critique

### 1.1 Repository & Submission Standards

As noted above, the repository contains generated and OS-local files. This alone signals an unfamiliarity with standard Unity project hygiene and collaborative workflows. In a production team, committing `Library/` causes constant merge conflicts and forces every collaborator to re-import the entire project from scratch.

### 1.2 Folder Structure

The top-level `Assets/0_Project/Modules/` organisation reflects a modular intent, but execution is inconsistent and the nesting is excessive for the project scope. The deeper levels (`HomeScreen/Footer/Assets/Anim/`) add path depth without adding clarity. Several issues were identified:

- **Template and demo assets polluting the project tree**
  - `Assets/Settings/Scenes/URP2DSceneTemplate.unity` — Unity-generated template, no relation to the project
  - `Assets/Settings/Lit2DSceneTemplate.scenetemplate` — Unity-generated scene template for creating new 2D scenes during development; not a runtime or build-time dependency. Weighs **3.8 MB**, of which ~2 MB are raw RGBA pixel data for an embedded editor thumbnail texture. No production value whatsoever.
  - `Assets/Plugins/Demigiant/DOTweenPro Examples/` — three example scenes (`DOTweenAnimation_Basics`, `DOTweenAnimation_Advanced`, `DOTweenPath`) included in build-eligible paths
  - TextMesh Pro demo resources present under `Assets/TextMesh Pro/`
  - None of these belong in a production submission. They inflate build times, pollute the scene list in the Build Settings dialog, and add noise to every asset search.

- **`Assets/Settings/` should be renamed to `Assets/RenderPipeline/`** — the folder contains only two assets that actually matter: `UniversalRP.asset` (the active render pipeline, referenced by `ProjectSettings/GraphicsSettings.asset`) and `Renderer2D.asset` (the 2D renderer data it depends on). The generic name `Settings/` gives no indication of what lives inside; `RenderPipeline/` is self-documenting and aligns with standard Unity project conventions.

- **`manifest.json` never trimmed from Unity defaults**
  Three packages are installed with no corresponding assets or scripts in the project: `com.unity.visualscripting` (Bolt — zero visual scripts present), `com.unity.timeline` (no `.playable` files), and `com.unity.multiplayer.center` (multiplayer hub in a single-player mobile game). These are Unity new-project defaults that were never revisited. They add unnecessary compilation targets, increase domain reload time on every script change, and signal the project manifest was never reviewed.

  **Applied Fix:** All three removed from `manifest.json`. `packages-lock.json` deleted so Unity regenerates it cleanly on next open.

- **Inconsistent prefab naming conventions**
  - `currencyBox.prefab` (camelCase) alongside `CurrencyBox_Coins.prefab` (PascalCase)
  - `SettingsButton.prefab` and `SettingsButton1.prefab` coexist — the latter appears to be an unresolved duplicate left behind without cleanup

- **No C# namespace on any custom script** — all 7 scripts live in the global namespace, creating collision risk as the project grows and making codebase navigation harder

- **Classes indented as if inside a namespace block, without one** — `SafeArea.cs`, `CameraResolutionCheck.cs`, `ButtonFooterController.cs`, `MenuFooterController.cs` all carry an extra indentation level with no enclosing namespace declaration, a clear indicator of copy-paste from an external source without cleanup

#### Applied Fix — Restructured Asset Layout

The `0_Project/Modules/` hierarchy was replaced with a **feature-based, two-level structure**. All materials and shaders are centralised in `_Shared/` as they are technical cross-feature assets. The underscore prefix sorts `_Shared/` to the top of the folder list in the Editor.

```
Assets/
 ├── _Shared/
 │    ├── Animations/   — generic popup/icon animations and controllers
 │    ├── Fonts/        — .ttf source files and SDF font assets
 │    ├── Materials/    — all .mat files (font materials + VFX materials)
 │    ├── Prefabs/      — Canvas, GenericPopup, shared button prefabs
 │    ├── Scripts/      — SafeArea, NavigationController, CameraResolutionCheck
 │    ├── Shaders/      — all .shadergraph files (BackgroundShader, GlowRays, ShinyStar)
 │    └── Sprites/      — shared UI sprites (icons, popup panels)
 ├── HomeScreen/
 │    ├── Animations/   — footer entrance/exit, button state controllers
 │    ├── Prefabs/      — header, footer, settings popup prefabs
 │    ├── Scripts/      — ButtonFooterController, MenuFooterController, SettingsPopupController
 │    └── Sprites/      — header, footer, settings popup sprites + background
 ├── LevelCompletedScreen/
 │    ├── Animations/   — MainReward animation clips and controller
 │    ├── Prefabs/      — reward elements, title, buttons
 │    ├── Scripts/      — TMP_CurvedText
 │    └── Sprites/      — screen-specific sprites
 ├── Scenes/
 ├── RenderPipeline/    — UniversalRP.asset, Renderer2D.asset, GlobalSettings, VolumeProfile
 └── TextMesh Pro/      — TMP essentials (shaders, fonts, resources)
```

This layout keeps all assets for a given screen in one place, limits nesting to two levels, and scales cleanly — adding a new screen means adding one folder at the same level.

### 1.3 Scene Hierarchy

The `HomeScreen` scene places **every element — Header, Footer, Popups, Background, and all UI screens — inside a single Canvas**.

```
Canvas
 ├── NoSafeArea
 │    └── Background
 └── SafeArea
      └── Content
           ├── Header
           ├── LevelCompletedButton
           ├── MenuFooter
           └── Popups
                └── SettingsPopup
```

While Unity permits this, it is not a scalable practice for several reasons:

- A single Canvas **re-batches its entire geometry** whenever any child's visual state changes. Splitting into multiple Canvases (static background, HUD, popup overlay) scopes re-batches and is the standard approach for production mobile UI.
- With everything as children of one Canvas the hierarchy becomes difficult to navigate and reason about as content grows.
- There is no **additive scene loading strategy**. Background, HUD, and overlay systems should ideally be independent scenes loaded additively, enabling content streaming and team parallelism.

The `NoSafeArea` / `SafeArea` sibling pattern for separating background from content is valid, but relies on a verbatim copy of a community-authored script (see §3.7).

**`LevelCompletedScreen` reuses the same Canvas prefab as `HomeScreen`.** Both scenes share an identical root prefab that bundles Canvas, CanvasScaler, `NoSafeArea/Background`, and `SafeArea/Content` into a single asset. This creates silent cross-scene coupling: any structural change to the prefab (CanvasScaler settings, render order, added components) propagates to both scenes simultaneously. It also means `LevelCompletedScreen` cannot have a different Canvas configuration — split into multiple Canvases, different sort order, different scaler settings — without breaking the shared prefab or creating a diverging variant.

The correct approach is for the Canvas prefab to contain **only** the Canvas + CanvasScaler configuration. Screen-specific structure (`NoSafeArea`, `SafeArea`, content nodes) should either live directly in the scene or as a Prefab Variant, keeping scenes independently configurable.

---

## 2. UI & Design System Review

### 2.1 Background Image — Aspect Ratio Distortion

The `Background` GameObject uses an `Image` component configured as follows:

| Setting | Value | Problem |
|---|---|---|
| Image Type | Simple | — |
| Anchors | Min (0,0) → Max (1,1) | Full-stretch |
| Preserve Aspect | **false** | ⚠ Image distorts to fill screen |
| Material | None (Default UI Material) | — |

With `Preserve Aspect` disabled and full-stretch anchors, the sprite is scaled to fill the entire screen rectangle regardless of its original proportions. On any device whose aspect ratio differs from the design reference — tablets, ultra-wide phones, any simulator preset — the background is visually distorted.

**Recommended fix:** Add an `AspectRatioFitter` component set to **Envelope Parent** mode, or move the background to a dedicated camera with an orthographic sprite renderer. For most mobile games the latter is preferred as it avoids UI Canvas overhead entirely for a full-screen static background.

### 2.2 Responsiveness & Safe Area

Safe area handling is present via the `SafeArea.cs` / `NoSafeArea` pattern, which is structurally correct. However:

- `CameraResolutionCheck.cs` attempts to detect phone vs. tablet to switch `CanvasScaler.matchWidthOrHeight`, but `Screen.dpi` can return `0` in certain environments (some Android devices, the Unity Editor), causing a division-by-zero that silently produces `NaN` and makes the detection unreliable (see §3.4).
- Hard-coded device presets cover only iPhone X, iPhone Xs Max, and Pixel 3 XL. Dynamic Island devices, modern Android with punch-hole cameras, and foldables are not handled.

### 2.3 Localisation Readiness

The brief explicitly requires:
> *"Consider using a base text prefab so future localisation components can be attached with minimal rework."*

There is no evidence of this in the delivered project:
- All TextMeshPro components contain hardcoded string values
- No key-based text system or `LocalizationManager` is present
- No base text prefab exists from which a localisation component could be attached

This is a **direct specification miss**.

### 2.4 Settings Popup — Missing Architecture

The brief requires:
> *"Build a base popup structure from which Settings Popup and future popups can inherit."*
> *"Implement background darkening and blurring in a scalable way — other popups should reuse these effects easily."*

`GenericPopup.prefab` and `GenericPopup.controller` exist as disconnected assets, but:
- `SettingsPopup` is **not** a Prefab Variant of `GenericPopup`
- `SettingsPopupController.cs` is entirely standalone and inherits nothing
- There is **no blur implementation**, no overlay dimmer layer
- No future popup can reuse anything from the current setup

#### Applied Fix — Reusable `BlurBackground` component (partial)

The "background darkening and blurring in a scalable way" half of the brief is now covered by `_Shared/Scripts/BlurBackground.cs`, a self-contained component that any popup can drop in as a child:

- **Lifecycle.** Capture + fade-in happen on `OnEnable`; fade-out via `await blurBg.HideAsync()` before the popup deactivates itself. The popup controller owns the timing; the blur owns the capture/blur/fade pipeline.
- **Capture pipeline.** Renders every enabled camera (minus an optional ignore-tag for overlay UI cameras) into a `RenderTexture`, downsamples to a configurable working size, then ping-pongs N blit passes through a temp buffer to apply the blur. Quality selectable: `Fast` / `Normal` / `Best` (1/2/3 passes).
- **Recapture on re-open.** Each `OnEnable` discards the previous textures and grabs a fresh frame — the world behind a popup may have changed between openings.
- **Resource hygiene.** `OnDisable` and `OnDestroy` release the temporary `RenderTexture`s; the cancellation token is linked to `destroyCancellationToken` so any in-flight fade aborts cleanly when the GameObject dies.
- **Zero DOTween.** Implemented with Unity 6 `Awaitable` (same `FadeAsync` shape as `BottomBarView`, `NavigationController`, `TMP_CurvedText`). The original component ported from a prior project used `DG.Tweening.DOFade` — removed alongside the rest of the DOTween dependency.
- **Composable.** A popup just adds it as a child with a `RawImage` + a blit material; no inheritance, no glue code. Any future popup gets the effect by composition rather than by inheriting from a base.

#### Applied Fix — `PopupController` base class (minimal)

The inheritance side of the brief is now satisfied at the script level: the close-animation / deactivate boilerplate that lived inside `SettingsPopupController` was extracted into `_Shared/Scripts/PopupController.cs`. Any feature-specific popup just inherits and gets the close lifecycle for free.

```csharp
// _Shared/Scripts/PopupController.cs
public class PopupController : MonoBehaviour
{
    private static readonly int Close = Animator.StringToHash("Close");
    [SerializeField] protected Animator animator;

    public virtual void OnCloseButtonClicked()      => animator?.SetTrigger(Close);
    public virtual void OnClosedAnimationCompleted() => gameObject.SetActive(false);   // Animation Event hook
}

// HomeScreen/Scripts/SettingsPopupController.cs
public class SettingsPopupController : PopupController { }    // Settings-specific state lands here later.
```

- **Methods are `public virtual`** so a subclass can override the close flow (e.g. confirm dialog before closing, or async cleanup).
- **`animator` is `protected`** so subclasses can drive it directly when they need to fire other triggers (Open, Shake, etc.).
- **`SettingsPopupController.cs` kept at its original path with its original GUID** so the existing prefab's `m_Script` reference resolves without needing a manual rebind — the class is now just an empty subclass.
- **Magic-string trigger.** The `Animator.StringToHash("Close")` is moved into the base but kept literal for now — see §3.6, that nit applies to the new home unchanged. A `[SerializeField] string closeTrigger = "Close"` would be a follow-up if needed.

**Still TBD:** the Prefab Variant chain. `SettingsPopup.prefab` is still a parallel prefab to `GenericPopup.prefab` instead of a Variant of it. That's a content-side restructure that requires re-authoring the prefab hierarchy in the Editor and is out of scope for a code-only pass.

### 2.5 Bottom Bar — Missing Event Contract

The brief specifies:
> *"Create a BottomBarView.cs script to: Fire 'ContentActivated' when a button toggles on its content. Fire 'Closed' when no content is toggled."*

This script does not exist. The delivered `MenuFooterController.cs` handles selection state internally but exposes neither of the required events. Any system that needs to react to footer navigation (content panel manager, analytics, tutorial system) has no contract to bind to.

#### Applied Fix — Renamed to `BottomBarView`, events added, polish pass

The delivered `MenuFooterController.cs` was renamed (file + class + `.meta` preserved so the prefab reference survives) to `BottomBarView.cs` to match the brief's contract. Two `UnityEvent`s were exposed:

- `ContentActivated : UnityEvent<ButtonFooterController>` — fires when a footer button is toggled on, passing the activated button so listeners can route to the correct content panel.
- `Closed : UnityEvent` — fires when the currently-selected button is toggled off and no content is active.

Behavioural polish added alongside the rename:

- **Snap on first selection** — when no button was previously selected (e.g. on `Start()` with `startSelected` set, or after a deselect), the indicator is placed immediately on the new button instead of sliding from its previous position. Slide is preserved when switching between two already-active selections.
- **Accordion expansion** — selecting a button reallocates horizontal space via `LayoutElement.flexibleWidth` (driven by the existing `HorizontalLayoutGroup`), animated in lockstep with the indicator. The selected cell grows, the others compress proportionally — a subtle "fisarmonica" effect.
- **Missing `UnselectedTransition` animation state added** — the original `ButtonFooter` Animator had no state covering the return from `Selected` to default, so a deselected button stayed visually stuck in the selected pose. The transition state was authored and wired in the controller.
- **Extended clickable area** — the button's hit-box was expanded beyond the sprite border via an invisible raycast target, so the user no longer has to land precisely on the icon. Important for thumb-reach ergonomics on mobile and matches the in-house feel of the reference gif.

### 2.6 Animation Polish

The footer entrance/exit animations and the Level Completed screen animations are present and structurally reasonable. The sine-wave text animation on the Level Completed title is a creative addition — however its implementation has critical performance problems detailed in §3.1 and §5.

---

## 3. Code & Architecture Audit

### 3.1 `SineWaveTextAnimation.cs` — Critical performance issue ✅ Replaced by `TMP_CurvedText`

```csharp
// DELIVERED — problematic
const float amplitude = 5f;    // not configurable from the Inspector
const float frequency = 2f;
const float waveOffset = 0.2f;

void Update()
{
    textMesh.ForceMeshUpdate();       // expensive — called every single frame
    var mesh = textMesh.mesh;         // allocates a new Vector3[] every frame
    var vertices = mesh.vertices;     // full array copy every frame
    // ...
    mesh.vertices = vertices;         // re-uploads full mesh to GPU every frame
    textMesh.canvasRenderer.SetMesh(mesh);
}
```

**Issues:**
- `ForceMeshUpdate()` called every frame is excessively costly
- `textMesh.mesh.vertices` allocates a new `Vector3[]` on every frame — constant GC pressure (60 allocations/second at 60fps)
- All three animation parameters are `const` — no artist can adjust them from the Inspector or override them per Prefab
- The correct pattern uses `TMP_TextInfo` to write directly into TMP's owned vertex buffers and then calls `UpdateGeometry()` — zero allocation, no full mesh re-upload, handles invisible characters correctly

See §5 for the full before/after analysis.

**Applied Resolution:** `SineWaveTextAnimation.cs` was removed entirely and replaced with `TMP_CurvedText`, a more advanced and performant text animation component. All three issues (per-frame allocation, full mesh re-upload, non-configurable parameters) are resolved at source.

---

### 3.2 `MenuFooterController.cs` (now `BottomBarView.cs`) — World-space DOTween on a UI element

```csharp
// BUG — DOMoveX operates in world space
indicator.transform.DOMoveX(_currentSlot.transform.position.x, .25f)
    .SetEase(Ease.OutSine)
    .OnComplete(() =>
    {
        // Redundant — the tween already placed it here
        indicator.transform.position = new Vector3(
            _currentSlot.transform.position.x,
            indicator.transform.position.y,
            indicator.transform.position.z);
    });
```

`DOMoveX` operates in **world space**. On a `Screen Space - Overlay` Canvas, world coordinates only coincide with screen coordinates at the Canvas reference resolution. On any other resolution — tablet, landscape phone, editor Game view at a custom size — the indicator animates to the wrong position.

Additional issues:
- The `OnComplete` callback re-assigns `transform.position` after the tween has already completed — it is redundant and misleading
- Tween duration `0.25f` is a magic number with no named constant
- DOTween is used **only here** across the entire project — one tween justifies carrying the full `Plugins/Demigiant/` dependency (~10 MB, three libraries)

#### Applied Fix — Async/Await with `Awaitable` (Unity 6), DOTween removed

The tween was replaced with a native `async/await` animation using Unity 6's `Awaitable.NextFrameAsync()`. DOTween (`Plugins/Demigiant/`) was removed from the project entirely.

```csharp
// AFTER
private async void MoveIndicator()
{
    if (_buttonSelected == null) return;

    indicator.gameObject.SetActive(true);

    // Cancel any in-flight animation; destroyCancellationToken auto-cancels on GO destroy
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
        indicator.anchoredPosition = new Vector2(targetX, indicator.anchoredPosition.y);
    }
    catch (System.OperationCanceledException)
    {
        // New button selected mid-animation — the new call handles the rest
    }
}
```

| | Before | After |
|---|---|---|
| **Positioning** | `DOMoveX` — world space ❌ | `anchoredPosition` — canvas space ✅ |
| **`indicator` type** | `GameObject` (requires `.transform` cast) | `RectTransform` — direct |
| **Cancellation** | `DOKill()` | `CancellationTokenSource` — standard C# pattern |
| **GO destruction** | unhandled | `destroyCancellationToken` — automatic |
| **Duration** | magic number `0.25f` | `[SerializeField] float indicatorMoveDuration` |
| **Easing** | `Ease.OutSine` (DOTween) | `Mathf.SmoothStep` — built-in equivalent |
| **Namespace** | global | `Tripledot.HomeScreen` |
| **External dependency** | DOTween (~10 MB) | none |

The script was subsequently renamed to `BottomBarView` and given the `ContentActivated` / `Closed` events plus snap-on-first-selection + accordion polish — see §2.5.

---

### 3.3 `NavigationController.cs` — Synchronous scene loading

```csharp
public void LoadScene(string sceneName)
{
    SceneManager.LoadScene(sceneName);  // blocks the main thread
}
```

- `SceneManager.LoadScene` is synchronous and **blocks the main thread** for the duration of the load — causes a visible freeze on mobile
- No `LoadSceneAsync` variant, no loading screen, no transition
- Accepts a raw string with no validation — an invalid scene name fails silently at runtime with no feedback

#### Applied Fix — Async load with fade overlay

Replaced with `LoadSceneAsync` + `allowSceneActivation = false` so the heavy single-frame activation work is hidden behind a fade. A full-screen `Image` overlay blocks input for the entire transition; the incoming scene authors its overlay at alpha 1 and fades out on `Start()`, giving a seamless cut between screens.

| | Before | After |
|---|---|---|
| **Load method** | `SceneManager.LoadScene` — blocks main thread | `LoadSceneAsync` + `allowSceneActivation = false` |
| **Transition** | hard cut | fade-out → activate → fade-in |
| **Input during load** | unblocked | `raycastTarget = true` on overlay for entire duration |
| **Time source** | — | `Time.unscaledDeltaTime` — works correctly if timescale is 0 |
| **Cancellation** | — | `destroyCancellationToken` — safe on GO destroy |

**Applied Fix.** `SceneManager.LoadScene` is not wrong in itself, but the HomeScreen → LevelCompletedScreen transition wasn't smooth: replaced with `LoadSceneAsync` + `allowSceneActivation=false` gated behind a per-scene `FadeOverlay` (`Image.color.a`, raycast-blocking), so the freeze is hidden behind a fade-to-black.

---

### 3.4 `CameraResolutionCheck.cs` — Division by zero, magic enum values

```csharp
// Screen.dpi can return 0 → NaN/Infinity → silent logic failure
float screenWidth  = Screen.width  / Screen.dpi;
float screenHeight = Screen.height / Screen.dpi;

// Magic numbers — these integer values are never used as integers
enum DeviceType
{
    Phone  = 2,
    Tablet = 3
}
```

- `Screen.dpi` returns `0` on certain Android devices and frequently in the Editor. No guard clause means `isTablet` evaluates on `NaN` and silently defaults to phone on every device.
- The `DeviceType` enum assigns arbitrary integer values (`Phone = 2`, `Tablet = 3`) that are never used as integers anywhere — meaningless and confusing.

**Fix:**
```csharp
if (Screen.dpi <= 0f) return; // or use a safe fallback value
```

---

### 3.5 `ButtonFooterController.cs` — Click listener never removed

```csharp
void Start()
{
    footerBtn.onClick.AddListener(() => OnButtonClickedEvent?.Invoke(this));
    // No OnDestroy() → RemoveListener never called
}
```

The anonymous lambda added in `Start()` cannot be cleanly removed later. If the GameObject is destroyed and reinstantiated (pooling, scene reload), a duplicate listener accumulates on each instantiation — potential memory leak and double-invocation bug.

---

### 3.6 `SettingsPopupController.cs` — Magic string triggers, no null guard

```csharp
animator.SetTrigger("Close");  // typo-prone, refactor-unsafe
```

- No `Animator.StringToHash` cache, no static constant
- Renaming the Animator parameter breaks this silently at runtime
- No null check on `animator`

**Partially addressed.** The script's logic was lifted into `PopupController` (see §2.4). On the way, the `Animator.StringToHash("Close")` cache was kept (no more per-call string lookup) and a null guard was added around `animator.SetTrigger(...)`. What is *still* magic-string is the literal `"Close"` — if an artist renames the Animator trigger, the code breaks silently. A `[SerializeField] string closeTrigger = "Close"` would close that gap; left as a follow-up to keep the refactor minimal.

---

### 3.7 `SafeArea.cs` — Unnecessary per-frame work, community script

```csharp
void Update()
{
    Refresh(); // compares 4 values every single frame
}
```

- `Refresh()` runs in `Update()` and compares four values every frame, despite the safe area virtually never changing during normal gameplay.
- Device-specific `NSA_*` arrays are instance fields instead of `static readonly` — one copy per component instance in memory.
- This is a well-known community script widely circulated on the Unity forums. Submitting it verbatim, without attribution or any adaptation, is a concern at mid-level seniority and would be flagged in any code review.

#### Applied Fix — Portrait-only, single `Awake()` call

The app targets portrait-only. The safe area is a constant for the lifetime of the session — it never needs re-reading. The entire 247-line community script was replaced with a 14-line implementation: read `Screen.safeArea` once in `Awake()`, convert to normalised anchors, done. No `Update()`, no orientation tracking, no hardcoded device simulation tables (Unity 6's built-in Device Simulator handles Editor simulation natively).

```csharp
using UnityEngine;

namespace Tripledot.Shared
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        void Awake()
        {
            Rect safe = Screen.safeArea;
            var rect = GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(safe.x / Screen.width,
                                         safe.y / Screen.height);
            rect.anchorMax = new Vector2((safe.x + safe.width)  / Screen.width,
                                         (safe.y + safe.height) / Screen.height);
        }
    }
}
```

| | Before | After |
|---|---|---|
| **Lines** | 247 | 14 |
| **`Update()`** | every frame, 4 comparisons | removed |
| **Device simulation** | 4 hardcoded `NSA_*` instance arrays | Unity 6 Device Simulator (built-in) |
| **Orientation handling** | polled | not needed — portrait-only |
| **Namespace** | global | `Tripledot.Shared` |
| **Origin** | verbatim community script | original |

---

### 3.8 General — Architecture

- **No C# namespaces** on any of the 7 custom scripts
- **Mixed class responsibilities**: `MenuFooterController` manages both selection state logic and indicator animation; `CameraResolutionCheck` mixes device detection with `CanvasScaler` mutation — separation of concerns is weak throughout
- **`SafeArea.cs` class indented inside a phantom namespace block** (and same for `CameraResolutionCheck`, `ButtonFooterController`, `MenuFooterController`) — consistent sign of copy-paste without cleanup

---

## 4. Performance

| Issue | Impact | Location |
|---|---|---|
| Repository ships ~1 GB of generated files | Blocks cloning, wastes CI/CD storage | Submission / `.gitignore` |
| Demo and template assets in build-eligible paths | Inflates build size, pollutes asset DB | `Assets/Settings/` (URP2DSceneTemplate), `TextMesh Pro/` (TMP demos) — DOTween fully removed ✅ |
| Unused packages in `manifest.json` (`visualscripting`, `timeline`, `multiplayer.center`) | Medium — extra compilation targets, slower domain reload on every script change | `Packages/manifest.json` ✅ removed |
| Single Canvas for all UI | Full re-batch on any UI state change | `HomeScreen.unity` hierarchy |
| `ForceMeshUpdate()` + `mesh.vertices` every frame | ✅ **Fixed** — `SineWaveTextAnimation.cs` removed, replaced with `TMP_CurvedText` (see §3.1, §5) | `LevelCompletedScreen` |
| `Refresh()` polling in `Update()` | ✅ **Fixed** — portrait-only app, safe area read once in `Awake()`, `Update()` removed, 247 lines → 14 | `SafeArea.cs` (see §3.7) |
| Synchronous `SceneManager.LoadScene` | ✅ **Fixed** — `LoadSceneAsync` + fade overlay + input block, `Time.unscaledDeltaTime` | `NavigationController.cs` (see §3.3) |
| Background image distorts on non-reference devices | ✅ **Fixed** — `AspectRatioFitter` (Envelope Parent) + Square POT + ASTC compression | `HomeScreen.unity` — `Background` (see §2.1, §6) |
| `DOMoveX` world-space on Canvas element | ✅ **Fixed** — replaced with `anchoredPosition` + `Awaitable`, DOTween (~10 MB) removed | `MenuFooterController.cs` (see §3.2) |

---

## 5. Deep-Dive Analysis — `SineWaveTextAnimation.cs` → replaced by `TMP_CurvedText`

Selected because it combines a **critical per-frame performance issue** with a direct **artist-facing configurability problem**, and the analysis demonstrates clear knowledge of TMP's intended geometry API.

> **Final resolution:** the script was removed entirely and replaced with `TMP_CurvedText`, a more advanced component that solves all three problems at source. The before/after refactor below documents what a correct in-place fix would have looked like.

### Before

```csharp
public class SineWaveTextAnimation : MonoBehaviour
{
    const float amplitude  = 5f;
    const float frequency  = 2f;
    const float waveOffset = 0.2f;

    private TMP_Text   textMesh;
    private Vector3[]  originalVertices;

    void Start()
    {
        textMesh = GetComponent<TMP_Text>();
        textMesh.ForceMeshUpdate();
        originalVertices = textMesh.mesh.vertices;
    }

    void Update()
    {
        textMesh.ForceMeshUpdate();
        var mesh     = textMesh.mesh;
        var vertices = mesh.vertices;          // new Vector3[] every frame

        for (int i = 0; i < vertices.Length; i++)
        {
            int   charIndex = i / 4;           // assumes all chars are visible
            float wave      = Mathf.Sin(Time.time * frequency + charIndex * waveOffset);
            vertices[i].y   = originalVertices[i].y + wave * amplitude;
        }

        mesh.vertices = vertices;
        textMesh.canvasRenderer.SetMesh(mesh); // full mesh state rebuild
    }
}
```

### After

```csharp
public class SineWaveTextAnimation : MonoBehaviour
{
    [SerializeField] private float amplitude  = 5f;
    [SerializeField] private float frequency  = 2f;
    [SerializeField] private float waveOffset = 0.2f;

    private TMP_Text _textMesh;

    void Awake()
    {
        _textMesh = GetComponent<TMP_Text>();
    }

    void Update()
    {
        _textMesh.ForceMeshUpdate();
        TMP_TextInfo textInfo = _textMesh.textInfo;

        for (int charIdx = 0; charIdx < textInfo.characterCount; charIdx++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[charIdx];
            if (!charInfo.isVisible) continue;           // skip spaces / newlines

            int      matIdx  = charInfo.materialReferenceIndex;
            int      vertIdx = charInfo.vertexIndex;
            Vector3[] verts  = textInfo.meshInfo[matIdx].vertices; // TMP-owned buffer — no alloc

            float wave = Mathf.Sin(Time.time * frequency + charIdx * waveOffset) * amplitude;
            for (int v = 0; v < 4; v++)
                verts[vertIdx + v].y += wave;
        }

        // Geometry-only update — skips TMP's internal state rebuilds
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
            _textMesh.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
    }
}
```

### What changed and why

| | Before | After |
|---|---|---|
| **Parameters** | `const` — compile-time only | `[SerializeField]` — Inspector + Prefab overridable |
| **Vertex access** | `mesh.vertices` → new `Vector3[]` every frame | `textInfo.meshInfo[].vertices` → TMP-owned buffer, **zero allocation** |
| **Mesh upload** | `SetMesh()` — full mesh state rebuild | `UpdateGeometry()` — geometry-only, **TMP's intended API** |
| **Invisible chars** | `i / 4` assumes all chars rendered | `charInfo.isVisible` correctly skips spaces and newlines |
| **Component cache** | `GetComponent` in `Start` | `GetComponent` in `Awake` — available earlier in the lifecycle |

---

## 6. Visual & Artistic Polish

### Immediate fixes

- **Background distortion** — switching to `AspectRatioFitter` (Envelope Parent) or moving the background to a dedicated orthographic camera immediately fixes the aspect ratio deformation across all devices.

- **`Home_Background` texture import settings** — the source texture was non-POT, which prevents hardware compression and locks the Max Size field. Changing the format to **Square POT** unlocks proper mobile compression and gives full control over the resolution cap.
  - Set `Compression` to **ASTC** (iOS / modern Android) or **Automatic** (Unity selects ASTC / ETC2 per platform) — both are hardware-decoded by the GPU and cost a fraction of the VRAM compared to the RGBA32 fallback Unity uses for NPOT textures
  - Disable `Generate Mip Maps` — a fullscreen 2D UI image is never sampled at reduced resolution, mips only waste memory
  - Combined with `AspectRatioFitter`, this fixes both the display side (no distortion) and the memory side (correct compression) of the same background issue

### Shader Graph — Issues and Performance Considerations

A review of all three custom shaders reveals a pattern of **property ranges that do not match actual values**, hardcoded constants, and debug nodes left in production graphs.

| Shader | Key Issues |
|---|---|
| `ShinyStar` | `Speed` and `CycleTime` default `2.0` on range `0–1` — slider clamps silently; `Rotation` default `154°` on range `0–1` — completely non-functional from Inspector; no `Intensity` control exposed |
| `GlowRays` | `Rotation Speed A/B` defaults `20`/`30` on range `0–1`; `Number of Rays` exposed as float `0–1` instead of integer; `Color` defaults to `(0,0,0,0)`; scale factor `37.67` hardcoded with no label; **Preview nodes left in the graph** |
| `BackgroundShader` | Tiling offset Y hardcoded at `-0.5`; gradient direction and scale not exposed; pattern tiling not artist-controllable; Alpha hardcoded to `1.0`; `PatternOpacity` default `0.1` makes the pattern nearly invisible |

In all three cases the properties were added without verifying the Inspector UX — any artist opening a material sees sliders that are either broken or clamped to a fraction of the intended range.

#### ⚠️ Broader concern — Shader Graph has a cost

A recurring beginner mistake is treating Shader Graph as a zero-cost tool. Every node executes on the GPU per fragment, per frame. `GlowRays` runs Polar Coordinates, multiple Rotate nodes, Sine/Cosine and Saturate passes on every pixel of the overlay — on a mobile GPU this is a measurable ALU cost.

**More performant alternatives for these specific effects:**

- **Background gradient** — a static raster texture is free at runtime; a Shader Graph gradient recalculates every fragment every frame for no visual gain over a 64×64 PNG
- **Shiny star shimmer / glow rays** — better handled as a **sprite animation with additive blending** on a pre-baked texture atlas; the GPU cost drops to a single texture sample + blend op
- **Glow rays specifically** — a **Particle System** with a single additive ray sprite, rotation driven by the particle's angular velocity, is trivially cheap and gives artists direct control over spread, density, and lifecycle without touching shader code

The principle: reach for Shader Graph when the effect genuinely requires procedural variation at runtime (e.g. a distortion field driven by gameplay data). For decorative loops with no dynamic input, a raster + animation approach is always cheaper and easier to iterate on.

### Animation & VFX

- **`TMP_CurvedText`** ✅ — replaced `SineWaveTextAnimation` entirely. The new component resolves all three original issues (per-frame allocation, full mesh re-upload, non-configurable parameters) and provides a richer artist-facing interface out of the box.
- **Particle systems** — `ParticleMat_Sparkles` and `ParticleMat_Stars` should have **GPU Instancing** enabled on their materials. Sorting Layer assignments should be verified to avoid overdraw conflicts with the background shader at render time.

---

## Priority Matrix

| Priority | Issue | Category |
|---|---|---|
| **P0** | Repository ships ~1 GB of generated files; Windows extraction fails with path-too-long error | Submission / Delivery |
| ~~**P0**~~ ✅ | ~~`BottomBarView.cs` entirely absent — contracted API not delivered~~ **Fixed** — `MenuFooterController.cs` renamed to `BottomBarView.cs`, `ContentActivated` / `Closed` events added, plus snap/accordion polish, missing `UnselectedTransition` Animator state, and extended click area (see §2.5) | Specification |
| **P0** ⚠ partial | Settings Popup: ~~no blur/overlay system~~ **blur/overlay** covered by reusable `BlurBackground` (`_Shared/`, Awaitable-based, DOTween removed). ~~no extensible base popup architecture~~ **base class** `PopupController` extracted (`_Shared/`); `SettingsPopupController` now inherits from it. **Prefab Variant chain still missing** (`SettingsPopup` should be a Variant of `GenericPopup`) — content-side restructure. See §2.4. | Specification |
| ~~**P1**~~ ✅ | ~~Background image distorts on non-reference-resolution devices~~ **Fixed** — `AspectRatioFitter` (Envelope Parent) + Square POT + ASTC compression (see §2.1, §6) | UI / Visual |
| **P1** | Single Canvas for all UI — no batching isolation | Architecture / Performance |
| ~~**P1**~~ ✅ | ~~`SineWaveTextAnimation`: per-frame heap allocations + non-configurable parameters~~ **Fixed** — script removed, replaced with `TMP_CurvedText` (see §3.1, §5) | Code / Performance |
| ~~**P1**~~ ✅ | ~~`MenuFooterController`: `DOMoveX` world-space bug on Canvas element~~ **Fixed** — async/await + `anchoredPosition`, DOTween removed (see §3.2) | Code / Bug |
| ~~**P1**~~ ✅ | ~~`NavigationController`: synchronous scene loading causes main-thread freeze~~ **Fixed** — `LoadSceneAsync` + fade overlay (see §3.3) | Code |
| **P2** | `CameraResolutionCheck`: division by zero when `Screen.dpi == 0` | Code / Bug |
| **P2** | No C# namespace on any custom script | Architecture |
| **P2** | No localisation infrastructure — direct specification miss | Specification |
| **P2** | Template/demo assets in build path (`URP2DSceneTemplate`, TMP demos) + rename `Assets/Settings/` → `Assets/RenderPipeline/` — `Lit2DSceneTemplate` and DOTween examples already removed ✅ | Project Standards |
| **P3** | `ButtonFooterController`: `onClick` listener never removed — potential memory leak | Code |
| **P3** ⚠ partial | Magic string Animator triggers in `SettingsPopupController` — the literal `"Close"` is now centralised in `PopupController` (see §2.4 / §3.6) with `StringToHash` cache + null guard, but is still a magic string. A serialized trigger name would close it fully. | Code |
| **P3** | Inconsistent prefab naming (`currencyBox`, `SettingsButton1`) | Project Standards |
| ~~**P3**~~ ✅ | ~~`SafeArea.cs`: polling `Refresh()` every frame + verbatim community script with no attribution~~ **Fixed** — rewritten from scratch, 247 lines → 14 (see §3.7) | Performance / Standards |
