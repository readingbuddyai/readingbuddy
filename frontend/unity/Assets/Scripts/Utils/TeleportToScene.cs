using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TriggerDebugger : MonoBehaviour
{
    private void Reset()
    {
        var c = GetComponent<Collider>();
        c.isTrigger = true;
        var rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[TriggerDebugger] ENTER by {other.name}");
    }
    private void OnTriggerStay(Collider other)
    {
        Debug.Log($"[TriggerDebugger] STAY  by {other.name}");
    }
    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"[TriggerDebugger] EXIT  by {other.name}");
    }
}
