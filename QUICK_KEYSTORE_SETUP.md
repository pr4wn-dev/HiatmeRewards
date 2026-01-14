# Quick Keystore Setup Guide

## Step 1: Open PowerShell

Open PowerShell (not as administrator, just regular PowerShell).

## Step 2: Navigate to Project

```powershell
cd C:\Users\rneal\source\repos\HiatmeRewards
```

## Step 3: Run the Script

**If you get an execution policy error, use this command instead:**

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\create-keystore.ps1
```

**Or simply:**
```powershell
.\create-keystore.ps1
```

## Step 4: Follow the Prompts

The script will ask you to:
1. **Enter a keystore password** (at least 6 characters - save this securely!)
2. **Re-enter the password** to confirm
3. **Enter an alias password** (can be the same as keystore password)
4. **Enter your name** (e.g., "Your Name" or "Hiatme")
5. **Enter organizational unit** (optional - just press Enter)
6. **Enter organization** (e.g., "Hiatme")
7. **Enter city** (e.g., "Your City")
8. **Enter state** (e.g., "Your State")
9. **Enter country code** (e.g., "US" for United States)
10. **Confirm** (type "yes")

## Step 5: Save Your Information

**IMPORTANT:** Save these details securely (password manager recommended):
- Keystore location: `C:\Users\rneal\.android\keystores\hiatme-upload.keystore`
- Keystore password: (the password you entered)
- Alias: `hiatme-upload`
- Alias password: (the password you entered)

## Step 6: Configure in Visual Studio

1. Open your project in Visual Studio
2. Right-click `HiatmeApp.csproj` → **Properties**
3. Go to **"Android Package Signing"** (or search for "Signing")
4. Check **"Sign the .APK file using the following keystore details"**
5. Click **Browse** and navigate to: `C:\Users\rneal\.android\keystores\hiatme-upload.keystore`
6. Enter your **keystore password**
7. Select alias: **hiatme-upload** (from dropdown)
8. Enter your **alias password**
9. Click **OK**

## Alternative: Manual Keystore Creation

If the script doesn't work, you can create it manually:

```powershell
keytool -genkey -v -keystore "$env:USERPROFILE\.android\keystores\hiatme-upload.keystore" -alias hiatme-upload -keyalg RSA -keysize 2048 -validity 10000
```

Then follow the same prompts as above.

## Troubleshooting

**"Java keytool not found"**
- Install Java JDK from https://adoptium.net/
- Or set JAVA_HOME environment variable

**"Keystore password is too short"**
- Use at least 6 characters

**"Too many failures"**
- Start over and be careful with password entry
