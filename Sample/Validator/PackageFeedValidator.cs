﻿using AllOverIt.Extensions;
using AllOverIt.Validation;
using FluentValidation;
using SlnDependencyDiagramGenerator.Config;

namespace AllOverItDependencyDiagram.Validator
{
    internal sealed class PackageFeedValidator : ValidatorBase<PackageFeed>
    {
        static PackageFeedValidator()
        {
            DisablePropertyNameSplitting();
        }

        public PackageFeedValidator()
        {
            RuleFor(model => model.SourceUri).NotEmpty();

            When(model => model.Username.IsNotNullOrEmpty() || model.Password.IsNotNullOrEmpty(), () =>
            {
                RuleFor(model => model.Username).NotEmpty();
                RuleFor(model => model.Password).NotEmpty();
            });
        }
    }
}