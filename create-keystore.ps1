# PowerShell script to create Android signing keystore for Hiatme App
# This creates a keystore for uploading to Google Play Store

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Hiatme App - Keystore Creation Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if Java keytool is available
$keytoolPath = $null
$javaPaths = @(
    "keytool",
    "$env:JAVA_HOME\bin\keytool.exe",
    "C:\Program Files\Java\jdk-*\bin\keytool.exe",
    "C:\Program Files (x86)\Java\jdk-*\bin\keytool.exe"
)

foreach ($path in $javaPaths) {
    if ($path -eq "keytool") {
        $result = Get-Command keytool -ErrorAction SilentlyContinue
        if ($result) {
            $keytoolPath = "keytool"
            break
        }
    } else {
        $resolved = Resolve-Path $path -ErrorAction SilentlyContinue
        if ($resolved -and (Test-Path $resolved[0])) {
            $keytoolPath = $resolved[0]
            break
        }
    }
}

if (-not $keytoolPath) {
    Write-Host "ERROR: Java keytool not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install Java JDK and ensure it's in your PATH, or" -ForegroundColor Yellow
    Write-Host "set JAVA_HOME environment variable." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Download Java JDK from: https://adoptium.net/" -ForegroundColor Cyan
    exit 1
}

Write-Host "Found keytool at: $keytoolPath" -ForegroundColor Green
Write-Host ""

# Create secure directory for keystore (outside project)
$keystoreDir = "$env:USERPROFILE\.android\keystores"
if (-not (Test-Path $keystoreDir)) {
    New-Item -ItemType Directory -Path $keystoreDir -Force | Out-Null
    Write-Host "Created keystore directory: $keystoreDir" -ForegroundColor Green
}

$keystoreFile = "$keystoreDir\hiatme-upload.keystore"
$alias = "hiatme-upload"

# Check if keystore already exists
if (Test-Path $keystoreFile) {
    Write-Host "WARNING: Keystore already exists at: $keystoreFile" -ForegroundColor Yellow
    $overwrite = Read-Host "Do you want to overwrite it? (yes/no)"
    if ($overwrite -ne "yes") {
        Write-Host "Aborted." -ForegroundColor Yellow
        exit 0
    }
    Remove-Item $keystoreFile -Force
}

Write-Host ""
Write-Host "You will be prompted to enter information for the keystore:" -ForegroundColor Cyan
Write-Host "  - Keystore password (save this securely!)" -ForegroundColor Yellow
Write-Host "  - Alias password (can be same as keystore password)" -ForegroundColor Yellow
Write-Host "  - Your name/company information" -ForegroundColor Yellow
Write-Host ""
Write-Host "IMPORTANT: Save your passwords in a secure password manager!" -ForegroundColor Red
Write-Host ""

$continue = Read-Host "Press Enter to continue, or type 'cancel' to abort"
if ($continue -eq "cancel") {
    Write-Host "Aborted." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Generating keystore..." -ForegroundColor Cyan
Write-Host ""

# Generate keystore
$keytoolArgs = @(
    "-genkey",
    "-v",
    "-keystore", $keystoreFile,
    "-alias", $alias,
    "-keyalg", "RSA",
    "-keysize", "2048",
    "-validity", "10000"
)

try {
    if ($keytoolPath -eq "keytool") {
        & keytool $keytoolArgs
    } else {
        & $keytoolPath $keytoolArgs
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "Keystore created successfully!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Keystore location: $keystoreFile" -ForegroundColor Cyan
        Write-Host "Alias: $alias" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Yellow
        Write-Host "1. Open Visual Studio" -ForegroundColor White
        Write-Host "2. Right-click HiatmeApp.csproj → Properties" -ForegroundColor White
        Write-Host "3. Go to 'Android Package Signing'" -ForegroundColor White
        Write-Host "4. Browse to: $keystoreFile" -ForegroundColor White
        Write-Host "5. Enter your keystore password" -ForegroundColor White
        Write-Host "6. Select alias: $alias" -ForegroundColor White
        Write-Host "7. Enter alias password" -ForegroundColor White
        Write-Host ""
        Write-Host "See SETUP_ANDROID_SIGNING.md for detailed instructions." -ForegroundColor Cyan
    } else {
        Write-Host "ERROR: Failed to create keystore" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
