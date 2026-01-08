# How to Access Git Credential Manager

Git Credential Manager on Windows is integrated into **Windows Credential Manager**. Here's how to access it:

## Method 1: Search Windows (Easiest)

1. Press the **Windows key** on your keyboard
2. Type: **"Credential Manager"** or **"Credential"**
3. Click on **"Credential Manager"** or **"Windows Credential Manager"**

## Method 2: Control Panel

1. Press **Windows key + R** to open Run dialog
2. Type: `control /name Microsoft.CredentialManager`
3. Press Enter

## Method 3: Settings App

1. Press **Windows key + I** to open Settings
2. Search for **"Credential Manager"**
3. Click on **"Windows Credential Manager"**

## Method 4: Command Line

1. Open PowerShell or Command Prompt
2. Type: `cmdkey /list`
   - This shows all stored credentials
3. To manage GitHub credentials specifically:
   - Look for entries starting with `git:https://github.com`
   - To delete: `cmdkey /delete:git:https://github.com`

## What You'll See

Once opened, you'll see:
- **Web Credentials** - Contains GitHub and other web-based credentials
- **Windows Credentials** - Contains local/network credentials

### For GitHub:
1. Click on **"Web Credentials"** tab
2. Look for entries like:
   - `git:https://github.com`
   - `github.com`
3. Click the arrow to expand and see details
4. Click **"Edit"** to modify or **"Remove"** to delete

## Quick Commands (PowerShell)

### View all Git credentials:
```powershell
cmdkey /list | Select-String "git"
```

### Remove GitHub credentials:
```powershell
cmdkey /delete:git:https://github.com
```

### Remove all GitHub credentials:
```powershell
cmdkey /list | Select-String "github" | ForEach-Object { 
    $line = $_.Line
    if ($line -match 'Target: (.+)') {
        cmdkey /delete:$matches[1]
    }
}
```

## When You'll Need This

You might need to access credentials when:
- Git asks for credentials repeatedly
- You need to update your GitHub token
- You're getting authentication errors
- You want to switch GitHub accounts

## Alternative: Use Git Command Line

You can also manage credentials directly through Git:

```powershell
# View stored credentials
git credential-manager list

# Erase stored credentials (will prompt again next time)
git credential-manager erase
```

**Note:** The exact command may vary depending on your Git version. Some versions use:
- `git credential fill`
- `git credential approve`
- `git credential reject`

