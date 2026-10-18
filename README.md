# Deprem Farkındalık Oyunu - Senaryo, Diyalog ve Tasarım Dokümanı

Bu doküman, oyunun baştan sona tüm hikaye akışını, karakter diyaloglarını, zaman döngüsü (Time Loop) mekaniklerini ve sahne yapısını içermektedir.

---

## 📌 Genel Bakış ve Temel Fikir

* **Tema:** Deprem Farkındalığı & Afet Öncesi Hazırlık.
* **Ana Karakter:** Mert (Oyuncu).
* **Yan Karakterler:** Arkadaş 1 (`male01_2`), Arkadaş 2 (`male02_2`), AFAD Arama Kurtarma Ekipleri.
* **Merkezi Mesaj:** *"O gün 'bana bir şey olmaz' demeyenler kurtuldu. Farkındalık afetten önce başlar."*
* **Çekirdek Mekanik:** Zaman Döngüsü (Time Loop). Oyuncu gerekli afet hazırlığını yapmaz veya yanlış kararlar verirse bina çöker ve aynı güne (Saat 20:00 - Kahve dökme uyanışına) geri döner. Sadece doğru hazırlık, doğru yönlendirme ve doğru refleksler ile enkaz altından kurtulabilir.

---

## 🎬 1. BÖLÜM: KABUS / ÖNGÖRÜ (PREMONITION)

* **Sahne:** `Assets/Scenes/Intro.unity`
* **Yönetici:** `IntroSequenceController.cs`
* **Karakterler:** Mert (Sen), Arkadaş 1, Arkadaş 2

### Akış ve Diyaloglar:

1. **Giriş (Sohbet):**  
   Ekran siyahtan yavaşça açılır (`FadeFromBlack`). Üç arkadaş oturma odasında masanın etrafında oturmaktadır.
   * **Arkadaş 1:** *"Harbiden dünkü parti çok iyiydi!"*
   * **Arkadaş 2:** *"Evet ya, efsaneydi."*
   * **Mert (Altyazı / Eylem):** *(Kahvesinden bir yudum alır...)*
   * **Arkadaş 1:** *"Mert harbi ya, sen dün gece neydin öyle!"*
   * **Mert:** *"Eyvallah, siz de fena değildiniz..."*

2. **Deprem Başlar (Event 1):**  
   Ekran şiddetle sallanmaya başlar, nesneler devrilir, deprem uğultusu duyulur.
   * **Arkadaş 1:** *"Lan! Masa sallanıyor, deprem oluyor!"*

3. **Panik ve Kaçış:**  
   Karakterler panik içinde ayağa fırlar ve kapıya doğru koşmaya başlar.
   * **Arkadaş 2:** *"Çabuk dışarı koş!!"*

4. **Bina Çöker (Event 2):**  
   Ekran yavaşça kararırken bina büyük bir gümbürtüyle fiziksel olarak çöker.
   * **Ekran Kararır:** Zifiri karanlık, uzaktan boğuk sirenler ve çığlıklar yankılanır.
   * **Ekran Mesajı:**  
     > **[ Enkaz Altı ]**  
     > *"Uzaktan boğuk siren sesleri, AFAD çağrıları ve çığlıklar duyuluyor..."*

---

## 🎬 2. BÖLÜM: ZAMAN DÖNGÜSÜ & HAZIRLIK (EV VE MARKET)

* **Sahne:** `Assets/Scenes/EvMarket.unity`
* **Yönetici:** `EvMarketStoryManager.cs`
* **Karakterler:** Mert, Arkadaş 1, Arkadaş 2, Dışarıdaki Sokak & Market

---

### **Stage 0: Kahve Dökme Uyanışı (Saat 20:00)**
* Mert kabustan sıçrayarak uyanır. Elindeki kahve kupası dökülür. Duvardaki saat **20:00**'ı gösterir.
  * **Arkadaş 2:** *"Mert iyi misin? Az önce kahveni üzerine döktün!"*
  * **Mert:** *"Bi an daldım... Üstümü silip geleyim."*
  * **Görev:** *"Aşağı mutfağa ilerle (Mutfak girişindeki deprem posterine bak)."*

