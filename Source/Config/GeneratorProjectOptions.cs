using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Config
{
    public class GeneratorProjectOptions : IGeneratorProjectOptions
    {
        public string SolutionPath { get; set; }
        public IReadOnlyCollection<string> RegexToInclude { get; set; }
        public int IndividualTransitiveDepth { get; set; }
        public int AllTransitiveDepth { get; set; }
    }
}