// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Build.Engine;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.Dotnet;
using Docfx.Tests.Common;
using FluentAssertions;
using Xunit.Abstractions;
using YamlDotNet.Serialization;

namespace Docfx.Tests;

[Collection("docfx STA")]
public class JsonUtilityTest : IDisposable
{
    private readonly ITestOutputHelper output;
    private readonly TestLoggerListener logListener = new();

    public JsonUtilityTest(ITestOutputHelper output)
    {
        this.output = output;
        Logger.UnregisterAllListeners();
        Logger.RegisterListener(logListener);
    }

    public void Dispose()
    {
        Logger.UnregisterAllListeners();
    }

    [Theory]
    [InlineData("docs/docfx.json")]
    [InlineData("samples/csharp/docfx.json")]
    [InlineData("samples/extensions/docfx.json")]
    [InlineData("samples/seed/docfx.json")]
    [InlineData("test/docfx.Tests/Assets/docfx.json_build/docfx.json")]
    [InlineData("test/docfx.Tests/Assets/docfx.json_empty/docfx.json")]
    [InlineData("test/docfx.Tests/Assets/docfx.json_metadata/docfx.json")]
    [InlineData("test/docfx.Tests/Assets/docfx.json_metadata/docfxWithFilter.json")]
    [InlineData("test/docfx.Tests/Assets/docfx.json_metadata_build/docfx.json")]
    public void JsonSchemaTest_Docfx_Json(string path)
    {
        // Arrange
        var json = LoadAsJsonText(path);

        // Act
        JsonUtility.Validate<DocfxConfig>(json, fileNameHint: path);

        // Assert
        logListener.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("src/Docfx.Dotnet/Resources/defaultfilterconfig.yml")]
    [InlineData("test/Docfx.Dotnet.Tests/TestData/filterconfig.yml")]
    [InlineData("test/Docfx.Dotnet.Tests/TestData/filterconfig_attribute.yml")]
    [InlineData("test/Docfx.Dotnet.Tests/TestData/filterconfig_docs_sample.yml")]
    public void JsonSchemaTest_FilterConfig(string path)
    {
        // Arrange
        var json = LoadAsJsonText(path);

        // Act
        JsonUtility.Validate<ConfigFilterRule>(json, fileNameHint: path);

        // Assert
        logListener.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("test/docfx.Snapshot.Tests/SamplesTest.CSharp/api/toc.json.view.verified.json")]
    [InlineData("test/docfx.Snapshot.Tests/SamplesTest.Extensions/api/toc.json.view.verified.json")]
    [InlineData("test/docfx.Snapshot.Tests/SamplesTest.Extensions/toc.json.view.verified.json")]
    [InlineData("test/docfx.Snapshot.Tests/SamplesTest.Seed/api/toc.json.view.verified.json")]
    [InlineData("test/docfx.Snapshot.Tests/SamplesTest.Seed/apipage/toc.json.view.verified.json")]
    [InlineData("test/docfx.Snapshot.Tests/SamplesTest.Seed/articles/toc.json.view.verified.json")]
    [InlineData("test/docfx.Snapshot.Tests/SamplesTest.Seed/md/toc.json.view.verified.json")]
    [InlineData("test/docfx.Snapshot.Tests/SamplesTest.Seed/pdf/toc.json.view.verified.json")]
    [InlineData("test/docfx.Snapshot.Tests/SamplesTest.Seed/restapi/toc.json.view.verified.json")]
    [InlineData("test/docfx.Snapshot.Tests/SamplesTest.Seed/toc.json.view.verified.json")]
    public void JsonSchemaTest_Toc_Json(string path)
    {
        // Arrange
        var json = LoadAsJsonText(path);

        // Act
        JsonUtility.Validate<TocItemViewModel>(json, fileNameHint: path);

        // Assert
        logListener.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("test/Docfx.Build.RestApi.WithPlugins.Tests/TestData/swagger/toc.yml")]
    [InlineData("test/docfx.Snapshot.Tests/SamplesTest.SeedMarkdown/toc.verified.yml")]
    public void JsonSchemaTest_Toc_Yaml(string path)
    {
        // Arrange
        var json = LoadAsJsonText(path);

        // Act
        JsonUtility.Validate<TocItemViewModel>(json, fileNameHint: path);

        // Assert
        logListener.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("test/Docfx.Build.Tests/TestData/xrefmap.json")]
    public void JsonSchemaTest_XrefMap_Json(string path)
    {
        // Arrange
        var json = LoadAsJsonText(path);

        // Act
        JsonUtility.Validate<XRefMap>(json, fileNameHint: path);

        // Assert
        logListener.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("test/Docfx.Build.Tests/TestData/xrefmap.yml")]
    [InlineData("test/docfx.Snapshot.Tests/SamplesTest.CSharp/xrefmap.verified.yml")]
    [InlineData("test/docfx.Snapshot.Tests/SamplesTest.Extensions/xrefmap.verified.yml")]
    [InlineData("test/docfx.Snapshot.Tests/SamplesTest.Seed/xrefmap.verified.yml")]
    public void JsonSchemaTest_XrefMap_Yaml(string path)
    {
        // Arrange
        var json = LoadAsJsonText(path);

        // Act
        JsonUtility.Validate<XRefMap>(json, fileNameHint: path);

        // Assert
        logListener.Items.Should().BeEmpty();
    }


    [Theory]
    [InlineData("test/docfx.Tests/Assets/docfx.json_invalid_format/docfx.json")]
    public void JsonSchemaTest_Docfx_Json_InvalidFormat(string path)
    {
        // Arrange
        var json = LoadAsJsonText(path);

        // Act
        JsonUtility.Validate<DocfxConfig>(json, fileNameHint: path);

        // Assert
        logListener.Items.Should().HaveCount(1);
        logListener.Items[0].Message.Should().Be("[/metadata] ViolateSchema: type: Value is \"object\" but should be \"array\"");
    }

    [Theory]
    [InlineData("test/docfx.Tests/Assets/docfx.json_invalid_key/docfx.json")]
    public void JsonSchemaTest_Docfx_Json_InvalidType(string path)
    {
        // Arrange
        var json = LoadAsJsonText(path);

        // Act
        JsonUtility.Validate<DocfxConfig>(json, fileNameHint: path);

        // Assert
        logListener.Items.Should().HaveCount(1);
        logListener.Items[0].Message.Should().Be("[/invalid] ViolateSchema: There is no corresponding schema definition");
    }

    /// <summary>
    /// Load file content as JsonElement.
    /// </summary>
    private static string LoadAsJsonText(string path)
    {
        var solutionDir = PathHelper.GetSolutionFolder();

        var filePath = Path.Combine(solutionDir, path);

        if (!File.Exists(filePath))
            throw new FileNotFoundException(filePath);

        switch (Path.GetExtension(filePath))
        {
            case ".json":
                return File.ReadAllText(filePath);
            case ".yml":
                // Need to serialize to JSON with YamlDotNet.
                var yaml = File.ReadAllText(filePath);
                var yamlObject = YamlUtility.Deserialize<object>(new StringReader(yaml));

                var serializer = new SerializerBuilder()
                                   .JsonCompatible()
                                   .Build();
                return serializer.Serialize(yamlObject);

            default:
                throw new NotSupportedException(path);
        }
    }
}
