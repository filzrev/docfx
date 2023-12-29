using System;
using System.Buffers.Text;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Renderers.Html;

namespace Docfx.MarkdigEngine.Extensions;

/// <summary>
/// An HTML renderer for a <see cref="CodeBlock"/> and <see cref="FencedCodeBlock"/>.
/// </summary>
/// <seealso cref="HtmlObjectRenderer{CodeBlock}" />
public class CustomCodeBlockRenderer : CodeBlockRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodeBlockRenderer"/> class.
    /// </summary>
    public CustomCodeBlockRenderer()
    {
    }

    protected override void Write(HtmlRenderer renderer, CodeBlock obj)
    {
        if (obj is FencedCodeBlock fencedCodeBlock
          && fencedCodeBlock.Info is string info
          && info.Equals("plantuml", StringComparison.OrdinalIgnoreCase))
        {
            WritePlantUmlTag(renderer, fencedCodeBlock);
            return;
        }

        // Fallback to default CodeBlockRenderer
        base.Write(renderer, obj);
    }

    private static void WritePlantUmlTag(HtmlRenderer renderer, FencedCodeBlock fencedCodeBlock)
    {
        // TODO: support custom server url/format.
        const string PlantUmlServerUrl = "https://www.plantuml.com/plantuml";
        const string Format = "svg";

        // Get PlatntUML code.
        var plantUmlCode = fencedCodeBlock.Lines.ToString();

        // Get deflate encoded bytes
        var encodedBytes = GetDeflateEncodedBytes(plantUmlCode);

        // Get custom Base64 encoded string.
        var base64Content = PlantUmlBase64Encoder.Encode(encodedBytes);
        string altText = "";
        string umlImageUrl = $"{PlantUmlServerUrl}/{Format}/{base64Content}";

        renderer.EnsureLine();
        renderer.Write($"<img src='{umlImageUrl}' alt='{altText}' />");
        renderer.EnsureLine();
        return;
    }

    private static byte[] GetDeflateEncodedBytes(string plantUmlCode)
    {
        var bytes = Encoding.UTF8.GetBytes(plantUmlCode);

        using var ms = new MemoryStream();
        using (var stream = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
        {
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
        }
        return ms.ToArray();
    }

    private static class PlantUmlBase64Encoder
    {
        public static string Encode(byte[] data)
        {
            var base64RequiredLength = Base64.GetMaxEncodedToUtf8Length(data.Length);
            var buffer = ArrayPool<byte>.Shared.Rent(base64RequiredLength);
            try
            {
                Base64.EncodeToUtf8(data, buffer, out int bytesConsumed, out int bytesWritten, isFinalBlock: true);
                ReplaceBase64Chars(buffer.AsSpan(0, bytesWritten));

                return Encoding.UTF8.GetString(buffer, 0, bytesWritten);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static void ReplaceBase64Chars(Span<byte> buffer)
        {
            const string Base64Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/" + "=";
            const string PlantUmlBase64Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_" + "=";

            for (int i = 0; i < buffer.Length; ++i)
            {
                var ch = (char)buffer[i];
                buffer[i] = (byte)PlantUmlBase64Chars[Base64Chars.IndexOf(ch)];
            }
        }
    }
}

