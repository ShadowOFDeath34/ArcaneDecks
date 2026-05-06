# ArcaneDecks: Goblin Siege — Uretim Ortami (Production) Kurulum Rehberi

> **Son guncelleme:** 2026-05-06
> **Durum:** Mac gerektirmeyen adimlar HAZIR (hemen uygulanabilir). Mac gerektiren adimlar BEKLEMEDE.

---

## 1. Genel Bakis ve Oncelik Sirasi

Bu rehber, ArcaneDecks: Goblin Siege'in alpha surumunu canli (production) ortama tasimak icin gereken TUM adimlari icerir. Adimlar, **Mac gerektiren / gerektirmeyen** seklinde ikiye ayrilmistir.

### 1.1 Mac Gerektirmeyen Adimlar (Hemen Yapilabilir)

| # | Gorev | Neden Gerekli | Tahmini Sure |
|---|---|---|---|
| 1 | Supabase production migration uygulama | Sezonluk etkinlik tablolari ve RLS guvenlik politikalarini veritabanina ekler | 15 dk |
| 2 | Vercel production ortam degiskenleri | Backend'in Sentry, PostHog, Supabase, JWT ile calismasini saglar | 10 dk |
| 3 | GitHub Secrets ayarlama | CI/CD pipeline'inin gizli anahtarlara erismesi gerekir | 10 dk |
| 4 | Git tag ile ilk alpha release olusturma | DesktopGL + Android build'lerini test etmek icin | 5 dk |

### 1.2 Mac Gerektiren Adimlar (Beklemede)

| # | Gorev | Neden Gerekli | Tahmini Sure |
|---|---|---|---|
| 5 | Apple Developer hesabi acma / satin alma | iOS uygulamasi yayinlamak ve imzalamak icin zorunlu | 1-2 gun |
| 6 | iOS imzalama sertifikasi olusturma | IPA dosyasinin Apple tarafindan kabul edilmesi icin | 30 dk |
| 7 | iOS Provisioning Profile olusturma | Hangi cihazlarda calisacagini belirler | 20 dk |
| 8 | GitHub Secrets'e Apple sertifikalari ekleme | CI pipeline'i otomatik imzalayabilsin | 15 dk |
| 9 | CI'da iOS IPA build testi | Gercek cihazda calisacak build uretimi | 20 dk |

---

## 2. Adim 1 — Supabase Production Migration Uygulama

**Mac gerektirmez.** Sadece web tarayici gerektirir.

### 2.1 Supabase Dashboard'a Giris

