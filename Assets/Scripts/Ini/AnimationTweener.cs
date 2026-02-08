using UnityEngine;
using DG.Tweening;

namespace UnlimitedScrollUI
{
    public class AnimationTweener : MonoBehaviour
    {
        [Header("Tween Settings")]
        [SerializeField] private float startYPos;
        [SerializeField] private float endYPos;
        [SerializeField] private float tweenDuration = 1f;
        [SerializeField] private Ease easeType = Ease.InOutSine;

        private void Start()
        {
            Vector3 startPos = new Vector3(transform.localPosition.x, startYPos, transform.localPosition.z);
            Vector3 endPos = new Vector3(transform.localPosition.x, endYPos, transform.localPosition.z);

            transform.DOLocalMoveY(endYPos, tweenDuration).SetEase(easeType)
            .SetLoops(-1, LoopType.Restart).From(startYPos);                    // This goes from start to end, then teleports to start,
                                                                                // continously because of ".SetLoops(-1, LoopType.Restart).From(startYPos")
                                                                                // "LoopType.Restart" makes it teleport to startYPos


            // // Continuous up-down/ to fro tween 
            // transform.DOLocalMoveY(endYPos, tweenDuration).SetEase(easeType)
            // .SetLoops(-1, LoopType.Yoyo).From(startYPos).SetDelay(0.3f);     // A brief delay before looping again  
                                                                                // This loops to and fro because of "LoopType.Yoyo"
        }
    }
}
