// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Docfx.Common;
using Docfx.Tests.Common;
using FluentAssertions;

namespace Docfx.Tests;

[Collection("docfx STA")]
public class CommandLineHelpContractTest
{
    /// <summary>
    /// Verify docfx help contents.
    /// </summary>
    [Fact]
    public static Task CommandLineHelpOutputTest()
    {
        using var sw = new StringWriter();
        Console.SetOut(sw);

        try
        {
            // Print help contents to writer
            RunHelpCommand();
            RunHelpCommand("init");
            RunHelpCommand("build");
            RunHelpCommand("metadata");
            RunHelpCommand("serve");
            RunHelpCommand("pdf");
            RunHelpCommand("template");
            RunHelpCommand("download");
            RunHelpCommand("merge");

            var helpText = sw.ToString();
            return Verify(new Target("txt", helpText)).UseFileName("Help").AutoVerify(includeBuildServer: false);
        }
        finally
        {
            Console.SetOut(Console.Out);
        }
    }

    private static void RunHelpCommand(string command = "")
    {
        Console.WriteLine($"> docfx {command} --help");
        Program.Main([command, "--help"]).Should().Be(0);
        Console.WriteLine();
    }
}
