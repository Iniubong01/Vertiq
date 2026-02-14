using UnityEngine;
using DG.Tweening;

public class BackgroundRotation : MonoBehaviour
{
    [SerializeField] float rotationDuration = 2f; // seconds per full spin

    void Start()
    {
        transform.DORotate( new Vector3(0f, 0f, 360f), rotationDuration,
        RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1);
    }
}