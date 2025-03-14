using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace VYaml.SourceGenerator;

static class SymbolExtensions
{
    public static bool ContainsAttribute(this ISymbol symbol, INamedTypeSymbol attribute)
    {
        return symbol.GetAttributes().Any(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, attribute));
    }

    public static AttributeData? GetAttribute(this ISymbol symbol, INamedTypeSymbol attribute)
    {
        return symbol.GetAttributes().FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, attribute));
    }

    public static AttributeData? GetImplAttribute(this ISymbol symbol, INamedTypeSymbol implAttribute)
    {
        return symbol.GetAttributes().FirstOrDefault(x =>
        {
            if (x.AttributeClass == null) return false;
            if (x.AttributeClass.EqualsUnconstructedGenericType(implAttribute)) return true;

            foreach (var item in x.AttributeClass.GetAllBaseTypes())
            {
                if (item.EqualsUnconstructedGenericType(implAttribute))
                {
                    return true;
                }
            }
            return false;
        });
    }

    public static IEnumerable<ISymbol> GetAllMembers(this INamedTypeSymbol? symbol)
    {
        var ignoredSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

        while (symbol != null && symbol.SpecialType != SpecialType.System_Object)
        {
            foreach (var member in symbol.GetMembers())
            {
                if (member.IsAbstract || ignoredSymbols.Contains(member)) continue;
                yield return member;
                foreach (var ignoredSymbol in GetIgnoredSymbols(member))
                {
                    ignoredSymbols.Add(ignoredSymbol);
                }
            }

            symbol = symbol.BaseType;
        }

        IEnumerable<ISymbol> GetIgnoredSymbols(ISymbol? memberSymbol)
        {
            while (memberSymbol != null)
            {
                switch (memberSymbol)
                {
                    case IPropertySymbol { OverriddenProperty: { } overriddenProperty }:
                        memberSymbol = overriddenProperty;
                        break;
                    case IEventSymbol { OverriddenEvent: { } overriddenEvent }:
                        memberSymbol = overriddenEvent;
                        break;
                    case IMethodSymbol { OverriddenMethod: { } overriddenMethod }:
                        memberSymbol = overriddenMethod;
                        break;
                    default:
                        yield break;
                }
                yield return memberSymbol;
            }
        }
    }

    public static bool InheritsFrom(this INamedTypeSymbol symbol, INamedTypeSymbol baseSymbol)
    {
        var baseName = baseSymbol.ToString();
        while (true)
        {
            if (symbol.ToString() == baseName)
            {
                return true;
            }
            if (symbol.BaseType != null)
            {
                symbol = symbol.BaseType;
                continue;
            }
            break;
        }
        return false;
    }

    public static IEnumerable<INamedTypeSymbol> GetAllBaseTypes(this INamedTypeSymbol symbol)
    {
        var t = symbol.BaseType;
        while (t != null)
        {
            yield return t;
            t = t.BaseType;
        }
    }

    public static bool EqualsUnconstructedGenericType(this INamedTypeSymbol left, INamedTypeSymbol right)
    {
        var l = left.IsGenericType ? left.ConstructUnboundGenericType() : left;
        var r = right.IsGenericType ? right.ConstructUnboundGenericType() : right;
        return SymbolEqualityComparer.Default.Equals(l, r);
    }
}
