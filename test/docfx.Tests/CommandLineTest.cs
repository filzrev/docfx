// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Spectre.Console;

namespace Docfx.Tests;

[Collection("docfx STA")]
public class CommandLineTest
{
    [Fact]
    public static void PrintsHelp()
    {
        var savedValue = AnsiConsole.Profile.Capabilities.Ansi;
        AnsiConsole.Profile.Capabilities.Ansi = false;
        try
        {

            //Assert.Equal(0, Program.Main(["-h"]));
            //Assert.Equal(0, Program.Main(["--help"]));
            //Assert.Equal(0, Program.Main(["build", "--help"]));
            //Assert.Equal(0, Program.Main(["serve", "--help"]));
            //Assert.Equal(0, Program.Main(["metadata", "--help"]));
            //Assert.Equal(0, Program.Main(["pdf", "--help"]));
            //Assert.Equal(0, Program.Main(["init", "--help"]));
            Assert.Equal(0, Program.Main(["download", "--help"]));
            //Assert.Equal(0, Program.Main(["merge", "--help"]));
            //Assert.Equal(0, Program.Main(["template", "--help"]));
        }
        finally
        {
            AnsiConsole.Profile.Capabilities.Ansi = savedValue;
        }
    }
}
