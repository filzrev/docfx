// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using global::YamlDotNet.Core;
using global::YamlDotNet.Helpers;
using global::YamlDotNet.Serialization.Utilities;
using global::YamlDotNet.Serialization;
using YamlDotNet.Core.Events;
using System.Reflection;
using System.Text;
using YamlDotNet.Serialization.NamingConventions;
using System.ComponentModel;

#nullable enable

namespace Docfx.YamlSerialization.NodeDeserializers;


// This class is based on YamlDotNet 15.3.0 implementation.
//   https://github.com/aaubry/YamlDotNet/blob/v15.3.0/YamlDotNet/Serialization/NodeDeserializers/ScalarNodeDeserializer.cs
//
// And modify deserialization behavior of `AttemptUnknownTypeDeserialization`
// - Modify Regex to match entire text.
// - Use `TryParse` insteadof `Parse`.
public sealed class ScalarNodeDeserializerWrapper : INodeDeserializer
{
    private readonly INodeDeserializer nodeDeserializer;

    private readonly ITypeConverter typeConverter = new ReflectionTypeConverter();
    private readonly INamingConvention enumNamingConvention = NullNamingConvention.Instance;

    public ScalarNodeDeserializerWrapper(INodeDeserializer nodeDeserializer)
    {
        this.nodeDeserializer = nodeDeserializer;
    }

