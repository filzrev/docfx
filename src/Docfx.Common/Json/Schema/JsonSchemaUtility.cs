// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.Json;
using Docfx.Common;
using Docfx.Exceptions;
using Json.More;
using Json.Schema;

#nullable enable

namespace Docfx.Common;

internal class JsonSchemaUtility
{
#pragma warning disable format
    private static readonly Lazy<JsonSchema> DocfxConfigSchema  = new (() => LoadJsonSchema("schemas/docfx.schema.json"));
    private static readonly Lazy<JsonSchema> TocSchema          = new (() => LoadJsonSchema("schemas/toc.schema.json"));
    private static readonly Lazy<JsonSchema> XRefMapSchema      = new (() => LoadJsonSchema("schemas/xrefmap.schema.json"));
    private static readonly Lazy<JsonSchema> FilterConfigSchema = new (() => LoadJsonSchema("schemas/filterconfig.schema.json"));
#pragma warning restore format

    private static readonly EvaluationOptions EvaluationOptions = new()
    {
        ValidateAgainstMetaSchema = false,
        OutputFormat = OutputFormat.Hierarchical,
        RequireFormatValidation = true,
    };

    static JsonSchemaUtility()
    {
        // Customize error messages returned from JsonSchema.Net.
        // Definitions: https://github.com/json-everything/json-everything/blob/master/src/JsonSchema/Localization/Resources.resx
        ErrorMessages.FalseSchema = "There is no corresponding schema definition"; // All values fail against the false schema
        ErrorMessages.Enum = "The specified enum value([[received]]) is defined in the schema. Make sure the case matches to enum definitions ([[values]])"; // Value should match one of the values specified by the enum
    }

    /// <summary>
    /// Validate json with schema that specified by type argument.
    /// </summary>
    public static bool Validate<T>(
        string json,
        string fileNameHint,
        [NotNullWhen(false)] out ValidationError[] errors)
    {
        var typeName = typeof(T).Name;
        var targetSchema = typeName switch
        {
            "DocfxConfig" => DocfxConfigSchema.Value,
            "ConfigFilterRule" => FilterConfigSchema.Value,
            "TocItemViewModel" => TocSchema.Value,
            "XRefMap" => XRefMapSchema.Value,
            _ => throw new NotSupportedException(typeName),
        };

        return Validate(targetSchema, json, fileNameHint, out errors);
    }

    private static bool Validate(
        JsonSchema schema,
        string json,
        string fileNameHint,
        [NotNullWhen(false)] out ValidationError[] errors)
    {
        JsonDocument document;

        // Try to load as JsonDocument. if failed. Add validation error and return false.
        try
        {
            document = JsonDocument.Parse(json, SystemTextJsonUtility.DefaultDocumentOptions);
        }
        catch (JsonException ex)
        {
            var line = ex.LineNumber;
            var message = ex.Message;
            errors =
               [
                    new ValidationError
                    {
                        ErrorCode = "InvalidInputFile",
                        Location = $"{fileNameHint}#L{line})",
                        Message= message,
                        Type = "JsonException",
                    }
               ];
            return false;
        }

        return Validate(schema, document, fileNameHint, out errors);
    }

    private static bool Validate(
        JsonSchema schema,
        JsonDocument document,
        string fileNameHint,
        [NotNullWhen(false)] out ValidationError[] errors)
    {
        var result = schema.Evaluate(document, EvaluationOptions);

        if (result.IsValid)
        {
            errors = [];
            return true;
        }

        var results = new List<ValidationError>();
        ExtractValidationErrors(result, document, results);

        errors = results.ToArray();
        return false;
    }

    /// <summary>
    /// Recursivly visit EvaluationResults node and extract validation errors.
    /// </summary>
    private static void ExtractValidationErrors(EvaluationResults node, JsonDocument document, List<ValidationError> results)
    {
        // If parent node is valid. Skip child node errors. (Because AnyOf schema validate all possible type)
        if (node.IsValid)
            return;

        // Handle errors
        if (node.HasErrors)
        {
            foreach (var (key, message) in node.Errors!)
            {
                var location = node.InstanceLocation;
                // TODO: Gets line number of error. (Currently related API is not available: https://github.com/dotnet/runtime/issues/28482)

                results.Add(new ValidationError
                {
                    ErrorCode = "ViolateSchema",
                    Type = key,
                    Location = location.ToString(),
                    Message = message,
                });
            }
        }

        // If details exists. Visit node recursively.
        if (node.HasDetails)
        {
            foreach (var detail in node.Details.Where(x => !x.IsValid))
            {
                ExtractValidationErrors(detail, document, results);
            }
            return;
        }
    }

    /// <summary>
    /// Load JSON Schema from assembly embedded resource.
    /// </summary>
    private static JsonSchema LoadJsonSchema(string path)
    {
        const string ThisAssenblyName = "Docfx.Common";
        string resourceName = $"{ThisAssenblyName}.{path.Replace('/', '.')}";

        // Get JSON schema from embedded resource.
        using var stream = typeof(JsonSchemaUtility).Assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new DocfxException($"Specified embedded resource (key: {resourceName}) is not found.");
        }

        using var streamReader = new StreamReader(stream);
        var schemaText = streamReader.ReadToEnd();

        var schema = JsonSchema.FromText(schemaText, SystemTextJsonUtility.DefaultSerializerOptions);
        SchemaRegistry.Global.Register(schema);

        return schema;
    }

    public class ValidationError
    {
        /// <summary>
        /// Error Code (e.g. `ViolateSchema`, `InvalidInputFile`)
        /// </summary>
        public string ErrorCode { get; init; } = "";

        /// <summary>
        /// Location that cause validation error (e.g. `/build/content`)
        /// </summary>
        public string Location { get; init; } = "";

        /// <summary>
        /// Type of validation error. (e.g. "", "type", "oneof", "pattern")
        /// </summary>
        public string Type { get; init; } = "";

        /// <summary>
        /// Error message.
        /// </summary>
        public string Message { get; init; } = "";

        /// <summary>
        /// Gets formatted error message.
        /// </summary>
        public override string ToString()
        {
            return Type != ""
                ? $"[{Location}] {ErrorCode}({Type}): {Message}"
                : $"[{Location}] {ErrorCode}: {Message}";
        }
    }
}
