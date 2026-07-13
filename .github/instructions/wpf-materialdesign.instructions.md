---
applyTo: "**/*.xaml,**/Features/Theming/**/*.cs"
---

# WPF Material Design Styling

This file covers the MaterialDesignInXamlToolkit (MDIX) theming setup for WPF applications.

---

## App.xaml Setup

```xml
<Application x:Class="MyApp.App"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             ...>
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <materialDesign:BundledTheme
                    BaseTheme="Light"
                    PrimaryColor="DeepPurple"
                    SecondaryColor="Lime" />
                <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign3.Defaults.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

**Rules:**

- `BundledTheme` MUST come before `MaterialDesign3.Defaults.xaml` in merged dictionaries order.
- `BaseTheme` is always `"Light"` in XAML. Set the actual theme at startup from persisted settings (see Theme Switching below).
- `PrimaryColor` and `SecondaryColor` use SWATCH names (Teal, Cyan, DeepPurple, Lime, etc.), not hex values.
- Use `MaterialDesign3.Defaults.xaml`. Do not use the older `MaterialDesignTheme.Defaults.xaml` or `MaterialDesign2.Defaults.xaml`.

---

## Window Configuration

**For MDIX v5.0.0+ (recommended):** Use `Style="{StaticResource MaterialDesignWindow}"`. This is the officially documented approach and is used by the MDIX v3 demo app. Despite the `StaticResource` in the style key name, the style's internal setters use `DynamicResource` in v5.x and respond correctly to runtime theme changes.

```xml
<Window ...
    Style="{StaticResource MaterialDesignWindow}"
    ...>
```

**For MDIX v3.0.0–v4.9.0:** The `MaterialDesignWindow` style in these versions set `Window.Background` via a `StaticResource` that did not respond to runtime theme changes. If you target these older versions, set the attributes directly:

```xml
<Window ...
    TextElement.Foreground="{DynamicResource MaterialDesign.Brush.Foreground}"
    Background="{DynamicResource MaterialDesign.Brush.Background}"
    ...>
```

**Defensive fallback (works for all versions):** Set both the style and the direct attributes. The direct `DynamicResource` attributes take precedence over the style setters (local value > style setter), so theme switching always works regardless of MDIX version:

```xml
<Window ...
    Style="{StaticResource MaterialDesignWindow}"
    TextElement.Foreground="{DynamicResource MaterialDesign.Brush.Foreground}"
    Background="{DynamicResource MaterialDesign.Brush.Background}"
    ...>
```

For `UserControl` views, no special attributes are needed. Use `DynamicResource` for every brush reference.

---

## Theme Switching

### Theme Value Type

Define a closed set of theme values. Do not pass raw strings.

**Preferred — using `EnrichedEnum<T>` from AllOverIt** (when the `AllOverIt` package is available):

```csharp
using AllOverIt.Patterns.Enumeration;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

[JsonConverter(typeof(AppThemeJsonConverter))]
public sealed class AppTheme : EnrichedEnum<AppTheme>
{
    public static readonly AppTheme Light = new(1);
    public static readonly AppTheme Dark = new(2);

    public AppTheme(int value, [CallerMemberName] string name = "")
        : base(value, name)
    {
    }
}
```

The JSON converter writes `Name` and deserializes via the built-in `AppTheme.From(string)` method:

```csharp
public sealed class AppThemeJsonConverter : JsonConverter<AppTheme>
{
    public override AppTheme? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var name = reader.GetString();
        return name is null ? null : AppTheme.From(name);
    }

    public override void Write(Utf8JsonWriter writer, AppTheme value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Name);
    }
}
```

**Fallback — plain sealed class** (when no external dependencies are desired):

```csharp
public sealed class AppTheme
{
    public static readonly AppTheme Light = new("Light");
    public static readonly AppTheme Dark = new("Dark");

    public string Name { get; }

    private AppTheme(string name) => Name = name;

    public override string ToString() => Name;
}
```

With the fallback, also provide a `JsonConverter<AppTheme>` that reads/writes `Name` and register it via `[JsonConverter(typeof(AppThemeJsonConverter))]`.

### Theme Service

The canonical MDIX runtime theme switch. Create this once, register as a singleton:

```csharp
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;

