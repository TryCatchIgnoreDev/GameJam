using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Postere tıklandığında veya [E] ile incelendiğinde açılan yüksek çözünürlüklü detay okuma modalı.
/// </summary>
public class PosterReaderPopup : MonoBehaviour
{
    public Texture2D posterTexture;
    public bool IsOpen => isPopupOpen;
    private StoryInteractable interactable;
    private bool isPopupOpen = false;
    private static GUIStyle modalStyle;
    private static GUIStyle titleStyle;
    private static GUIStyle bodyStyle;
    private static GUIStyle quoteStyle;
    private static GUIStyle closeBtnStyle;
    private static Texture2D modalBgTex;

    private void Awake()
    {
        interactable = GetComponent<StoryInteractable>();
        if (posterTexture == null)
        {
            Renderer r = GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null && r.sharedMaterial.mainTexture != null)
            {
                posterTexture = r.sharedMaterial.mainTexture as Texture2D;
            }
#if UNITY_EDITOR
            if (posterTexture == null)
            {
                posterTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/poster.jpg");
            }
#endif
        }
    }

    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.OnInteracted += HandleInteracted;
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.OnInteracted -= HandleInteracted;
        }
    }

    private int openedFrame = -1;

    private void HandleInteracted(StoryInteractable src)
    {
        isPopupOpen = !isPopupOpen;
        if (isPopupOpen)
        {
            openedFrame = Time.frameCount;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        if (!isPopupOpen) return;
        if (Time.frameCount <= openedFrame) return;

        // E veya ESC veya Space ile kapat
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            (Keyboard.current.escapeKey.wasPressedThisFrame ||
             Keyboard.current.eKey.wasPressedThisFrame ||
             Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            ClosePopup();
        }
#else
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
        {
            ClosePopup();
        }
#endif
    }

    public void ClosePopup()
    {
        isPopupOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnGUI()
    {
        if (!isPopupOpen) return;

        EnsurePopupStyles();

        // 1. Ekranı Karart
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, modalStyle);

        // 2. Eğer posterTexture mevcutsa görsel olarak büyük ekranda çiz
        if (posterTexture != null)
        {
            float maxW = Mathf.Min(1100f, Screen.width - 60f);
            float maxH = Mathf.Min(680f, Screen.height - 100f);

            float aspect = (float)posterTexture.width / Mathf.Max(1, posterTexture.height);
            float posterW = maxW;
            float posterH = posterW / aspect;

            if (posterH > maxH)
            {
                posterH = maxH;
                posterW = posterH * aspect;
            }

            float posterX = (Screen.width - posterW) / 2f;
            float posterY = (Screen.height - posterH - 55f) / 2f;

            // Arka çerçeve paneli
            Rect frameRect = new Rect(posterX - 10f, posterY - 10f, posterW + 20f, posterH + 20f);
            GUI.Box(frameRect, GUIContent.none, modalStyle);

            // Poster Görseli
            Rect posterRect = new Rect(posterX, posterY, posterW, posterH);
            GUI.DrawTexture(posterRect, posterTexture, ScaleMode.StretchToFill);

            // Kapat Butonu
            float btnW = 260f;
            float btnH = 42f;
            float btnX = (Screen.width - btnW) / 2f;
            float btnY = posterY + posterH + 18f;

            if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), "Kapat [E / ESC / Boşluk]", closeBtnStyle))
            {
                ClosePopup();
            }
            return;
        }

        // Fallback: Yazılı Modal
        float panelW = Mathf.Min(780f, Screen.width - 60f);
        float panelH = Mathf.Min(640f, Screen.height - 60f);
        float panelX = (Screen.width - panelW) / 2f;
        float panelY = (Screen.height - panelH) / 2f;

        Rect modalRect = new Rect(panelX, panelY, panelW, panelH);
        GUI.Box(modalRect, GUIContent.none, modalStyle);

        GUILayout.BeginArea(new Rect(panelX + 35f, panelY + 30f, panelW - 70f, panelH - 60f));

        GUILayout.Label("DEPREM ÖNCESİ, ANI VE SONRASI LİSTESİ", titleStyle);
        GUILayout.Space(12);

        string itemsText = "✗ Merdivenlere ve asansöre KOŞMA!\n" +
                           "  (Sarsıntı sırasında merdivenler kopabilir, asansör boşluğuna düşülebilir.)\n\n" +
                           "✗ Pencere ve balkonlardan UZAK DUR!\n" +
                           "  (Camlar şiddetle patlayıp parçalanır, balkonlar ilk çöken yerlerdir.)\n\n" +
                           "✓ Sağlam bir nesnenin yanına ÇÖK - KAPAN - TUTUN!\n" +
                           "  (Masa veya sağlam baza yanında hayat üçgeni oluştur, başını ve enseni koru!)";

        GUILayout.Label(itemsText, bodyStyle);
        GUILayout.Space(25);

        GUILayout.Label("\"O gün binlerce insan bana bi şey olmaz diye uyudu.\"", quoteStyle);
        GUILayout.Space(25);

        if (GUILayout.Button("Kapat [E / ESC]", closeBtnStyle, GUILayout.Height(42)))
        {
            ClosePopup();
        }

        GUILayout.EndArea();
    }

    private static void EnsurePopupStyles()
    {
        if (modalBgTex == null)
        {
            modalBgTex = new Texture2D(1, 1);
            modalBgTex.SetPixel(0, 0, new Color(0.08f, 0.09f, 0.12f, 0.96f));
            modalBgTex.Apply();
        }

        if (modalStyle == null)
        {
            modalStyle = new GUIStyle(GUI.skin.box);
            modalStyle.normal.background = modalBgTex;
        }

        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 24;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = new Color(0.95f, 0.25f, 0.25f, 1f);
        }

        if (bodyStyle == null)
        {
            bodyStyle = new GUIStyle(GUI.skin.label);
            bodyStyle.fontSize = 17;
            bodyStyle.normal.textColor = Color.white;
            bodyStyle.wordWrap = true;
        }

        if (quoteStyle == null)
        {
            quoteStyle = new GUIStyle(GUI.skin.label);
            quoteStyle.fontSize = 18;
            quoteStyle.fontStyle = FontStyle.Italic;
            quoteStyle.alignment = TextAnchor.MiddleCenter;
            quoteStyle.normal.textColor = new Color(1f, 0.85f, 0.45f, 1f);
            quoteStyle.wordWrap = true;
        }

        if (closeBtnStyle == null)
        {
            closeBtnStyle = new GUIStyle(GUI.skin.button);
            closeBtnStyle.fontSize = 16;
            closeBtnStyle.fontStyle = FontStyle.Bold;
            closeBtnStyle.normal.textColor = Color.white;
        }
    }
}
