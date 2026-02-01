# Antigravity Çift Hesap Kurulum Scripti
# Bu script, mevcut Antigravity kurulumunuzu kullanarak ikinci bir izole profil oluşturur.

# 1. Antigravity'nin Exe yolunu bulmaya çalışalım (Genellikle AppData içindedir)
$possiblePaths = @(
    "$env:LOCALAPPDATA\Programs\Antigravity\Antigravity.exe",
    "$env:LOCALAPPDATA\Programs\cursor\Cursor.exe", # Eğer Cursor tabanlıysa
    "$env:APPDATA\Local\Programs\Antigravity\Antigravity.exe",
    "C:\Program Files\Antigravity\Antigravity.exe"
)

$targetExe = ""

foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $targetExe = $path
        break
    }
}

# Eğer otomatik bulamazsak kullanıcıdan isteyelim (Burayı manuel düzenleyebilirsiniz)
if ($targetExe -eq "") {
    Write-Host "Antigravity.exe otomatik bulunamadı. Lütfen scriptin içindeki yolu manuel düzenleyin." -ForegroundColor Red
    # Örnek: $targetExe = "C:\Kullanıcılar\Adınız\AppData\Local\Programs\Antigravity\Antigravity.exe"
    exit
}

Write-Host "Antigravity bulundu: $targetExe" -ForegroundColor Green

# 2. İkinci hesap için veri klasörü oluştur
$profileDir = "$env:USERPROFILE\AntigravityProfiles\SecondaryAccount"
if (-not (Test-Path $profileDir)) {
    New-Item -ItemType Directory -Force -Path $profileDir | Out-Null
    Write-Host "Profil klasörü oluşturuldu: $profileDir" -ForegroundColor Cyan
}

# 3. Masaüstüne Kısayol Oluştur
$WshShell = New-Object -comObject WScript.Shell
$DesktopPath = [Environment]::GetFolderPath("Desktop")
$ShortcutPath = "$DesktopPath\Antigravity - Yedek Hesap.lnk"

$Shortcut = $WshShell.CreateShortcut($ShortcutPath)
$Shortcut.TargetPath = $targetExe
# EN ÖNEMLİ KISIM: --user-data-dir parametresi ile izole bir profil açıyoruz
$Shortcut.Arguments = "--user-data-dir `"$profileDir`""
$Shortcut.Description = "Antigravity'i ikinci bir hesapla başlatır"
$Shortcut.Save()

Write-Host "Kısayol masaüstüne oluşturuldu: Antigravity - Yedek Hesap" -ForegroundColor Green
Write-Host "Bu kısayolu ilk açtığında Antigravity sıfırdan kurulmuş gibi gelecek. Diğer hesabınla giriş yap, bir daha sormayacak!" -ForegroundColor Yellow
