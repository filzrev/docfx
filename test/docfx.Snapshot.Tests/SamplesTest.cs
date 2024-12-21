// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Docfx.Dotnet;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Actions;
using UglyToad.PdfPig.Annotations;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Outline;

namespace Docfx.Tests;

[Collection("docfx STA")]
[Trait("Stage", "Snapshot")]
public class SamplesTest : IDisposable
{
    private static readonly string s_samplesDir = Path.GetFullPath("../../../../../samples");

    private const string DOCFX_SOURCE_REPOSITORY_URL = nameof(DOCFX_SOURCE_REPOSITORY_URL);

    public SamplesTest()
    {
        Environment.SetEnvironmentVariable(DOCFX_SOURCE_REPOSITORY_URL, "https://github.com/dotnet/docfx");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(DOCFX_SOURCE_REPOSITORY_URL, null);
    }

    private class SamplesFactAttribute : FactAttribute
    {
        public SamplesFactAttribute()
        {
            // When target framework is changed.
            // It need to modify TargetFrameworks property of `docfx.Snapshot.Tests.csproj`
#if !NET8_0
            Skip = "Skip by target framework";
#endif
        }
    }

    ////[SamplesFact]
    ////[UseCustomBranchName("main")]
    ////public async Task Seed()
    ////{
    ////    var samplePath = $"{s_samplesDir}/seed";
    ////    Clean(samplePath);

    ////    Exec("dotnet", $"build \"{s_samplesDir}/seed/dotnet/assembly/BuildFromAssembly.csproj\"");

    ////    if (Debugger.IsAttached)
    ////    {
    ////        Program.Main([$"{samplePath}/docfx.json"]);
    ////    }
    ////    else
    ////    {
    ////        var docfxPath = Path.GetFullPath(OperatingSystem.IsWindows() ? "docfx.exe" : "docfx");
    ////        Exec(docfxPath, $"{samplePath}/docfx.json");
    ////    }

    ////    Parallel.ForEach(Directory.EnumerateFiles($"{samplePath}/_site", "*.pdf", SearchOption.AllDirectories), PdfToJson);

    ////    await VerifyDirectory($"{samplePath}/_site", IncludeFile, fileScrubber: ScrubFile).AutoVerify(includeBuildServer: false);

    ////    void PdfToJson(string path)
    ////    {
    ////        using var document = PdfDocument.Open(path);

    ////        var pdf = new
    ////        {
    ////            document.NumberOfPages,
    ////            Pages = document.GetPages().Select(p => new
    ////            {
    ////                p.Number,
    ////                p.NumberOfImages,
    ////                Text = ExtractText(p),
    ////                Links = p.ExperimentalAccess.GetAnnotations().Select(ToLink).ToArray(),
    ////            }).ToArray(),
    ////            Bookmarks = document.TryGetBookmarks(out var bookmarks) ? ToBookmarks(bookmarks.Roots) : null,
    ////        };

    ////        var json = JsonSerializer.Serialize(pdf, new JsonSerializerOptions
    ////        {
    ////            WriteIndented = true,
    ////            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    ////        });

    ////        File.WriteAllText(Path.ChangeExtension(path, ".pdf.json"), json);

    ////        object ToLink(Annotation a) => a.Action switch
    ////        {
    ////            GoToAction g => new { Goto = g.Destination },
    ////            UriAction u => new { u.Uri },
    ////        };

    ////        object ToBookmarks(IEnumerable<BookmarkNode> nodes)
    ////        {
    ////            return nodes.Select(node => node switch
    ////            {
    ////                DocumentBookmarkNode d => (object)new { node.Title, Children = ToBookmarks(node.Children), d.Destination },
    ////                UriBookmarkNode d => new { node.Title, Children = ToBookmarks(node.Children), d.Uri },
    ////            }).ToArray();
    ////        }
    ////    }
    ////}

    ////[SamplesFact]
    ////[UseCustomBranchName("main")]
    ////public async Task SeedMarkdown()
    ////{
    ////    var samplePath = $"{s_samplesDir}/seed";
    ////    var outputPath = nameof(SeedMarkdown);
    ////    Clean(samplePath);

