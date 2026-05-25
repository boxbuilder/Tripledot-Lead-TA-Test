# Tripledot Lead TA Test: Project Review

> Lead Technical Artist Assessment · Nicola Sirago

---

## Approach

Rather than rebuilding the project from scratch, I focused on **accurate diagnosis and targeted fixes** that respect the established structure; the same pragmatic approach I apply daily in production, where clean, incremental improvements are more valuable than wholesale rewrites.

Many of the issues I found (shader problems, text rendering, incorrect UI configurations) are documented in detail in [`LEAD_TECH_REVIEW.md`](./LEAD_TECH_REVIEW.md). Where possible, fixes were applied directly to the project rather than left as comments.

---

## Overall Assessment

Read along the three axes the brief calls out — high-level critique, UI & design system, code & architecture. Specifics live in [`LEAD_TECH_REVIEW.md`](./LEAD_TECH_REVIEW.md) and in the per-screen sections that follow.

**Project hygiene & structure.** Below the bar expected for a mid-level submission. The repository ships material that should never be committed, packages were never trimmed from Unity defaults, naming conventions drift within the same feature, and the codebase carries no namespace declarations. The folder layout signals an intent toward modularity that never lands on a convention. The project runs, but it is not something another engineer can drop into without a cleanup pass first.

**UI & Design System.** The screens are recognisable from the PSD/GIF references but consistently stop at "it lays out" rather than "it matches." Several items from the brief are absent rather than imperfect — base popup architecture, blur/overlay, localisation infrastructure, Bottom Bar event contract. Responsiveness depends on a verbatim community script, animation polish is uneven, and some state machines are incomplete or in tension with the documented behaviour. The pattern is of a candidate solving "make it appear on screen" and then moving on.

**Code & Architecture.** The C# is readable and functionally correct on the happy path, but reads more like adapted forum patterns than production practice. The recurring tells — magic-string identifiers, `const` parameters that should be Inspector-driven, listeners added without removal, unguarded edge cases, an external dependency carried for a single use — are individually minor and cumulatively diagnostic. Class responsibilities are blurred across view, animation, and selection logic in several places.

Cumulatively, this places the submission **at the junior–mid boundary**: a candidate who can deliver a working feature on the target device, but who has not yet absorbed the rigour, code-review discipline, and performance-aware instincts that a mid-level production role assumes.

---

## Bottom Bar

The original implementation is glitchy: it lacks an unfocused animation state, and `ButtonFooterController` has several correctness issues, including a click listener that is never removed.

**Before**

<img width="427" height="157" alt="BottomBarBefore" src="https://github.com/user-attachments/assets/516f7245-a3ed-4ae0-9ec7-7e88817c52b0" />

The rewritten version covers all required animation states, uses no external tweener (DOTween has been removed from the project entirely), has zero per-frame allocations, and is implemented more robustly.

**After**

<img width="427" height="157" alt="BottomBarAfter" src="https://github.com/user-attachments/assets/09770d11-335b-4159-9b79-41a128976a42" />

---

## Screen Ratio & Safe Area

The original background distorts on any device whose aspect ratio differs from the design reference. The `SafeArea` script is a verbatim copy from the Unity Forums, adopted despite known issues documented in the very same thread.

**Before**

<img width="310" height="408" alt="ScreenDevicesBefore" src="https://github.com/user-attachments/assets/07d4fbc9-f0a7-4aa8-b5d5-108127336a24" />

The new `SafeArea` is a 14-line replacement that reads `Screen.safeArea` once in `Awake()`: no per-frame polling, no hardcoded device tables. The background uses an `AspectRatioFitter` (Envelope Parent) and a Square POT texture with ASTC compression, so it fills the screen correctly on every aspect ratio without distortion.

**After**

<img width="310" height="408" alt="ScreenDevicesAfter" src="https://github.com/user-attachments/assets/72202be6-f67b-43b6-abe0-e875747d5242" />

---

## Settings Popup

The original is missing several elements from the brief: no scalable popup architecture, no blur, no localisation readiness. It also has graphical glitches: the background clips under the notch and the image spills past the screen edges.

**Before**

<img width="370" height="751" alt="PopupOpenBefore" src="https://github.com/user-attachments/assets/4f69d6cd-8317-48de-a0cb-2a909ad1bff7" />

The new popup is built on an extensible base and includes a mobile-friendly blur effect, the same technique currently used in Scrabble GO and Monopoly GO. Localisation infrastructure is not covered in this test.

**After**

<img width="370" height="751" alt="PopupOpenAfter" src="https://github.com/user-attachments/assets/4e1ac70a-77a3-49a4-b44d-410af182bbd8" />

---

## Level Completed Screen

The delivered screen falls short of the brief's target of "impressive animation and creative flair" on several counts:

- The scene transition uses synchronous `LoadScene` with no preloading, causing a visible freeze that cuts off the opening animation
- The Shader Graph effects (`GlowRays`, `ShinyStar`) add measurable GPU overhead without meaningful visual return; the same results could be achieved with a small rasterised texture or a lightweight particle system
- All elements appear simultaneously with no reveal sequence

**Before**

<img width="370" height="751" alt="LevelCompleteBefore" src="https://github.com/user-attachments/assets/bdb5a708-9096-4a97-b78e-0644b3ae8ca6" />

In the revised version, the scene is preloaded before the transition begins, eliminating the freeze. `SineWaveTextAnimation` (a source of constant per-frame heap allocations) has been replaced with `TMP_CurvedText`. Elements are introduced sequentially, giving the screen a more polished feel.

**After**

<img width="370" height="751" alt="LevelCompleteAfter" src="https://github.com/user-attachments/assets/11a8d34f-21b8-48d5-bcb8-aa0deb2b6f1d" />

---

## "Level Completed!" Title Text

Faithfully reproducing a Photoshop text style in TMP is a labour-intensive process that rarely achieves 100% accuracy; depending on the project's art direction, fully rasterised text is often the more pragmatic choice.

The original diverges significantly from the PSD reference: no bevel, no drop shadow, a generic glow. The glint FX and sparkles called out in the layout are missing entirely.

**Photoshop reference**

<img width="811" height="361" alt="Screenshot 2026-05-25 173453" src="https://github.com/user-attachments/assets/97284fd8-f3c2-4bd4-80fd-3e1b03d178a9" />

**Before:** different style, no bevel, no shadow, approximate glow, no glint or sparkles

<img width="811" alt="Screenshot 2026-05-25 173028" src="https://github.com/user-attachments/assets/5d8ab808-6a26-4c18-97ca-40df2ce3c7a9" />

**After:** style matched to PSD; glint and sparkle VFX not yet implemented

<img width="811" alt="Screenshot 2026-05-25 180058" src="https://github.com/user-attachments/assets/ae3ead76-5246-4fc9-81c2-767e50e80ed1" />
