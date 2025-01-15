using FluentAssertions;
using System;
using Xunit;

namespace Serilog.Enrichers.CallStack.Tests;

public class CallStackEnricherConfigurationTests
{
    [Fact]
    public void Constructor_CreatesInstanceWithDefaultValues()
    {
        // Act
        var config = new CallStackEnricherConfiguration();

        // Assert
        config.IncludeMethodName.Should().BeTrue();
        config.IncludeMethodParameters.Should().BeFalse();
        config.UseFullParameterTypes.Should().BeFalse();
        config.IncludeTypeName.Should().BeTrue();
        config.UseFullTypeName.Should().BeFalse();
        config.IncludeFileName.Should().BeTrue();
        config.UseFullFileName.Should().BeFalse();
        config.IncludeLineNumber.Should().BeTrue();
        config.IncludeColumnNumber.Should().BeFalse();
        config.IncludeAssemblyName.Should().BeFalse();
        
        config.MethodNamePropertyName.Should().Be("MethodName");
        config.TypeNamePropertyName.Should().Be("TypeName");
        config.FileNamePropertyName.Should().Be("FileName");
        config.LineNumberPropertyName.Should().Be("LineNumber");
        config.ColumnNumberPropertyName.Should().Be("ColumnNumber");
        config.AssemblyNamePropertyName.Should().Be("AssemblyName");
        
        config.FrameOffset.Should().Be(0);
        config.SuppressExceptions.Should().BeTrue();
        config.OnException.Should().BeNull();
        
        config.SkipNamespaces.Should().BeEmpty();
        config.SkipTypes.Should().BeEmpty();
    }

    [Fact]
    public void SkipNamespace_WithValidNamespace_AddsToCollection()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration();
        var testNamespace = "Test.Namespace";

        // Act
        var result = config.SkipNamespace(testNamespace);

