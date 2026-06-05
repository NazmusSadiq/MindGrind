using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorSceneTrigger : MonoBehaviour
{
    [SerializeField] private DoorSceneLoader doorSceneLoader;

    private void Awake()
    {
        if (doorSceneLoader == null)
            doorSceneLoader = GetComponentInParent<DoorSceneLoader>();
    }

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();

        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (doorSceneLoader == null)
        {
            Debug.LogWarning("DoorSceneTrigger could not find a DoorSceneLoader in its parent.", this);
            return;
        }

        doorSceneLoader.TryLoadScene(other);
    }
}
