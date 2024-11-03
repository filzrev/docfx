// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using Docfx.DataContracts.Common;
using Docfx.DataContracts.ManagedReference;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

#nullable enable

namespace Docfx.Dotnet;

internal static class RoslynSymbolExtensions
{
    public static bool IsDerivedFrom(this ITypeSymbol symbol, ISymbol targetSymbol, bool isDirectOnly = false)
    {
        if (targetSymbol is not ITypeSymbol targetTypeSymbol)
            return false;

        if (!isDirectOnly)
            return symbol.InheritsFromIgnoringConstruction(targetTypeSymbol);

        var baseType = symbol.BaseType;
        if (baseType == null)
            return false;

        return SymbolEquivalenceComparer.Instance.Equals(baseType.OriginalDefinition, targetSymbol.OriginalDefinition);
    }
}
