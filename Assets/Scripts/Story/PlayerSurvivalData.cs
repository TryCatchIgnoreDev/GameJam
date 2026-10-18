using System.Collections.Generic;

/// <summary>
/// Sahneler arası (EvMarket -> UnderRubble) oyuncunun hayatta kalma ve envanter durumunu taşıyan statik veri sınıfı.
/// Deprem çantasının 6 hayati öğesi:
/// 1. El Feneri (flashlight)
/// 2. Deprem Düdüğü (whistle)
/// 3. Su (water_jug)
/// 4. İlk Yardım Kiti (first_aid)
/// 5. Uyku Tulumu & Termal Battaniye (sleeping_bag)
/// 6. Dayanıklı Konserve Gıda (canned_food)
/// </summary>
public static class PlayerSurvivalData
{
    // Alışveriş tamamlandı mı?
    public static bool HasCompletedShopping = false;

    // 6 Hayati Deprem Çantası Öğesi
    public static bool HasFlashlight = false;
    public static bool HasWhistle = false;
    public static bool HasWater = false;
    public static bool HasFirstAid = false;
    public static bool HasBlanket = false; // sleeping_bag
    public static bool HasFood = false;    // canned_food

    // Çantaya konan toplam hayati eşya sayısı (Maks: 6)
    public static int EssentialItemCount = 0;

    // Masa başında arkadaşlar korundu mu?
    public static bool FriendsSavedAtTable = false;

    // Çantaya yerleştirilen tüm eşya ID'lerinin kümesi
    public static HashSet<string> PlacedItemIds = new HashSet<string>();

    /// <summary>
    /// Çantada 6 hayati eşyanın hepsi eksiksiz bulunuyor mu?
    /// Biri bile eksikse false döner.
    /// </summary>
    public static bool HasAllEssentialItems()
    {
        return HasFlashlight && HasWhistle && HasWater && HasFirstAid && HasBlanket && HasFood;
    }

    /// <summary>
    /// Çantada eksik olan hayati eşyaların Türkçe adlarını liste olarak döndürür.
    /// </summary>
    public static List<string> GetMissingEssentialItemNames()
    {
        List<string> missing = new List<string>();
        if (!HasFlashlight) missing.Add("El Feneri");
        if (!HasWhistle) missing.Add("Deprem Düdüğü");
        if (!HasWater) missing.Add("İçme Suyu (5L / Şişe)");
        if (!HasFirstAid) missing.Add("İlk Yardım Kiti");
        if (!HasBlanket) missing.Add("Uyku Tulumu / Termal Battaniye");
        if (!HasFood) missing.Add("Dayanıklı Konserve Gıda");
        return missing;
    }

    /// <summary>
    /// Öncelik sırasına göre eksik olan ilk kritik eşyanın ID'sini döndürür.
    /// Sıralama: Fener -> Düdük -> Su -> İlk Yardım -> Battaniye -> Konserve
    /// </summary>
    public static string GetPrimaryMissingItemId()
    {
        if (!HasFlashlight) return "flashlight";
        if (!HasWhistle) return "whistle";
        if (!HasWater) return "water_jug";
        if (!HasFirstAid) return "first_aid";
        if (!HasBlanket) return "sleeping_bag";
        if (!HasFood) return "canned_food";
        return null;
    }

    /// <summary>
    /// Belirli bir eksik eşya ID'sine göre dramatik ölüm / kayıp gerekçesini döndürür.
    /// </summary>
    public static string GetDeathReasonForMissingItem(string missingItemId)
    {
        switch (missingItemId)
        {
            case "flashlight":
                return "Deprem çantanda EL FENERİ olmadığı için zifiri karanlıkta hiçbir şey göremedin. Molozların altında sıkışıp kaldın ve kurtarma ekiplerine ışıkla yerini belli edemedin...";

            case "whistle":
                return "Deprem çantanda DEPREM DÜDÜĞÜ olmadığı için boğazın toz ve molozla dolduğunda sesini AFAD ekiplerine duyuramadın. Ekipler ses alamayınca diğer enkaza geçti ve unutuldun...";

            case "water_jug":
                return "Deprem çantanda İÇME SUYU olmadığı için enkaz altında geçen saatler boyunca susuz kaldın. Şiddetli dehidrasyon ve böbrek yetmezliği nedeniyle kurtarma ekipleri sana ulaşamadan bilincini yitirdin...";

            case "first_aid":
                return "Binanın çöküşü sırasında düşen moloz ve cam parçalarıyla ağır yaralanmıştın. Deprem çantanda İLK YARDIM KİTİ bulunmadığı için kanamayı durduramadın ve aşırı kan kaybından yaşamını yitirdin...";

            case "sleeping_bag":
                return "Gece enkaz altında hava sıcaklığı sıfırın altına düştü. Deprem çantanda UYKU TULUMU veya TERMAL BATTANİYE olmadığı için dondurucu ayaza dayanamadın ve şiddetli hipotermiden hayatını kaybettin...";

            case "canned_food":
                return "Günler süren zorlu kurtarma çalışmasında açlık ve enerji kaybına yenik düştün. Deprem çantanda DAYANIKLI KONSERVE GIDA bulunmadığı için kan şekerin ölümcül seviyeye indi ve direncin tükendi...";

            default:
                return "Deprem çantandaki hayati eşyalar eksik olduğu için enkaz altında hayatta kalamadın...";
        }
    }

    /// <summary>
    /// Eksik olan ilk kritik eşyaya dayalı birincil dramatik ölüm gerekçesi.
    /// </summary>
    public static string GetPrimaryDeathReason()
    {
        string primaryId = GetPrimaryMissingItemId();
        if (string.IsNullOrEmpty(primaryId))
        {
            return "Deprem çantandaki eksiklikler nedeniyle enkaz altında hayatta kalamadın...";
        }
        return GetDeathReasonForMissingItem(primaryId);
    }

    /// <summary>
    /// Bir zaman döngüsü (Time Loop) yeniden başladığında verileri sıfırlar.
    /// </summary>
    public static void ResetData()
    {
        HasCompletedShopping = false;
        HasFlashlight = false;
        HasWhistle = false;
        HasWater = false;
        HasFirstAid = false;
        HasBlanket = false;
        HasFood = false;
        EssentialItemCount = 0;
        FriendsSavedAtTable = false;
        PlacedItemIds.Clear();
    }
}
