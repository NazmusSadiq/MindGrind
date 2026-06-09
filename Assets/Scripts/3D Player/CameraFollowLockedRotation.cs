using UnityEngine;

public class CameraFollowLockedRotation : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -6f);
    [SerializeField] private Vector3 lockedEulerAngles = new Vector3(60f, 0f, 0f);

    private void LateUpdate()
    {
        if (!enabled) return;

        if (target == null)
        {
            return;
        }

        transform.position = target.position + offset;
        transform.rotation = Quaternion.Euler(lockedEulerAngles);
    }
}