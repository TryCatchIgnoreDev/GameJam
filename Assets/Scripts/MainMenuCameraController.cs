using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Ana Menü sahnesinde enkaz alanına ve polis aracına bakan sinematik kamera kontrolcüsü.
/// Hafif nefes alma, yavaş yörünge/kayma hareketi ve fare pozisyonuna göre zarif bir derinlik (parallax) sağlar.
/// </summary>
public class MainMenuCameraController : MonoBehaviour
{
    [Header("Odak & Pozisyon")]
    public Transform lookTarget;
    public Vector3 basePosition = new Vector3(0f, 2.8f, -7.5f);
    public Vector3 baseRotationEuler = new Vector3(8f, 0f, 0f);

    [Header("Sinematik Salınım")]
    public float swaySpeed = 0.4f;
    public float swayAmountX = 0.35f;
    public float swayAmountY = 0.15f;

    [Header("Fare Parallaks")]
    public float mouseParallaxAmount = 0.25f;
    public float mouseSmoothSpeed = 3.0f;

    private Vector2 currentMouseOffset;
    private Vector2 targetMouseOffset;
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = GameSettings.CameraFOV;
        }
    }

    private void Start()
    {
        if (basePosition == Vector3.zero)
        {
            basePosition = transform.position;
            baseRotationEuler = transform.eulerAngles;
        }

        // Fare imlecini serbest bırak ve görünür yap
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        // Fare pozisyonuna göre normalize edilmiş ekran offseti (-1 .. 1)
        Vector2 mousePos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            mousePos = Mouse.current.position.ReadValue();
        }
#else
        mousePos = Input.mousePosition;
#endif

        if (Screen.width > 0 && Screen.height > 0)
        {
            float normX = (mousePos.x / Screen.width - 0.5f) * 2f;
            float normY = (mousePos.y / Screen.height - 0.5f) * 2f;
            targetMouseOffset = new Vector2(normX, normY) * mouseParallaxAmount;
        }

        currentMouseOffset = Vector2.Lerp(currentMouseOffset, targetMouseOffset, Time.deltaTime * mouseSmoothSpeed);

        // Sinematik yavaş salınım
        float time = Time.time * swaySpeed;
        float offsetX = Mathf.Sin(time) * swayAmountX + currentMouseOffset.x;
        float offsetY = Mathf.Cos(time * 0.7f) * swayAmountY + currentMouseOffset.y * 0.5f;

        transform.position = basePosition + new Vector3(offsetX, offsetY, 0f);

        if (lookTarget != null)
        {
            Quaternion targetRot = Quaternion.LookRotation((lookTarget.position - transform.position).normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 3.5f);
        }
        else
        {
            transform.rotation = Quaternion.Euler(
                baseRotationEuler.x - currentMouseOffset.y * 2.0f,
                baseRotationEuler.y + currentMouseOffset.x * 3.0f,
                baseRotationEuler.z
            );
        }
    }

    /// <summary>
    /// FOV ayarı değiştirildiğinde anında yansıtır.
    /// </summary>
    public void RefreshFOV()
    {
        if (cam != null)
        {
            cam.fieldOfView = GameSettings.CameraFOV;
        }
    }
}
