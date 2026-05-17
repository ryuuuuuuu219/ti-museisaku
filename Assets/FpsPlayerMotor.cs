using UnityEngine;
using UnityEngine.InputSystem;

public sealed class FpsPlayerMotor : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float minPitch = -82f;
    [SerializeField] private float maxPitch = 82f;

    [Header("Move")]
    [SerializeField] private float walkSpeed = 3.8f;
    [SerializeField] private float eyeLevel = 1.62f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Walk Bob")]
    [SerializeField] private float bobFrequency = 8.5f;
    [SerializeField] private float bobDrop = 0.075f;
    [SerializeField] private float bobReturnSpeed = 14f;

    private Transform cameraTransform;
    private Transform playerYawRoot;
    private CharacterController characterController;
    private float pitch;
    private float bobTimer;
    private float currentBobDrop;

    public void Configure(
        Transform playerCamera,
        CharacterController controller,
        float configuredMouseSensitivity,
        float configuredMinPitch,
        float configuredMaxPitch,
        float configuredWalkSpeed,
        float configuredGravity,
        float configuredBobFrequency,
        float configuredBobDrop,
        float configuredBobReturnSpeed)
    {
        cameraTransform = playerCamera;
        characterController = controller;
        mouseSensitivity = configuredMouseSensitivity;
        minPitch = configuredMinPitch;
        maxPitch = configuredMaxPitch;
        walkSpeed = configuredWalkSpeed;
        bobFrequency = configuredBobFrequency;
        bobDrop = configuredBobDrop;
        bobReturnSpeed = configuredBobReturnSpeed;
        pitch = NormalizePitch(cameraTransform.localEulerAngles.x);
    }

    private void Awake()
    {
        if (cameraTransform == null)
        {
            cameraTransform = transform;
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        BuildYawRootFromCurrentCameraPose();
    }

    private void Update()
    {
        if (cameraTransform == null)
        {
            return;
        }

        HandleCursorToggle();
        HandleLook();
        HandleMove();
        HandleActionInputs();
    }

    private void HandleLook()
    {
        Vector2 lookDelta = Mouse.current == null ? Vector2.zero : Mouse.current.delta.ReadValue();

        playerYawRoot.Rotate(Vector3.up, lookDelta.x * mouseSensitivity, Space.Self);

        pitch = Mathf.Clamp(pitch - lookDelta.y * mouseSensitivity, minPitch, maxPitch);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMove()
    {
        Vector2 moveInput = ReadMoveInput();
        Vector3 forward = Vector3.ProjectOnPlane(playerYawRoot.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(playerYawRoot.right, Vector3.up).normalized;
        Vector3 move = forward * moveInput.y + right * moveInput.x;
        move = Vector3.ClampMagnitude(move, 1f);

        Vector3 nextPosition = playerYawRoot.position + move * walkSpeed * Time.deltaTime;
        float targetEyeY = GetTerrainHeight(nextPosition) + eyeLevel;
        UpdateWalkBob(moveInput.sqrMagnitude > 0.01f);
        nextPosition.y = targetEyeY - eyeLevel;

        if (characterController != null)
        {
            characterController.enabled = false;
            playerYawRoot.position = nextPosition;
            characterController.enabled = true;
            cameraTransform.localPosition = new Vector3(0f, eyeLevel - currentBobDrop, 0f);
            return;
        }

        playerYawRoot.position = nextPosition;
        cameraTransform.localPosition = new Vector3(0f, eyeLevel - currentBobDrop, 0f);
    }

    private static Vector2 ReadMoveInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        Vector2 move = Vector2.zero;
        move.x += keyboard.dKey.isPressed ? 1f : 0f;
        move.x -= keyboard.aKey.isPressed ? 1f : 0f;
        move.y += keyboard.wKey.isPressed ? 1f : 0f;
        move.y -= keyboard.sKey.isPressed ? 1f : 0f;
        return Vector2.ClampMagnitude(move, 1f);
    }

    private void UpdateWalkBob(bool isWalking)
    {
        if (isWalking)
        {
            bobTimer += Time.deltaTime * bobFrequency;
            currentBobDrop = Mathf.Abs(Mathf.Sin(bobTimer)) * bobDrop;
            return;
        }

        bobTimer = 0f;
        currentBobDrop = Mathf.Lerp(currentBobDrop, 0f, bobReturnSpeed * Time.deltaTime);
    }

    private static void HandleActionInputs()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            Debug.Log("MVP Attack input");
        }

        if (mouse.rightButton.wasPressedThisFrame)
        {
            Debug.Log("MVP Stream monitor input");
        }
    }

    private static void HandleCursorToggle()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        bool shouldLock = Cursor.lockState != CursorLockMode.Locked;
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shouldLock;
    }

    private static float NormalizePitch(float eulerX)
    {
        return eulerX > 180f ? eulerX - 360f : eulerX;
    }

    private void SnapToEyeLevel()
    {
        Vector3 position = transform.position;
        position.y = GetTerrainHeight(position) + eyeLevel;
        transform.position = position;
    }

    private void BuildYawRootFromCurrentCameraPose()
    {
        Vector3 initialCameraPosition = cameraTransform.position;
        Vector3 rootPosition = initialCameraPosition;
        rootPosition.y = GetTerrainHeight(initialCameraPosition);

        Quaternion initialCameraRotation = cameraTransform.rotation;
        float initialYaw = initialCameraRotation.eulerAngles.y;
        pitch = Mathf.Clamp(NormalizePitch(initialCameraRotation.eulerAngles.x), minPitch, maxPitch);

        GameObject root = new GameObject("FPS Player Yaw Root");
        playerYawRoot = root.transform;
        playerYawRoot.SetPositionAndRotation(rootPosition, Quaternion.Euler(0f, initialYaw, 0f));

        cameraTransform.SetParent(playerYawRoot, false);
        cameraTransform.localPosition = new Vector3(0f, eyeLevel, 0f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private float GetTerrainHeight(Vector3 position)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            return terrain.SampleHeight(position) + terrain.transform.position.y;
        }

        Vector3 rayOrigin = position + Vector3.up * 1000f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 2000f, groundMask, QueryTriggerInteraction.Ignore))
        {
            return hit.point.y;
        }

        return 0f;
    }
}
