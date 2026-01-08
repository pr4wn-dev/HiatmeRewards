# How to Update Your Project on Another PC

If you have an outdated version of the project, here's how to update it to the latest version.

## Quick Update (Recommended)

Open PowerShell or Command Prompt in your project folder and run:

```powershell
# Navigate to your project folder
cd C:\Users\YourUsername\source\repos\HiatmeRewards

# Check current status
git status

# Fetch the latest changes
git fetch origin

# Pull the latest changes
git pull origin master
```

## Step-by-Step Instructions

### Step 1: Check for Uncommitted Changes

First, see if you have any local changes that might conflict:

```powershell
cd C:\Users\YourUsername\source\repos\HiatmeRewards
git status
```

**If you see "nothing to commit, working tree clean":**
- You're safe to pull - proceed to Step 2

**If you see modified files:**
- You have local changes. Options:
  - **Option A:** Commit your changes first (if you want to keep them)
  - **Option B:** Stash your changes (temporarily save them)
  - **Option C:** Discard your changes (if you don't need them)

### Step 2: Update from Remote

```powershell
# Make sure you're on the master branch
git checkout master

# Pull the latest changes
git pull origin master
```

## If You Have Local Changes You Want to Keep

### Option 1: Commit Your Changes First
```powershell
git add .
git commit -m "My local changes"
git pull origin master
```

### Option 2: Stash Your Changes (Save for Later)
```powershell
# Save your changes temporarily
git stash

# Pull the latest changes
git pull origin master

# Reapply your changes (if needed)
git stash pop
```

## If You Have Local Changes You Don't Need

```powershell
# Discard all local changes
git reset --hard HEAD

# Pull the latest changes
git pull origin master
```

## If You Get Merge Conflicts

If `git pull` shows conflicts:

1. **See what files have conflicts:**
   ```powershell
   git status
   ```

2. **Resolve conflicts:**
   - Open the conflicted files in Cursor
   - Look for conflict markers: `<<<<<<<`, `=======`, `>>>>>>>`
   - Edit the file to resolve the conflict
   - Remove the conflict markers

3. **After resolving:**
   ```powershell
   git add .
   git commit -m "Resolved merge conflicts"
   ```

## Force Update (Nuclear Option - Use with Caution)

**⚠️ WARNING: This will discard ALL local changes and make your local repo match the remote exactly.**

Only use this if:
- You don't have any important local changes
- You want to completely reset to match the remote

```powershell
# Fetch latest
git fetch origin

# Reset to match remote exactly
git reset --hard origin/master

# Clean up any untracked files
git clean -fd
```

## Verify the Update

After pulling, verify you have the latest:

```powershell
# Check the latest commit
git log -1

# Check status
git status
```

## For the PHP Website Project

Same process, just different folder:

```powershell
cd C:\Projects\Hiatme-PHP-Website
git status
git pull origin master
```

## Troubleshooting

### "Your branch is behind 'origin/main'"
This is normal - just run `git pull origin master`

### "Your branch and 'origin/main' have diverged"
This means you have local commits that aren't on remote. You may need to:
```powershell
git pull --rebase origin master
```

### "Permission denied" or Authentication errors
You may need to re-authenticate:
- Git will prompt you for credentials
- Or use: `git credential-manager erase` then try again

### "fatal: refusing to merge unrelated histories"
This usually happens on first setup. Use:
```powershell
git pull origin master --allow-unrelated-histories
```

