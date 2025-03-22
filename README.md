# SkinTray

**SkinTray** is a Rainmeter plugin written in C# that creates a customizable system tray icon with dynamic features. It supports various mouse events including left, right, middle clicks, double-clicks, and mouse wheel actions. Additionally, it offers a dark mode for the context menu when configured, making it an ideal choice for users who want a feature-rich and modern tray icon experience in Rainmeter.

## Features

- **Dynamic Icon Updates:** Change the tray icon on the fly without causing flickering.
- **Multiple Mouse Actions:** 
  - Left, Right, and Middle mouse clicks.
  - Double-click events.
  - Mouse wheel actions (scroll up and down).
- **Custom Context Menus:** 
  - Supports dynamic menu creation.
  - Option to render the context menu in dark mode using a custom renderer.
- **Error Handling:** Logs helpful messages when configuration issues occur (e.g., missing icon file).
- **Seamless Integration:** Works smoothly within the Rainmeter environment.

## Configuration

SkinTray is configured through Rainmeter skin settings. Below is an example configuration snippet:

```ini
[Rainmeter]
Update=1000
AccurateText=1
DynamicWindowSize=1

[Metadata]
Name=SkinTray
Author=NS Tech Bytes
Information=This skin tests the Tray Icon Plugin with various mouse actions.
License=MIT License

[Variables]
Action=Perform Action in Tray Icon

[Calc]
Measure=Calc
Formula=Calc = 0 ? 1 : 0

[MeasureTrayIcon]
Measure=Plugin
Plugin=SkinTray
Disabled=0
Icon="#@#Icon[Calc].ico"
ToolTipText=Tray Icon [Calc]
;Dark or White Context Menu (optional)
DarkContext=0
; Mouse click actions
LeftMouseUpAction=[!Log "Left Mouse Action"][!SetVariable Action  "Left Mouse Action"][!UpdateMeter *][!Redraw]
RightMouseUpAction=[!Log "Right Mouse Action"][!SetVariable Action  "Right Mouse Action"][!UpdateMeter *][!Redraw]
MiddleMouseUpAction=[!Log "MiddleMouseUpAction"][!SetVariable Action  "MiddleMouseUpAction"][!UpdateMeter *][!Redraw]
DoubleClickAction=[!Log "Double Click Action"][!SetVariable Action  "Double Click Action"][!UpdateMeter *][!Redraw]
; Mouse wheel actions
MouseWheelUpAction=[!Log "Mouse ScrollUp"][!SetVariable Action  "Mouse ScrollUp"][!UpdateMeter *][!Redraw]
MouseWheelDownAction=[!Log "Mouse Scrolldown"][!SetVariable Action  "Mouse Scrolldown"][!UpdateMeter *][!Redraw]
; Context menu configuration (optional)
ContextMenuItemCount=2
ContextMenuItem1=Action One
ContextMenuAction1=[!Log "Context Menu Action One"][!SetVariable Action  "Context Menu Action One"][!UpdateMeter *][!Redraw]
ContextMenuItem2=Action Two
ContextMenuAction2=[!Log "Context Menu Action Two"][!SetVariable Action  "Context Menu Action Two"][!UpdateMeter *][!Redraw]
DynamicVariables=1

[MeterString]
Meter=String
Text=#Action#
X=100
Y=50
H=100
W=200
StringAlign=CenterCenter
SolidColor=11f1ab
AntiAlias=1
DynamicVariables=1

```

### Configuration Table

| Key                     | Description                                                      | Default Value             |
|-------------------------|------------------------------------------------------------------|---------------------------|
| `Icon`                  | Path to the tray icon file.                                      | `""` (empty)              |
| `ToolTipText`           | Tooltip text for the tray icon.                                  | `"Tray Icon Plugin"`      |
| `LeftMouseUpAction`     | Action executed on left mouse button click.                      | `""` (empty)              |
| `RightMouseUpAction`    | Action executed on right mouse button click.                     | `""` (empty)              |
| `MiddleMouseUpAction`   | Action executed on middle mouse button click.                    | `""` (empty)              |
| `DoubleClickAction`     | Action executed on a double click.                               | `""` (empty)              |
| `MouseWheelUpAction`    | Action executed when the mouse wheel scrolls up.                 | `""` (empty)              |
| `MouseWheelDownAction`  | Action executed when the mouse wheel scrolls down.               | `""` (empty)              |
| `ContextMenuItemCount`  | Number of context menu items to create.                          | `0`                       |
| `ContextMenuItemX`      | Text for the Xth context menu item (replace X with the item number). | `""` (empty)              |
| `ContextMenuActionX`    | Action for the Xth context menu item.                            | `""` (empty)              |
| `DarkContext`           | If set to 1, displays the context menu in dark mode.             | `0`                       |

## Building the Plugin

To build the **SkinTray** plugin:

1. **Prerequisites:**
   - Visual Studio (2017 or later recommended)
   - .NET Framework (compatible version used by Rainmeter, typically 4.x)
   - Windows Forms libraries

2. **Steps:**
   - Open the solution file in Visual Studio.
   - Ensure that your project is targeting the correct .NET Framework.
   - Build the project to generate `SkinTray.dll`.

3. **Installation:**
   - Copy `SkinTray.dll` into Rainmeter's plugin folder, typically:
     ```
     C:\Users\<YourUsername>\Documents\Rainmeter\Plugins\
     ```
   - Refresh or reload your Rainmeter skin to load the new plugin.

## Usage

After installation and configuration:

- The tray icon will appear in the system tray.
- Use the defined mouse actions (left, right, middle, double-click, mouse wheel) to trigger actions as specified in your Rainmeter configuration.
- Right-clicking (or using your configured mouse actions) will open the context menu, which can appear in dark mode if `DarkContext` is set to `1`.

## Troubleshooting

- **Icon Flickering:**  
  If you experience flickering when changing the icon dynamically, ensure that only the icon property is updated rather than recreating the entire NotifyIcon object.
  
- **Configuration Issues:**  
  Make sure all paths and actions are correctly set in your Rainmeter config. Check the Rainmeter log for detailed error messages.

## Contributing

Contributions are welcome! Feel free to fork the repository and submit pull requests if you have improvements or fixes.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Author

**nstechbytes**

For any issues, suggestions, or feedback, please open an issue or contact me directly.

---

*Happy skinning with Rainmeter!*
