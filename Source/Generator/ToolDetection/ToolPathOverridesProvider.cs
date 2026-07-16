using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Generator.ToolDetection;

/// <summary>
/// Provides the current set of explicit tool path overrides. Invoked on every lookup so
/// that consumers always see the latest configuration without needing to push updates.
/// </summary>
/// <returns>The current overrides keyed by tool name (e.g. "d2", "mmdc").</returns>
public delegate Dictionary<string, string> ToolPathOverridesProvider();
