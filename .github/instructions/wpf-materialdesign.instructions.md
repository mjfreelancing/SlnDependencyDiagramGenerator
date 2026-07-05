---
applyTo: "**/*.xaml,**/Features/Theming/**/*.cs"
---

# WPF Material Design Styling

This file covers the MaterialDesignInXamlToolkit (MDIX) theming setup used by `SlnDependencyStudio.Wpf`. It exists because runtime theme switching in MDIX has sharp edges — wrong brush keys, deprecated APIs, and misleading documentation patterns can waste hours.

---

## Stack & Versions

| Dependency             | Version                     | Notes                                            |
| ---------------------- | --------------------------- | ------------------------------------------------ |
| `MaterialDesignThemes` | 5.2.1                       | Control theming, `BundledTheme`, `PaletteHelper` |
| `ReactiveUI.WPF`       | 23.2.27                     | `ReactiveWindow<T>`, `ReactiveUserControl<T>`    |
| Target                 | `net10.0-windows10.0.19041` |                                                  |

No MahApps.Metro. Custom window chrome is not required and was intentionally dropped (see `/memories/repo/wpf-technical-notes.md`).

---

## App.xaml Setup

```xml
<Application x:Class="SlnDependencyStudio.Wpf.App"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             ...>
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <materialDesign:BundledTheme
                    BaseTheme="Light"
                    PrimaryColor="Teal"
                    SecondaryColor="Cyan" />
                <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign3.Defaults.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

**Rules:**

- `BundledTheme` MUST come before `MaterialDesign3.Defaults.xaml` in merged dictionaries order.
- `BaseTheme` is always `"Light"` in XAML. The bootstrapper overrides it at startup from persisted settings.
- `PrimaryColor` and `SecondaryColor` use SWATCH names (Teal, Cyan, DeepPurple, etc.), not hex values.
- Use `MaterialDesign3.Defaults.xaml` (not `MaterialDesignTheme.Defaults.xaml` or `MaterialDesign2.Defaults.xaml`).

---

## Window Configuration

**Do NOT use `Style="{StaticResource MaterialDesignWindow}"`.** This style sets Window `Background` via a `StaticResource`-based setter that does not respond to runtime theme changes. It also provides custom title bar chrome that is not needed.

### Correct pattern (every `ReactiveWindow<T>`):

```xml
<reactiveui:ReactiveWindow ...
    TextElement.Foreground="{DynamicResource MaterialDesign.Brush.Foreground}"
    Background="{DynamicResource MaterialDesign.Brush.Background}"
    ...>
```

### For `ReactiveUserControl<T>` views:

No special window-level attributes needed. Use `DynamicResource` for all brush references within the control.

---

## Theme Switching

### ThemeService (`Features/Theming/ThemeService.cs`)

The canonical MDIX runtime theme switch pattern:

```csharp
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;

public void ApplyTheme(string theme)
{
    var paletteHelper = new PaletteHelper();
    var currentTheme = paletteHelper.GetTheme();
    currentTheme.SetBaseTheme(theme == "Dark" ? BaseTheme.Dark : BaseTheme.Light);
    paletteHelper.SetTheme(currentTheme);
}
```

### Startup wiring (`SlnDependencyWpfAppBootstrapper.cs`)

```csharp
// Load persisted settings, then apply theme BEFORE creating main window.
await _applicationSettingsService.LoadAsync();
_themeService.ApplyTheme(_applicationSettingsService.CurrentSettings.Theme);
var mainWindow = (MainWindow)_viewFactory.CreateViewFor<MainWindowViewModel>();
mainWindow.Show();
```

### Settings live preview (`SettingsEditorViewModel.cs`)

```csharp
this.WhenAnyValue(vm => vm.IsDarkTheme)
    .Subscribe(isDark => themeService.ApplyTheme(isDark ? "Dark" : "Light"));