public sealed class ThemeService
{
    public void ApplyTheme(AppTheme theme)
    {
        var paletteHelper = new PaletteHelper();
        var currentTheme = paletteHelper.GetTheme();

        currentTheme.SetBaseTheme(theme == AppTheme.Dark
            ? BaseTheme.Dark
            : BaseTheme.Light);

        paletteHelper.SetTheme(currentTheme);
    }
}
```

### Startup Wiring

Apply the persisted theme **before** showing any windows:

```csharp
await settings.LoadAsync();
themeService.ApplyTheme(settings.Theme);
mainWindow.Show();
```

### Live Preview (Settings Dialog)

When a user toggles theme in settings, apply it immediately without waiting for Save:

```csharp
isDarkThemeSubscription = this.WhenAnyValue(vm => vm.IsDarkTheme)
    .Subscribe(isDark => themeService.ApplyTheme(isDark ? AppTheme.Dark : AppTheme.Light));
```

On Cancel, revert to the original theme that was active when the dialog opened. Capture it before the live-preview subscription fires.

**Important:** `WhenAnyValue` fires the initial value on subscription. If you capture the original theme and subscribe in the same constructor, capture first — subscription will immediately re-apply it (which is fine since it matches).

---

## Brush Key Reference

### Valid Canonical Keys

All canonical keys follow the `MaterialDesign.Brush.<Category>.<Property>` naming convention. Always use these — never the obsolete short-form keys.

**Surfaces & Backgrounds:**

| Key                                    | Usage                              |
| -------------------------------------- | ---------------------------------- |
| `MaterialDesign.Brush.Background`      | Window / page background           |
| `MaterialDesign.Brush.Card.Background` | Card / elevated surface background |
| `MaterialDesign.Brush.Card.Border`     | Card border                        |

**Text:**

| Key                                    | Usage                           |
| -------------------------------------- | ------------------------------- |
| `MaterialDesign.Brush.Foreground`      | Primary text                    |
| `MaterialDesign.Brush.ForegroundLight` | Secondary / hint / subdued text |

**Primary & Secondary Accent Colors:**

| Key                                         | Usage                        |
| ------------------------------------------- | ---------------------------- |
| `MaterialDesign.Brush.Primary`              | Primary accent (mid hue)     |
| `MaterialDesign.Brush.Primary.Light`        | Primary light variant        |
| `MaterialDesign.Brush.Primary.Dark`         | Primary dark variant         |
| `MaterialDesign.Brush.Secondary`            | Secondary accent (mid hue)   |
| `MaterialDesign.Brush.Secondary.Light`      | Secondary light variant      |
| `MaterialDesign.Brush.Secondary.Dark`       | Secondary dark variant       |
| `MaterialDesign.Brush.Primary.Foreground`   | Text on primary background   |
| `MaterialDesign.Brush.Secondary.Foreground` | Text on secondary background |

**Validation & Separators:**

| Key                                         | Usage                              |
| ------------------------------------------- | ---------------------------------- |
| `MaterialDesign.Brush.ValidationError`      | Error indicators                   |
| `MaterialDesign.Brush.Separator.Background` | Dividers, borders between sections |

Additional keys exist for TextBox, ComboBox, DataGrid, ToolBar, ListBox, ToggleButton, ScrollBar, TabControl, GridSplitter, and other controls. The complete list is in the MDIX test suite: [`MaterialDesignThemes.UITests/WPF/Theme/ThemeTests.g.cs`](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/blob/master/tests/MaterialDesignThemes.UITests/WPF/Theme/ThemeTests.g.cs) — search for `GetBrushResourceNames()`.

---

## ScrollBar & ScrollViewer Styling

MDIX provides two built-in ScrollViewer/ScrollBar styles. Choose based on whether the scrollbar should always be visible or auto-hide.

### Standard Themed ScrollViewer

When `MaterialDesign3.Defaults.xaml` is loaded, all `ScrollViewer` and `ScrollBar` controls pick up the default Material Design theme automatically. Set `VerticalScrollBarVisibility="Auto"` (the default) for a scrollbar that appears when needed and is visibly styled:

```xml
<ScrollViewer VerticalScrollBarVisibility="Auto">
    <!--  Scrollable content  -->
