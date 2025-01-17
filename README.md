# DXF Inspect

A cross-platform DXF file viewer and inspector built with [Avalonia UI](https://avaloniaui.net/). DXF Inspect allows you to explore and analyze DXF (Drawing Exchange Format) files with an intuitive tree-based interface.

![MIT License](https://img.shields.io/github/license/wieslawsoltes/dxfinspect)

## Features

- Cross-platform support (Windows, macOS, Linux)
- Tree-based DXF file structure visualization
- Detailed group code information and descriptions
- Advanced filtering capabilities:
  - Filter by line range
  - Filter by group code
  - Filter by data content
- Hierarchical data exploration
- Multi-tab interface for viewing multiple files
- Context menu actions for quick operations
- Copy functionalities:
  - Copy code
  - Copy data
  - Copy code + data
  - Copy entire object tree
- Expand/Collapse all nodes
- Open selected nodes in new tabs
- Support for DXF group code information lookup
- Light and dark theme support

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
4. The main interface shows a tree view of the DXF structure

### Understanding the Interface

- **Tree View**: The main area shows the hierarchical structure of your DXF file
  - **Lines**: Shows the line range in the file where this element appears
  - **Code**: The DXF group code number
  - **Data**: The actual content/value
  - **Value Type**: The expected data type for this group code
  - **Description**: Detailed description of what this group code represents

### Navigation

- Click the triangles (▶) to expand/collapse tree nodes
- Use "Expand All" button to show all nodes
- Use "Collapse All" button to collapse the entire tree
- Switch between multiple files using the tabs at the top
- Double-click on tabs to rename them

### Filtering and Searching

#### Basic Filters
- **Code Filter**: 
  - Enter a group code number to show only matching entries
  - Click the "X" button to clear the filter
  - Example: Enter "0" to show only entity type indicators

- **Data Filter**:
  - Enter any text to filter by content
  - The filter is case-insensitive
  - Partial matches are supported
  - Click the "X" button to clear the filter
  - Example: Enter "LINE" to show all elements containing "LINE" in their data

- **Line Range**:
  - Set start and end line numbers to focus on specific sections
  - Use the "X" buttons to reset individual start/end values
  - Use the rightmost "X" to reset both values
  - Example: Set range 100-200 to focus on that section of the file

#### Advanced Filtering
Right-click on any element to access advanced filtering options:
- "Filter by Line Range": Shows only elements within the selected element's line range
- "Filter by Data": Shows all elements matching the selected element's data
- "Filter by Code": Shows all elements with the same group code

### Context Menu Operations

Right-click on any element to access additional options:

1. **Filtering Options**:
   - Filter by Line Range
   - Filter by Data
   - Filter by Code
   - Reset Filters
   - Reset Line Range

2. **Copy Options**:
   - Copy Code: Copies just the group code
   - Copy Data: Copies just the data content
   - Copy Code + Data: Copies both code and data
   - Copy Object Tree: Copies the entire subtree starting from selected element

3. **Tab Operations**:
   - Open in New Tab: Creates a new tab showing just the selected element and its children

### Tips and Tricks

- Use "Open in New Tab" to focus on specific sections of large files
- Combine filters to narrow down your search (e.g., specific code within a line range)
- Reset individual filters instead of all filters to refine your search
- Use "Copy Object Tree" to export specific sections for documentation or analysis
- Watch the file name display to always know which file you're viewing
- Use line ranges to understand the structure of your DXF file

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.TXT) file for details.

## Acknowledgments

- [Avalonia UI](https://avaloniaui.net/) for the cross-platform UI framework
- [ReactiveUI](https://reactiveui.net/) for the MVVM framework

## Author

Wiesław Šoltés
