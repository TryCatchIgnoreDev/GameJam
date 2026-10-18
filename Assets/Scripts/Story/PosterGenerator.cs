using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Hikayedeki mutfak girişinde duran ve anneannesinin gönderdiği "Deprem Farkındalık Posteri"ni
/// hem prosedürel görsel doku (Texture2D) hem de 3D duvar objesi (GameObject) olarak oluşturan yardımcı sınıf.
/// </summary>
public static class PosterGenerator
{
    private const string PosterName = "DepremFarkindalikPosteri";

    /// <summary>
    /// Koyu krem/kağıt arka plan, üstte kırmızı uyarı şeridi, ortada belirgin uyarı kutucukları
    /// (MERDİVEN kullanma, PENCERE kenarından uzak dur, ÇÖK - KAPAN - TUTUN şeması) ve
    /// alt kısımda koyu kırmızı şerit içeren şık bir poster dokusu üretir.
    /// </summary>
    public static Texture2D GeneratePosterTexture(int width = 512, int height = 720)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.name = "Tex_EarthquakePoster_Procedural";
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[width * height];

        // 1. Koyu Krem / Kağıt Arka Plan
        Color32 paperBg = new Color32(238, 230, 214, 255);
        Color32 paperDark = new Color32(225, 215, 196, 255);
        for (int y = 0; y < height; y++)
        {
            float t = (float)y / height;
            byte r = (byte)Mathf.Lerp(paperDark.r, paperBg.r, t);
            byte g = (byte)Mathf.Lerp(paperDark.g, paperBg.g, t);
            byte b = (byte)Mathf.Lerp(paperDark.b, paperBg.b, t);
            Color32 gradCol = new Color32(r, g, b, 255);

            for (int x = 0; x < width; x++)
            {
                // Hafif kağıt dokusu gürültüsü
                int noise = ((x * 17 + y * 31) % 7) - 3;
                byte pr = (byte)Mathf.Clamp(gradCol.r + noise, 0, 255);
                byte pg = (byte)Mathf.Clamp(gradCol.g + noise, 0, 255);
                byte pb = (byte)Mathf.Clamp(gradCol.b + noise, 0, 255);
                pixels[y * width + x] = new Color32(pr, pg, pb, 255);
            }
        }

        // Renk Paleti
        Color32 redBanner = new Color32(204, 28, 28, 255);
        Color32 darkRedFooter = new Color32(120, 16, 16, 255);
        Color32 cautionYellow = new Color32(245, 195, 35, 255);
        Color32 borderDark = new Color32(60, 50, 45, 255);
        Color32 greenSafe = new Color32(32, 148, 64, 255);
        Color32 cardBg = new Color32(248, 245, 238, 240);

        // 2. Dış Çerçeve
        DrawBorderRect(pixels, width, height, 8, 8, width - 16, height - 16, 3, borderDark);

        // 3. Üst Kırmızı Uyarı Şeridi (Y: 610 - 710)
        int headerY = height - 105;
        int headerH = 95;
        FillRect(pixels, width, height, 12, headerY, width - 24, headerH, redBanner);

        // Şerit kenarlarına ikaz çizgisi (Sarı / Siyah diyagonal)
        DrawHazardBar(pixels, width, height, 12, headerY - 10, width - 24, 8, cautionYellow, borderDark);

        // 4. Orta Bölüm: 3 Belirgin Uyarı Kartı
        // KART 1: MERDİVEN KULLANMA (Tehlike / Kırmızı)
        int card1Y = 430;
        int cardH = 145;
        int cardW = width - 40;
        int cardX = 20;

        FillRect(pixels, width, height, cardX, card1Y, cardW, cardH, cardBg);
        DrawBorderRect(pixels, width, height, cardX, card1Y, cardW, cardH, 2, redBanner);
        // Sol tarafa kırmızı dikey vurgu şeridi
        FillRect(pixels, width, height, cardX, card1Y, 12, cardH, redBanner);
        // Sembol: Merdiven ve Kırmızı Çarpı (✗)
        DrawStairsIcon(pixels, width, height, cardX + 50, card1Y + 72, 34, borderDark);
        DrawSymbolCross(pixels, width, height, cardX + 50, card1Y + 72, 28, 5, redBanner);