1. Tarayicinda [https://app.supabase.com](https://app.supabase.com) adresine git.
2. ArcaneDecks projesini sec.
3. Sol menuden **SQL Editor**'u sec.
4. Ustteki **New query** butonuna tikla.

### 2.2 Sezonluk Etkinlik Tablolarini Olusturma

1. `backend/supabase/migrations/0003_seasonal_events.sql` dosyasinin icerigini kopyala.
   - Bu dosyayi nasil bulursun: `ArcaneDecks/backend/supabase/migrations/` klasorunde `0003_seasonal_events.sql` adinda.
2. Kopyaladigin SQL kodunu Supabase SQL Editor'unun bos metin alanina yapistir.
3. Sag alttaki **Run** butonuna tikla.
4. Sonuc: `Success. No rows returned.` mesaji gormelisin.

### 2.3 Row Level Security (RLS) Politikalarini Etkinlestirme

1. `backend/supabase/migrations/20260506_enable_rls.sql` dosyasinin icerigini kopyala.
2. Yeni bir query olustur (tekrar **New query** butonu).
3. Kopyaladigin SQL kodunu yapistir.
4. **Run** butonuna tikla.

> **Onemli:** Bu adim RLS guvenlik katmanini aktiflestirir. Backend `service_role` key ile calistigi icin backend API'ler bu politikalarin disinda kalir. Ancak dogrudan Supabase client kutuphanesi kullanan bir eklenti yaparsan bu politikalar devreye girer.

### 2.4 Dogrulama

Migration basarili olduysa:

1. Sol menuden **Database** -> **Tables** sec.
2. Asagidaki tablolarin listelendigini kontrol et:
   - `seasonal_events`
   - `seasonal_event_entries`
   - `seasonal_event_claims`
3. `seasonal_events` tablosuna tikla.
4. Ustteki **Policies** sekmesine tikla.
5. `Allow read active events` politikasinin oldugunu kontrol et.

Eger tum bu kontrolleri gectiysen, **Adim 1 tamamlandi.**

---

## 3. Adim 2 — Vercel Production Ortam Degiskenleri

**Mac gerektirmez.** Sadece web tarayici gerektirir.

### 3.1 Gerekli Anahtarlarin Listesi

| Degisken Adi | Nereden Alinir | Ne Ise Yarar |
|---|---|---|
| `SENTRY_DSN` | [https://sentry.io](https://sentry.io) -> Proje Ayarlari -> DSN | Cokme raporlama |
| `POSTHOG_API_KEY` | [https://app.posthog.com](https://app.posthog.com) -> Proje -> Settings -> Project API Key | Analitik takip |
| `SUPABASE_URL` | Supabase Dashboard -> Proje -> Settings -> API -> URL | Veritabani baglantisi |
| `SUPABASE_SERVICE_ROLE_KEY` | Supabase Dashboard -> Proje -> Settings -> API -> service_role key | Server-side veritabani erisimi |
| `JWT_SECRET` | Kendin uret (asagida anlatiliyor) | JWT token imzalama |

### 3.2 JWT Secret Uretme

JWT_SECRET'i kendin uretmen gerekir. En guvenli yol:

1. [https://generate-random.org/api-token-generator](https://generate-random.org/api-token-generator) adresine git.
2. **512-bit** (64 karakter) secenegini sec.
3. Uretilen rastgele string'i bir not defterine kaydet.
4. Bu deger `JWT_SECRET` olacak.

> **GUVENLIK UYARISI:** JWT_SECRET hic kimseyle paylasilmamali. GitHub'a, e-postaya, sohbetlere yazilmamali. Sadece Vercel Dashboard'a girilmeli.

### 3.3 Vercel Dashboard'a Giris

1. [https://vercel.com/dashboard](https://vercel.com/dashboard) adresine git.
2. ArcaneDecks backend projesini sec.
3. Ust menuden **Settings** sekmesine tikla.
4. Sol menuden **Environment Variables** secenegini bul ve tikla.

### 3.4 Degisken Ekleme (Tek Tek)

Her bir degisken icin ayni akisi tekrarla:

1. **Name** kutusuna degisken adini yaz (ornegin `SENTRY_DSN`).
2. **Value** kutusuna gercek degeri yapistir.
3. **Environment** bolumunde sadece **Production** kutucugunu isaretle (Preview ve Development istege bagli).
4. **Save** butonuna tikla.

Bu islemi 5 degisken icin tekrarla:
- `SENTRY_DSN`
- `POSTHOG_API_KEY`
- `SUPABASE_URL`
- `SUPABASE_SERVICE_ROLE_KEY`
- `JWT_SECRET`

### 3.5 Yeniden Deploy Etme

Degiskenler eklendikten sonra:

1. Vercel Dashboard -> ArcaneDecks projesi -> **Deployments** sekmesi.
2. En son deployment'in yanindaki uc noktaya (...) tikla.
3. **Redeploy** secenegini sec.
4. Deployment basarili oldugunda yesil tik goreceksin.

> **Hata durumunda:** Eger deploy basarisiz olursa Vercel log'larina tiklayip hatayi okuyabilirsin. Genellikle eksik bir degisken veya yanlis `SUPABASE_SERVICE_ROLE_KEY` nedeniyle olur.

Eger deploy basarili olduysa, **Adim 2 tamamlandi.**

---

## 4. Adim 3 — GitHub Secrets Ayarlama

**Mac gerektirmez.** Sadece web tarayici gerektirir.

GitHub Actions CI pipeline'i, Sentry, PostHog, Supabase gibi servislere baglanmak icin gizli anahtarlara ihtiyac duyar. Bu anahtarlar GitHub repo'nun icinde **sifrelenmis** olarak tutulur.

### 4.1 GitHub Repo Ayarlari

1. Tarayicinda GitHub'a git: [https://github.com/MuhammedEnesMert/ArcaneDecks](https://github.com/MuhammedEnesMert/ArcaneDecks) (repo URL'ni kendi kullanici adina gore degistir).
2. Ust menuden **Settings** sekmesine tikla.
3. Sol menuden en alttan **Secrets and variables** -> **Actions** secenegini tikla.
4. **New repository secret** butonuna tikla.

### 4.2 Eklenecek Secret'lar

Asagidaki secret'lari tek tek ekle (Adim 2'deki ayni degerleri kullan):

| Secret Adi | Deger |
|---|---|
| `SENTRY_DSN` | Sentry DSN'in |
| `POSTHOG_API_KEY` | PostHog API Key'in |
| `SUPABASE_URL` | Supabase URL'in |
| `SUPABASE_SERVICE_ROLE_KEY` | Supabase service_role key'in |
| `JWT_SECRET` | Urettigin 64 karakterlik JWT secret |

Her biri icin:
1. **Name** kutusuna secret adini yaz.
2. **Secret** kutusuna degeri yapistir.
3. **Add secret** butonuna tikla.

### 4.3 Dogrulama

Tum secret'lar eklendiginde, GitHub Actions workflow'larinin calismasi gerekir.

1. Repo ana sayfasinda **Actions** sekmesine tikla.
2. Herhangi bir workflow calistir (ornegin `Test Game` workflow'unu manuel tetikle).
3. Eger tum yesil tik ise, secret'lar dogru calisiyor demektir.

Eger basarili olduysan, **Adim 3 tamamlandi.**

---

## 5. Adim 4 — Ilk Alpha Release Olusturma (Test)

**Mac gerektirmez.** Terminal veya GitHub web arayuzu ile yapilir.

Bu adim, DesktopGL ve Android build'lerinin CI uzerinden dogru calistigini test eder. iOS build bu asamada sadece simulator icindir.

### 5.1 Git Tag Olusturma

Terminal (PowerShell) uzerinden:

```powershell
cd C:\Users\Muhammed\Desktop\Game\ArcaneDecks
git tag -a v0.1.0-alpha -m "ArcaneDecks Alpha 0.1.0"
git push origin v0.1.0-alpha
```

> **Not:** Eger henuz git tag kullanmadiysan, `git tag` komutu yerel bir etiket olusturur. `git push origin v0.1.0-alpha` ile bu etiketi GitHub'a gonderirsin.

### 5.2 GitHub Actions'in Calismasini Izleme

1. GitHub repo -> **Actions** sekmesi.
2. `Release Alpha Build` workflow'u otomatik olarak baslamis olmali.
3. Uzerine tikla ve 4 job'un calismasini izle:
   - `build-desktopgl` (ubuntu-latest uzerinde)
   - `build-android` (windows-latest uzerinde)
   - `build-ios` (macos-latest uzerinde — simulator build)
   - `release` (ubuntu-latest uzerinde)

### 5.3 Release'i Kontrol Etme

Tum job'lar basarili oldugunda:

1. GitHub repo -> sag taraf **Releases** bolumu.
2. `v0.1.0-alpha` adli bir release gorunmeli.
3. Uzerine tikla.
4. **Assets** bolumunde uc ZIP dosyasi olmali:
   - `ArcaneDecks-DesktopGL-Win64.zip`
   - `ArcaneDecks-Android.zip`
   - `ArcaneDecks-iOS.zip`

### 5.4 DesktopGL Build'ini Test Etme

1. `ArcaneDecks-DesktopGL-Win64.zip`'i indir.
2. ZIP'i bir klasore ac.
3. `ArcaneDecks.DesktopGL.exe`'ye cift tikla.
4. Oyun aciliyorsa, **Adim 4 tamamlandi.**

> **Hata durumunda:** Eger `apiBaseUrl` nedeniyle baglanti hatasi verirse, `Game1.cs` icindeki `apiBaseUrl` degerini gecici olarak `localhost` yap ve tekrar derle. Production testi icin gercek Vercel URL'ni kullanman gerekir.

---

## 6. Mac Gerektiren Adimlar (Beklemede — Mac Bulunana Kadar)

Asagidaki adimlar **sadece iOS fiziksel cihaz build'i** icin gereklidir. DesktopGL ve Android build'ler yukaridaki adimlarla tamamen calisir durumdadir.

### 6.1 Adim 5 — Apple Developer Hesabi

Apple Developer hesabi iOS uygulamasi yayinlamak icin zorunludur. Maliyeti yilda **99 USD**'dir.

1. [https://developer.apple.com/programs/enroll/](https://developer.apple.com/programs/enroll/) adresine git.
2. Apple ID'n ile giris yap veya yeni bir Apple ID olustur.
3. Bireysel (Individual) veya sirket (Organization) secenegini sec.
4. Kisisel bilgilerini gir ve odeme yap.
5. Onay sureci 1-2 gun surer.

### 6.2 Adim 6 — iOS Imzalama Sertifikasi (Mac + Xcode Gerektirir)

> **Bu adim sadece macOS uzerinde yapilabilir.**

1. Mac bilgisayarina Xcode'u yukle (App Store veya [https://developer.apple.com/xcode/](https://developer.apple.com/xcode/)).
2. Xcode'u ac.
3. Menu -> **Xcode** -> **Preferences** -> **Accounts** sekmesi.
4. Apple ID'ni ekle.
5. Alttaki **Manage Certificates** butonuna tikla.
6. Sol alttaki **+** butonuna tikla -> **Apple Development** sec.
7. Sertifika olusturulduktan sonra export et.

### 6.3 Adim 7 — iOS Provisioning Profile (Mac Gerektirir)

1. [https://developer.apple.com/account/resources/profiles/list](https://developer.apple.com/account/resources/profiles/list) adresine git.
2. **Profiles** sekmesine tikla.
3. **+** butonuna tikla -> **iOS App Development** sec.
4. App ID olarak `com.phantomforge.arcandedecks` (veya senin Bundle ID'n) sec.
5. Olusturdugun sertifikayi sec.
6. Test cihazlarini (iPhone/iPad) ekle.
7. Profile'i indir ve `ArcaneDecks_iOS.mobileprovision` olarak kaydet.

### 6.4 Adim 8 — GitHub Secrets'e Apple Sertifikalari Ekleme

> **Bu adim Mac'te yapilan sertifikalarin base64'e cevrilmesi ile yapilir.**

Mac uzerinde terminal ac ve su komutlari calistir:

```bash
# Sertifikayi base64'e cevir
base64 -i Certificates.p12 -o cert_base64.txt

# Provisioning profile'i base64'e cevir
base64 -i ArcaneDecks_iOS.mobileprovision -o prov_base64.txt
```

Bu dosyalarin iceriklerini kopyala ve GitHub repo -> Settings -> Secrets -> Actions kisminda su secret'lari ekle:

| Secret Adi | Deger |
|---|---|
| `APPLE_CERTIFICATE` | `cert_base64.txt` icerigi |
| `APPLE_CERTIFICATE_PASSWORD` | Sertifika export ederken belirledigin sifre |
| `APPLE_PROVISION_PROFILE` | `prov_base64.txt` icerigi |

### 6.5 Adim 9 — CI'da iOS IPA Build Testi

Bu adim tamamen otomatiktir. GitHub Actions `build-ios` job'u zaten `macos-latest` runner kullaniyor. Sadece Apple sertifikalari eklendikten sonra:

1. Yeni bir Git tag olustur: `git tag -a v0.1.1-alpha -m "iOS IPA test"`
2. `git push origin v0.1.1-alpha`
3. GitHub Actions -> `Release Alpha Build` workflow'unu izle.
4. `build-ios` job'u artik `.ipa` dosyasi uretmeli.

---

## 7. Sik Karsilasilan Sorunlar ve Cozumleri

### 7.1 "NETSDK1085" Hatasi (Android Build)

**Sorun:** Android publish adiminda `NETSDK1085` hatasi.
**Cozum:** `build-game.yml` ve `release.yml`'de Android publish adimindan `--no-build` kaldirildi. Eger hala aliyorsan:
- `dotnet publish` komutundan `--no-build` bayragini kaldir.
- Ya da once `dotnet build` sonra `dotnet publish --no-build` kullan.

### 7.2 "JAVAC0000" Hatasi (Android AdMob)

**Sorun:** Android build sirasinda Java generics erasure hatasi.
**Cozum:** `AndroidAdService.cs` icinde `InterstitialLoadCallbackBase` abstract sinifi ve `JNINativeWrapper.CreateDelegate` kullanimi zaten cozuldu. Eger hala aliyorsan MonoGame veya Xamarin Google Play Services Ads surumunu guncelle.

### 7.3 Supabase RLS Etkinlestikten Sonra Client Baglanti Hatasi

**Sorun:** RLS aktif olduktan sonra dogrudan Supabase client'tan veri okuyamama.
**Cozum:** Bu beklenen bir durum. Backend `service_role` key ile calistigi icin API endpoint'leri RLS'den etkilenmez. Eger gelecekte mobil client tarafi Supabase client kutuphanesi ile dogrudan DB'ye baglanirsa, `anon` role icin uygun RLS politikalari zaten tanimlandi.

### 7.4 Vercel Deploy'da "Module not found" Hatasi

**Sorun:** Backend deploy edildiginde `sentry` veya `posthog` modul bulunamadi hatasi.
**Cozum:** `npm install` calismamis olabilir. Vercel Dashboard -> Deployment log'larina bak. Eger eksik modul varsa `backend/package.json`'i kontrol et ve `npm install` calistir.

### 7.5 JWT Token "invalid signature" Hatasi

**Sorun:** Client'tan gonderilen JWT token backend tarafindan reddediliyor.
**Cozum:**
1. Vercel'deki `JWT_SECRET` ile lokal `.env`'deki `JWT_SECRET` ayni mi kontrol et.
2. Secret en az 32 karakter mi kontrol et.
3. Vercel'i yeniden deploy et.

---

## 8. Kontrol Listesi (Kopyala — Yapistir — Isaretle)

Mac gerektirmeyen adimlar:

- [ ] **Adim 1:** Supabase migration `0003_seasonal_events.sql` uygulandi
- [ ] **Adim 1:** Supabase migration `20260506_enable_rls.sql` uygulandi
- [ ] **Adim 1:** `seasonal_events`, `seasonal_event_entries`, `seasonal_event_claims` tablolari olustu
- [ ] **Adim 2:** Vercel'e `SENTRY_DSN` eklendi
- [ ] **Adim 2:** Vercel'e `POSTHOG_API_KEY` eklendi
- [ ] **Adim 2:** Vercel'e `SUPABASE_URL` eklendi
- [ ] **Adim 2:** Vercel'e `SUPABASE_SERVICE_ROLE_KEY` eklendi
- [ ] **Adim 2:** Vercel'e `JWT_SECRET` eklendi
- [ ] **Adim 2:** Vercel production yeniden deploy edildi
- [ ] **Adim 3:** GitHub Secrets'e tum anahtarlar eklendi
- [ ] **Adim 4:** `v0.1.0-alpha` tag'i push edildi
- [ ] **Adim 4:** GitHub Actions release workflow'u basarili tamamlandi
- [ ] **Adim 4:** DesktopGL build'i localde acilip test edildi

Mac gerektiren adimlar (gelecekte):

- [ ] **Adim 5:** Apple Developer hesabi satin alindi
- [ ] **Adim 6:** iOS Development sertifikasi olusturuldu
- [ ] **Adim 7:** iOS Provisioning Profile indirildi
- [ ] **Adim 8:** GitHub Secrets'e `APPLE_CERTIFICATE`, `APPLE_CERTIFICATE_PASSWORD`, `APPLE_PROVISION_PROFILE` eklendi
- [ ] **Adim 9:** CI uzerinden iOS IPA build testi basarili

---

## 9. Sonraki Asamalar (Beta ve Otesi)

Alpha kurulumu tamamlandiktan sonra planlanan adimlar:

1. **Beta 0.2.0:** Ek kartlar (40+ kart hedefi), yeni dusmanlar, ses efektleri.
2. **Beta 0.3.0:** Steam basarimlari (Steamworks.NET basarim entegrasyonu tamamlanacak).
3. **Beta 0.4.0:** Google Play Store ve App Store'a gonderim hazirligi (store asset'leri, ekran goruntuleri, aciklama metni).
4. **1.0.0 Lansman:** Steam + Google Play + App Store ayni anda.

---

> **Bu rehberi kullanirken takildigin bir yer olursa, hata mesajini tam olarak kopyalip bana gonder.** Hata mesajlari genellikle cozumun %80'ini icerir.
