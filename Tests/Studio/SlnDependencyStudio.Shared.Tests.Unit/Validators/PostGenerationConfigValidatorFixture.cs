using AllOverIt.Validation;
using AllOverIt.Validation.Extensions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SlnDependencyDiagramGenerator.Tests.Shared;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Validators;
using SlnDependencyStudio.Shared.Validators.Contexts;
using System;

namespace SlnDependencyStudio.Shared.Tests.Unit.Validators;

public class PostGenerationConfigValidatorFixture
{
    [Fact]
    public void Should_Pass_When_Disabled()
    {
        var invoker = CreateValidationInvoker();
        var config = new PostGenerationConfig { Enabled = false };

        Should.NotThrow(() => invoker.AssertValidation(config));
    }

    [Fact]
    public void Should_Fail_When_Enabled_And_Command_Is_Empty()
    {
        var invoker = CreateValidationInvoker();
        var config = new PostGenerationConfig { Enabled = true, Command = string.Empty };

        var exception = Should.Throw<ValidationException>(() => invoker.AssertValidation(config));

        exception.Errors.ShouldContain(error =>
            error.PropertyName.Contains("Command", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Should_Pass_When_Enabled_And_Command_Is_Valid()
    {
        var invoker = CreateValidationInvoker();
        var config = new PostGenerationConfig { Enabled = true, Command = "dotnet" };

        Should.NotThrow(() => invoker.AssertValidation(config));
    }

    [Fact]
    public void Should_Fail_When_WorkingDirectory_Does_Not_Exist()
    {
        var invoker = CreateValidationInvoker();
        var config = new PostGenerationConfig
        {
            Enabled = true,
            Command = "dotnet",
            WorkingDirectory = @"X:\DoesNotExist\Path"
        };

        var context = new PostGenerationConfigContext
        {
            ConfigDirectory = Environment.CurrentDirectory
        };

        var exception = Should.Throw<ValidationException>(() =>
            invoker.AssertValidation(config, context));

        exception.Errors.ShouldContain(error =>
            error.PropertyName.Contains("WorkingDirectory", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Should_Pass_When_WorkingDirectory_Exists()
    {
        using var tempDir = new DisposableTempDirectory();

        var invoker = CreateValidationInvoker();
        var config = new PostGenerationConfig
        {
            Enabled = true,
            Command = "dotnet",
            WorkingDirectory = tempDir.DirectoryPath
        };

        var context = new PostGenerationConfigContext
        {
            ConfigDirectory = Environment.CurrentDirectory
        };

        Should.NotThrow(() => invoker.AssertValidation(config, context));
    }

    [Fact]
    public void Should_Fail_When_Command_Contains_Invalid_Path_Chars()
    {
        var invoker = CreateValidationInvoker();
        var config = new PostGenerationConfig
        {
            Enabled = true,
            Command = "cmd|test"
        };

        var exception = Should.Throw<ValidationException>(() => invoker.AssertValidation(config));

        exception.Errors.ShouldContain(error =>
            error.PropertyName.Contains("Command", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Should_Pass_When_Arguments_Contain_Quoted_Spaces()
    {
        var invoker = CreateValidationInvoker();
        var config = new PostGenerationConfig
        {
            Enabled = true,
            Command = "dotnet",
            Arguments = "--file \"my file.txt\""
        };

        Should.NotThrow(() => invoker.AssertValidation(config));
    }

    [Fact]
    public void Should_Fail_When_Arguments_Contain_Invalid_Path_Chars()
    {
        var invoker = CreateValidationInvoker();
        var config = new PostGenerationConfig
        {
            Enabled = true,
            Command = "dotnet",
            Arguments = "restore|test"
        };

        var exception = Should.Throw<ValidationException>(() => invoker.AssertValidation(config));

        exception.Errors.ShouldContain(error =>
            error.PropertyName.Contains("Arguments", System.StringComparison.OrdinalIgnoreCase));
    }

    private static IValidationInvoker CreateValidationInvoker()
    {
        var services = new ServiceCollection();

        services.AddValidationInvoker(validationRegistry =>
        {
            validationRegistry.AutoRegisterValidators<ValidationRegistrar>();
        });

        using var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IValidationInvoker>();
    }

}