```

When cancelling the settings dialog, revert to the original theme via `SettingsEditorViewModel.GetOriginalTheme()`.

---

## Brush Key Reference

### VALID Canonical Keys (always use these)

All canonical keys follow the `MaterialDesign.Brush.<Category>.<Property>` naming convention:

**Surfaces & Backgrounds:**
| Key | Usage |
|---|---|
| `MaterialDesign.Brush.Background` | Window / page background |
| `MaterialDesign.Brush.Card.Background` | Card / elevated surface background |
| `MaterialDesign.Brush.Card.Border` | Card border |

**Text:**
| Key | Usage |
|---|---|
| `MaterialDesign.Brush.Foreground` | Primary text |
| `MaterialDesign.Brush.ForegroundLight` | Secondary / hint / subdued text |

**Primary & Secondary Colors:**
| Key | Usage |
|---|---|
| `MaterialDesign.Brush.Primary` | Primary accent (mid hue) |
| `MaterialDesign.Brush.Primary.Light` | Primary light variant |
| `MaterialDesign.Brush.Primary.Dark` | Primary dark variant |
| `MaterialDesign.Brush.Secondary` | Secondary accent (mid hue) |
| `MaterialDesign.Brush.Secondary.Light` | Secondary light variant |
| `MaterialDesign.Brush.Secondary.Dark` | Secondary dark variant |
| `MaterialDesign.Brush.Primary.Foreground` | Text on primary background |
| `MaterialDesign.Brush.Secondary.Foreground` | Text on secondary background |

**Validation & Separators:**
| Key | Usage |
|---|---|
| `MaterialDesign.Brush.ValidationError` | Error indicators |
| `MaterialDesign.Brush.Separator.Background` | Dividers, borders between sections |

**Additional keys exist** for TextBox, ComboBox, DataGrid, ToolBar, ListBox, and other controls. The complete list is available in the MDIX test suite: [`MaterialDesignThemes.UITests/WPF/Theme/ThemeTests.g.cs`](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/blob/master/tests/MaterialDesignThemes.UITests/WPF/Theme/ThemeTests.g.cs) — search for `GetBrushResourceNames()`.

---

## Obsolete Keys — NEVER USE

These keys exist only as backward-compatibility shims in `MaterialDesignTheme.ObsoleteBrushes.xaml`. They are forwarded via `StaticResource` and may not update correctly on theme changes. **Always use the canonical replacement.**

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

These look like MDIX keys but do NOT exist in the MDIX codebase. They will silently resolve to WPF defaults and never respond to theme changes:

| Fake Key                       | Correct Replacement                    |
| ------------------------------ | -------------------------------------- |
| `MaterialDesignHintForeground` | `MaterialDesign.Brush.ForegroundLight` |

---

## Problem-Solving Resources

When theming doesn't work, consult these in order:

1. **This file** — check against the obsolete/fake key lists first.

2. **MDIX generated sources** (in the NuGet package or [GitHub](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)):
   - `ResourceDictionaryExtensions.g.cs` — `LoadThemeColors()` shows which obsolete keys map to which canonical keys; `ApplyThemeColors()` shows all keys updated on theme change.
   - `ThemeColors.json` — source of truth for brush definitions, obsolete keys, and alternate keys.
   - `tests/MaterialDesignThemes.UITests/WPF/Theme/ThemeTests.g.cs` — `GetBrushResourceNames()` lists every valid canonical key; `GetObsoleteBrushResourceNames()` lists every obsolete key.
   - `build/MigrateBrushes.ps1` — official obsolete→canonical migration script.
   - `PaletteHelper.cs` — shows `GetResourceDictionary()` looks for `IMaterialDesignThemeDictionary` in `Application.Current.Resources.MergedDictionaries`.

3. **Official MDIX docs:**
   - [Getting Started](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/wiki/Getting-Started)
   - [Advanced Theming](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/wiki/Advanced-Theming)
   - [Brush Names](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/wiki/Brush-Names)

4. **MDIX demo app:** `src/MainDemo.Wpf` in the MDIX repo — demonstrates correct window setup.

5. **Context7:** Query `MaterialDesignInXamlToolkit` for API syntax and patterns.

---

## Common Pitfalls

### StaticResource in window style

`Style="{StaticResource MaterialDesignWindow}"` caches the Light theme background. Remove it.

### Obsolete brush keys

Keys like `MaterialDesignPaper`, `MaterialDesignBackground`, `PrimaryHueMidBrush` may appear to work at design time but fail on theme switch. Always use `MaterialDesign.Brush.*` keys.

### Silent resolution failure

`MaterialDesignHintForeground` does not exist in MDIX. WPF silently falls back to its default foreground (black/white), producing text that never changes with the theme.

### BundledTheme BaseTheme is only for initial load

The `BaseTheme="Light"` in App.xaml is overridden at startup by the bootstrapper. Do not change it to `"Dark"` — the bootstrapper reads the persisted preference.

### Do not use both BundledTheme and PaletteHelper for initial setup

`BundledTheme` handles initial resource dictionary merging. `PaletteHelper` handles runtime changes. They are complementary, not alternatives.

---

## View Patterns (from this project)

### ReactiveWindow<T>

```xml
<reactiveui:ReactiveWindow ...
    TextElement.Foreground="{DynamicResource MaterialDesign.Brush.Foreground}"
    Background="{DynamicResource MaterialDesign.Brush.Background}">
```

- MainWindow: uses `MaterialDesign.Brush.Background` on Window, `MaterialDesign.Brush.Card.Background` on inner content grid.
- SettingsWindow: `WindowStyle="ToolWindow"`, no `MaterialDesignWindow` style, `Background="{DynamicResource MaterialDesign.Brush.Background}"`.

### ReactiveUserControl<T>

- All brushes use `DynamicResource`, never `StaticResource`.
- Primary text: `MaterialDesign.Brush.Foreground`
- Secondary/hint text: `MaterialDesign.Brush.ForegroundLight`
- Card surfaces: `MaterialDesign.Brush.Card.Background` with optional `MaterialDesign.Brush.Background` inner content
- Separators/dividers: `MaterialDesign.Brush.Separator.Background`
- Accent colors: `MaterialDesign.Brush.Primary` or `MaterialDesign.Brush.Secondary`

### Dialog content (ErrorMessageDialog, ConfirmDiscardDialog)

- Hosted via `DialogHost`, which handles theming automatically.
- Still use canonical `DynamicResource` keys for any explicit brush references.
