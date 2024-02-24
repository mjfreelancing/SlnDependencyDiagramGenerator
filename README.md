# SlnDependencyDiagramGenerator
Generates D2 diagram files and images for a Visual Studio Solution.

![](https://img.shields.io/badge/.NET-8.0-C56EE0.svg)
![](https://img.shields.io/badge/.NET-7.0-55A9EE.svg)

[![NuGet](https://img.shields.io/nuget/vpre/SlnDependencyDiagramGenerator?color=E3505C)](https://www.nuget.org/packages/SlnDependencyDiagramGenerator/absoluteLatest/)
[![NuGet](https://img.shields.io/nuget/dt/SlnDependencyDiagramGenerator?color=FFC33C)](https://www.nuget.org/packages/SlnDependencyDiagramGenerator/absoluteLatest/)


## Overview

This package makes it possible to parse a Visual Studio Solution file to determine all of the `.csproj` references it contains.
These files are then parsed (after filtering against a regex defined in the configuration) to determine all explicit framework
and package references. Based on the configuration, the generator will then optionally determine what additional transitive
(implicit) package references are used.

After gathering all of the information, the generator will produce a 'Dependency Summary' in markdown format,
along with one or more [D2](https://d2lang.org/) diagram files, as well as either `png`, `svg`, or `pdf` diagrams.

This [example](./Sample/Output/net8.0/slndependencydiagramgenerator.png) has been produced from the solution in this repository.


## Configuration

The generator offers extensive configuration options that define what projects are parsed, what diagrams are generated,
how those diagrams are styled, what format to export the diagrams, and what target frameworks to process.


At runtime, the `DependencyGeneratorConfig` class provides all configuration options. The sample application included with
the repository generates diagrams for the solution containing this sample application and the `SlnDependencyDiagramGenerator`
package, binding the configuration from its' `appsettings.json` file.




```json
{
    "options": {
        "packageFeeds": [
            {
                "sourceUri": "https://api.nuget.org/v3/index.json",
                "username": null,
                "password": null
            }
        ],

        "projects": {
            "solutionPath": "..\\..\\..\\..\\SlnDependencyDiagramGenerator.sln",

            "regexToInclude": [
                "\\\\.*\\.csproj"
            ],

            "individual": {
                "enabled": true,
                "includeDependencies": true,
                "transitiveDepth": 1
            },

            "all": {
                "enabled": true,
                "includeDependencies": true,
                "transitiveDepth": 1
            }
        },

        "diagram": {
            "direction": "left",

            "frameworkStyle": {
                "fill": "#ECCBC0",
                "opacity": 0.8
            },

            "packageStyle": {
                "fill": "#ADD8E6",
                "opacity": 0.8
            },

            "transitiveStyle": {
                "fill": "#FFEC96",
                "opacity": 0.8
            },

            "groupName": "Dependency Diagram Generator",
            "groupNameAlias": "ddg"
        },

        "export": {
            "clearContents": true,
            "rootPath": "..\\..\\..\\Output",
            "imageFormats": [ "png" ]
        },

        "targetFrameworks": [ "net8.0" ]
    }
}
```

An explanation of each section is provided below.


### PackageFeeds
Specifies one or more nuget feeds, with authorization credentials if required.

* **SourceUri**: The NuGet feed Uri.
* **Username**: The authentication username. Set to `null` if not required.
* **Password**: The authentication password. Set to `null` if not required.


### Projects
Specifies project related options that determine which projects for a given solution are resolved and the depth of their
package dependency graph.

* **SolutionPath**: The relative or fully-qualified path to the solution file to be parsed.
* **RegexToInclude**: One or more regex patterns to match solution projects to be processed.
* **Individual**: Specifies options specific to the processing of individual projects in a solution.
* **All**: Specifies options specific to the processing of all projects in the solution (collectively).

The `Individual` and `All` nodes provide these options:

* **Enabled**: Indicates if this project scope will be processed.
* **IncludeDependencies**: Indicates if framework and package dependencies should be processed.
* **TransitiveDepth**: Indicates how deep to traverse implicit (transitive) package references. Must be 0 or more.


### Diagram
Specifies diagram options that determine how the diagram will be styled.

* **Direction**: Specifies the direction the diagram flows towards. The default is `Left`.
* **FrameworkStyle**: The fill style to use for framework dependencies referenced by a project.
* **PackageStyle**: The fill style to use for explicit package dependencies referenced by a project.
* **TransitiveStyle**: The fill style to use for transitive (implicit) package dependencies referenced by a project.
* **GroupName**: The name (title) to use for the group of projects parsed.
* **GroupNameAlias**: The alias to use in the D2 generated file to represent the group of projects parsed.

`FrameworkStyle`, `PackageStyle`, and `TransitiveStyle` provide these options:

* **Fill**: The CSS or RGB fill color.
* **Opacity**: The opacity. This should be a value between 0 and 1.


### Export
Specifies export path and image format options.

* **ClearContents**: When True, clears the contents of the folder that combines `RootPath` and the target framework
  being processed.
* **RootPath**: The relative or fully-qualified export root path for the generated diagram files and images.
  A sub-folder will be created for each target framework processed.
* **ImageFormats**: The diagram image formats to create. Can be empty, or one or more of "png", "svg", "pdf".


### TargetFrameworks
Specifies the target frameworks to resolve for all nuget package references. The values should be a target
framework moniker, such as `net6.0`, `net7.0`, `net8.0`, etc. Refer to the following link for a list of
valid monikers: [https://learn.microsoft.com/en-us/dotnet/standard/frameworks](https://learn.microsoft.com/en-us/dotnet/standard/frameworks).



## Limitations
Visual Studio solutions utilizing a `Directory.Builds.props` that include `<FrameworkReference>` or `<PackageReference>`
elements will not be processed as expected. The current implementation explicitly parses the `.csproj` files contained
within the solution.