</ScrollViewer>
```

The standard themed scrollbar has a visible track and thumb that respond to the current theme (light/dark) and respect primary/secondary accent colors.

### Minimal ScrollBar (Auto-Hide)

Use `MaterialDesignScrollBarMinimal` for a thinner scrollbar that auto-hides — ideal for navigation drawers or compact lists:

```xml
<ScrollViewer>
    <ScrollViewer.Resources>
        <Style TargetType="ScrollBar" BasedOn="{StaticResource MaterialDesignScrollBarMinimal}" />
    </ScrollViewer.Resources>
    <!--  Content  -->
</ScrollViewer>
```

The MDIX demo app uses `MaterialDesignScrollBarMinimal` in its left navigation drawer. This style works well for content areas where you don't want the scrollbar chrome distracting from the primary UI.

### ScrollBar Brush Keys

The following `MaterialDesign.Brush.ScrollBar.*` keys are updated on theme change and can be used for custom scrollbar templates:

| Key                                              | Usage                        |
| ------------------------------------------------ | ---------------------------- |
| `MaterialDesign.Brush.ScrollBar.Background`      | Track background             |
| `MaterialDesign.Brush.ScrollBar.Border`          | Track/thumb border           |
| `MaterialDesign.Brush.ScrollBar.Foreground`      | Thumb foreground (grip)      |
| `MaterialDesign.Brush.ScrollBar.MouseOver`       | Thumb on hover               |
| `MaterialDesign.Brush.ScrollBar.MouseOverBorder` | Thumb border on hover        |
| `MaterialDesign.Brush.ScrollBar.Pressed`         | Thumb while pressed/dragging |
| `MaterialDesign.Brush.ScrollBar.PressedBorder`   | Thumb border while pressed   |

### Layout Recommendation

Place a `ScrollViewer` around only the content that needs scrolling — do not wrap an entire page in a single `ScrollViewer`. This keeps headers, CTAs, and other fixed-position elements visible while the scrollable area moves independently. Use a `Grid` with `RowDefinition Height="*"` for the scrollable row so it fills all remaining space.

## Obsolete Keys — NEVER USE

These keys exist only as backward-compatibility shims in `MaterialDesignTheme.ObsoleteBrushes.xaml`. They are forwarded via `StaticResource` and may not update correctly on theme changes.

| Obsolete Key                         | Canonical Replacement                          |
| ------------------------------------ | ---------------------------------------------- |
| `MaterialDesignPaper`                | `MaterialDesign.Brush.Background`              |
| `MaterialDesignBackground`           | `MaterialDesign.Brush.Card.Background`         |
| `MaterialDesignCardBackground`       | `MaterialDesign.Brush.Card.Background`         |
| `MaterialDesignBody`                 | `MaterialDesign.Brush.Foreground`              |
| `MaterialDesignBodyLight`            | `MaterialDesign.Brush.ForegroundLight`         |
| `MaterialDesignCheckBoxOff`          | `MaterialDesign.Brush.ForegroundLight`         |
| `MaterialDesignTextBoxBorder`        | `MaterialDesign.Brush.ForegroundLight`         |
| `MaterialDesignDivider`              | `MaterialDesign.Brush.Separator.Background`    |
| `MaterialDesignValidationErrorBrush` | `MaterialDesign.Brush.ValidationError`         |
| `MaterialDesignChipBackground`       | `MaterialDesign.Brush.Chip.Background`         |
| `MaterialDesignColumnHeader`         | `MaterialDesign.Brush.Header.Foreground`       |
| `MaterialDesignTextAreaBorder`       | `MaterialDesign.Brush.Header.Foreground`       |
| `PrimaryHueMidBrush`                 | `MaterialDesign.Brush.Primary`                 |
| `PrimaryHueLightBrush`               | `MaterialDesign.Brush.Primary.Light`           |
| `PrimaryHueDarkBrush`                | `MaterialDesign.Brush.Primary.Dark`            |
| `SecondaryHueMidBrush`               | `MaterialDesign.Brush.Secondary`               |
| `SecondaryHueLightBrush`             | `MaterialDesign.Brush.Secondary.Light`         |
| `SecondaryHueDarkBrush`              | `MaterialDesign.Brush.Secondary.Dark`          |
| `MaterialDesignFlatButtonClick`      | `MaterialDesign.Brush.Button.FlatClick`        |
| `MaterialDesignFlatButtonRipple`     | `MaterialDesign.Brush.Button.FlatRipple`       |
| `MaterialDesignToolBarBackground`    | `MaterialDesign.Brush.ToolBar.Background`      |
| `MaterialDesignToolBackground`       | `MaterialDesign.Brush.ToolBar.Item.Background` |
| `MaterialDesignToolForeground`       | `MaterialDesign.Brush.ToolBar.Item.Foreground` |

### Fake Keys — NEVER USE

These look like MDIX keys but do NOT exist in the codebase. WPF silently falls back to its default brush, producing colors that never change with the theme:

| Fake Key                       | Correct Replacement                    |
| ------------------------------ | -------------------------------------- |
| `MaterialDesignHintForeground` | `MaterialDesign.Brush.ForegroundLight` |

---

## Problem-Solving Resources

When theming breaks, consult these in order:

1. **This file** — check against the obsolete/fake key lists first.

2. **MDIX generated sources** (in the NuGet package or [GitHub repo](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)):
   - `ResourceDictionaryExtensions.g.cs` — `LoadThemeColors()` shows which obsolete keys map to which canonical keys; `ApplyThemeColors()` shows every key updated on theme change.
   - `ThemeColors.json` — source of truth for brush definitions, obsolete keys, and alternate keys.
   - `tests/MaterialDesignThemes.UITests/WPF/Theme/ThemeTests.g.cs` — `GetBrushResourceNames()` lists every valid canonical key; `GetObsoleteBrushResourceNames()` lists every obsolete key.
   - `build/MigrateBrushes.ps1` — official obsolete→canonical migration script.
   - `PaletteHelper.cs` — `GetResourceDictionary()` looks for `IMaterialDesignThemeDictionary` in `Application.Current.Resources.MergedDictionaries`.

3. **Official docs:**
   - [Getting Started](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/wiki/Getting-Started)
   - [Advanced Theming](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/wiki/Advanced-Theming)
   - [Brush Names](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/wiki/Brush-Names)

4. **MDIX demo app:** `src/MainDemo.Wpf` in the repo — demonstrates correct window and theme setup.

5. **Context7:** Query `MaterialDesignInXamlToolkit` for API syntax and patterns.

---

## Modal Dialog Pattern

All modal dialogs shown via `DialogHost.Show()` follow a consistent visual structure. Use `MessageDialog` for simple info/error messages or create a purpose-specific dialog that inherits this pattern.

### Structure

```
materialDesign:Card (Padding="0", Margin="24", MaxWidth="480")
└── Grid
    ├── Column 0: 5px accent Border (semantic colour)
    └── Column 1: StackPanel (Margin="24,20,24,20")
        ├── Header: horizontal StackPanel
        │   ├── 44×44 Ellipse (Opacity="0.12") — tinted icon background
        │   ├── PackIcon (24×24, centred in ellipse)
        │   └── TitleText (FontSize="18", SemiBold)
        ├── Message body (FontSize="14", ForegroundLight, LineHeight="22")
        ├── 1px separator (Separator.Background, Margin="0,20,0,16")
        └── Action buttons (right-aligned, horizontal)