---

### **Stage 1: Mutfağa İniş ve Postere İlk Bakış**
* Oyuncu alt kattaki mutfağa iner. Duvarda anneannesinin gönderdiği **"Deprem Farkındalık Posteri"** asılıdır.
* Postere yaklaşıp `[E]` ile inceler:
* **Flashback Anı:** Ekran kızıl darbeyle titrer, saat anlık olarak **21:01** olur, yıkılan evin sesleri duyulur.
  * **Ekran Mesajı:** **SAAT 21:01** *(Aklına yıkılan ev ve bağrışmalar gelir...)*
  * **Mert:** *"Merdiven.. eğilme.. kapanma.. pencere..."*
* Saat tekrar **20:00**'a döner:
  * **Mert:** *(Saate bakar: 20:00)* *"Ya gerçekleşirse? Anneannem de böyle derdi bana, bu posteri o göndermişti..."*

---

### **Stage 2: Peçeteyi Alma**
* Mutfak tezgahındaki peçeteye yaklaşıp `[E]` ile alır.
  * **Mert:** *"Peçeteyi aldım. Arkadaşların yanına gidip onları uyarmalıyım."*
  * **Görev:** *"Yukarı çık ve arkadaşlarının yanına git."*

---

### **Stage 3: Arkadaşların Yanına Dönüş & Müteahhit Konuşması**
* Mert yukarı çıkar ve arkadaşlarına durumu anlatmaya çalışır:
  * **Mert:** *"İçimde kötü bi his var, sanki deprem çok yakın... Parkta mı otursak?"*
  * **Arkadaş 1:** *"Oğlum senin yükselenin İkizler değil mi? Sabah Twitter'da gördüm, Merkür gerilemen varmış aşırı paranoyaklık yaparmış, dinlen bence."*
  * **Arkadaş 2:** *"Kanka birazdan maç başlayacak, kola ve cips alıp gelsene hem hava al."*
  * **Mert:** *"Tamam..."*
* Mert merdivenlerden aşağı inerken arkadaşlarının arkasından yaptığı konuşmayı duyar:
  * **Arkadaş 1:** *"Müteahhit dedi zaten bu bina 9'a bile dayanır test etmişler! Hem binada oğlu oturuyor, yoksa oturtur muydu burda?"*
  * **Arkadaş 2:** *"Aynen kanka bence de."*

---

### **Stage 4: Postere 2. Bakış & Geri Sayımın Başlaması**
* Merdivenin başındaki postere tekrar bakar:
  * **Posterdeki Vurucu Yazı:**  
    > **DEPREM ANINDA HAYATTA KALMA REHBERİ**  
    > ✗ Merdivenlere ve asansöre KOŞMA!  
    > ✗ Pencere ve balkonlardan UZAK DUR!  
    > ✓ Sağlam bir nesnenin yanına ÇÖK - KAPAN - TUTUN!  
    > **"O gün binlerce insan bana bi şey olmaz diye uyudu."**
* Mert gerçeğin farkına varır:
  * **Mert:** *"O ana az kaldı, ya gerçekse harekete geçmeliyim!"*
  * *(Sağ üst köşede kırmızı **01:30 (90 Saniye)** sayacı işlemeye başlar!)*
  * **Mert:** *"Markete gitmeliyim ve düdük, su, fener, konserve, ilk yardım kiti, battaniye almalıyım. Hızlı olmalıyım az kaldı!"*

---

