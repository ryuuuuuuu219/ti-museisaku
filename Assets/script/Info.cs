using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class Info : MonoBehaviour
{
    [SerializeField] Canvas infoCanvasA;
    [SerializeField] Canvas infoCanvasB;
    [SerializeField] Light infoLightA;
    [SerializeField] float debugSanity = 10f;

    public enum InfoState
    {
        A,
        B,
        C
    }

    public InfoState currentState = InfoState.A;

    public float GetSanity()
    {
        return debugSanity;
    }

    void Start()
    {
        ConfigureInfoCamera();
        ApplyState();
    }

    public void ChangeInfo()
    {
        switch (currentState)
        {
            case InfoState.A:
                currentState = InfoState.B;
                break;
            case InfoState.B:
                currentState = InfoState.C;
                break;
            case InfoState.C:
                currentState = InfoState.A;
                break;
        }

        ApplyState();
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.rightButton.wasPressedThisFrame)
        {
            ChangeInfo();
        }
    }

    void ApplyState()
    {
        bool isStateA = currentState == InfoState.A;
        bool isStateB = currentState == InfoState.B;

        SetCanvasVisible(infoCanvasA, isStateA);
        SetCanvasVisible(infoCanvasB, isStateB);
        infoLightA.intensity = currentState == InfoState.C ? 1f : 0f;
    }

    void SetCanvasVisible(Canvas targetCanvas, bool visible)
    {
        if (targetCanvas == infoCanvasB)
        {
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        targetCanvas.enabled = visible;
        targetCanvas.transform.localScale = visible ? Vector3.one : Vector3.zero;
        targetCanvas.overrideSorting = true;
        targetCanvas.sortingOrder = visible ? 100 : 0;
    }

    void ConfigureInfoCamera()
    {
        Camera infoCamera = infoCanvasB.worldCamera;
        Camera mainCamera = Camera.main;
        if (infoCamera == null || mainCamera == null || infoCamera == mainCamera)
        {
            return;
        }

        UniversalAdditionalCameraData mainCameraData = mainCamera.GetUniversalAdditionalCameraData();
        UniversalAdditionalCameraData infoCameraData = infoCamera.GetUniversalAdditionalCameraData();
        if (mainCameraData == null || infoCameraData == null)
        {
            return;
        }

        infoCameraData.renderType = CameraRenderType.Overlay;
        if (!mainCameraData.cameraStack.Contains(infoCamera))
        {
            mainCameraData.cameraStack.Add(infoCamera);
        }
    }
}
