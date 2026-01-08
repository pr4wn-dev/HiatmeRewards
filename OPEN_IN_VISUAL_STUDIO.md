# How to Open Your MAUI App in Visual Studio

## Method 1: Open Solution File Directly (Easiest)

1. **Navigate to your project folder:**
   ```
   C:\Users\YourUsername\source\repos\HiatmeRewards
   ```

2. **Double-click the solution file:**
   - Look for: `HiatmeApp.sln`
   - Double-click it
   - Visual Studio will open automatically

## Method 2: From Visual Studio

1. **Open Visual Studio**
2. **File → Open → Project/Solution** (or press `Ctrl+Shift+O`)
3. Navigate to: `C:\Users\YourUsername\source\repos\HiatmeRewards`
4. Select: `HiatmeApp.sln`
5. Click **Open**

## Method 3: Open Recent (After First Time)

1. **Open Visual Studio**
2. **File → Open Recent → Projects and Solutions**
3. Click on `HiatmeApp.sln` from the list

## Method 4: Drag and Drop

1. **Open Visual Studio**
2. **Open File Explorer** and navigate to your project folder
3. **Drag** `HiatmeApp.sln` into Visual Studio
4. Drop it anywhere in the Visual Studio window

## First Time Setup

### If Visual Studio Prompts You:

1. **Restore NuGet Packages:**
   - Visual Studio should do this automatically
   - If not: Right-click solution → **Restore NuGet Packages**

2. **Select Target Platform:**
   - At the top toolbar, you'll see platform dropdowns
   - Select your target (e.g., Android, iOS, Windows)

3. **Set Startup Project:**
   - Right-click on `HiatmeApp` project in Solution Explorer
   - Select **Set as Startup Project**

## Verify It's Loaded Correctly

After opening, you should see:

✅ **Solution Explorer** shows:
- `HiatmeApp` project
- Folders: Pages, ViewModels, Services, Controls, etc.
- References and Dependencies

✅ **No errors** in Error List (View → Error List)

✅ **Platforms** available in the toolbar dropdown

## If You Get Errors

### Error: "Project file cannot be opened"
- Make sure you cloned the full repository
- Run: `git pull origin master` to ensure you have all files

### Error: "NuGet packages not found"
- Right-click solution → **Restore NuGet Packages**
- Or: **Tools → NuGet Package Manager → Package Manager Console**
- Run: `dotnet restore`

### Error: ".NET MAUI workload not installed"
- Open **Visual Studio Installer**
- Click **Modify**
- Ensure **.NET Multi-platform App UI development** is checked
- Click **Modify** to install

### Error: "SDK not found"
- Open **Visual Studio Installer**
- Click **Modify**
- Check **.NET desktop development** workload
- Install if needed

## Quick Reference

**Solution File Location:**
```
C:\Users\YourUsername\source\repos\HiatmeRewards\HiatmeApp.sln
```

**Project File:**
```
C:\Users\YourUsername\source\repos\HiatmeRewards\HiatmeApp.csproj
```

## Pro Tips

### Pin to Taskbar:
1. Right-click `HiatmeApp.sln`
2. **Pin to Start** or create a shortcut
3. Double-click to open anytime

### Add to Favorites in File Explorer:
1. In File Explorer, navigate to the folder
2. Click the **star icon** to add to Quick Access
3. Quick access from File Explorer sidebar

### Set as Default Project:
- Visual Studio will remember your last opened project
- It will appear in **File → Open Recent** at the top

