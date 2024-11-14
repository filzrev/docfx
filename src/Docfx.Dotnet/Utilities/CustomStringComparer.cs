// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Buffers;

namespace Docfx.Dotnet;

/// <summary>
/// StringComparer that emulate <see cref="StringComparer.InvariantCultureIgnoreCase"> behavior.
/// </summary>
internal class CustomStringComparer : IComparer<string>
{
    public static readonly CustomStringComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        var xSpan = x.AsSpan();
        var ySpan = y.AsSpan();

        int minLength = Math.Min(xSpan.Length, ySpan.Length);
        for (int i = 0; i < minLength; ++i)
        {
            var xChar = xSpan[i];
            var yChar = ySpan[i];

            if (xChar == yChar)
                continue;

            if (char.IsAscii(xChar) && char.IsAscii(yChar))
            {
                int result = CompareAsciiChar(xChar, yChar);
                if (result != 0)
                    return result;
                continue;
            }
            else
            {
                int result = xChar.CompareTo(yChar);
                if (result != 0)
                    return result;
                continue;
            }
        }

        return x.Length.CompareTo(y.Length);
    }

    // Compare ASCII char with custom order and ignore case.
    private static int CompareAsciiChar(char xChar, char yChar)
    {
        xChar = char.ToUpperInvariant(xChar);
        yChar = char.ToUpperInvariant(yChar);

        var xOrder = AsciiCharSortOrders[xChar];
        var yOrder = AsciiCharSortOrders[yChar];

        return xOrder.CompareTo(yOrder);
    }

    // ASCII character sort order lookup table.
    // It's based on `StringComparer.CurrentCultureIgnoreCase`'s sort order
    private static readonly byte[] AsciiCharSortOrders =
    [
        0,    // NUL
        1,    // SOH
        2,    // STX
        3,    // ETX
        4,    // EOT
        5,    // ENQ
        6,    // ACK
        7,    // BEL
        8,    // BS
        28,   // FS
        29,   // GS
        30,   // RS
        31,   // US
        32,   // SP
        9,    // HT
        10,   // LF
        11,   // VT
        12,   // FF
        13,   // CR
        14,   // SO
        15,   // SI
        16,   // DLE
        17,   // DC1
        18,   // DC2
        19,   // DC3
        20,   // DC4
        21,   // NAK
        22,   // SYN
        23,   // ETB
        24,   // CAN
        25,   // EM
        26,   // SUB
        33,   //  
        39,   // !
        43,   // "
        55,   // #
        65,   // $
        56,   // %
        54,   // &
        42,   // '
        44,   // (
        45,   // )
        51,   // *
        59,   // +
        36,   // ,
        35,   // -
        41,   // .
        52,   // /
        66,   // 0
        67,   // 1
        68,   // 2
        69,   // 3
        70,   // 4
        71,   // 5
        72,   // 6
        73,   // 7
        74,   // 8
        75,   // 9
        38,   // :
        37,   // ;
        60,   // <
        61,   // =
        62,   // >
        40,   // ?
        50,   // @
        76,   // A
        78,   // B
        80,   // C
        82,   // D
        84,   // E
        86,   // F
        88,   // G
        90,   // H
        92,   // I
        94,   // J
        96,   // K
        98,   // L
        100,  // M
        102,  // N
        104,  // O
        106,  // P
        108,  // Q
        110,  // R
        112,  // S
        114,  // T
        116,  // U
        118,  // V
        120,  // W
        122,  // X
        124,  // Y
        126,  // Z
        46,   // [
        53,   // \
        47,   // ]
        58,   // ^
        34,   // _
        57,   // `
        77,   // a
        79,   // b
        81,   // c
        83,   // d
        85,   // e
        87,   // f
        89,   // g
        91,   // h
        93,   // i
        95,   // j
        97,   // k
        99,   // l
        101,  // m
        103,  // n
        105,  // o
        107,  // p
        109,  // q
        111,  // r
        113,  // s
        115,  // t
        117,  // u
        119,  // v
        121,  // w
        123,  // x
        125,  // y
        127,  // z
        48,   // {
        63,   // |
        49,   // }
        64,   // ~
        27,   // ESC
    ];
}