        // KART 2: PENCERE VE BALKONDAN UZAK DUR (Tehlike / Kırmızı)
        int card2Y = 265;
        FillRect(pixels, width, height, cardX, card2Y, cardW, cardH, cardBg);
        DrawBorderRect(pixels, width, height, cardX, card2Y, cardW, cardH, 2, redBanner);
        // Sol tarafa kırmızı dikey vurgu şeridi
        FillRect(pixels, width, height, cardX, card2Y, 12, cardH, redBanner);
        // Sembol: Pencere ve Kırmızı Çarpı (✗)
        DrawWindowIcon(pixels, width, height, cardX + 50, card2Y + 72, 32, borderDark);
        DrawSymbolCross(pixels, width, height, cardX + 50, card2Y + 72, 28, 5, redBanner);

        // KART 3: ÇÖK - KAPAN - TUTUN (Hayat Kurtaran / Yeşil)
        int card3Y = 100;
        FillRect(pixels, width, height, cardX, card3Y, cardW, cardH, cardBg);
        DrawBorderRect(pixels, width, height, cardX, card3Y, cardW, cardH, 3, greenSafe);
        // Sol tarafa yeşil dikey vurgu şeridi
        FillRect(pixels, width, height, cardX, card3Y, 12, cardH, greenSafe);
        // Sembol: Hayat Üçgeni / Masa ve Yeşil Onay (✓)
        DrawTableTriangleIcon(pixels, width, height, cardX + 50, card3Y + 72, 36, borderDark);
        DrawSymbolCheck(pixels, width, height, cardX + 50, card3Y + 72, 28, 5, greenSafe);

