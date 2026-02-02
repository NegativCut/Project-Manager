# Engineering Project Manager

A Windows desktop application for managing engineering projects across multiple software platforms (Altium, Solidworks, Visual Studio, etc.).

## Features

- **Project Numbering**: Auto-incrementing project numbers (0001, 0002, etc.) with format: `0001_ProjectName_Rev1`
- **Revision Control**: Create new revisions with selective file copying from previous versions
- **Multi-App Support**: Track which applications are used per project (Altium, Solidworks, Visual Studio, Documents, and custom apps)
- **Centralized Datasheets**: Link datasheets from a central repository to multiple projects
- **File Browser**: View and open files directly from the app, organized by software type
- **To-Do Lists**: Master task list per project, subdivided by application
- **Directory Management**: Automatically creates and maintains organized folder structures

## Folder Structure

The app organizes files by **software type first**, then by **project**:

```
D:\Engineering\
├── Altium\
│   ├── 0001_CircuitBoard_Rev1\
│   ├── 0001_CircuitBoard_Rev2\
│   └── 0002_PowerSupply_Rev1\
├── Solidworks\
│   ├── 0001_CircuitBoard_Rev1\
│   └── 0002_PowerSupply_Rev1\
├── VisualStudio\
│   └── 0001_CircuitBoard_Rev1\
├── Documents\
│   ├── 0001_CircuitBoard_Rev1\
│   └── 0002_PowerSupply_Rev1\
└── DATASHEETS\
    ├── LM7805.pdf
    ├── ATmega328P.pdf
    └── ...
```

## Installation

### Prerequisites
- Windows 10 or later
- .NET 6.0 Runtime or SDK
- Visual Studio 2022 (for development)

### Build Instructions

1. **Open the project in Visual Studio 2022**
   - Open `ProjectManager.sln` or the folder containing `ProjectManager.csproj`

2. **Restore NuGet packages**
   ```
   Right-click on the solution → Restore NuGet Packages
   ```
   Or use the command line:
   ```
   dotnet restore
   ```

3. **Build the project**
   - Press `Ctrl+Shift+B` or use Build → Build Solution
   - Or from command line:
   ```
   dotnet build
   ```

4. **Run the application**
   - Press `F5` to run with debugging
   - Or press `Ctrl+F5` to run without debugging
   - Or from command line:
   ```
   dotnet run
   ```

### Publishing as Standalone Application

To create a standalone executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The executable will be in: `bin\Release\net6.0-windows\win-x64\publish\ProjectManager.exe`

## First-Time Setup

1. **Launch the application**
2. **Configure Engineering Root Directory**
   - Click "Settings" button
   - Set your Engineering Root (default: `D:\Engineering`)
   - Click Save

The app will automatically create the necessary folder structure.

## Usage Guide

### Creating a New Project

1. Click **"New Project"**
2. Enter the **Project Name** (the number auto-increments)
3. Select **Status** (Planning, In Progress, etc.)
4. **Check the applications** this project will use
5. Click **"Create Project"**

The app will automatically create folders for each selected application.

### Creating a Revision

1. **Select an existing project** from the list
2. Click **"New Revision"**
3. **Check which app folders** to copy files from (Option B strategy)
4. Click **"Create Revision"**

Files from selected apps will be copied to the new revision folders.

### Linking Datasheets

1. Select a project
2. Go to **"Datasheets"** tab
3. Click **"Link Datasheet"**
4. Browse to `D:\Engineering\DATASHEETS\` and select files
5. Click Open

Datasheets are stored centrally and linked to projects (no duplication).

### Managing Tasks

1. Select a project
2. Go to **"To-Do List"** tab
3. Click **"Add Task"**
4. Select the app, set priority, and enter description
5. Check tasks as complete directly in the grid

### Adding Custom Applications

1. Click **"Manage Apps"**
2. Type the app name (e.g., "KiCad", "AutoCAD")
3. Click **"Add App"**
4. The app will be available for all projects

### Viewing Files

1. Select a project
2. Go to **"Files"** tab
3. Files are organized by application
4. **Click a filename** to open it in its default program
5. **Click "Open [App] Folder"** to browse in Windows Explorer

## Database Location

Project data is stored in SQLite database:
- Windows: `C:\Users\[YourName]\AppData\Roaming\ProjectManager\projects.db`

## Project Information Tracked

- Project Number (auto-increment)
- Project Name
- Revision Number
- Date Created
- Status (Planning, In Progress, Review, Complete, On Hold, Cancelled)
- Issues/Notes
- Apps Used
- Linked Datasheets
- To-Do Tasks (with completion status and priority)

## Tips

- Use the **Search** box to filter projects by number or name
- The app **automatically saves** changes when you edit project details
- **Double-click a datasheet** in the grid to open it
- You can **edit task descriptions and priorities** directly in the grid
- **Check/uncheck apps** in the Overview tab to add/remove app folders

## Troubleshooting

### "Database is locked" error
- Close any other instances of the app
- Check that no other program is accessing the database file

### Folders not appearing
- Click the **"Refresh Files"** button
- Verify the Engineering Root path in Settings
- Ensure you have write permissions to the directory

### Missing files after creating revision
- Verify which apps you selected to copy from
- Check the source revision folders have files

## Future Enhancements (Ideas)

- Export project list to Excel
- Search within datasheets
- Project templates
- Cloud backup integration
- Git integration for code projects
- BOM (Bill of Materials) tracking

## License

This project is provided as-is for personal and commercial use.

## Support

For issues or feature requests, please contact your development team.
