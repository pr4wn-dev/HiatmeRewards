# How to Sync Cursor Settings Between PCs

This guide will help you ensure all your Cursor settings, including agent settings, sync between your PCs.

## Automatic Sync (If You're Logged In)

If you're logged into the same Cursor account on both PCs, most settings should sync automatically. However, let's verify and ensure everything is set up correctly.

## Step 1: Verify You're Logged In

1. Open Cursor
2. Click on your **profile icon** (bottom left) or go to **File → Preferences → Settings**
3. Check if you see your account name/email
4. If not logged in, click **Sign In** and use the same account

## Step 2: Enable Settings Sync

1. Press **`Ctrl+,`** to open Settings
2. Search for: **"sync"**
3. Look for: **"Settings Sync"** or **"Sync"**
4. Enable it if it's not already enabled

**Or manually:**
1. **File → Preferences → Settings** (or `Ctrl+,`)
2. Click on the **gear icon** (⚙️) in the top right
3. Select **"Turn on Settings Sync"**
4. Choose what to sync:
   - ✅ Settings
   - ✅ Keybindings
   - ✅ Extensions
   - ✅ UI State
   - ✅ Snippets
   - ✅ Workspace Settings (optional)

## Step 3: Agent Settings

### Find Agent Settings:

1. Press **`Ctrl+,`** to open Settings
2. Search for: **"agent"** or **"AI"** or **"cursor"**
3. Look for settings like:
   - **Cursor: Agent Settings**
   - **Cursor: Model**
   - **Cursor: Temperature**
   - **Cursor: Max Tokens**
   - **Cursor: System Prompt**

### Common Agent Settings to Check:

**Model Selection:**
- **Cursor: Model** - Which AI model to use (Claude, GPT-4, etc.)
- Make sure it's set the same on both PCs

**Agent Behavior:**
- **Cursor: Agent Mode** - Auto-suggest, manual, etc.
- **Cursor: Auto-apply Suggestions** - Whether to auto-apply code suggestions
- **Cursor: Show Inline Suggestions** - Show suggestions as you type

**Advanced:**
- **Cursor: Temperature** - Creativity level (0-1)
- **Cursor: Max Tokens** - Response length
- **Cursor: System Prompt** - Custom instructions for the AI

## Step 4: Export/Import Settings (Manual Backup)

If automatic sync isn't working, you can manually export and import:

### Export Settings:

1. Press **`Ctrl+Shift+P`** to open Command Palette
2. Type: **"Preferences: Open User Settings (JSON)"**
3. Copy the entire contents
4. Save to a file (e.g., `cursor-settings.json`)
5. Upload to cloud storage or copy to other PC

### Import Settings:

1. On the other PC, press **`Ctrl+Shift+P`**
2. Type: **"Preferences: Open User Settings (JSON)"**
3. Paste the settings you exported
4. Save (`Ctrl+S`)

## Step 5: Workspace Settings

Workspace-specific settings are stored in `.vscode/settings.json` in your project folder. These should be in Git, so they'll sync automatically when you pull the repo.

### Check Workspace Settings:

1. In your project folder, look for: `.vscode/settings.json`
2. If it doesn't exist, create it:
   ```json
   {
     "cursor.agent.enabled": true,
     "cursor.agent.model": "claude-3-5-sonnet",
     // Add other workspace-specific settings
   }
   ```

## Step 6: Verify Sync on Other PC

After setting up on the first PC:

1. **On the other PC:**
   - Log into the same Cursor account
   - Open Settings (`Ctrl+,`)
   - Check that your settings match

2. **If settings don't match:**
   - Wait a few minutes (sync can take time)
   - Restart Cursor
   - Manually check key settings

## Key Settings to Verify Match:

### Agent/AI Settings:
- [ ] Model selection (Claude, GPT-4, etc.)
- [ ] Auto-suggest enabled/disabled
- [ ] Inline suggestions on/off
- [ ] Temperature setting
- [ ] System prompt (if customized)

### Editor Settings:
- [ ] Font size
- [ ] Theme
- [ ] Tab size
- [ ] Word wrap
- [ ] Auto-save

### Extension Settings:
- [ ] Extensions installed
- [ ] Extension configurations

## Troubleshooting

### Settings Not Syncing?

1. **Check Account:**
   - Make sure you're logged into the same account on both PCs
   - Sign out and sign back in if needed

2. **Manual Sync:**
   - Press **`Ctrl+Shift+P`**
   - Type: **"Settings Sync: Turn on"**
   - Or: **"Settings Sync: Sync Now"**

3. **Check Sync Status:**
   - Look at the bottom status bar
   - Should show sync icon if enabled

4. **Force Sync:**
   - Sign out of Cursor
   - Sign back in
   - Settings should re-sync

### Agent Settings Not Working?

1. **Check Cursor Subscription:**
   - Some agent features require a paid plan
   - Verify your subscription is active

2. **Restart Cursor:**
   - Close completely
   - Reopen
   - Settings should apply

3. **Check Workspace Settings:**
   - Workspace settings override user settings
   - Check `.vscode/settings.json` in your project

## Quick Checklist

Before switching PCs, verify:

- [ ] Logged into same Cursor account
- [ ] Settings Sync enabled
- [ ] Agent model selected
- [ ] Key extensions installed
- [ ] Theme/editor preferences set
- [ ] Workspace settings in Git (if needed)

## Pro Tips

### Tip 1: Use Workspace Settings for Project-Specific Settings
Store project-specific agent settings in `.vscode/settings.json` so they're in Git and sync automatically.

### Tip 2: Document Your Preferred Settings
Keep a note of your preferred agent settings so you can quickly verify they match.

### Tip 3: Test Sync
Make a small setting change on one PC, wait a minute, then check if it appears on the other PC.

### Tip 4: Backup Settings File
Periodically export your settings JSON as a backup in case sync fails.

