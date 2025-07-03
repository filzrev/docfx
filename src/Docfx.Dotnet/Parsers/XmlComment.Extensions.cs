// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Docfx.Common;
using Docfx.DataContracts.ManagedReference;
using Docfx.Plugins;

namespace Docfx.Dotnet;

internal partial class XmlComment
{
    private static IEnumerable<LinkInfo> GetMultipleLinkInfo(XDocument doc, string selector)
    {
        var nodes = doc.XPathSelectElements(selector).ToArray();

        foreach (var node in nodes)
        {
            string altText = node.GetInnerXml().Trim();
            if (string.IsNullOrEmpty(altText))
            {
                altText = null;
            }

            string commentId = node.Attribute("cref")?.Value ?? "";

            string refId = node.Attribute("refId")?.Value ?? "";

            if (refId != "")
            {
                yield return new LinkInfo
                {
                    AltText = altText,
                    LinkId = refId,
                    CommentId = commentId,
                    LinkType = LinkType.CRef
                };
                continue;
            }

            if (commentId != "")
            {
                // Check if cref type is valid and trim prefix
                var match = CommentIdRegex().Match(commentId);
                if (match.Success)
                {
                    var id = match.Groups["id"].Value;
                    var type = match.Groups["type"].Value;
                    if (type == "Overload")
                    {
                        id += '*';
                    }

                    yield return new LinkInfo
                    {
                        AltText = altText,
                        LinkId = id,
                        CommentId = commentId,
                        LinkType = LinkType.CRef
                    };
                }
                continue;
            }

            string url = node.Attribute("href")?.Value ?? "";
            if (!string.IsNullOrEmpty(url))
            {
                yield return new LinkInfo
                {
                    AltText = altText ?? url,
                    LinkId = url,
                    LinkType = LinkType.HRef
                };
            }
        }
    }

    private IEnumerable<ExceptionInfo> GetMultipleCrefInfo(XDocument doc, string selector)
    {
        var nodes = doc.XPathSelectElements(selector).ToArray();
        foreach (var node in nodes)
        {
            string description = GetXmlValue(node);
            string commentId = node.Attribute("cref")?.Value ?? "";
            string refId = node.Attribute("refId")?.Value ?? "";

            if (refId != "")
            {
                yield return new ExceptionInfo
                {
                    Description = description,
                    Type = refId,
                    CommentId = commentId,
                };
                continue;
            }

            if (commentId == "")
                continue;

            // Check if exception type is valid and trim prefix
            var match = CommentIdRegex().Match(commentId);
            if (match.Success)
            {
                var id = match.Groups["id"].Value;
                var type = match.Groups["type"].Value;
                if (type == "T")
                {
                    yield return new ExceptionInfo
                    {
                        Description = description,
                        Type = id,
                        CommentId = commentId,
                    };
                }
            }
        }
    }

    private string GetSingleNodeValue(XDocument doc, string selector)
    {
        var elem = doc.XPathSelectElement(selector);

        return GetXmlValue(elem);
    }

    private Dictionary<string, string> GetListContent(XDocument doc, string xpath, string contentType, XmlCommentParserContext context)
    {
        var result = new Dictionary<string, string>();
        var nodes = doc.XPathSelectElements(xpath).ToArray();

        foreach (var node in nodes)
        {
            var name = node.Attribute("name")?.Value;
            if (name == null)
                continue;

            string description = GetXmlValue(node);

            if (!result.TryAdd(name, description))
            {
                string path = context.Source?.Remote != null ? Path.Combine(EnvironmentContext.BaseDirectory, context.Source.Remote.Path) : context.Source?.Path;
                Logger.LogWarning($"Duplicate {contentType} '{name}' found in comments, the latter one is ignored.", file: StringExtension.ToDisplayPath(path), line: context.Source?.StartLine.ToString());
            }
        }

        return result;
    }

    private IEnumerable<string> GetMultipleExampleNodes(XDocument doc, string selector)
    {
        // Gets nodes with XPath selector
        var nodes = doc.XPathSelectElements(selector).ToArray();
        foreach (var node in nodes)
        {
            var xml = GetXmlValue(node);
            yield return xml;
        }
    }

