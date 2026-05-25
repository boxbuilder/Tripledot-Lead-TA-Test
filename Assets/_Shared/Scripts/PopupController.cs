using UnityEngine;

namespace Tripledot.Shared
{
    /// <summary>
    /// Base behaviour for popups. Owns the contract for closing:
    ///   1) <see cref="OnCloseButtonClicked"/> fires the "Close" Animator trigger
    ///      (wire this from the close button's UnityEvent).
    ///   2) <see cref="OnClosedAnimationCompleted"/> is invoked at the end of the close clip
    ///      via an Animation Event, and deactivates the GameObject.
    ///
    /// Feature-specific popups (Settings, Shop, Reward...) inherit from this and add their
    /// own state/behaviour on top — they get the close lifecycle for free.
    /// </summary>
    public class PopupController : MonoBehaviour
    {
        private static readonly int Close = Animator.StringToHash("Close");

        [SerializeField] protected Animator animator;

        public virtual void OnCloseButtonClicked()
        {
            if (animator != null)
                animator.SetTrigger(Close);
        }

        // Called from an Animation Event at the end of the "Close" clip.
        public virtual void OnClosedAnimationCompleted()
        {
            gameObject.SetActive(false);
        }
    }
}