    ////    Program.Main(["metadata", $"{samplePath}/docfx.json", "--outputFormat", "markdown", "--output", outputPath]);

    ////    await VerifyDirectory(outputPath).AutoVerify(includeBuildServer: false);
    ////}

    ////[SamplesFact]
    ////[UseCustomBranchName("main")]
    ////public async Task CSharp()
    ////{
    ////    var samplePath = $"{s_samplesDir}/csharp";
    ////    Clean(samplePath);

    ////    await DotnetApiCatalog.GenerateManagedReferenceYamlFiles($"{samplePath}/docfx.json");
    ////    await Docset.Build($"{samplePath}/docfx.json");

    ////    await VerifyDirectory($"{samplePath}/_site", IncludeFile).AutoVerify(includeBuildServer: false);
    ////}

    [SamplesFact]
    //[UseCustomBranchName("main")]
    public async Task Extensions()
    {
        TestContext.Current.TestOutputHelper.WriteLine("::: 0. " + DateTime.Now.ToString("HH:mm:ss.fff"));
        var samplePath = $"{s_samplesDir}/extensions";
        Clean(samplePath);

        TestContext.Current.TestOutputHelper.WriteLine("::: 1. " + DateTime.Now.ToString("HH:mm:ss.fff"));
        Exec("dotnet", $"build -c Release \"{samplePath}/build\" -bl:{samplePath}/../../test.binlog"); // Run dotnet build samples/build
        TestContext.Current.TestOutputHelper.WriteLine("::: 2. " + DateTime.Now.ToString("HH:mm:ss.fff"));
        Exec("dotnet", "run --no-build -c Release --project build", workingDirectory: samplePath);
        TestContext.Current.TestOutputHelper.WriteLine("::: 3." + DateTime.Now.ToString("HH:mm:ss.fff"));

        var tasl = VerifyDirectory($"{samplePath}/_site", IncludeFile).AutoVerify(includeBuildServer: false);
        TestContext.Current.TestOutputHelper.WriteLine("::: 4. " + DateTime.Now.ToString("HH:mm:ss.fff"));

        await tasl;
        Console.WriteLine("::: 5." + DateTime.Now.ToString("HH:mm:ss.fff"));

        await Task.Yield();
    }

    private static int Exec(string filename, string args, string workingDirectory = null)
    {
        var execTask = ProcessHelper.ExecAsync(
            filename,
            args,
            workingDirectory,
            environmentVariables: [],
            TestContext.Current.CancellationToken);

        return execTask.GetAwaiter().GetResult();
    }

    private static void Clean(string samplePath)
    {
        if (Directory.Exists($"{samplePath}/_site"))
            Directory.Delete($"{samplePath}/_site", recursive: true);

        if (Directory.Exists($"{samplePath}/_site_pdf"))
            Directory.Delete($"{samplePath}/_site_pdf", recursive: true);
    }

    private static bool IncludeFile(string file)
    {
        return Path.GetExtension(file) switch
        {
            ".json" => Path.GetFileName(file) != "manifest.json",
            ".yml" or ".md" => true,
            _ => false,
        };
    }

    private void ScrubFile(string path, StringBuilder builder)
    {
        if (Path.GetExtension(path) == ".json" && JsonNode.Parse(builder.ToString()) is JsonObject obj)
        {
            obj.Remove("__global");
            obj.Remove("_systemKeys");
            builder.Clear();
            builder.Append(JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            }));
        }
    }

    private string ExtractText(Page page)
    {
        // Gets PDF text content
        var text = ContentOrderTextExtractor.GetText(page, new ContentOrderTextExtractor.Options { ReplaceWhitespaceWithSpace = true });

        // string.Normalize is not works when using `Globalization Invariant Mode`.
        StringBuilder sb = new(text);

        // Normalize known ligature chars. (Note: `string.Normalize` is not works when using `Globalization Invariant Mode`)
        sb.Replace("ﬀ", "ff");
        sb.Replace("ﬃ", "ffi");
        sb.Replace("ﬂ", "fl");
        sb.Replace("ﬁ", "fi");

        // Normalize newline char.
        sb.Replace("\r\n", "\n");

        return sb.ToString();
    }
}