        // 5. Alt Koyu Kırmızı Şerit (Y: 12 - 85)
        int footerY = 12;
        int footerH = 75;
        FillRect(pixels, width, height, 12, footerY, width - 24, footerH, darkRedFooter);
        // İnce altın sarısı üst çizgi
        DrawHLine(pixels, width, height, 12, footerY + footerH - 2, width - 24, 2, cautionYellow);

        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        return tex;
    }

    /// <summary>
    /// Assets/poster.jpg dosyasını oyunun kullandığı resmi poster dokusu olarak yükler/döndürür.
    /// </summary>
    public static Texture2D GetOfficialPosterTexture()
    {
#if UNITY_EDITOR
        var edTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/poster.jpg");
        if (edTex != null) return edTex;
#endif
        var resTex = Resources.Load<Texture2D>("poster");
        if (resTex != null) return resTex;

        string diskPath = System.IO.Path.Combine(Application.dataPath, "poster.jpg");
        if (System.IO.File.Exists(diskPath))
        {
            byte[] bytes = System.IO.File.ReadAllBytes(diskPath);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(bytes))
            {
                tex.name = "Tex_EarthquakePoster_Official";
                return tex;
            }
        }

        return GeneratePosterTexture(1024, 512);
    }

    /// <summary>
    /// poster.jpg dokusunu kullanan resmi poster materyalini döndürür.
    /// </summary>
    public static Material GetPosterMaterial()
    {
#if UNITY_EDITOR
        var edMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_EarthquakePoster.mat");
        if (edMat != null) return edMat;
#endif
        Texture2D posterTex = GetOfficialPosterTexture();
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");
        if (litShader == null) litShader = Shader.Find("Unlit/Texture");

        Material posterMat = new Material(litShader);
        posterMat.name = "M_EarthquakePoster";
        posterMat.mainTexture = posterTex;
        if (posterMat.HasProperty("_BaseMap")) posterMat.SetTexture("_BaseMap", posterTex);
        if (posterMat.HasProperty("_Smoothness")) posterMat.SetFloat("_Smoothness", 0.05f);
        return posterMat;
    }

    /// <summary>
    /// Verilen poster objesinin (örneğin 02_Trigger_Mutfak_Posteri) resmi poster.jpg materyaline,
    /// standart Cube mesh'e ve doğru 2:1 ölçeğe (1.4m x 0.7m x 0.02m) sahip olmasını garanti eder.
    /// Eski çakışan PosterCanvas alt objesini pasifleştirir.
    /// </summary>
    public static void EnsurePosterSetup(GameObject posterObj)
    {
        if (posterObj == null) return;

        // 1. MeshFilter ve Cube mesh
        MeshFilter mf = posterObj.GetComponent<MeshFilter>();
        if (mf == null) mf = posterObj.AddComponent<MeshFilter>();
        if (mf.sharedMesh == null)
        {
            GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mf.sharedMesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(tempCube);
        }

        // 2. MeshRenderer ve poster.jpg Materyali
        MeshRenderer rend = posterObj.GetComponent<MeshRenderer>();
        if (rend == null) rend = posterObj.AddComponent<MeshRenderer>();
        rend.sharedMaterial = GetPosterMaterial();

        // 3. Ölçek (2:1 en boy oranı: 1.4m genişlik, 0.7m yükseklik, 0.02m kalınlık)
        posterObj.transform.localScale = new Vector3(1.4f, 0.7f, 0.02f);

        // 4. Collider
        BoxCollider col = posterObj.GetComponent<BoxCollider>();
        if (col == null) col = posterObj.AddComponent<BoxCollider>();
        col.size = Vector3.one;

        // 5. Eski metin canvas'ını devre dışı bırak
        Transform canvasChild = posterObj.transform.Find("PosterCanvas");
        if (canvasChild != null)
        {
            canvasChild.gameObject.SetActive(false);
        }

        // 6. StoryInteractable
        StoryInteractable interactable = posterObj.GetComponent<StoryInteractable>();
        if (interactable == null) interactable = posterObj.AddComponent<StoryInteractable>();
        interactable.interactableType = InteractableType.Poster;
        interactable.interactableId = "kitchen_earthquake_poster";
        interactable.promptText = "[E] Deprem Posterini İncele";
        interactable.interactDistance = 3.2f;
        interactable.requireFacing = true;
        interactable.highlightMarker = true;
        interactable.markerHeightOffset = 0.45f;

        if (posterObj.GetComponent<PosterReaderPopup>() == null)
        {
            posterObj.AddComponent<PosterReaderPopup>();
        }
    }

    /// <summary>
    /// poster.jpg dokusunu kullanan 3D pano objesi (Cube: 1.4m x 0.7m x 0.02m) oluşturur.
    /// </summary>
    public static GameObject CreatePosterObject(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        // 1. Temel 3D Pano (Cube: 1.4 x 0.7 x 0.02m - poster.jpg 2:1 En Boy Oranı)
        GameObject posterObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        posterObj.name = PosterName;
        posterObj.transform.position = position;
        posterObj.transform.rotation = rotation;
        posterObj.transform.localScale = new Vector3(1.4f, 0.7f, 0.02f);

        if (parent != null)
        {
            posterObj.transform.SetParent(parent, true);
        }

        // 2. Resmi poster.jpg Materyal ve Doku Ataması
        Renderer renderer = posterObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = GetPosterMaterial();
        }

        // 3. StoryInteractable ve BoxCollider
        BoxCollider col = posterObj.GetComponent<BoxCollider>();
        if (col == null)
        {
            col = posterObj.AddComponent<BoxCollider>();
        }
        col.size = Vector3.one;

        StoryInteractable interactable = posterObj.GetComponent<StoryInteractable>();
        if (interactable == null)
        {
            interactable = posterObj.AddComponent<StoryInteractable>();
        }
        interactable.interactableType = InteractableType.Poster;
        interactable.interactableId = "kitchen_earthquake_poster";
        interactable.promptText = "[E] Deprem Posterini İncele";
        interactable.interactDistance = 3.2f;
        interactable.requireFacing = true;
        interactable.highlightMarker = true;
        interactable.markerHeightOffset = 0.45f;

        // Poster okuma modalı bileşeni
        if (posterObj.GetComponent<PosterReaderPopup>() == null)
        {
            posterObj.AddComponent<PosterReaderPopup>();
        }

        Debug.Log($"[PosterGenerator] Deprem Posteri (poster.jpg) başarıyla oluşturuldu -> Konum: {position}");
        return posterObj;
    }


    /// <summary>
    /// Postere bağlı World-Space Canvas oluşturarak başlık, maddeler ve alt vurucu metni yerleştirir.
    /// </summary>
    private static void BuildWorldSpaceCanvas(Transform posterTransform)
    {
        GameObject canvasObj = new GameObject("PosterCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObj.transform.SetParent(posterTransform, false);

        // Küpün ön yüzü local -Z doğrultusundadır (birim küp yüzeyi z = -0.5f olduğu için -0.55f)
        canvasObj.transform.localPosition = new Vector3(0f, 0f, -0.55f);
        canvasObj.transform.localRotation = Quaternion.identity;

        // Üst obje zaten (0.8, 1.1, 0.02) ölçeğinde olduğu için 1/width ve 1/height ile çarpıyoruz
        float canvasWidth = 800f;
        float canvasHeight = 1100f;
        canvasObj.transform.localScale = new Vector3(1f / canvasWidth, 1f / canvasHeight, 1f);

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5;

        RectTransform canvasRt = canvasObj.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(canvasWidth, canvasHeight);

        // --- 1. Başlık Alanı ---
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(canvasObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -22f);
        titleRt.sizeDelta = new Vector2(760f, 120f);

        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "DEPREM ANINDA HAYATTA KALMA REHBERİ";
        titleTmp.fontSize = 34;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;
        titleTmp.enableWordWrapping = true;

        // Alt etiket
        GameObject subtitleObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subtitleObj.transform.SetParent(canvasObj.transform, false);
        RectTransform subRt = subtitleObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.5f, 1f);
        subRt.anchorMax = new Vector2(0.5f, 1f);
        subRt.pivot = new Vector2(0.5f, 1f);
        subRt.anchoredPosition = new Vector2(0f, -112f);
        subRt.sizeDelta = new Vector2(760f, 32f);

        TextMeshProUGUI subTmp = subtitleObj.GetComponent<TextMeshProUGUI>();
        subTmp.text = "AFAD & AKUT BİLGİLENDİRME TALİMATNAMESİ";
        subTmp.fontSize = 18;
        subTmp.fontStyle = FontStyles.Bold;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.color = new Color(1f, 0.85f, 0.35f, 1f);

        // --- 2. Maddeler Alanı ---
        GameObject itemsObj = new GameObject("ItemsText", typeof(RectTransform), typeof(TextMeshProUGUI));
        itemsObj.transform.SetParent(canvasObj.transform, false);
        RectTransform itemsRt = itemsObj.GetComponent<RectTransform>();
        itemsRt.anchorMin = new Vector2(0.5f, 0.5f);
        itemsRt.anchorMax = new Vector2(0.5f, 0.5f);
        itemsRt.pivot = new Vector2(0.5f, 0.5f);
        itemsRt.anchoredPosition = new Vector2(50f, 20f);
        itemsRt.sizeDelta = new Vector2(660f, 680f);

        TextMeshProUGUI itemsTmp = itemsObj.GetComponent<TextMeshProUGUI>();
        // Metin formatı ve aralıkları
        itemsTmp.text = "<color=#D92626><size=32><b>[X] Merdivenlere ve asansöre KOŞMA!</b></size></color>\n" +
                        "<size=19><color=#443834>Sarsıntı anında merdivenler rezonansla anında yıkılabilir, asansörler kilitlenir.</color></size>\n\n\n" +
                        "<color=#D92626><size=32><b>[X] Pencere ve balkonlardan UZAK DUR!</b></size></color>\n" +
                        "<size=19><color=#443834>Camlar patlayarak ölümcül şarapnel etkisi yaratır, balkonlar dayanıksızdır.</color></size>\n\n\n" +
                        "<color=#16943C><size=32><b>[+] Sağlam bir nesnenin yanına ÇÖK - KAPAN - TUTUN!</b></size></color>\n" +
                        "<size=19><color=#443834>Masa veya sağlam baza yanında cenin pozisyonu al, başını ve enseni koru!</color></size>";
        itemsTmp.fontSize = 24;
        itemsTmp.alignment = TextAlignmentOptions.Left;
        itemsTmp.color = new Color(0.15f, 0.12f, 0.12f, 1f);
        itemsTmp.enableWordWrapping = true;

        // --- 3. Alt Vurucu Yazı ---
        GameObject quoteObj = new GameObject("QuoteText", typeof(RectTransform), typeof(TextMeshProUGUI));
        quoteObj.transform.SetParent(canvasObj.transform, false);
        RectTransform quoteRt = quoteObj.GetComponent<RectTransform>();
        quoteRt.anchorMin = new Vector2(0.5f, 0f);
        quoteRt.anchorMax = new Vector2(0.5f, 0f);
        quoteRt.pivot = new Vector2(0.5f, 0f);
        quoteRt.anchoredPosition = new Vector2(0f, 22f);
        quoteRt.sizeDelta = new Vector2(760f, 95f);

        TextMeshProUGUI quoteTmp = quoteObj.GetComponent<TextMeshProUGUI>();
        quoteTmp.text = "\"O gün binlerce insan bana bi şey olmaz diye uyudu.\"";
        quoteTmp.fontSize = 25;
        quoteTmp.fontStyle = FontStyles.Italic;
        quoteTmp.alignment = TextAlignmentOptions.Center;
        quoteTmp.color = new Color(1f, 0.92f, 0.78f, 1f);
        quoteTmp.enableWordWrapping = true;
    }

    #region Prosedürel Çizim Yardımcıları
    private static void FillRect(Color32[] pixels, int texW, int texH, int x, int y, int w, int h, Color32 color)
    {
        int xEnd = Mathf.Min(x + w, texW);
        int yEnd = Mathf.Min(y + h, texH);
        for (int py = Mathf.Max(0, y); py < yEnd; py++)
        {
            int row = py * texW;
            for (int px = Mathf.Max(0, x); px < xEnd; px++)
            {
                pixels[row + px] = color;
            }
        }
    }

    private static void DrawBorderRect(Color32[] pixels, int texW, int texH, int x, int y, int w, int h, int thickness, Color32 color)
    {
        // Üst ve Alt
        FillRect(pixels, texW, texH, x, y, w, thickness, color);
        FillRect(pixels, texW, texH, x, y + h - thickness, w, thickness, color);
        // Sol ve Sağ
        FillRect(pixels, texW, texH, x, y, thickness, h, color);
        FillRect(pixels, texW, texH, x + w - thickness, y, thickness, h, color);
    }

    private static void DrawHLine(Color32[] pixels, int texW, int texH, int x, int y, int length, int thickness, Color32 color)
    {
        FillRect(pixels, texW, texH, x, y, length, thickness, color);
    }

    private static void DrawHazardBar(Color32[] pixels, int texW, int texH, int x, int y, int w, int h, Color32 col1, Color32 col2)
    {
        int stripeW = 14;
        for (int py = y; py < y + h && py < texH; py++)
        {
            for (int px = x; px < x + w && px < texW; px++)
            {
                bool isStripe = ((px + py) / stripeW) % 2 == 0;
                pixels[py * texW + px] = isStripe ? col1 : col2;
            }
        }
    }

    private static void DrawSymbolCross(Color32[] pixels, int texW, int texH, int cx, int cy, int size, int thickness, Color32 color)
    {
        int half = size / 2;
        int tHalf = thickness / 2;
        for (int i = -half; i <= half; i++)
        {
            for (int t = -tHalf; t <= tHalf; t++)
            {
                SetPixelSafe(pixels, texW, texH, cx + i, cy + i + t, color);
                SetPixelSafe(pixels, texW, texH, cx + i, cy - i + t, color);
            }
        }
    }

    private static void DrawSymbolCheck(Color32[] pixels, int texW, int texH, int cx, int cy, int size, int thickness, Color32 color)
    {
        int tHalf = thickness / 2;
        // Sol kısa bacak (-12, -4) -> (0, -14)
        for (int i = -14; i <= 0; i++)
        {
            int py = cy + (i + 14) - 10;
            int px = cx + i;
            for (int t = -tHalf; t <= tHalf; t++)
            {
                SetPixelSafe(pixels, texW, texH, px, py + t, color);
            }
        }
        // Sağ uzun bacak (0, -14) -> (16, 14)
        for (int i = 0; i <= 18; i++)
        {
            int py = cy + (i * 24 / 18) - 10;
            int px = cx + i;
            for (int t = -tHalf; t <= tHalf; t++)
            {
                SetPixelSafe(pixels, texW, texH, px, py + t, color);
            }
        }
    }

    private static void DrawStairsIcon(Color32[] pixels, int texW, int texH, int cx, int cy, int size, Color32 color)
    {
        int steps = 4;
        int stepW = size / steps;
        int stepH = size / steps;
        int startX = cx - size / 2;
        int startY = cy - size / 2;

        for (int i = 0; i < steps; i++)
        {
            FillRect(pixels, texW, texH, startX + i * stepW, startY, stepW, (i + 1) * stepH, color);
        }
    }

    private static void DrawWindowIcon(Color32[] pixels, int texW, int texH, int cx, int cy, int size, Color32 color)
    {
        int half = size / 2;
        DrawBorderRect(pixels, texW, texH, cx - half, cy - half, size, size, 3, color);
        // Artı çerçevesi
        FillRect(pixels, texW, texH, cx - 1, cy - half, 2, size, color);
        FillRect(pixels, texW, texH, cx - half, cy - 1, size, 2, color);
    }

    private static void DrawTableTriangleIcon(Color32[] pixels, int texW, int texH, int cx, int cy, int size, Color32 color)
    {
        int half = size / 2;
        // Masa üst tablası
        FillRect(pixels, texW, texH, cx - half, cy + half - 6, size, 6, color);
        // Sol ayak
        FillRect(pixels, texW, texH, cx - half + 2, cy - half, 5, size - 6, color);
        // Sağ ayak
        FillRect(pixels, texW, texH, cx + half - 7, cy - half, 5, size - 6, color);
    }

    private static void SetPixelSafe(Color32[] pixels, int texW, int texH, int x, int y, Color32 color)
    {
        if (x >= 0 && x < texW && y >= 0 && y < texH)
        {
            pixels[y * texW + x] = color;
        }
    }
    #endregion
}
