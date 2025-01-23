# DXF Inspect

A cross-platform DXF file viewer and inspector built with [Avalonia UI](https://avaloniaui.net/). DXF Inspect allows you to explore and analyze DXF (Drawing Exchange Format) files with an intuitive tree-based interface.

[![CI](https://github.com/wieslawsoltes/dxfinspect/actions/workflows/build.yml/badge.svg)](https://github.com/wieslawsoltes/dxfinspect/actions/workflows/build.yml)
[![Deploy to GitHub Pages](https://github.com/wieslawsoltes/dxfinspect/actions/workflows/pages.yml/badge.svg)](https://github.com/wieslawsoltes/dxfinspect/actions/workflows/pages.yml)

![MIT License](https://img.shields.io/github/license/wieslawsoltes/dxfinspect)

![image](https://github.com/user-attachments/assets/6886a6c5-b5d6-490c-aef1-42e8f6156534)
![image](https://github.com/user-attachments/assets/7587620e-58f5-4552-928e-f8837030a6d9)
![image](https://github.com/user-attachments/assets/60070e6d-9839-4b6d-ba09-34f49548cda3)
![image](https://github.com/user-attachments/assets/c23a000e-cd81-42c4-8f1c-6716efa5f161)

## Features

- Cross-platform support (Windows, macOS, Linux)
- Tree-based DXF file structure visualization with efficient data handling
- Smart caching system for improved performance with large files
- Memory-efficient tag processing using weak references
- Detailed group code information and descriptions
- Advanced filtering capabilities:
    - Filter by line range
    - Filter by group code with exact match and case sensitivity options
    - Filter by data content with partial match and case sensitivity options
    - Auto-complete suggestions for both code and data filters
    - Real-time filter updates
- Hierarchical data exploration with size information
- Multi-tab interface for viewing multiple files or sections
- Progress tracking during file loading
- Comprehensive context menu actions including:
    - Copy code
    - Copy data
    - Copy code + data
    - Copy entire object tree
    - Filter operations
    - Open in new tab
- Size information display showing data size in appropriate units (B, KB, MB, GB)
- Performance optimizations for handling large DXF files
- Theme support using Semi.Avalonia styling

## Installation

### Prerequisites

- .NET 9.0 SDK or later

### Building from Source

1. Clone the repository:
```bash
git clone https://github.com/wieslawsoltes/dxfinspect.git
cd dxfinspect
```

2. Build the project:
```bash
dotnet build
```

3. Run the application:
```bash
dotnet run
```

## Usage

### Getting Started

1. Launch DXF Inspect
2. Click the "Load DXF" button in the top-left corner
3. Select one or more DXF files to open - each file will open in a new tab
4. The main interface shows a tree view of the DXF structure with size information

### Understanding the Interface

The tree view displays:
- **Lines**: Line range in the file
- **Code**: DXF group code number
- **Data**: Content value
- **Size**: Data size in appropriate units
- **Value Type**: Expected data type for the group code
- **Description**: Detailed group code description

### Navigation and Filtering

#### Tree Navigation
- Use expand/collapse controls for tree nodes
- "Expand All" and "Collapse All" buttons for quick navigation
- Multi-tab interface for multiple files or views

#### Advanced Filtering System
- **Code Filters**:
    - Auto-complete from available codes
    - Toggle exact match and case sensitivity
    - Multiple filter tags support
    - Quick reset options

- **Data Filters**:
    - Auto-complete from available values
    - Toggle exact match and case sensitivity
    - Multiple filter tags support
    - Real-time filtering

- **Line Range Filter**:
    - Precise control over line number range
    - Individual reset controls for start/end
    - Quick full range reset

### Context Menu Features

Right-click any element to access:

1. **Filter Operations**:
    - Filter by Line Range
    - Filter by Data
    - Filter by Code
    - Reset options for all filters

2. **Copy Operations**:
    - Copy Code
    - Copy Data
    - Copy Code + Data
    - Copy Object Tree (includes all child elements)

3. **View Operations**:
    - Open in New Tab (creates focused view of selected element)

### Performance Features

- Smart caching system for DXF tags
- Memory-efficient processing using weak references
- Optimized parsing for large files
- Progress tracking during file loading
- Efficient filter application system

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.TXT) file for details.