### **Stage 5 & 6: Sokak, Market (Deprem Çantası Bulmacası) ve Eve Dönüş**
* Apartmandan sokağa çıkar. Sokağın ortasındaki Markete girer (`[E]`).
* **Tetris Çantası Arayüzü:** 6x6'lık ızgara çantaya kritik eşyalar yerleştirilir:
  * **Gerekli Deprem Eşyaları:** Su (5L), **Deprem Düdüğü**, **El Feneri**, İlk Yardım Kiti, Konserve Paketi, Uyku Tulumu/Battaniye.
  * **Tuzak Eşyalar:** Kola ve Cips Paketi, Oyun Konsolu, Buharlı Ütü.
* Alışveriş bitince süre dolmadan apartmana koşar:
  * **Mert:** *"Eşyaları aldım! Deprem olmadan acilen apartmana dönüp arkadaşların yanına çıkmalıyım!"*

---

### **Stage 7: Saat 21:00 Depremi & Kritik Karar Anı**
* Mert arkadaşlarının yanına yetiştiğinde saat tam **21:00** olur:
  * **Mert:** *"Yetiştim! Saat tam 21:00..."*
* **BÜYÜK DEPREM BAŞLAR!** Bina şiddetle sarsılır:
  * **Arkadaş 1:** *"DEPREM OLUYOR! ÇILDIRACAĞIM, BİNA ÇOK FENA SALLANIYOR!"*
  * **Arkadaş 2:** *"MERDİVENLERE Mİ KOŞALIM YOKSA PENCEREYE Mİ?! NE YAPACAĞIZ?!"*
* **Karar Ekranı:**
  * `[1]` *"Pencere kenarına / Merdivenlere koşun!"*
  * `[2]` *(Doğru Seçenek)* *"Sağlam masanın yanına ÇÖK - KAPAN - TUTUN!"*

---

### ⚠️ ZAMAN DÖNGÜSÜ BAŞARISIZLIK (FAIL / LOOP) YOLLARI

1. **SENARYO A (Süre Bitti / Markete Gidilmedi):**  
   * 90 saniye biter ve eve yetişemezse:  
   * 💥 **"Molozların altında arkadaşların kurtulamadı... (Zamanında hazırlık yapıp eve yetişemedin)"**  
   * 🔄 *Döngü:* Ekran kararır, Saat 20:00 kahve dökme anından tekrar uyanır.

2. **SENARYO B (Arkadaşlara Yanlış Talimat - Pencere/Merdiven):**  
   * Seçenek `[1]` seçilirse arkadaşlar merdivene koşar:  
   * **Arkadaş 1:** *"Merdivenlere koşuuun!!"*  
   * Merdiven çöker!  
   * 💥 **"Merdivenler ve pencere kenarı çöktü! Arkadaşların kurtulamadı..."**  
   * 🔄 *Döngü:* Saat 20:00'dan uyanır.

3. **SENARYO C (Kendisi Masanın Yanına Çömelmezse):**  
   * Arkadaşlarına masayı söyler (`"Tamam, masanın yanına çöküyoruz!"`) fakat kendisi 12 saniye içinde masanın yanına geçip `[E]` / Çömelme ile kapanmazsa:  
   * 💥 **"Evin tavanı çöktü! Masanın yanına Çök-Kapan-Tutun yapmadığın için kurtulamadın..."**  
   * 🔄 *Döngü:* Saat 20:00'dan uyanır.

4. **SENARYO D (Çantaya Fener veya Düdük Alınmadıysa):**  
   * Çantaya fener veya düdük konulmadıysa enkaz altında uyanır:  
   * 💥 **"Enkaz altında karanlıkta kaldın ve sesini AFAD ekiplerine duyuramadın... (Deprem çantanda Fener ve Düdük yoktu!)"**  
   * 🔄 *Döngü:* Saat 20:00'dan uyanır.

---

### **Doğru Karar ve Uygulama:**
* Mert masanın yanına cenin pozisyonunda kapanır:
  * **Mert:** *"Masanın yanına cenin pozisyonunda kapandım, başımı koruyorum!"*
