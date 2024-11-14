// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Xunit;

#nullable enable

namespace Docfx.Dotnet.Tests;

public class CustomStringComparerTest
{
    [Theory]
    [InlineData("_", "_")]
    [InlineData("A", "A")]
    [InlineData("a", "A")]
    [InlineData("Z", "z")]
    [InlineData("test", "test")]
    [InlineData("test", "TEST")]
    public void StringComparerTest_Equals(string? value1, string? value2)
    {
        // Act
        var result = CustomStringComparer.Instance.Compare(value1, value2);

        // Assert
        result.Should().Be(0);

        // Additional test for `InvariantCultureIgnoreCase`
        if (!IsInvariantGlobalizationMode())
        {
            var result2 = StringComparer.InvariantCultureIgnoreCase.Compare(value1, value2);
            result2.Should().Be(0);
        }
    }

    [Theory]
    [InlineData("_", "A")]
    [InlineData("0", "A")]
    [InlineData("a", "Z")]
    [InlineData("test", "test_")]
    [InlineData("_", "1")]
    [InlineData("<", "1")]
    [InlineData("a_a", "a_b")]
    [InlineData("_", "`")]
    [InlineData(null, "test")]
    public void StringComparerTest_ForwardOrder(string? value1, string? value2)
    {
        // Act
        var result = CustomStringComparer.Instance.Compare(value1, value2);

        // Assert
        result.Should().BeLessThan(0);

        if (!IsInvariantGlobalizationMode())
        {
            var result2 = StringComparer.InvariantCultureIgnoreCase.Compare(value1, value2);
            result2.Should().BeLessThan(0);
        }
    }

    [Theory]
    [InlineData("A", "_")]
    [InlineData("A", "0")]
    [InlineData("Z", "a")]
    [InlineData("1", "_")]
    [InlineData("1", "<")]
    [InlineData("a_b", "a_a")]
    [InlineData("`", "_")]
    [InlineData("test", null)]
    public void StringComparerTest_ReverseOrder(string? value1, string? value2)
    {
        // Act
        var result = CustomStringComparer.Instance.Compare(value1, value2);

        // Assert
        result.Should().BeGreaterThan(0);

        if (!IsInvariantGlobalizationMode())
        {
            var result2 = StringComparer.InvariantCultureIgnoreCase.Compare(value1, value2);
            result2.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void StringComparerTest_SortOrder()
    {
        var data = new[]
        {
              "__",
              "__a",
              "_1",
              "a_a",
              "A_a",
              "AAA",
              "AAA<ABC>",
              "AAAA",
              "IRoutedView",
              "IRoutedView_`1",
              "IRoutedView`1",
              "IRoutedView<TViewModel>",
              "IRoutedView1",
            "IRoutedViewModel",
        };

        data.Order(CustomStringComparer.Instance).Should().ContainInOrder(data);

        if (!IsInvariantGlobalizationMode())
        {
            data.Order(StringComparer.InvariantCultureIgnoreCase).Should().ContainInOrder(data);
        }
    }

    private static bool IsInvariantGlobalizationMode()
    {
        return AppContext.TryGetSwitch("System.Globalization.Invariant", out bool isEnabled) && isEnabled;
    }
}
