# FormField Usage Convention

All labeled fields on settings pages use the `FormField` control. This ensures consistent visual rhythm across Project, Solution, Export, and Diagrams pages.

## Layout

```
┌──────────────────────────────────────────────┐
│ Bold Title (FontSize=14, FontWeight=Bold)     │
│ Lighter description text (FontSize=12)        │  ← optional, via Description property
│ [ input control goes here ]                   │  ← child element of FormField
│ ⚠ validation error (red, FontSize=12)          │  ← auto-hidden when null/empty
└──────────────────────────────────────────────┘
```

No separator lines. Fields are spaced with `Margin="0,0,0,24"` on each FormField.

## Properties

| Property          | Type      | Required | Notes                                                      |
| ----------------- | --------- | -------- | ---------------------------------------------------------- |
| `Title`           | `string`  | Yes      | Bold label above the input                                 |
| `Description`     | `string?` | No       | Lighter text between title and input                       |
| child element     | `object`  | Yes      | Any WPF control: TextBox, ToggleButton, ItemsControl, etc. |
| `ValidationError` | `string?` | No       | Red error text below the input                             |

## Field Type Patterns

### Text input

```xml
<studioControls:FormField Title="Export root"
                          Description="The root directory for generated files.">
    <TextBox Text="{Binding RootPath.Value, ...}" />
</studioControls:FormField>
```

### Text input with Browse button

```xml
<studioControls:FormField Title="Solution path"
                          Description="Path to the .sln or .slnx file.">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="Auto" />
        </Grid.ColumnDefinitions>
        <TextBox Grid.Column="0" Text="{Binding SolutionPath.Value, ...}" />
        <Button Grid.Column="1" Content="Browse" ... />
    </Grid>
</studioControls:FormField>
```

### Toggle switch

```xml
<studioControls:FormField Title="Clear output folder"
                          Description="When enabled, clears sub-folders before generating.">
    <ToggleButton HorizontalAlignment="Left"
                  IsChecked="{Binding ClearContents.Value}"
                  Style="{StaticResource MaterialDesignSwitchToggleButton}" />
</studioControls:FormField>
```

Note: `HorizontalAlignment="Left"` is required — `MaterialDesignSwitchToggleButton` stretches by default.

### Checkbox group

```xml
<studioControls:FormField Title="Diagram formats"
                          Description="Select one or more diagram output formats.">
    <ItemsControl ItemsSource="{Binding FormatToggles}">
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <WrapPanel Orientation="Horizontal" />
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <CheckBox IsChecked="{Binding IsChecked}" ...>
                    ...
                </CheckBox>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</studioControls:FormField>
```

## Validation Wiring

In the view code-behind, use `BindValidation` targeting `FormField.ValidationError`:

```csharp
this.WhenActivated(disposables =>
{
    this.BindValidation(
            ViewModel,
            vm => vm.RootPath.Value,
            view => view.ExportRootFormField.ValidationError)
        .DisposeWith(disposables);
});
```

For validation rules based on collection counts (e.g., "at least one format must be selected"):

```csharp
this.BindValidation(
        ViewModel,
        vm => vm.Formats.Value.Count,
        view => view.FormatsFormField.ValidationError)
    .DisposeWith(disposables);
```

## Page Layout

Each page follows the same outer structure:

```xml
<ScrollViewer VerticalScrollBarVisibility="Auto">
    <StackPanel Margin="24">
        <!-- Header: PackIcon + title + subtitle -->
        <StackPanel Margin="0,0,0,24" Orientation="Horizontal">...</StackPanel>

        <!-- Form fields, each with Margin="0,0,0,24" -->
        <studioControls:FormField Margin="0,0,0,24" Title="..." Description="...">...</studioControls:FormField>
        <studioControls:FormField Margin="0,0,0,24" Title="..." Description="...">...</studioControls:FormField>
    </StackPanel>
</ScrollViewer>
```