        // Assert
        result.Should().BeSameAs(config); // Method chaining
        config.SkipNamespaces.Should().Contain(testNamespace);
    }

    [Fact]
    public void SkipNamespace_WithNullNamespace_ThrowsArgumentNullException()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration();

        // Act & Assert
        var act = () => config.SkipNamespace(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("namespace");
    }

    [Fact]
    public void SkipType_WithValidTypeName_AddsToCollection()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration();
        var testType = "Test.Type.Name";

        // Act
        var result = config.SkipType(testType);

        // Assert
        result.Should().BeSameAs(config); // Method chaining
        config.SkipTypes.Should().Contain(testType);
    }

    [Fact]
    public void SkipType_WithNullTypeName_ThrowsArgumentNullException()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration();

        // Act & Assert
        var act = () => config.SkipType(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("typeName");
    }

    [Fact]
    public void WithFrameOffset_WithValidOffset_SetsFrameOffset()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration();

        // Act
        var result = config.WithFrameOffset(5);

        // Assert
        result.Should().BeSameAs(config); // Method chaining
        config.FrameOffset.Should().Be(5);
    }

    [Fact]
    public void WithFrameOffset_WithNegativeOffset_SetsToZero()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration();

        // Act
        var result = config.WithFrameOffset(-5);

        // Assert
        result.Should().BeSameAs(config); // Method chaining
        config.FrameOffset.Should().Be(0);
    }

    [Fact]
    public void WithExceptionHandling_SetsExceptionHandlingProperties()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration();
        var exceptionHandler = new Action<Exception>(ex => { });

        // Act
        var result = config.WithExceptionHandling(false, exceptionHandler);

        // Assert
        result.Should().BeSameAs(config); // Method chaining
        config.SuppressExceptions.Should().BeFalse();
        config.OnException.Should().BeSameAs(exceptionHandler);
    }

    [Fact]
    public void WithIncludes_SetsIncludeProperties()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration();

        // Act
        var result = config.WithIncludes(
            methodName: false,
            typeName: false,
            fileName: false,
            lineNumber: false,
            columnNumber: true,
            assemblyName: true);

        // Assert
        result.Should().BeSameAs(config); // Method chaining
        config.IncludeMethodName.Should().BeFalse();
        config.IncludeTypeName.Should().BeFalse();
        config.IncludeFileName.Should().BeFalse();
        config.IncludeLineNumber.Should().BeFalse();
        config.IncludeColumnNumber.Should().BeTrue();
        config.IncludeAssemblyName.Should().BeTrue();
    }

    [Fact]
    public void WithPropertyNames_WithAllParameters_SetsAllPropertyNames()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration();

        // Act
        var result = config.WithPropertyNames(
            methodName: "CustomMethod",
            typeName: "CustomType",
            fileName: "CustomFile",
            lineNumber: "CustomLine",
            columnNumber: "CustomColumn",
            assemblyName: "CustomAssembly");

        // Assert
        result.Should().BeSameAs(config); // Method chaining
        config.MethodNamePropertyName.Should().Be("CustomMethod");
        config.TypeNamePropertyName.Should().Be("CustomType");
        config.FileNamePropertyName.Should().Be("CustomFile");
        config.LineNumberPropertyName.Should().Be("CustomLine");
        config.ColumnNumberPropertyName.Should().Be("CustomColumn");
        config.AssemblyNamePropertyName.Should().Be("CustomAssembly");
    }

    [Fact]
    public void WithPropertyNames_WithNullValues_KeepsOriginalValues()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration();
        var originalMethodName = config.MethodNamePropertyName;
        var originalTypeName = config.TypeNamePropertyName;

        // Act
        var result = config.WithPropertyNames(methodName: null, typeName: null);

        // Assert
        result.Should().BeSameAs(config); // Method chaining
        config.MethodNamePropertyName.Should().Be(originalMethodName);
        config.TypeNamePropertyName.Should().Be(originalTypeName);
    }

    [Fact]
    public void WithPropertyNames_WithEmptyValues_KeepsOriginalValues()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration();
        var originalMethodName = config.MethodNamePropertyName;
        var originalTypeName = config.TypeNamePropertyName;

        // Act
        var result = config.WithPropertyNames(methodName: "", typeName: "");

        // Assert
        result.Should().BeSameAs(config); // Method chaining
        config.MethodNamePropertyName.Should().Be(originalMethodName);
        config.TypeNamePropertyName.Should().Be(originalTypeName);
    }

    [Fact]
    public void WithFullNames_SetsFullNameProperties()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration();

        // Act
        var result = config.WithFullNames(
            fullTypeName: true,
            fullFileName: true,
            fullParameterTypes: true);

        // Assert
        result.Should().BeSameAs(config); // Method chaining
        config.UseFullTypeName.Should().BeTrue();
        config.UseFullFileName.Should().BeTrue();
        config.UseFullParameterTypes.Should().BeTrue();
    }

    [Fact]
    public void WithMethodParameters_SetsMethodParameterProperties()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration();

        // Act
        var result = config.WithMethodParameters(
            includeParameters: true,
            useFullParameterTypes: true);

        // Assert
        result.Should().BeSameAs(config); // Method chaining
        config.IncludeMethodParameters.Should().BeTrue();
        config.UseFullParameterTypes.Should().BeTrue();
    }

    [Fact]
    public void FluentConfiguration_CanChainMultipleMethods()
    {
        // Act
        var config = new CallStackEnricherConfiguration()
            .SkipNamespace("Test.Namespace")
            .SkipType("Test.Type")
            .WithFrameOffset(2)
            .WithIncludes(methodName: true, typeName: false)
            .WithPropertyNames(methodName: "Method")
            .WithFullNames(fullTypeName: true)
            .WithMethodParameters(includeParameters: true)
            .WithExceptionHandling(false);

        // Assert
        config.SkipNamespaces.Should().Contain("Test.Namespace");
        config.SkipTypes.Should().Contain("Test.Type");
        config.FrameOffset.Should().Be(2);
        config.IncludeMethodName.Should().BeTrue();
        config.IncludeTypeName.Should().BeFalse();
        config.MethodNamePropertyName.Should().Be("Method");
        config.UseFullTypeName.Should().BeTrue();
        config.IncludeMethodParameters.Should().BeTrue();
        config.SuppressExceptions.Should().BeFalse();
    }
}