using System;

namespace SlnDependencyDiagramGenerator.Exceptions
{
    public sealed class PackageReferenceNotResolvedException : Exception
    {
        public PackageReferenceNotResolvedException(string packageName, string packageVersion)
            : base($"Could not resolve the package {packageName} v{packageVersion}.")
        {
        }
    }
}