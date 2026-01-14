# Fix Visual Studio "File Deleted/Renamed/Moved" Error

## Step 1: Close Visual Studio Completely
1. Close ALL Visual Studio windows
2. Check Task Manager to make sure no `devenv.exe` processes are running
3. If you see any, end the process

## Step 2: Run This PowerShell Script

Open PowerShell as Administrator and run:

```powershell
cd C:\Users\rneal\source\repos\HiatmeRewards

# Delete Visual Studio cache folders
Remove-Item -Recurse -Force .vs -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force obj -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force bin -ErrorAction SilentlyContinue

# Restore packages
dotnet restore --force

# Build to regenerate everything
dotnet build
```

## Step 3: Reopen Visual Studio
1. Open Visual Studio
2. Open your solution
3. Wait for it to finish loading and restoring packages
4. Build → Rebuild Solution

## Alternative: If Still Having Issues

If the error persists, try this in Visual Studio:

1. **Close Visual Studio**
2. **Delete the `.vs` folder** in your project directory
3. **In Visual Studio:**
   - Tools → Options → Projects and Solutions → General
   - Uncheck "Restore NuGet packages on build"
   - Click OK
   - Check it again
   - Click OK
4. **Build → Clean Solution**
5. **Build → Rebuild Solution**

## If Files Are Locked

If you get "access denied" errors, some process is locking the files:

1. Close Visual Studio
2. Close any other programs that might be using the project
3. Restart your computer (if needed)
4. Then run the cleanup script again
