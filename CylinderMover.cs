using UnityEngine;
using System.Collections;

public class CylinderMover : MonoBehaviour
{
    private bool isUp = false;
    private Vector3 originalPosition;

    public float moveAmount = 3f;
    public float speed = 2f;

    void Start()
    {
        originalPosition = transform.position;
    }

    public void TriggerMovement()
    {
        StopAllCoroutines();
        Vector3 target = isUp ? originalPosition : originalPosition + Vector3.up * moveAmount;
        StartCoroutine(MoveTo(target));
        isUp = !isUp;
    }

    IEnumerator MoveTo(Vector3 target)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (!rb) yield break;

        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            Vector3 nextPos = Vector3.MoveTowards(transform.position, target, speed * Time.fixedDeltaTime);
            rb.MovePosition(nextPos);
            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(target);
    }
}