    public bool Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value)
    {
        if (!parser.TryConsume<Scalar>(out var scalar))
        {
            value = null;
            return false;
        }

        // Strip off the nullable & fsharp option type, if present
        var underlyingType = Nullable.GetUnderlyingType(expectedType)
            ?? FsharpHelper.GetOptionUnderlyingType(expectedType)
            ?? expectedType;

        if (underlyingType.GetTypeInfo().IsEnum)
        {
            var enumName = enumNamingConvention.Reverse(scalar.Value);
            value = Enum.Parse(underlyingType, enumName, true);
            return true;
        }

        var typeCode = GetTypeCode(underlyingType);
        switch (typeCode)
        {
            case TypeCode.Boolean:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Single:
            case TypeCode.Double:
            case TypeCode.Decimal:
            case TypeCode.String:
            case TypeCode.Char:
            case TypeCode.DateTime:
                return nodeDeserializer.Deserialize(parser, expectedType, nestedObjectDeserializer, out value);

            default:
                if (expectedType == typeof(object))
                {
                    if (!scalar.IsKey)
                    {
                        value = AttemptUnknownTypeDeserialization(scalar);
                    }
                    else
                    {
                        // Default to string
                        value = scalar.Value;
                    }
                }
                else
                {
                    value = typeConverter.ChangeType(scalar.Value, expectedType, enumNamingConvention);
                }
                break;
        }
        return true;
    }

    private object? AttemptUnknownTypeDeserialization(Scalar value)
    {
        if (value.Style == ScalarStyle.SingleQuoted ||
            value.Style == ScalarStyle.DoubleQuoted ||
            value.Style == ScalarStyle.Folded)
        {
            return value.Value;
        }
        var v = value.Value;
        object? result;

        switch (v)
        {
            case "":
            case "~":
            case "null":
            case "Null":
            case "NULL":
                return null;
            case "true":
            case "True":
            case "TRUE":
                return true;
            case "false":
            case "False":
            case "FALSE":
                return false;
            default:
                if (Regex.IsMatch(v, "^0x[0-9a-fA-F]+$")) //base16 number
                {
                    if (TryAndSwallow(() => Convert.ToByte(v, 16), out result)) { }
                    else if (TryAndSwallow(() => Convert.ToInt16(v, 16), out result)) { }
                    else if (TryAndSwallow(() => Convert.ToInt32(v, 16), out result)) { }
                    else if (TryAndSwallow(() => Convert.ToInt64(v, 16), out result)) { }
                    else if (TryAndSwallow(() => Convert.ToUInt64(v, 16), out result)) { }
                    else
                    {
                        //we couldn't parse it, default to a string. It's probably to big.
                        result = v;
                    }
                }
                else if (Regex.IsMatch(v, "^0o[0-9a-fA-F]+$")) //base8 number
                {
                    if (TryAndSwallow(() => Convert.ToByte(v, 8), out result)) { }
                    else if (TryAndSwallow(() => Convert.ToInt16(v, 8), out result)) { }
                    else if (TryAndSwallow(() => Convert.ToInt32(v, 8), out result)) { }
                    else if (TryAndSwallow(() => Convert.ToInt64(v, 8), out result)) { }
                    else if (TryAndSwallow(() => Convert.ToUInt64(v, 8), out result)) { }
                    else
                    {
                        //we couldn't parse it, default to a string. It's probably to big.
                        result = v;
                    }
                }
                else if (Regex.IsMatch(v, @"^[-+]?(\.[0-9]+|[0-9]+(\.[0-9]*)?)([eE][-+]?[0-9]+)?$")) //regular number
                {
                    if (byte.TryParse(v, out var byteValue)) { result = byteValue; }
                    else if (short.TryParse(v, out var shortValue)) { result = shortValue; }
                    else if (int.TryParse(v, out var intValue)) { result = intValue; }
                    else if (long.TryParse(v, out var longValue)) { result = longValue; }
                    else if (ulong.TryParse(v, out var ulongValue)) { result = ulongValue; }
                    else if (double.TryParse(v, out var doubleValue))
                    {
                        // NET Core 3.0 or later will return infinity if the specified value is greater than float.MaxValue.
                        var floatValue = (float)doubleValue;
                        result = !float.IsNormal(floatValue)
                            ? floatValue
                            : (object)doubleValue;
                    }
                    else
                    {
                        //we couldn't parse it, default to string, It's probably too big
                        result = v;
                    }
                }
                else if (Regex.IsMatch(v, @"^[-+]?(\.inf|\.Inf|\.INF)$")) //infinities
                {
                    if (v.StartsWith("-"))
                    {
                        result = float.NegativeInfinity;
                    }
                    else
                    {
                        result = float.PositiveInfinity;
                    }
                }
                else if (Regex.IsMatch(v, @"^(\.nan|\.NaN|\.NAN)$")) //not a number
                {
                    result = float.NaN;
                }
                else
                {
                    // not a known type, so make it a string.
                    result = v;
                }
                break;
        }

        return result;
    }

    private static bool TryAndSwallow(Func<object> attempt, out object? value)
    {
        try
        {
            value = attempt();
            return true;
        }
        catch
        {
            value = null;
            return false;
        }
    }

    public static TypeCode GetTypeCode(Type type)
    {
        var isEnum = type.GetTypeInfo().IsEnum;
        if (isEnum)
        {
            type = Enum.GetUnderlyingType(type);
        }

        if (type == typeof(bool))
        {
            return TypeCode.Boolean;
        }
        else if (type == typeof(char))
        {
            return TypeCode.Char;
        }
        else if (type == typeof(sbyte))
        {
            return TypeCode.SByte;
        }
        else if (type == typeof(byte))
        {
            return TypeCode.Byte;
        }
        else if (type == typeof(short))
        {
            return TypeCode.Int16;
        }
        else if (type == typeof(ushort))
        {
            return TypeCode.UInt16;
        }
        else if (type == typeof(int))
        {
            return TypeCode.Int32;
        }
        else if (type == typeof(uint))
        {
            return TypeCode.UInt32;
        }
        else if (type == typeof(long))
        {
            return TypeCode.Int64;
        }
        else if (type == typeof(ulong))
        {
            return TypeCode.UInt64;
        }
        else if (type == typeof(float))
        {
            return TypeCode.Single;
        }
        else if (type == typeof(double))
        {
            return TypeCode.Double;
        }
        else if (type == typeof(decimal))
        {
            return TypeCode.Decimal;
        }
        else if (type == typeof(DateTime))
        {
            return TypeCode.DateTime;
        }
        else if (type == typeof(string))
        {
            return TypeCode.String;
        }
        else
        {
            return TypeCode.Object;
        }
    }
}