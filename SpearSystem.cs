using UnityEngine;
using System.Collections;

public class SpearSystem : MonoBehaviour
{
    [Header("Draw Back Settings")]
    public float drawBackDelay = 0.25f;
    public float drawBackDistance = 1.5f;

    [Header("Throw Settings")]
    public float throwForce = 30f;
    public float throwUpwardForce = 2f;
    public float throwTorque = 10f;

    [Header("Position Settings")]
    public Vector3 holdOffset = new Vector3(0.5f, -0.3f, 0.8f);
    public Vector3 holdRotation = new Vector3(0f, 270f, 0f);
    public float smoothSpeed = 15f;

    [Header("Recall Settings")]
    public float recallSpeed = 30f;
    public float recallRotationSpeed = 720f;
    public float minDistanceToPickup = 1f;
    public AnimationCurve recallCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool isDrawingBack = false;
    private bool readyToThrow = false;
    private bool isRecalling = false;
    public bool wasThrown = false;
    private bool isBeingHeld = false;
    private Coroutine drawBackCoroutine;
    private Coroutine recallCoroutine;
    private Rigidbody rb;
    private PickUpSystem pickUpSystem;
    private Transform mainCamera;
    private Quaternion targetRotation;
    private Vector3 originalScale;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pickUpSystem = FindObjectOfType<PickUpSystem>();
        mainCamera = Camera.main.transform;
        targetRotation = Quaternion.Euler(holdRotation);
        originalScale = transform.localScale;
    }

    private void Update()
    {
        if (!rb.isKinematic && !isRecalling)
        {
            return;
        }

        if (rb.isKinematic && !isRecalling && isBeingHeld)
        {
            UpdatePosition();
        }

        HandleInput();
    }

    public bool IsBeingHeld()
    {
        return isBeingHeld;
    }

    public void SetBeingHeld(bool held)
    {
        isBeingHeld = held;
    }

    private void HandleInput()
    {
        if (!isBeingHeld && !isRecalling) return;

        if (Input.GetMouseButtonDown(1) && !isDrawingBack && !isRecalling)
        {
            StartDrawBack();
        }
        else if (Input.GetMouseButtonUp(1) && isDrawingBack)
        {
            CancelDrawBack();
        }

        if (Input.GetKeyDown(KeyCode.Q) && readyToThrow)
        {
            ThrowSpear();
        }
    }

    private void UpdatePosition()
    {
        Vector3 targetPosition = mainCamera.position +
                               (mainCamera.right * holdOffset.x) +
                               (mainCamera.up * holdOffset.y) +
                               (mainCamera.forward * holdOffset.z);

        if (isDrawingBack && readyToThrow)
        {
            targetPosition -= mainCamera.forward * drawBackDistance;
        }

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, mainCamera.rotation * targetRotation, Time.deltaTime * smoothSpeed);
    }

    private void StartDrawBack()
    {
        if (drawBackCoroutine != null)
        {
            StopCoroutine(drawBackCoroutine);
        }
        drawBackCoroutine = StartCoroutine(DrawBackSequence());
    }

    private void CancelDrawBack()
    {
        if (drawBackCoroutine != null)
        {
            StopCoroutine(drawBackCoroutine);
        }
        isDrawingBack = false;
        readyToThrow = false;
    }

    private IEnumerator DrawBackSequence()
    {
        isDrawingBack = true;
        float elapsedTime = 0;

        while (elapsedTime < drawBackDelay)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / drawBackDelay;
            yield return null;
        }

        readyToThrow = true;
    }

    public void StartRecall()
    {
        if (recallCoroutine != null)
        {
            StopCoroutine(recallCoroutine);
        }

        isRecalling = true;
        rb.isKinematic = true;
        rb.useGravity = false;
        recallCoroutine = StartCoroutine(RecallSequence());
    }

    private IEnumerator RecallSequence()
    {
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        while (isRecalling)
        {
            elapsedTime += Time.deltaTime;

            Vector3 targetPosition = mainCamera.position +
                                   (mainCamera.right * holdOffset.x) +
                                   (mainCamera.up * holdOffset.y) +
                                   (mainCamera.forward * holdOffset.z);

            float journeyLength = Vector3.Distance(startPosition, targetPosition);
            float distanceCovered = elapsedTime * recallSpeed;
            float fractionOfJourney = distanceCovered / journeyLength;
            fractionOfJourney = recallCurve.Evaluate(fractionOfJourney);

            transform.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);
            transform.rotation = Quaternion.Slerp(startRotation, mainCamera.rotation * targetRotation, fractionOfJourney);

            if (Vector3.Distance(transform.position, targetPosition) < minDistanceToPickup)
            {
                CompleteRecall();
                break;
            }

            yield return null;
        }
    }

    private void CompleteRecall()
    {
        isRecalling = false;
        wasThrown = false;

        rb.isKinematic = true;
        rb.useGravity = false;
        transform.localScale = originalScale;

        if (pickUpSystem != null)
        {
            // TryPickUp yerine ForcePickup çağırıyoruz
            pickUpSystem.ForcePickup(gameObject);
        }
    }

    private void ThrowSpear()
    {
        wasThrown = true; // Önce bu flag'i set ediyoruz

        if (pickUpSystem != null)
        {
            pickUpSystem.SetLastThrownSpear(this);
            pickUpSystem.DropObject();
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            Vector3 throwDirection = mainCamera.forward;
            rb.AddForce(throwDirection * throwForce + Vector3.up * throwUpwardForce, ForceMode.Impulse);
            rb.AddTorque(transform.right * throwTorque, ForceMode.Impulse);
        }

        isDrawingBack = false;
        readyToThrow = false;
        isBeingHeld = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isRecalling) return;
    }
}