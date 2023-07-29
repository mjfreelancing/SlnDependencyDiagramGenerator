using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Config
{
    public class GeneratorProjectOptions
    {
        public string SolutionPath { get; init; }
        public IReadOnlyCollection<string> RegexToInclude { get; init; }
        public int IndividualTransitiveDepth { get; init; }
        public int AllTransitiveDepth { get; init; }
    }
}