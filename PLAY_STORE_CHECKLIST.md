# Google Play Store Release Checklist

## ✅ Completed Items

1. **Removed 3-second splash screen delay** - Removed `Thread.Sleep(3000)` from MainActivity.cs for better user experience
2. **Hidden debug tools** - Debug tools in ViewLogPage are now hidden by default (IsVisible="False")

## 🔧 Required Actions Before Publishing

### 1. App Signing Configuration ⚠️ CRITICAL
   - **Status**: ⚠️ **NEEDS SETUP**
   - You need to configure app signing for Play Store release
   - Options:
     - **Option A (Recommended)**: Use Google Play App Signing
       - Let Google manage your signing key
       - Upload an upload key, Google manages the app signing key
     - **Option B**: Use your own keystore
       - Create a keystore file: `keytool -genkey -v -keystore hiatme-release.keystore -alias hiatme -keyalg RSA -keysize 2048 -validity 10000`
       - Store it securely (NOT in the repository)
       - Configure in Visual Studio: Project Properties → Android → Archive → Signing
   
   **Action Required**: Set up signing configuration in Visual Studio before building the release APK/AAB

### 2. Version Information
   - Current version: `1.0` (ApplicationDisplayVersion)
   - Current version code: `1` (ApplicationVersion)
   - **Status**: ✅ Looks good for initial release
   - **Note**: Increment these for future updates

### 3. Package Name
   - Current: `com.companyname.hiatmeapp`
   - **Status**: ✅ Configured
   - **Note**: This cannot be changed after first release, so ensure it's correct

### 4. API Configuration
   - Base URL: `https://hiatme.com` ✅ (Production URL)
   - **Status**: ✅ Configured correctly

### 5. Permissions Review
   - ✅ Network permissions
   - ✅ Notification permissions
   - ✅ Location permissions (fine, coarse, background)
   - ✅ Battery optimization exemption request
   - **Status**: ✅ All permissions appear necessary for app functionality
   - **Note**: Ensure you have privacy policy that explains location usage

### 6. Debug Code
   - ✅ Removed splash screen delay
   - ✅ Hidden debug tools in ViewLogPage
   - ⚠️ Debug logging still present (System.Diagnostics.Debug.WriteLine) - These are fine as they don't appear in release builds
   - ⚠️ Test methods in AppShell.cs (OnTestLoginClicked, OnTestHomeClicked) - Not called from UI, but consider removing

### 7. Google Services
   - ✅ google-services.json present in Platforms/Android/
   - **Status**: ✅ Configured
   - **Action**: Verify the google-services.json matches your Firebase/OneSignal project

### 8. App Icons & Assets
   - ✅ App icon configured (appicon.svg)
   - ✅ Splash screen configured (splash.svg)
   - **Status**: ✅ Configured
   - **Action**: Verify icons look good at all sizes

### 9. Build Configuration
   - **Action Required**: Build a Release version (not Debug)
   - In Visual Studio: Build → Configuration Manager → Set to "Release"
   - Build → Archive for Publishing → Create Android App Bundle (.aab) or APK

### 10. Testing Checklist
   Before submitting, test:
   - [ ] Login/Logout flow
   - [ ] Vehicle assignment
   - [ ] Location tracking
   - [ ] Push notifications
   - [ ] Day off requests
   - [ ] Vehicle issues reporting
   - [ ] Profile updates
   - [ ] App works on different Android versions (API 21+)
   - [ ] App handles network errors gracefully
   - [ ] App works when location permissions are denied

### 11. Play Store Console Requirements
   - [ ] Create app listing in Google Play Console
   - [ ] App name, description, screenshots
   - [ ] Privacy policy URL (required for location permissions)
   - [ ] Content rating questionnaire
   - [ ] Store listing graphics (icon, feature graphic, screenshots)
   - [ ] Age rating information

### 12. Privacy Policy ⚠️ REQUIRED
   - **Status**: ⚠️ **REQUIRED**
   - Google Play requires a privacy policy for apps that:
     - Access location data ✅ (Your app does)
     - Collect user data
     - Use sensitive permissions
   - **Action**: Create and host a privacy policy, then add URL to Play Console

### 13. Target SDK
   - Current: API 21 (Android 5.0) minimum
   - **Status**: ✅ Good coverage
   - **Note**: Consider updating minimum to API 24+ for better security in future

## 📋 Pre-Submission Steps

1. **Build Release Version**
   ```
   - Open in Visual Studio
   - Set Configuration to "Release"
   - Build → Archive for Publishing
   - Create Android App Bundle (.aab) - Recommended for Play Store
   ```

2. **Test the Release Build**
   - Install the release APK/AAB on a test device
   - Verify all features work correctly
   - Check performance and battery usage

3. **Prepare Store Listing**
   - App name, short description, full description
   - Screenshots (phone, tablet if supported)
   - Feature graphic (1024x500)
   - App icon (512x512)
   - Privacy policy URL

4. **Upload to Play Console**
   - Create new app in Google Play Console
   - Upload the .aab file
   - Complete store listing
   - Submit for review

## ⚠️ Important Notes

- **App Signing**: This is the most critical step. Without proper signing, you cannot publish to Play Store.
- **Privacy Policy**: Required for apps with location permissions. Must be publicly accessible.
- **First Release**: Package name cannot be changed after first release.
- **Version Codes**: Must increment with each release (currently at 1).

## 🚀 Quick Start Commands

If using command line to build:
```bash
dotnet build -c Release
dotnet publish -c Release -f net9.0-android
```

Then use Visual Studio's "Archive for Publishing" feature to create the signed .aab file.