* Tavan büyük bir gürültüyle çöker (`GameEvents.TriggerEvent2()`). Ekran kararır:
  > **[ Enkaz Altı ]**  
  > *"Masanın oluşturduğu yaşam üçgeninde hayattasınız...\nUzaktan arama kurtarma sesleri duyuluyor..."*
* Final sahnesine (`End_UnderRubble.unity`) geçilir.

---

## 🎬 3. BÖLÜM: FİNAL - ENKAZ ALTI & AFAD KURTARIŞI

* **Sahne:** `Assets/Scenes/End_UnderRubble.unity`
* **Yönetici:** `UnderRubbleSceneManager.cs`
* **Karakterler:** Mert, Baygın Arkadaşlar (Yaşam Üçgeninde), AFAD Ekipleri

### Akış ve Diyaloglar:

1. **Uyanış:**  
   Ekran loş enkaz ortamına açılır. Karakter çift katmanlı beton bloklarla kapalı, gökyüzünün görünmediği dar yaşam üçgeninde ayaktadır. Fener kapalıdır.
   * **Mert:** *"Gözlerimi açtım... Zifiri karanlık, molozların altındayız. Feneri bulmalıyım!"*
   * **Yönlendirme:** `[F] El Fenerini Aç`

2. **Fener Açılır:**  
   Oyuncu `[F]` tuşuna basar. Fener beton blokları ve molozları aydınlatır.
   * **Mert:** *"Yaşam üçgeni bizi korumuş ama tavan her an çökebilir... Bir ses çıkarmam lazım!"*
   * 10 saniye boyunca oyuncu dar alanda etrafı inceler. Fener açılmadan AFAD asla gelmez.

3. **AFAD Megafon Seslenmesi:**  
   10 saniye dolunca yukarıdaki molozların arasından megafon sesi gelir:
   * **AFAD Arama Kurtarma:** *"AŞAĞIDA KİMSE VAR MI?! SESİMİ DUYAN VARSA SES VERSİN YA DA BİR YERE VURSUN!"*
   * **Yönlendirme:** `[E] Deprem Düdüğünü Çal (AFAD'a Sesini Duyur)`

4. **Düdük Çalma & Kurtarılış:**  
   Oyuncu `[E]` tuşuna basar. Düdük sesi yankılanır (`TİİİİTTT! TİİİİTTT!`):
   * **Mert:** *"BURADAYIZ! ENKAZIN ALTINDAYIZ!"*
   * **AFAD Görevlisi:** *"SESİ ALDIK! TAM BURADALAR! EKİP BURAYA GELSİN, ÇIKARIYORUZ!"*

5. **Sinematik Tepe Çekimi & Kapanış:**  
   * Beyaz arama kurtarma projektörü moloz deliklerinden içeri vurur.
   * Karakter dondurulur.
   * Kamera tavandaki çatlaktan gökyüzüne süzülür (`GlideCameraToOuttro`); 120 metrelik devasa yıkılmış şehir enkazı, sirenleri yanan polis/AFAD araçları ve moloz dağları yukarıdan kadraja girer.
   * Final farkındalık mesajı ekranda belirir:

> ### HAZIRLIK HAYAT KURTARIR
> **"O gün 'bana bir şey olmaz' demeyenler kurtuldu.**  
> **Doğru bilgi, hazırlık ve sakinlik hayatta tutar.**  
> **Farkındalık afetten önce başlar."**

---

## 🎮 Kontroller Özeti

| Tuş | Eylem |
|---|---|
| **W, A, S, D** | Yürüme / Hareket |
| **Mouse** | Bakış Açısı / Kamera Kontrolü |
| **E** | Etkileşim (İncele, Peçeteyi Al, Markete Gir, Kapıyı Aç, Düdük Çal) |
| **F** | El Fenerini Aç / Kapat |
| **1 / 2** | Deprem Anında Arkadaşları Yönlendirme Seçimleri |
| **C / Ctrl** | Çömelme (Masa Yanında Çök-Kapan-Tutun) |
