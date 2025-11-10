using UnityEngine;
using DG.Tweening;

namespace UnlimitedScrollUI
{
    public class MascotAnimationTweener : MonoBehaviour
    {
        [Header("Tween Positions")]
        [SerializeField] private float posA;   // Start position on y axis
        [SerializeField] private float posB;   // Mid position on y axis
        [SerializeField] private float posC;   // end position on y axis

        [Header("Tween Settings")]
        [SerializeField] private float slowSpeed = 1.5f;   // Speed between A & B
        [SerializeField] private float fastSpeed = 0.6f;   // Speed between B & C
        [SerializeField] private float waitTime = 3f;      // Wait time at C
        [SerializeField] private Ease easeType = Ease.InOutSine;    // This is reponsible for the tweening end effect, could be bounce, elastic, etc.

        private void Start()
        {
            // Store the current local position
            Vector3 currentPos = transform.localPosition;

            Vector3 a = new Vector3(currentPos.x, posA, currentPos.z);
            Vector3 b = new Vector3(currentPos.x, posB, currentPos.z);
            Vector3 c = new Vector3(currentPos.x, posC, currentPos.z);

            // Build the smooth looping sequence
            Sequence seq = DOTween.Sequence();

            seq.Append(transform.DOLocalMoveY(b.y, slowSpeed).SetEase(easeType)) // A to B goes gently
               .Append(transform.DOLocalMoveY(c.y, fastSpeed).SetEase(Ease.OutSine)) // B to C a bit faster
               .AppendInterval(waitTime) // wait a bit at C
               .Append(transform.DOLocalMoveY(b.y, fastSpeed).SetEase(Ease.InSine)) // C to B return
               .Append(transform.DOLocalMoveY(a.y, slowSpeed).SetEase(easeType)) // B to A gently back to start
               .SetLoops(-1, LoopType.Yoyo); // continuously loop
        }
    }
}
