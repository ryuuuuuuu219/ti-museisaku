using UnityEngine;
using UnityEngine.InputSystem;

public sealed class HorrorFpsMvpController : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float minPitch = -82f;
    [SerializeField] private float maxPitch = 82f;

    [Header("Move")]
    [SerializeField] private float walkSpeed = 3.8f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float eyeHeight = 1.62f;

    [Header("Walk Bob")]
    [SerializeField] private float bobFrequency = 8.5f;
    [SerializeField] private float bobDrop = 0.075f;
    [SerializeField] private float bobReturnSpeed = 14f;

    [Header("Light")]
    [SerializeField] private float lightRange = 16f;
    [SerializeField] private float lightIntensity = 4f;
    [SerializeField] private float lightSpotAngle = 58f;

    private Transform playerRoot;
    private CharacterController characterController;
    private Light carriedLight;
    private float pitch;
    private float verticalVelocity;
    private float bobTimer;
    private Vector3 cameraRestLocalPosition;

    private void Awake()
    {
        BuildMvpSceneIfNeeded();
        BuildPlayerRig();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleCursorToggle();
        HandleLook();
        HandleMove();
        HandleActionInputs();
    }

    private void BuildPlayerRig()
    {
        playerRoot = new GameObject("Horror FPS Player").transform;
        playerRoot.position = new Vector3(0f, 1f, -5f);
        playerRoot.rotation = Quaternion.identity;

        characterController = playerRoot.gameObject.AddComponent<CharacterController>();
        characterController.height = 1.8f;
        characterController.radius = 0.32f;
        characterController.center = new Vector3(0f, 0.9f, 0f);
        characterController.skinWidth = 0.04f;

        transform.SetParent(playerRoot, false);
        transform.localPosition = new Vector3(0f, eyeHeight, 0f);
        transform.localRotation = Quaternion.identity;
        cameraRestLocalPosition = transform.localPosition;

        carriedLight = gameObject.AddComponent<Light>();
        carriedLight.type = LightType.Spot;
        carriedLight.range = lightRange;
        carriedLight.intensity = lightIntensity;
        carriedLight.spotAngle = lightSpotAngle;
        carriedLight.innerSpotAngle = lightSpotAngle * 0.58f;
        carriedLight.shadows = LightShadows.Soft;
        carriedLight.color = new Color(1f, 0.92f, 0.78f);
    }

    private void BuildMvpSceneIfNeeded()
    {
        foreach (Light sceneLight in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (sceneLight.type == LightType.Directional)
            {
                Destroy(sceneLight.gameObject);
            }
        }

        if (GameObject.Find("MVP Floor") != null)
        {
            return;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.01f, 0.012f, 0.016f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.012f, 0.014f, 0.018f);
        RenderSettings.fogDensity = 0.045f;

        CreatePrimitive("MVP Floor", PrimitiveType.Cube, new Vector3(0f, -0.05f, 0f), new Vector3(18f, 0.1f, 24f), new Color(0.12f, 0.12f, 0.115f));
        CreatePrimitive("MVP Back Wall", PrimitiveType.Cube, new Vector3(0f, 1.5f, 7f), new Vector3(18f, 3f, 0.2f), new Color(0.095f, 0.1f, 0.11f));
        CreatePrimitive("MVP Left Wall", PrimitiveType.Cube, new Vector3(-9f, 1.5f, 0f), new Vector3(0.2f, 3f, 24f), new Color(0.08f, 0.085f, 0.095f));
        CreatePrimitive("MVP Right Wall", PrimitiveType.Cube, new Vector3(9f, 1.5f, 0f), new Vector3(0.2f, 3f, 24f), new Color(0.08f, 0.085f, 0.095f));
        CreatePrimitive("MVP Pillar A", PrimitiveType.Cube, new Vector3(-3.5f, 1f, 1.5f), new Vector3(0.9f, 2f, 0.9f), new Color(0.13f, 0.12f, 0.11f));
        CreatePrimitive("MVP Pillar B", PrimitiveType.Cube, new Vector3(3.2f, 1f, -1.8f), new Vector3(1.1f, 2f, 1.1f), new Color(0.13f, 0.12f, 0.11f));
    }

    private static void CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
    {
        GameObject primitive = GameObject.CreatePrimitive(type);
        primitive.name = name;
        primitive.transform.SetPositionAndRotation(position, Quaternion.identity);
        primitive.transform.localScale = scale;

        Renderer renderer = primitive.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.material.color = color;
    }

    private void HandleLook()
    {
        Vector2 lookDelta = Mouse.current == null ? Vector2.zero : Mouse.current.delta.ReadValue();
        playerRoot.Rotate(Vector3.up, lookDelta.x * mouseSensitivity, Space.Self);

        pitch = Mathf.Clamp(pitch - lookDelta.y * mouseSensitivity, minPitch, maxPitch);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMove()
    {
        Vector2 moveInput = ReadMoveInput();
        Vector3 move = playerRoot.forward * moveInput.y + playerRoot.right * moveInput.x;
        move = Vector3.ClampMagnitude(move, 1f);

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -1f;
        }

        verticalVelocity += gravity * Time.deltaTime;
        Vector3 velocity = move * walkSpeed;
        velocity.y = verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);

        UpdateWalkBob(moveInput.sqrMagnitude > 0.01f && characterController.isGrounded);
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
            float downwardStep = Mathf.Abs(Mathf.Sin(bobTimer)) * bobDrop;
            transform.localPosition = cameraRestLocalPosition + Vector3.down * downwardStep;
            return;
        }

        bobTimer = 0f;
        transform.localPosition = Vector3.Lerp(transform.localPosition, cameraRestLocalPosition, bobReturnSpeed * Time.deltaTime);
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
}