    private string GetXmlValue(XElement node)
    {
        if (node is null)
            return null;

        if (_context.SkipMarkup)
            return TrimEachLine(node.GetInnerXml());

        // Gets markdown text from XElement. (Insert empty line between Markdown/HTML tags)
        var xml = GetMarkdownText(node);

        if (xml.Contains("Pricing models are us"))
            ;

        // Trim extra indents.
        xml = TrimEachLine(xml);

        // Decode XML Entity References to markdown
        return GetInnerXmlAsMarkdown(xml);
    }

    private static string GetMarkdownText(XElement elem)
    {
        // Select HTML block tag nodes by XPath
        var nodes = elem.XPathSelectElements(BlockTagsXPath.Value).ToArray();

        // Insert HTML/Markdown separator lines
        foreach (var node in nodes)
        {
            if (node.NeedEmptyLineBefore())
            {
                node.InsertEmptyLineBefore();
            }

            if (node.NeedEmptyLineAfter())
            {
                node.AddAfterSelf(new XText("\n"));
            }
        }

        return elem.GetInnerXml();
    }

    // List of block tags that defined by CommonMark
    // https://spec.commonmark.org/0.31.2/#html-blocks
    private static readonly string[] BlockTags =
    {
        // List of block tags that defined by CommonMark
        "ol",
        "p",
        "table",
        "ul",

        // Recommended XML tags for C# documentation comments
        "example",
        
        // Other tags
        "pre",
    };

    private static readonly Lazy<string> BlockTagsXPath = new(string.Join(" | ", BlockTags.Select(tagName => $".//{tagName}")));
}

