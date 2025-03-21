# Rainmeter Tray Icon Plugin

A simple Rainmeter plugin written in C# to display a tray icon in the system tray with customizable actions and tooltips. This plugin allows users to add a tray icon to their Rainmeter skins and interact with it using left and right mouse buttons.

## Features

- Displays a tray icon in the system tray.
- Customizable icon, tooltip, and click actions (left and right mouse clicks).
- Supports both left and right click actions configured in Rainmeter.
- Built using C# and .NET Framework.

## Installation

### Prerequisites

- **Rainmeter** (v4.0 or later)
- **.NET Framework** (v4.8 or later) installed on your system.

### Steps

1. **Clone this repository**:

   ```bash
   git clone https://github.com/yourusername/RainmeterTrayIconPlugin.git
   ```
2. **Build the Plugin**:

   - Open the project in **Visual Studio**.
   - Set the target framework to **.NET Framework 4.8**.
   - Build the solution to generate the `SkinTray.dll`.
3. **Copy the Plugin DLL**:

   - Place the compiled `SkinTray.dll` in your Rainmeter plugins folder. Typically, the folder is located at:
     ```
     C:\Program Files\Rainmeter\Plugins\
     ```
4. **Set up Your Rainmeter Skin**:

   - Create or modify a skin and add the following code to a `.ini` file:
     ```ini
     [Rainmeter]
     Update=1000
     AccurateText=1

     [MeasureTray]
     Measure=Plugin
     Plugin=SkinTray
     Icon=#@#icon.ico
     ToolTipText=TrayIcon Example
     LeftMouseUpAction=[!ToggleFade]
     RightMouseUpAction=[!SkinMenu]
     ```
   - Ensure your icon is placed in the skin's `@Resources` folder (or the specified directory).
5. **Reload the Skin**:

   - Right-click the Rainmeter icon in the system tray and click **Refresh All** or **Reload** the skin.

## Configuration Options

- **IconName**: The path to the icon file (supports `.ico` format).

  - Example: `IconName=#@#icon.ico` (for an icon inside the `@Resources` folder).
- **ToolTipText**: The text that appears when you hover over the tray icon.
- **LeftMouseUpAction**: Action triggered when the user left-clicks on the tray icon.

  - Example: `LeftMouseUpAction=[!ToggleFade]`
- **RightMouseUpAction**: Action triggered when the user right-clicks on the tray icon.

  - Example: `RightMouseUpAction=[!SkinMenu]`

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Feel free to fork this repository and create pull requests for improvements or bug fixes. Any contributions are welcome!

## Acknowledgements

- [Rainmeter](https://www.rainmeter.net/) - For being an amazing desktop customization tool.
- [.NET Framework](https://dotnet.microsoft.com/en-us/) - For enabling easy Windows development.

## Issues

If you encounter any issues or have any questions, please feel free to open an issue.
