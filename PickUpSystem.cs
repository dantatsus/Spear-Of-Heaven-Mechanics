using UnityEngine;

public class PickUpSystem : MonoBehaviour
{
    [Header("References")]
    public Transform playerCamera;

    [Header("Pickup Settings")]
    public float pickupRange = 10.0f;
    public float holdDistance = 1.0f;
    public Vector3 holdPositionOffset = new Vector3(0.8f, -0.3f, 1.0f);
    public float pickupSmoothing = 25f;
    public float rotationSmoothing = 15f;

    [Header("Physics Settings")]
    public float dropUpwardForce = 0.1f;

    private GameObject pickedObject;
    private Rigidbody pickedRigidbody;
    private Vector3 originalScale;
    private Vector3 lastValidPosition;
    private Vector3 desiredPosition;
    private Transform originalParent;
    private SpearSystem spearSystem;

    private void Update()
    {
        HandleInput();
        if (pickedObject != null)
        {
            UpdateObjectPosition();
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (pickedObject == null)
                TryPickUp();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            if (pickedObject != null)
                DropObject();
        }
    }

    private void TryPickUp()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
        {
            if (hit.collider.CompareTag("Pickup"))
            {
                pickedObject = hit.collider.gameObject;
                pickedRigidbody = pickedObject.GetComponent<Rigidbody>();
                spearSystem = pickedObject.GetComponent<SpearSystem>();
                originalParent = pickedObject.transform.parent;
                originalScale = pickedObject.transform.localScale;

                pickedRigidbody.isKinematic = true;
                pickedRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                pickedRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

                pickedObject.transform.SetParent(null);
                pickedObject.layer = LayerMask.NameToLayer("Interactable");

                lastValidPosition = GetDesiredPosition();

                if (spearSystem == null)
                {
                    pickedObject.transform.rotation = playerCamera.rotation;
                }
            }
        }
    }

    private void UpdateObjectPosition()
    {
        desiredPosition = GetDesiredPosition();

        if (!IsPositionInsideCollider(desiredPosition))
        {
            lastValidPosition = desiredPosition;
        }
        else
        {
            desiredPosition = lastValidPosition;
        }

        pickedObject.transform.position = Vector3.Lerp(
            pickedObject.transform.position,
            desiredPosition,
            Time.deltaTime * pickupSmoothing
        );

        if (spearSystem == null)
        {
            pickedObject.transform.rotation = Quaternion.Slerp(
                pickedObject.transform.rotation,
                playerCamera.rotation,
                Time.deltaTime * rotationSmoothing
            );
        }

        pickedObject.transform.localScale = originalScale;
    }

    private Vector3 GetDesiredPosition()
    {
        Vector3 holdPos = playerCamera.position +
                         playerCamera.forward * holdDistance +
                         playerCamera.right * holdPositionOffset.x +
                         playerCamera.up * holdPositionOffset.y;
        return holdPos;
    }

    private bool IsPositionInsideCollider(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, 0.1f);
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject != pickedObject && !collider.isTrigger)
                return true;
        }
        return false;
    }

    private void DropObject()
    {
        if (pickedRigidbody != null)
        {
            PrepareObjectForDrop();
            pickedRigidbody.AddForce(Vector3.up * dropUpwardForce, ForceMode.Impulse);
            ResetPickupState();
        }
    }

    private void PrepareObjectForDrop()
    {
        pickedRigidbody.isKinematic = false;
        pickedRigidbody.useGravity = true;
        pickedRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        pickedRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

        pickedObject.layer = LayerMask.NameToLayer("Default");
        pickedObject.transform.SetParent(originalParent);
    }

    private void ResetPickupState()
    {
        spearSystem = null;
        pickedObject = null;
        pickedRigidbody = null;
        lastValidPosition = Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerCamera.position, pickupRange);
        }
    }
}