```

### Dependency Properties (Shared Contract)

Every modal dialog that uses this pattern should expose these dependency properties:

| Property         | Type     | Default                          | Purpose                                                                 |
| ---------------- | -------- | -------------------------------- | ----------------------------------------------------------------------- |
| `IconForeground` | `string` | `"MaterialDesign.Brush.Primary"` | DynamicResource key for icon, icon-background ellipse, and accent strip |
| `AccentBrushKey` | `string` | `"MaterialDesign.Brush.Primary"` | DynamicResource key for the accent strip independently of the icon      |

When `IconForeground` changes, the callback cascades to all three named elements via `SetResourceReference`:

```csharp
private static void OnIconForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    var dialog = (T)d;
    var key = (string)e.NewValue;
    dialog.IconControl.SetResourceReference(ForegroundProperty, key);
    dialog.IconBackground.SetResourceReference(Ellipse.FillProperty, key);
    dialog.AccentStrip.SetResourceReference(Border.BackgroundProperty, key);
}
```

### Semantic Accent Colours

| Context                   | `IconForeground`                         | Icon                 |
| ------------------------- | ---------------------------------------- | -------------------- |
| Information / neutral     | `MaterialDesign.Brush.Primary` (default) | `InformationOutline` |
| Warning / unsaved changes | `MaterialDesign.Brush.Primary` (default) | `AlertOutline`       |
| Error                     | `MaterialDesign.Brush.ValidationError`   | `ErrorOutline`       |

### Usage Examples

**Error dialog:**

```csharp
var dialog = new MessageDialog
{
    Title = "Open Failed",
    Message = "Could not open the project file.",
    IconKind = PackIconKind.ErrorOutline,
    IconForeground = "MaterialDesign.Brush.ValidationError"
};
await DialogHost.Show(dialog, "MainDialogHost");
```

**Info / close-prevention:**

```csharp
var dialog = new MessageDialog
{
    Title = "Operation in Progress",
    Message = "Please wait for it to complete or cancel it before closing.",
    IconKind = PackIconKind.InformationOutline
    // IconForeground defaults to Primary (blue accent)
};
await DialogHost.Show(dialog, "MainDialogHost");
```

**Unsaved changes confirmation:**

```csharp
var dialog = new ConfirmDiscardDialog
{
    Title = $"Save changes to \"{projectName}\"?",
    // IconForeground defaults to Primary, Icon is always AlertOutline
};
var result = await DialogHost.Show(dialog, "MainDialogHost");
```

### Existing Dialogs

| Dialog                 | Purpose                                               | File                              |
| ---------------------- | ----------------------------------------------------- | --------------------------------- |
| `DialogBase`           | Abstract base with shared DPs and cascading callbacks | `Views/DialogBase.cs`             |
| `MessageDialog`        | General-purpose info/error/warning                    | `Views/MessageDialog.xaml`        |
| `ConfirmDiscardDialog` | Save/Discard/Cancel confirmation                      | `Views/ConfirmDiscardDialog.xaml` |

When creating a new modal dialog, inherit from `DialogBase` and follow this pattern for visual consistency. Wire the named XAML elements to the base class properties in the constructor after `InitializeComponent()`:

```csharp
public MyNewDialog()
{
    InitializeComponent();

    DialogIcon = IconControl;
    DialogIconBackground = IconBackground;
    DialogAccentStrip = AccentStrip;
}
```

---

## Common Pitfalls

### StaticResource in window style (pre-v5.0.0 only)

In MDIX versions before 5.0.0, `Style="{StaticResource MaterialDesignWindow}"` cached the Light theme background because the style's setters used `StaticResource` internally. This was fixed in v5.0.0 — the style now uses `DynamicResource` internally. If you target v5.0.0+, the style is safe to use and is the officially recommended approach.

### Obsolete brush keys

Keys like `MaterialDesignPaper`, `MaterialDesignBackground`, `PrimaryHueMidBrush` may appear to work at design time but fail on theme switch. Always use `MaterialDesign.Brush.*` keys.

### Silent resolution failure

`MaterialDesignHintForeground` does not exist in MDIX. WPF silently falls back to its default foreground without any warning. Always verify brush keys against the canonical list.

### BundledTheme BaseTheme

`BaseTheme="Light"` in App.xaml is the initial value only. The runtime theme is set by `PaletteHelper.SetTheme()` at startup. Do not change the XAML value to `"Dark"` — apply the persisted preference programmatically.

### BundledTheme and PaletteHelper are complementary

`BundledTheme` handles initial resource dictionary merging at XAML load time. `PaletteHelper` handles runtime changes. You need both — they serve different purposes.

### Multiple merged dictionaries

`PaletteHelper.GetResourceDictionary()` returns the first `IMaterialDesignThemeDictionary` in `Application.Current.Resources.MergedDictionaries`. The `BundledTheme` implements this interface. If you add other dictionaries before `BundledTheme`, theme switching will target the wrong dictionary.
