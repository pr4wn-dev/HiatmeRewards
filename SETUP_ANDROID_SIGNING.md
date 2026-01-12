# Android App Signing Setup for Play Store

## Option 1: Google Play App Signing (Recommended) ⭐

This is the easiest and most secure option. Google manages your app signing key.

### Steps:
1. **Create a Keystore for Upload** (you'll use this to upload to Play Store)
   ```bash
   keytool -genkey -v -keystore hiatme-upload.keystore -alias hiatme-upload -keyalg RSA -keysize 2048 -validity 10000
   ```
   - When prompted, enter:
     - Password: (choose a strong password - save it securely!)
     - Name: Your name or company name
     - Organizational Unit: (optional)
     - Organization: Hiatme (or your company)
     - City: Your city
     - State: Your state
     - Country: US (or your country code)

2. **Store the keystore securely**
   - Save `hiatme-upload.keystore` in a secure location (NOT in the repository)
   - Save the password securely (password manager recommended)
   - The alias password can be the same as the keystore password

3. **Configure in Visual Studio:**
   - Right-click project → Properties
   - Go to "Android Package Signing"
   - Check "Sign the .APK file using the following keystore details"
   - Browse to your `hiatme-upload.keystore` file
   - Enter the keystore password
   - Select alias: `hiatme-upload`
   - Enter alias password (same as keystore password if you used the same)

4. **First Upload to Play Store:**
   - When you upload your first .aab file, Google will ask if you want to use Google Play App Signing
   - Select "Yes" - this is recommended
   - Google will manage your app signing key from then on
   - You'll only need your upload key for future uploads

## Option 2: Manual Keystore (Traditional Method)

If you prefer to manage your own signing key:

1. **Create the Keystore:**
   ```bash
   keytool -genkey -v -keystore hiatme-release.keystore -alias hiatme -keyalg RSA -keysize 2048 -validity 10000
   ```

2. **Configure in Visual Studio** (same as Option 1, step 3)

3. **Store securely** - You are responsible for keeping this keystore safe forever!

## Quick Setup Script

Run this in PowerShell (from project root):

```powershell
# Navigate to project directory
cd C:\Users\rneal\source\repos\HiatmeRewards

# Create a secure directory for keystore (outside of project)
$keystorePath = "$env:USERPROFILE\.android\keystores"
if (-not (Test-Path $keystorePath)) {
    New-Item -ItemType Directory -Path $keystorePath -Force
}

# Generate keystore
keytool -genkey -v -keystore "$keystorePath\hiatme-upload.keystore" -alias hiatme-upload -keyalg RSA -keysize 2048 -validity 10000

Write-Host "Keystore created at: $keystorePath\hiatme-upload.keystore"
Write-Host "IMPORTANT: Save your password securely!"
```

## Visual Studio Configuration Steps

1. **Open Project Properties:**
   - Right-click `HiatmeApp.csproj` → Properties
   - Or: Project → HiatmeApp Properties

2. **Navigate to Android Package Signing:**
   - In the left sidebar, find "Android Package Signing"
   - Or search for "Signing" in the search box

3. **Enable Signing:**
   - Check "Sign the .APK file using the following keystore details"
   - Browse to your keystore file
   - Enter keystore password
   - Select alias from dropdown
   - Enter alias password

4. **Save:**
   - Click OK/Apply
   - Visual Studio will save this to `HiatmeApp.csproj.user` (which is gitignored)

## Building Release Version

1. **Set Configuration to Release:**
   - Build → Configuration Manager
   - Set "Active solution configuration" to "Release"
   - Click Close

2. **Archive for Publishing:**
   - Build → Archive for Publishing
   - Wait for build to complete
   - Visual Studio will open the Archive Manager

3. **Create Android App Bundle:**
   - In Archive Manager, click "Distribute"
   - Select "Google Play"
   - Click "Next"
   - Select your signing identity
   - Click "Next"
   - Choose "Android App Bundle (.aab)"
   - Click "Create"
   - Save the .aab file

4. **Upload to Play Console:**
   - Go to Google Play Console
   - Create new app (if first time)
   - Go to Production → Create new release
   - Upload the .aab file

## Troubleshooting

### "Keystore file not found"
- Make sure the path is correct
- Use absolute path if relative path doesn't work

### "Wrong password"
- Double-check your keystore password
- Make sure you're using the correct alias password

### "Cannot find Java keytool"
- Install Java JDK if not installed
- Add Java bin directory to PATH, or use full path:
  - `"C:\Program Files\Java\jdk-XX\bin\keytool.exe"`

## Security Best Practices

1. **Never commit keystore to git** - It's already in .gitignore
2. **Backup your keystore** - Store in secure location (encrypted drive, password manager)
3. **Use Google Play App Signing** - Let Google manage the app signing key
4. **Keep upload key safe** - Even with Google Play App Signing, you need your upload key
5. **Document passwords** - Store in password manager, not in code

## Next Steps

After setting up signing:
1. Build a release version
2. Test the signed APK/AAB on a device
3. Upload to Play Console
4. Complete store listing
5. Submit for review
