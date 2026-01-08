# Setup Guide for Working on Another PC

This guide will help you set up Cursor and your development environment on another PC with minimal effort.

## Quick Start (TL;DR)

1. **Install Cursor** and login with your same account (syncs settings automatically)
2. **Install Git** and configure with your credentials
3. **Install Visual Studio 2022** with .NET MAUI workloads
4. **Clone repositories:**
   - `git clone https://github.com/pr4wn-dev/HiatmeRewards.git` → `C:\Users\YourUsername\source\repos\HiatmeRewards`
   - `git clone https://github.com/pr4wn-dev/Hiatme-PHP-Website.git` → `C:\Projects\Hiatme-PHP-Website`
5. **Open in Cursor** - File → Open Folder → Select `HiatmeRewards` folder
6. **GoDaddy sync works automatically** - just push to `main` branch

That's it! Your Cursor account will sync most settings automatically.

## Prerequisites
- Windows PC with admin access
- Internet connection
- GitHub account access

## Step 1: Install Required Software

### 1.1 Install Cursor
1. Download Cursor from https://cursor.sh
2. Install it on the new PC
3. **Login with the same Cursor account** - This will sync your Cursor settings, extensions, and preferences automatically

### 1.2 Install Git (if not already installed)
1. Download Git for Windows from https://git-scm.com/download/win
2. During installation:
   - Choose "Git Credential Manager" for credential storage
   - Use default options for everything else

### 1.3 Install .NET MAUI SDK (for the app project)
1. Download and install Visual Studio 2022 Community (or higher) from https://visualstudio.microsoft.com/
2. During installation, select:
   - **.NET Multi-platform App UI development** workload
   - **Mobile development with .NET** workload
   - **.NET desktop development** workload (optional but recommended)

## Step 2: Configure Git

### 2.1 Set up Git credentials
Open PowerShell or Command Prompt and run:
```powershell
git config --global user.name "bobby yontz"
git config --global user.email "megapr4wn@gmail.com"
```

### 2.2 Configure Git Credential Manager (for GitHub)
Git Credential Manager should automatically prompt you to login to GitHub when you first clone/pull. If not:
1. Go to GitHub Settings → Developer settings → Personal access tokens
2. Create a token with `repo` permissions
3. Use this token when prompted for credentials

## Step 3: Clone Your Repositories

### 3.1 Clone the MAUI App Repository
```powershell
cd C:\Users\YourUsername\source\repos
git clone https://github.com/pr4wn-dev/HiatmeRewards.git
```

**Note:** Replace `YourUsername` with your actual Windows username. If the `source\repos` folders don't exist, create them first.

### 3.2 Clone the PHP Website Repository
```powershell
cd C:\Projects
git clone https://github.com/pr4wn-dev/Hiatme-PHP-Website.git
```

When prompted, login with your GitHub credentials.

## Step 4: Open Workspace in Cursor

### 4.1 Open the MAUI App Project
1. Open Cursor
2. File → Open Folder
3. Navigate to `C:\Users\YourUsername\source\repos\HiatmeRewards`
4. Click "Select Folder"

### 4.2 Add the PHP Website as a Multi-Root Workspace (Optional)
If you want both projects open at once:
1. File → Add Folder to Workspace
2. Navigate to `C:\Projects\Hiatme-PHP-Website`
3. Click "Select Folder"
4. File → Save Workspace As... → Save as `hiatme-workspace.code-workspace`

## Step 5: Verify Git Sync

### 5.1 Check MAUI App Repository
```powershell
cd C:\Users\YourUsername\source\repos\HiatmeRewards
git status
git remote -v
```

### 5.2 Check PHP Website Repository
```powershell
cd C:\Projects\Hiatme-PHP-Website
git status
git remote -v
```

Both should show:
- `origin  https://github.com/pr4wn-dev/[REPO-NAME].git`

## Step 6: GoDaddy Git Sync Setup

The GoDaddy Git sync is configured on the server side, so you don't need to do anything special. When you push to the `main` branch of the PHP website repository, it should automatically sync to GoDaddy (if that's how it's configured).

To verify:
1. Make a small test change in the PHP website
2. Commit and push:
   ```powershell
   cd C:\Projects\Hiatme-PHP-Website
   git add .
   git commit -m "Test commit"
   git push origin main
   ```
3. Check if it syncs to GoDaddy (check your website or GoDaddy dashboard)

## Step 7: Restore Cursor Settings

Since you logged in with the same Cursor account, most settings should sync automatically. However, you may need to:

1. **Install Extensions**: Cursor should prompt you to install recommended extensions, or go to Extensions and install:
   - C# (by Microsoft)
   - .NET Extension Pack
   - Any other extensions you use

2. **Verify Settings**: Check Cursor settings (Ctrl+,) to ensure:
   - Editor settings match your preferences
   - Git settings are correct
   - File associations are set up

## Step 8: Test Everything

### 8.1 Test MAUI App
1. Open the solution file: `HiatmeRewards\HiatmeApp.sln`
2. Restore NuGet packages (should happen automatically)
3. Try building the project (Ctrl+Shift+B)

### 8.2 Test PHP Website
1. Navigate to `C:\Projects\Hiatme-PHP-Website`
2. Verify files are present
3. If you have a local PHP server, test it

## Troubleshooting

### Git Authentication Issues
If you get authentication errors:
1. Go to Windows Credential Manager (search "Credential Manager" in Windows)
2. Remove any GitHub credentials
3. Try pushing/pulling again - it will prompt for new credentials

### Cursor Not Syncing Settings
1. Check if you're logged into the same Cursor account
2. Go to Cursor Settings → Account and verify
3. Manually sync: Cursor Settings → Sync → Turn on Settings Sync

### Missing Dependencies
If the MAUI app won't build:
1. Open Visual Studio Installer
2. Modify your installation
3. Ensure all .NET MAUI workloads are installed
4. Restart Cursor

## Quick Reference

**Repository Locations:**
- MAUI App: `C:\Users\YourUsername\source\repos\HiatmeRewards`
- PHP Website: `C:\Projects\Hiatme-PHP-Website`

**GitHub Repositories:**
- MAUI App: `https://github.com/pr4wn-dev/HiatmeRewards.git`
- PHP Website: `https://github.com/pr4wn-dev/Hiatme-PHP-Website.git`

**Your Git Credentials:**
- Name: `bobby yontz`
- Email: `megapr4wn@gmail.com`

**Important:** Replace `YourUsername` with your actual Windows username on the new PC.

