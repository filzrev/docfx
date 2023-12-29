using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Markdig.Renderers;
using Markdig;

namespace Docfx.MarkdigEngine.Extensions;

internal class PlantUmlExtension : IMarkdownExtension
{
    // TODO: Support options.
    public PlantUmlExtension()
    {
    }

    public void Setup(MarkdownPipelineBuilder pipeline)
    {
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        var htmlRenderer = renderer as HtmlRenderer;
        var renderers = htmlRenderer?.ObjectRenderers;

        if (renderers != null && !renderers.Contains<CustomCodeBlockRenderer>())
        {
            // TODO: Need to insert renderer to proper position.
            renderers.Insert(0, new CustomCodeBlockRenderer());
        }
    }
}
