# Visual Studio 2026 - Android Signing Guide

In Visual Studio 2026, signing is configured differently than older versions. Here are the methods:

## Method 1: Archive Manager (Recommended) ⭐

This is the easiest way in VS 2026:

1. **Set Configuration to Release:**
   - Build → Configuration Manager
   - Set "Active solution configuration" to **Release**
   - Close

2. **Archive for Publishing:**
   - Build → **Archive for Publishing** (or Archive All)
   - Wait for the build to complete
   - The **Archive Manager** window will open

3. **Configure Signing in Archive Manager:**
   - In Archive Manager, select your Android archive
   - Click **"Distribute"** button
   - Select **"Google Play"**
   - Click **"Next"**
   - You'll see signing options:
     - If you see "Create new signing identity" → Click it
     - Browse to: `C:\Users\rneal\.android\keystores\hiatme-upload.keystore`
     - Enter keystore password
     - Select alias: `hiatme-upload`
     - Enter alias password
   - Click **"Next"**
   - Choose **"Android App Bundle (.aab)"**
   - Click **"Create"**

## Method 2: Project File Configuration

I've already added signing configuration to your `.csproj` file. You just need to add the passwords:

1. **Open `HiatmeApp.csproj.user`** (this file is gitignored, so it's safe for passwords)

2. **Add this content** (replace with your actual passwords):

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <IsFirstTimeProjectOpen>False</IsFirstTimeProjectOpen>
    <ActiveDebugFramework>net9.0-android</ActiveDebugFramework>
    <ActiveDebugProfile>Pixel 7 - API 35 (Android 15.0 - API 35)</ActiveDebugProfile>
    <SelectedPlatformGroup>Emulator</SelectedPlatformGroup>
    <DefaultDevice>pixel_7_-_api_35</DefaultDevice>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <AndroidSigningStorePass>YOUR_KEYSTORE_PASSWORD_HERE</AndroidSigningStorePass>
    <AndroidSigningKeyPass>YOUR_ALIAS_PASSWORD_HERE</AndroidSigningKeyPass>
  </PropertyGroup>
</Project>
```

3. **Replace the passwords** with your actual keystore and alias passwords

## Method 3: Environment Variables (Most Secure)

Set these environment variables before building:

**PowerShell:**
```powershell
$env:AndroidSigningStorePass = "your-keystore-password"
$env:AndroidSigningKeyPass = "your-alias-password"
```

**Command Prompt:**
```cmd
set AndroidSigningStorePass=your-keystore-password
set AndroidSigningKeyPass=your-alias-password
```

Then build from the same terminal.

## Quick Test

To test if signing is configured:

1. Set Configuration to **Release**
2. Build → **Archive for Publishing**
3. If signing is configured, it should sign automatically
4. If not, use Method 1 (Archive Manager) to configure it

## Troubleshooting

**"Keystore file not found"**
- The path in the .csproj uses `$(USERPROFILE)` which should resolve to your user folder
- Verify the file exists at: `C:\Users\rneal\.android\keystores\hiatme-upload.keystore`

**"Wrong password"**
- Double-check your passwords
- Make sure you're using the same password you entered when creating the keystore

**"Signing not working"**
- Use Method 1 (Archive Manager) - it's the most reliable in VS 2026
- The Archive Manager will prompt you for passwords if they're not set
