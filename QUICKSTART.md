# Quick Start Guide

## Building the Application

### Option 1: Using Visual Studio 2022

1. Open `ProjectManager.sln` in Visual Studio 2022
2. Wait for NuGet packages to restore automatically
3. Press `F5` to build and run

### Option 2: Using Command Line

```bash
# Navigate to the project folder
cd ProjectManager

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

## First Use

When you first launch the app:

1. The default Engineering Root will be set to `D:\Engineering`
2. You can change this in Settings if needed
3. The app will create:
   - `D:\Engineering\Altium\`
   - `D:\Engineering\Solidworks\`
   - `D:\Engineering\VisualStudio\`
   - `D:\Engineering\Documents\`
   - `D:\Engineering\DATASHEETS\`

## Creating Your First Project

1. Click **"New Project"**
2. The project number will be **0001** (auto)
3. Enter a name like **"PowerSupply"**
4. Select status: **"Planning"**
5. Check apps: **Altium**, **Solidworks**, **Documents**
6. Click **"Create Project"**

This creates:
- `D:\Engineering\Altium\0001_PowerSupply_Rev1\`
- `D:\Engineering\Solidworks\0001_PowerSupply_Rev1\`
- `D:\Engineering\Documents\0001_PowerSupply_Rev1\`

## Working with the Project

### Add some files to your project folders manually
Copy your Altium files to: `D:\Engineering\Altium\0001_PowerSupply_Rev1\`

### Refresh to see files
1. Select your project
2. Go to **Files** tab
3. Click **"Refresh Files"**
4. You'll see your files listed by app

### Add tasks
1. Go to **To-Do List** tab
2. Click **"Add Task"**
3. Select app: **"Altium"**
4. Task: **"Design schematic"**
5. Priority: **"High"**

### Link datasheets
1. First, copy some PDF datasheets to `D:\Engineering\DATASHEETS\`
2. In the app, go to **Datasheets** tab
3. Click **"Link Datasheet"**
4. Select your PDFs
5. They're now linked to this project

## Creating a Revision

When you're ready to create Rev2:

1. Select your **0001_PowerSupply_Rev1** project
2. Click **"New Revision"**
3. Check which apps to copy files from (e.g., just Altium)
4. Click **"Create Revision"**

This creates:
- `D:\Engineering\Altium\0001_PowerSupply_Rev2\` (with copied files)
- `D:\Engineering\Solidworks\0001_PowerSupply_Rev2\` (empty)
- `D:\Engineering\Documents\0001_PowerSupply_Rev2\` (empty)

## Next Steps

- Add more apps with **"Manage Apps"**
- Track project status (Planning → In Progress → Review → Complete)
- Add notes in the Issues field
- Search projects by typing in the search box

## Common Workflows

### Starting a circuit board project
1. New Project: "0001_CircuitBoard_Rev1"
2. Apps: Altium, Solidworks, Documents
3. Add task: "Create schematic" (Altium)
4. Add task: "Design enclosure" (Solidworks)
5. Link component datasheets

### Creating a firmware project
1. New Project: "0002_Firmware_Rev1"  
2. Apps: VisualStudio, Documents
3. Add tasks for development milestones
4. Track bugs in Issues field

### Making a revision after design review
1. Select project
2. New Revision
3. Select apps with changes (e.g., Altium only)
4. Files copy automatically
5. Update status to "In Progress"

That's it! You're ready to manage your engineering projects efficiently.
