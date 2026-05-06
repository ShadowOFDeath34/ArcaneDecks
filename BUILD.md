# ArcaneDecks: Goblin Siege — Build Rehberi

## Platform Derleme Talimatlari

### DesktopGL (Windows / macOS / Linux)

```bash
dotnet publish Platforms/ArcaneDecks.DesktopGL/ArcaneDecks.DesktopGL.csproj \
  -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true \
  -o publish/desktopgl
```

Cikti: `publish/desktopgl/ArcaneDecks.DesktopGL.exe`

**Icerik (Content) Yeniden Derleme:**
MonoGame icerik dosyalari (`Content/*.spritefont`, `Content/*.png`) proje derlenirken otomatik olarak `Content.mgcb` uzerinden `bin/DesktopGL/` altina `.xnb` olarak cevrilir. Eger icerik dosyalarinda degisiklik yapildiysa, eski `.xnb` dosyalarini silip projeyi yeniden derlemek gerekir.

### Android (APK / AAB)

```bash
dotnet publish Platforms/ArcaneDecks.Android/ArcaneDecks.Android.csproj \
  -c Release -f net9.0-android -o publish/android
```

Cikti: `publish/android/ArcaneDecks.Android-Signed.aab` (AAB)

Imzali APK icin:
```bash
dotnet publish ... -p:AndroidPackageFormat=apk
```

### iOS (IPA)

> **Kritik Sinirlama:** iOS IPA paketi **sadece macOS + Xcode** ile uretilebilir. Windows veya Linux uzerinde `dotnet publish -r ios-arm64` calistirildiginda yalnizca iOS Simulator derlemesi (`iossimulator-x64`) uretilir. Fiziksel cihazlar icin gerekli olan `.ipa` dosyasi olusmaz.

**macOS Gereksinimleri:**
- macOS 13+ (Ventura veya daha yenisi)
- Xcode 15+ (veya .NET 9 ile uyumlu en son surum)
- .NET 9 SDK
- `ios` is yuku (workload): `dotnet workload install ios`

**macOS Uzerinde Derleme:**
```bash
dotnet publish Platforms/ArcaneDecks.iOS/ArcaneDecks.iOS.csproj \
  -c Release -f net9.0-ios -r ios-arm64 \
  -p:CodesignKey="Apple Development: YOUR_NAME" \
  -p:CodesignProvision="YOUR_PROFILE" \
  -o publish/ios
```

Cikti: `publish/ios/ArcaneDecks.iOS.ipa`

**GitHub Actions ile macOS Uzerinden Derleme:**
`.github/workflows/build-game.yml` icinde `build-ios` jobu zaten `macos-latest` runner kullaniyor. CI/CD pipeline'inda iOS derlemesi icin gerekli olan `APPLE_CERTIFICATE` ve `APPLE_PROVISION_PROFILE` gizli anahtarlarinin GitHub Secrets'e eklenmesi gerekir.

### Testler

```bash
dotnet test Shared/ArcaneDecks.Core.Tests/ArcaneDecks.Core.Tests.csproj
```

## Ortam Degiskenleri (Vercel / GitHub Secrets / Local .env)

Tum ortam degiskenleri `backend/.env.example` dosyasinda sablon olarak mevcuttur. Uretim ortaminda gercek degerlerle doldurulmalidir.

- `SENTRY_DSN` — Sentry crash reporting icin gerekli. Sentry proje ayarlarindan DSN alinir.
- `POSTHOG_API_KEY` — PostHog oyun ici analitikleri icin gerekli. PostHog proje ayarlarindan API key alinir.
- `SUPABASE_URL` / `SUPABASE_SERVICE_ROLE_KEY` — Backend veritabani baglantisi
- `JWT_SECRET` — JWT token imzalama anahtari (en az 32 karakter, rastgele)
- `REVENUECAT_API_KEY` — RevenueCat REST API v2 anahtari
- `APPLE_CERTIFICATE` / `APPLE_CERTIFICATE_PASSWORD` — iOS imzalama sertifikasi
- `APPLE_PROVISION_PROFILE` — iOS provizyon profili

## Release Olusturma (Alpha Build)

GitHub Actions uzerinden otomatik release uretimi `.github/workflows/release.yml` ile yapilir.

### Yontem 1: Git Tag ile (Onerilen)

```bash
git tag -a v0.1.0-alpha -m "ArcaneDecks Alpha 0.1.0"
git push origin v0.1.0-alpha
```

Tag push edildiginde CI otomatik calisir:
1. `build-desktopgl` — Windows x64 self-contained EXE
2. `build-android` — APK / AAB
3. `build-ios` — iOS simulator build (fiziksel cihaz IPA icin Apple Developer Certificate gerekir)
4. `release` — Tum artifact'leri ZIP'leyip GitHub Release olusturur (prerelease)

### Yontem 2: Manuel Trigger

GitHub repo sayfasinda **Actions > Release Alpha Build > Run workflow** butonuyla istenen tag adi girilerek calistirilabilir.

### CI Release Ciktilari

| Platform | Cikti | Not |
|---|---|---|
| DesktopGL | `ArcaneDecks-DesktopGL-Win64.zip` | `ArcaneDecks.DesktopGL.exe` icerir; .NET kurulumu gerekmez (self-contained) |
| Android | `ArcaneDecks-Android.zip` | `.apk` ve `.aab` dosyalarini icerir |
| iOS | `ArcaneDecks-iOS.zip` | Simulator build; fiziksel cihaz IPA'si icin CI'da Apple Developer sertifikasi eklenmeli |
