# Create Release Build for Google Play Store

## Step 1: Set Configuration to Release

1. In Visual Studio, look at the top toolbar
2. Find the dropdown that says "Debug" 
3. Change it to **"Release"**

## Step 2: Archive for Publishing

1. Go to **Build** menu
2. Click **"Archive for Publishing"** (or **"Archive All"**)
3. Wait for the build to complete (this may take a few minutes)
4. The **Archive Manager** window will open automatically

## Step 3: Configure Signing in Archive Manager

1. In Archive Manager, you should see your Android archive listed
2. Click the **"Distribute"** button
3. Select **"Google Play"**
4. Click **"Next"**

5. **Signing Identity Dialog:**
   - You'll see options for signing
   - Click **"Create new signing identity"** or **"Select existing"**
   - Browse to: `C:\Users\rneal\.android\keystores\hiatme-upload.keystore`
   - Enter your **keystore password**
   - Select alias: **hiatme-upload** (from dropdown)
   - Enter your **alias password** (same as keystore password if you used the same)
   - Click **"Create"** or **"OK"**

6. **Distribution Method:**
   - Choose **"Android App Bundle (.aab)"** (recommended for Play Store)
   - Click **"Next"**

7. **Save Location:**
   - Choose where to save your .aab file
   - Click **"Create"**

## Step 4: Upload to Google Play Console

1. Go to [Google Play Console](https://play.google.com/console)
2. Select your app (or create a new app if first time)
3. Go to **Production** → **Create new release**
4. Upload your `.aab` file
5. Fill in release notes
6. Review and submit

## What You'll Need for Play Store

- ✅ App Bundle (.aab file) - you're creating this now
- ⚠️ Privacy Policy URL - required for apps with location permissions
- ⚠️ Store listing (description, screenshots, etc.)
- ⚠️ Content rating questionnaire

## Troubleshooting

**"Archive for Publishing" is grayed out:**
- Make sure Configuration is set to "Release"
- Make sure you have Android selected as target platform

**Signing errors:**
- Make sure the keystore file exists at the path
- Double-check your passwords

**Build errors:**
- Make sure all packages are restored (Build → Restore NuGet Packages)
- Clean and rebuild (Build → Clean Solution, then Build → Rebuild Solution)
