using Tripledot.Shared;

// Settings-specific behaviour goes here in the future (volume sliders, language picker,
// privacy toggles…). For now everything is provided by the PopupController base:
//   - OnCloseButtonClicked  → fires the Animator "Close" trigger
//   - OnClosedAnimationCompleted → deactivates the GameObject (Animation Event hook)
//
// Kept in the global namespace so the existing prefab's m_Script reference (by GUID)
// resolves without needing a manual rebind.
public class SettingsPopupController : PopupController
{
}
