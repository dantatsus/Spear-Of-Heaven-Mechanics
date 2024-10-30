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
    private SpearSystem lastThrownSpear;

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

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (lastThrownSpear != null && !lastThrownSpear.IsBeingHeld())
            {
                lastThrownSpear.StartRecall();
            }
        }
    }

    public void SetLastThrownSpear(SpearSystem spear)
    {
        lastThrownSpear = spear;
    }

    public void TryPickUp()
    {
        // Eğer zaten bir obje tutuyorsak, yeni bir obje alamayız
        if (pickedObject != null) return;

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

                if (spearSystem != null)
                {
                    spearSystem.SetBeingHeld(true);
                }

                pickedRigidbody.isKinematic = true;
                pickedRigidbody.useGravity = false;
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

    public void DropObject()
    {
        if (pickedRigidbody != null)
        {
            SpearSystem spear = pickedRigidbody.GetComponent<SpearSystem>();

            // Eğer normal bırakma ise (fırlatma değilse) lastThrownSpear'ı sıfırla
            if (spear != null && !spear.wasThrown)
            {
                lastThrownSpear = null;
            }

            PrepareObjectForDrop();
            pickedRigidbody.AddForce(Vector3.up * dropUpwardForce, ForceMode.Impulse);
            ResetPickupState();
        }
    }

    private void PrepareObjectForDrop()
    {
        if (spearSystem != null)
        {
            spearSystem.SetBeingHeld(false);
        }

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

    // Yeni fonksiyon - raycast kontrolü yapmadan direkt olarak objeyi alır
    public void ForcePickup(GameObject objectToPickup)
    {
        // Eğer zaten bir obje tutuyorsak, yeni bir obje alamayız
        if (pickedObject != null) return;

        pickedObject = objectToPickup;
        pickedRigidbody = pickedObject.GetComponent<Rigidbody>();
        spearSystem = pickedObject.GetComponent<SpearSystem>();
        originalParent = pickedObject.transform.parent;
        originalScale = pickedObject.transform.localScale;

        if (spearSystem != null)
        {
            spearSystem.SetBeingHeld(true);
        }

        pickedRigidbody.isKinematic = true;
        pickedRigidbody.useGravity = false;
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

    private void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerCamera.position, pickupRange);
        }
    }
}