file static class XmlElementExtensions
{
    public static bool TryGetNonWhitespacePrevNode(this XElement elem, out XNode result)
    {
        var prev = elem.PreviousNode;
        while (prev != null && prev.IsWhitespaceNode())
            prev = prev.PreviousNode;

        if (prev == null)
        {
            result = null;
            return false;
        }

        result = prev;
        return true;
    }

    public static bool TryGetNonWhitespaceNextNode(this XElement elem, out XNode result)
    {
        var next = elem.NextNode;
        while (next != null && next.IsWhitespaceNode())
            next = next.NextNode;

        if (next == null)
        {
            result = null;
            return false;
        }

        result = next;
        return true;
    }

    public static bool IsPrevElementNode(this XElement elem)
    {
        var prev = elem.PreviousNode;
        while (prev != null && prev.IsWhitespaceNode())
            prev = prev.PreviousNode;

        if (prev == null)
        {
            return elem.Parent is XElement;
        }
        else
        {
            return prev is XElement;
        }
    }

    public static bool IsNextElementNode(this XElement elem)
    {
        var next = elem.NextNode;
        while (next != null && next.IsWhitespaceNode())
            next = next.PreviousNode;

        if (next == null)
            return false;

        return next is XElement;
    }

    /// <summary>
    /// Gets inner XML fragments text of XElement.
    /// </summary>
    public static string GetInnerXml(this XElement elem)
    {
        var descendants = elem.Descendants();

        using var sw = new StringWriter();

        using var writer = XmlWriter.Create(sw, new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            ConformanceLevel = ConformanceLevel.Fragment, // Required to write XML partial fragment
            Indent = false,                               // Don't apply formatter. Preserve original indents
            NewLineChars = "\n",
        });

        var nodes = elem.Nodes().ToArray();
        foreach (var node in nodes)
        {
            node.WriteTo(writer);
        }
        writer.Flush();

        var xml = sw.ToString();

        xml = RemoveCommonIndent(xml);

        // Trim begining spaces/lines if starts with HTML tag.
        var firstNode = nodes.FirstOrDefault(x => !x.IsWhitespaceNode());
        if (firstNode != null && firstNode.NodeType == XmlNodeType.Element)
            xml = xml.TrimStart();

        // Trim ending spaces/lines if starts with HTML tag.
        var lastNode = nodes.LastOrDefault(x => !x.IsWhitespaceNode());
        if (lastNode != null && lastNode.NodeType == XmlNodeType.Element)
            xml = xml.TrimEnd();

        return xml;
    }

    /// <summary>
    /// Gets indented XML text representation of XElement.
    /// </summary>
    public static string ToIndentedXml(this XElement element)
    {
        using var sw = new StringWriter();
        using var writer = XmlWriter.Create(sw, new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = true,
            IndentChars = "    ",
            NewLineChars = "\n",
            NewLineOnAttributes = false,
        });

        element.WriteTo(writer);
        writer.Flush();

        return sw.ToString();
    }

    public static bool IsWhitespaceNode(this XNode node)
    {
        if (node is not XText textNode)
            return false;

        return textNode.Value.All(char.IsWhiteSpace);
    }

    private static string RemoveCommonIndent(string text)
    {
        var lines = text.Split('\n').ToArray();

        var inPre = false;
        var indentCounts = new List<int>();

        // Count lines indent count (Excluding <pre></pre> tag regions)
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("<pre", StringComparison.OrdinalIgnoreCase))
                inPre = true;

            if (!inPre && !string.IsNullOrWhiteSpace(line))
            {
                int indent = line.TakeWhile(c => c == ' ' || c == '\t').Count();
                indentCounts.Add(indent);
            }

            if (trimmed.EndsWith("</pre>", StringComparison.OrdinalIgnoreCase))
                inPre = false;
        }

        int minIndent = indentCounts.DefaultIfEmpty(0).Min();

        inPre = false;
        var resultLines = new List<string>();
        foreach (var line in lines)
        {
            if (!inPre && line.Length >= minIndent)
                resultLines.Add(line.Substring(minIndent));
            else
                resultLines.Add(line);

            // Update inPre flag.
            var trimmed = line.Trim();
            if (trimmed.StartsWith("<pre>", StringComparison.OrdinalIgnoreCase))
                inPre = true;
            if (trimmed.EndsWith("</pre>", StringComparison.OrdinalIgnoreCase))
                inPre = false;
        }

        var results = string.Join("\n", resultLines);

        return results + "\n";
    }

    public static bool NeedEmptyLineBefore(this XElement node)
    {
        if (!node.TryGetNonWhitespacePrevNode(out var prev))
            return false;

        switch (prev.NodeType)
        {
            // If prev node is HTML element. No need to insert new line.
            case XmlNodeType.Element:
                return false;

            // Text need to be parsed as Markdown. It need to ensure empty lines.
            case XmlNodeType.Text:
                var textNode = (XText)prev;
                var value = textNode.Value;

                // Contains markdown text. It require empty line separator.
                if (value.EndsWith("\n\n"))
                    return false;
                return true;

            default:
                return false;
        }
    }

    public static void InsertEmptyLineBefore(this XElement elem)
    {
        // This code path is not expected to be called. (Because it's skipped by NeedEmptyLineBefore)
        if (elem.PreviousNode is not XText textNode)
        {
            elem.AddBeforeSelf(new XText("\n"));
            return;
        }

        var span = textNode.Value.AsSpan();
        int index = span.LastIndexOf('\n');

        ReadOnlySpan<char> lastLine = index == -1
            ? span
            : span.Slice(index + 1);

        if (lastLine.Length > 0 && lastLine.IsWhiteSpace())
        {
            textNode.Value = textNode.Value.Substring(0, index + 1); // Prev node text ends with `\n`
            elem.AddBeforeSelf(new XText($"\n{lastLine}"));          // Insert new line and add indent
        }
        else
        {
            elem.AddBeforeSelf(new XText("\n"));
        }
    }

    public static bool NeedEmptyLineAfter(this XElement node)
    {
        if (!node.TryGetNonWhitespaceNextNode(out var next))
            return false;

        switch (next.NodeType)
        {
            // If next node is HTML element. No need to insert new line.
            case XmlNodeType.Element:
                return false;

            // Text need to be parsed as Markdown. It need to ensure empty lines.
            case XmlNodeType.Text:
                var textNode = (XText)next;
                var value = textNode.Value;

                // Contains markdown text. It require empty line separator.
                if (value.EndsWith("\n\n"))
                    return false;
                return true;

            default:
                return false;
        }
    }
}
