// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Docfx.Dotnet;

internal partial class XmlComment
{
    /// <summary>
    /// Gets markdown text from XElement
    /// </summary>
    private static string GetMarkdownText(XElement elem)
    {
        // Select HTML block tag nodes by XPath
        var nodes = elem.XPathSelectElements(BlockTagsXPath.Value).ToArray();

        // Insert HTML/Markdown separator lines
        foreach (var node in nodes)
        {
            if (node.NeedEmptyLineBefore())
                node.InsertEmptyLineBefore();

            if (node.NeedEmptyLineAfter())
                node.AddAfterSelf(new XText("\n"));
        }

        return elem.GetInnerXml();
    }

    private static string GetInnerXml(XElement elem)
        => elem.GetInnerXml();
}

// Define file scoped extension methods.
static file class XElementExtensions
{
    /// <summary>
    /// Gets inner XML text of XElement.
    /// </summary>
    public static string GetInnerXml(this XElement elem)
    {
        using var sw = new StringWriter();
        using var writer = XmlWriter.Create(sw, new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            ConformanceLevel = ConformanceLevel.Fragment, // Required to write XML partial fragment
            Indent = false,                               // Preserve original indents
            NewLineChars = "\n",                          // Use LF
        });

        var nodes = elem.Nodes().ToArray();
        foreach (var node in nodes)
        {
            node.WriteTo(writer);
        }
        writer.Flush();

        var xml = sw.ToString();

        // Remove shared indents
        xml = RemoveCommonIndent(xml);

        // Trim beginning spaces/lines if text starts with HTML tag.
        var firstNode = nodes.FirstOrDefault(x => !x.IsWhitespaceNode());
        if (firstNode != null && firstNode.NodeType == XmlNodeType.Element)
            xml = xml.TrimStart();

        // Trim ending spaces/lines if text ends with HTML tag.
        var lastNode = nodes.LastOrDefault(x => !x.IsWhitespaceNode());
        if (lastNode != null && lastNode.NodeType == XmlNodeType.Element)
            xml = xml.TrimEnd();

        return xml;
    }

    public static bool NeedEmptyLineBefore(this XElement node)
    {
        if (!node.TryGetNonWhitespacePrevNode(out var prev))
            return false;

        switch (prev.NodeType)
        {
            // If prev node is HTML element. No need to insert empty line.
            case XmlNodeType.Element:
                return false;

            // Ensure empty lines exists before node.
            case XmlNodeType.Text:
                var textNode = (XText)prev;

                if (textNode.Value.EndsWith("\n\n"))
                    return false;
                return true;

            default:
                return false;
        }
    }

    public static void InsertEmptyLineBefore(this XElement elem)
    {
        // This code path is not expected to be called. (Because it's skipped by NeedEmptyLineBefore)
        if (elem.PreviousNode is not XText prevTextNode)
        {
            elem.AddBeforeSelf(new XText("\n"));
            return;
        }

        var span = prevTextNode.Value.AsSpan();
        int index = span.LastIndexOf('\n');

        ReadOnlySpan<char> lastLine = index == -1
            ? span
            : span.Slice(index + 1);

        if (lastLine.Length > 0 && lastLine.IsWhiteSpace())
        {
            // Insert new line before indent of last line.
            prevTextNode.Value = prevTextNode.Value.Insert(index, "\n");
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

            // Ensure empty lines exists after node.
            case XmlNodeType.Text:
                var textNode = (XText)next;

                if (textNode.Value.EndsWith("\n\n"))
                    return false;
                return true;

            default:
                return false;
        }
    }

    private static bool IsWhitespaceNode(this XNode node)
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

        // Caluculate line's indent chars (<pre></pre> tag region is excluded)
        foreach (var line in lines)
        {
            if (!inPre && !string.IsNullOrWhiteSpace(line))
            {
                int indent = line.TakeWhile(c => c == ' ' || c == '\t').Count();
                indentCounts.Add(indent);
            }

            var trimmed = line.Trim();
            if (trimmed.StartsWith("<pre", StringComparison.OrdinalIgnoreCase))
                inPre = true;

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

        // Insert empty line to append `\n`.
        resultLines.Add("");

        return string.Join("\n", resultLines);
    }

    private static bool TryGetNonWhitespacePrevNode(this XElement elem, out XNode result)
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

    private static bool TryGetNonWhitespaceNextNode(this XElement elem, out XNode result)
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
}
