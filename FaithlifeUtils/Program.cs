using Serilog;
using System;

// Disable warnings when passing interpolated strings to Serilog
// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace FaithlifeUtils;

#pragma warning disable S1135 // Track uses of "TODO" tags
// For now, a simple tool to export notes from a single notebook into markdown
// Will likely extend later with additional commands
// TODO: Stream Amazon highlights into Verbum as notes?
// TODO: Some sort of tag graph view like Obsidian/Roam?
#pragma warning restore S1135 // Track uses of "TODO" tags

public class Program
{
    protected Program() { }

    private static void Main(string[] args)
    {
        FaithlifeConnector.AddResolver();
        using var log = new Logger();
        try
        {
            Run();
        }
        catch (Exception e)
        {
            Log.ForContext<Program>().Error($"{e.GetType().Name}: {e.Message}{Environment.NewLine}{e.StackTrace}");
        }
    }

    // Using a separate Run() method so FaithlifeConnector.AddResolver is invoked before we bring in the Faithlife assemblies
    private static void Run()
    {
        var config = Configuration.Load();
        using var connector = FaithlifeConnector.Create(config.LogosId, config.UserFolder);

        var notebook = connector.GetNotebook(config.NotebookName);
        MarkdownRenderer.RenderNotebook(connector, notebook, config.OutputFolder);
    }
}
