// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Docfx.YamlSerialization;
using FluentAssertions;
using Xunit;

namespace Docfx.Common.Tests;

public class JsonSchemaUtilityTest
{
    [Fact]
    public void ValidateDocfxJsonTest_Empty()
    {
        // Arrange
        var json = "{}";

        // Act
        bool isValid = JsonSchemaUtility.Validate<DocfxConfig>(json, "dummy.json", out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateDocfxJson_Invalid_Key()
    {
        // Arrange
        // lang=json
        var json = """
            {
              "build": {
                "not_defined_key": "value",
              }
            }
            """;

        // Act
        bool isValid = JsonSchemaUtility.Validate<DocfxConfig>(json, "dummy.json", out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().BeEquivalentTo(
        [
            new JsonSchemaUtility.ValidationError
            {
                Location ="/build/not_defined_key",
                ErrorCode = "ViolateSchema",
                Message ="There is no corresponding schema definition",
                Type = "",
            },
        ]);
    }

    [Fact]
    public void ValidateDocfxJson_Invalid_Enum()
    {
        // Arrange
        // lang=json
        var json = """
            {
              "metadata": [
                {
                  "outputFormat": "undefined"
                }
              ]
            }
            """;

        // Act
        bool isValid = JsonSchemaUtility.Validate<DocfxConfig>(json, "dummy.json", out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().BeEquivalentTo(
        [
            new JsonSchemaUtility.ValidationError
            {
                Location ="/metadata/0/outputFormat",
                ErrorCode = "ViolateSchema",
                Message ="The specified enum value(\"undefined\") is defined in the schema. Make sure the case matches to enum definitions ([\"mref\",\"markdown\",\"apiPage\"])",
                Type = "enum",
            },
        ]);
    }

    [Fact]
    public void ValidateFilterConfigYaml_Default()
    {
        // Arrange
        var yaml = """
            apiRules:
            - include:
                uidRegex: ^Microsoft\.DevDiv\.SpecialCase
            - exclude:
                uidRegex: ^Microsoft\.DevDiv
            """;

        var obj = YamlUtility.Deserialize<object>(new StringReader(yaml));
        var json = JsonUtility.Serialize(obj);

        // Act
        bool isValid = JsonSchemaUtility.Validate<ConfigFilterRule>(json, "toc.json", out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateFilterConfigYaml_Invalid()
    {
        // Arrange
        var yaml = """
            apiRules:
            - include:
              uidRegex: ^MyCompany\.MyNamespace\.Core.+
            - exclude:
              uidRegex: _[^.]+$
            """;

        var obj = YamlUtility.Deserialize<object>(new StringReader(yaml));
        var json = JsonUtility.Serialize(obj);

        // Act
        bool isValid = JsonSchemaUtility.Validate<ConfigFilterRule>(json, "toc.json", out var errors);

        // Assert
        isValid.Should().BeFalse();

        errors.Should().BeEquivalentTo(
        [
            new JsonSchemaUtility.ValidationError
            {
                Location ="/apiRules/0/include",
                ErrorCode = "ViolateSchema",
                Message ="Value is \"null\" but should be \"object\"",
                Type = "type",
            },
            new JsonSchemaUtility.ValidationError
            {
                Location ="/apiRules/0/uidRegex",
                ErrorCode = "ViolateSchema",
                Message ="There is no corresponding schema definition",
                Type = "",
            },
            new JsonSchemaUtility.ValidationError
            {
                Location ="/apiRules/1/exclude",
                ErrorCode = "ViolateSchema",
                Message ="Value is \"null\" but should be \"object\"",
                Type = "type",
            },
            new JsonSchemaUtility.ValidationError
            {
                Location ="/apiRules/1/uidRegex",
                ErrorCode = "ViolateSchema",
                Message ="There is no corresponding schema definition",
                Type = "",
            },
        ]);
    }

    [Fact]
    public void ValidateTocJson()
    {
        // Arrange
        // lang=json
        var json = """
            {
              "items": [
                { "name": "Docs", "href": "docs/" },
                { "name": "API", "href": "api/" },
              ]
            }
            """;

        // Act
        bool isValid = JsonSchemaUtility.Validate<TocItemViewModel>(json, "toc.json", out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    // Stub classes.(These classes can't accesseed from this project)
    private class DocfxConfig { }
    private class ConfigFilterRule { }
    private class TocItemViewModel { }
}
