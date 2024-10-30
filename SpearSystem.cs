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

    private bool isDrawingBack = false;
    private bool readyToThrow = false;
    private Coroutine drawBackCoroutine;
    private Rigidbody rb;
    private PickUpSystem pickUpSystem;
    private Transform mainCamera;
    private Quaternion targetRotation;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pickUpSystem = FindObjectOfType<PickUpSystem>();
        mainCamera = Camera.main.transform;
        targetRotation = Quaternion.Euler(holdRotation);
    }

    private void Update()
    {
        if (!rb.isKinematic) return;

        UpdatePosition();

        if (Input.GetMouseButtonDown(1) && !isDrawingBack)
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
        // Hedef pozisyonu kameraya göre hesapla
        Vector3 targetPosition = mainCamera.position +
                               (mainCamera.right * holdOffset.x) +
                               (mainCamera.up * holdOffset.y) +
                               (mainCamera.forward * holdOffset.z);

        // Eğer germe durumundaysa, pozisyonu geriye doğru ayarla
        if (isDrawingBack && readyToThrow)
        {
            targetPosition -= mainCamera.forward * drawBackDistance;
        }

        // Yumuşak geçişle pozisyonu ve rotasyonu güncelle
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
        Vector3 startPos = transform.position;
        float elapsedTime = 0;

        while (elapsedTime < drawBackDelay)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / drawBackDelay;

            // Gerilme animasyonunu yumuşak bir şekilde uygula
            Vector3 currentOffset = Vector3.Lerp(Vector3.zero, mainCamera.forward * drawBackDistance, Mathf.SmoothStep(0, 1, t));

            yield return null;
        }

        readyToThrow = true;
    }

    private void ThrowSpear()
    {
        if (pickUpSystem != null)
        {
            pickUpSystem.SendMessage("DropObject", SendMessageOptions.DontRequireReceiver);
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
    }
}