// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.Contracts;
using System.Text;
using Docfx.Common;
using Docfx.Plugins;
using HtmlAgilityPack;
using Spectre.Console;

#nullable enable

namespace Docfx.Build.Engine;

/// <summary>
/// PostProcessor to rewrite URLs of HTML files.
/// </summary>
[Export(nameof(UrlRewritePostProcessor), typeof(IPostProcessor))]
internal sealed class UrlRewritePostProcessor : IPostProcessor
{
    private Uri? BaseUri = null;
    private UrlRewriteMode Mode = UrlRewriteMode.None;

    private enum UrlRewriteMode
    {
        None,         // Skip URL Rewrite. Use file relative path.
        SiteRelative, // Rewrite to site relative URL.
        Absolute      // Rewrite to absolute URL.
    }

    /// <inheritdoc/>
    public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
    {
        // TODO: Currently PostProcessor can't access file's metadata. Use global metadata instead. (See: https://github.com/dotnet/docfx/issues/1294)
        if (!metadata.TryGetValue("_urlRewriteBase", out object? value))
        {
            Logger.LogWarning($"UrlRewritePostProcessor is loaded. But `_urlRewriteBase` setting is not exists.");
            return metadata;
        }

        var url = value as string;

        // If absolute URL is specified.
        if (Uri.TryCreate(url, UriKind.Absolute, out BaseUri))
        {
            Mode = UrlRewriteMode.Absolute;
            BaseUri = new Uri(url);
            return metadata;
        }
        else
        {
            // Check URL format. It need to be Site Relative Path format.
            if (url == null || !url.StartsWith('/'))
            {
                Logger.LogWarning($"UrlRewritePostProcessor is loaded. But `_urlRewriteBase` metadata value ({url}) is invalid. It must be Site Relative URL that starts with slash.");
                return metadata;
            }

            // Normalize Site Relative URL.
            if (!url.EndsWith('/'))
                url = $"{url}/";

            Mode = UrlRewriteMode.SiteRelative;
            BaseUri = new Uri($"file://{url}"); // Use `file` protocol to resolve site relative URL.

            return metadata;
        }
    }

    /// <inheritdoc/>
    public Manifest Process(Manifest manifest, string outputFolder)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(outputFolder);

        if (Mode == UrlRewriteMode.None || BaseUri == null)
            return manifest; // Skip PostProcessor.

        switch (Mode)
        {
            case UrlRewriteMode.None:
                return manifest; // Skip

            case UrlRewriteMode.Absolute:
                Logger.LogInfo($"UrlRewritePostProcessor is enabled. Relative URLs in HTML files are rewritten based on Absolute URL ({BaseUri.AbsoluteUri}).");
                break;

            case UrlRewriteMode.SiteRelative:
                Logger.LogInfo($"UrlRewritePostProcessor is enabled. Relative URLs in HTML files are rewritten based on Site Relative URL ({BaseUri.AbsolutePath})");
                break;
        }

        var htmlFiles = from item in manifest.Files ?? Enumerable.Empty<ManifestItem>()
                        from output in item.Output
                        where output.Key.Equals(".html", StringComparison.OrdinalIgnoreCase)
                        select new
                        {
                            output.Value.RelativePath, // Relative Path of HTML file.
                            output.Value.Metadata,     // Note: It need to explicitly set `FileModel.ManifestProperties` in build phase.
                        };

        foreach (var file in htmlFiles)
        {
            var relativePath = file.RelativePath;
            if (!EnvironmentContext.FileAbstractLayer.Exists(relativePath))
            {
                continue;
            }

            var document = new HtmlDocument() { BackwardCompatibility = false };

            try
            {
                using var stream = EnvironmentContext.FileAbstractLayer.OpenRead(relativePath);
                document.Load(stream, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Warning: Can't load content from {relativePath}: {ex.Message}");
                continue;
            }

            ProcessHtmlDocument(document, relativePath);

            using (var stream = EnvironmentContext.FileAbstractLayer.Create(relativePath))
            {
                document.Save(stream, Encoding.UTF8);
            }
        }

        return manifest;
    }

    private void ProcessHtmlDocument(HtmlDocument document, string relativePath)
    {
        RewriteTagAttributeUrls("script", "src");                        // Rewrite `src` attribute of `<script>` tag.
        RewriteTagAttributeUrls("link", "href");                         // Rewrite `href` attribute of `<link>` tag.
        RewriteTagAttributeUrls("a", "href");                            // Rewrite `href` attribute of `<a>` tag.
        RewriteTagAttributeUrls("img", "src");                           // Rewrite `src` attribute of `<img>` tag.
        RewriteTagAttributeUrls("meta", "content", DocfxMetaTagFilter);  // Rewrite `content` attribute of docfx related `<meta>` tag.
        return;

        void RewriteTagAttributeUrls(
            string tagName,
            string attributeName,
            Func<HtmlNode, bool>? filter = null)
        {
            var nodes = document.DocumentNode.SelectNodes($"//{tagName}[@{attributeName}]") ?? Enumerable.Empty<HtmlNode>();

            if (filter != null)
                nodes = nodes.Where(filter);

            foreach (HtmlNode node in nodes)
            {
                var attr = node.Attributes[attributeName];
                if (string.IsNullOrEmpty(attr.Value))
                    continue;

                attr.Value = ResolveUrl(relativePath, attr.Value);
            }
        }

        static bool DocfxMetaTagFilter(HtmlNode node)
        {
            var key = node.GetAttributeValue<string>("name", "");
            key = node.GetAttributeValue<string>("property", key); // `default` template use this attribute.

            return key switch
            {
                "docfx:navrel" => true,
                "docfx:tocrel" => true,
                "docfx:rel" => true,
                _ => false,
            };
        }
    }

    private string ResolveUrl(string relativePath, string targetUrl)
    {
        // If it can't parsed as URL. return original URL.
        if (!Uri.TryCreate(targetUrl, UriKind.RelativeOrAbsolute, out var resultUri))
            return targetUrl;

        // When linked URL is absolte URL. Return original URL.
        if (resultUri.IsAbsoluteUri)
            return targetUrl;

        // Resolve URL.
        var baseUri = new Uri(BaseUri!, relativePath);
        var resolvedUri = new Uri(baseUri, resultUri);

        switch (Mode)
        {
            case UrlRewriteMode.Absolute:
                return resolvedUri.ToString();

            case UrlRewriteMode.SiteRelative:
            default:
                return resolvedUri.PathAndQuery;
        }
    }
}
