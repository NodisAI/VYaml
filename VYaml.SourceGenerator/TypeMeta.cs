using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace VYaml.SourceGenerator;

public enum NamingConvention
{
    LowerCamelCase,
    UpperCamelCase,
    SnakeCase,
    KebabCase,
}

internal class UnionMeta(string subTypeTag, INamedTypeSymbol subTypeSymbol)
{
    public string SubTypeTag { get; set; } = subTypeTag;
    public INamedTypeSymbol SubTypeSymbol { get; set; } = subTypeSymbol;
    public string FullTypeName { get; } = subTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}

internal class TypeMeta
{
    public TypeDeclarationSyntax Syntax { get; }
    public INamedTypeSymbol Symbol { get; }
    public AttributeData YamlObjectAttribute { get; }
    public string TypeName { get; }
    public string FullTypeName { get; }
    public string TypeNameWithoutGenerics { get; }
    public IReadOnlyList<IMethodSymbol> Constructors { get; }
    public IReadOnlyList<UnionMeta> UnionMetas { get; }
    public NamingConvention NamingConventionByType { get; } = NamingConvention.LowerCamelCase;
    public IReadOnlyList<MemberMeta> MemberMetas => memberMetas ??= GetSerializeMembers();
    public bool IsUnion => UnionMetas.Count > 0;

    private readonly ReferenceSymbols references;
    private MemberMeta[]? memberMetas;

    public TypeMeta(
        TypeDeclarationSyntax syntax,
        INamedTypeSymbol symbol,
        AttributeData yamlObjectAttribute,
        ReferenceSymbols references)
    {
        Syntax = syntax;
        Symbol = symbol;
        this.references = references;

        TypeName = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        FullTypeName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        TypeNameWithoutGenerics = TypeName.Replace('<', '_').Replace('>', '_').Replace(',', '_').Replace(' ', '_');

        YamlObjectAttribute = yamlObjectAttribute;

        foreach (var arg in YamlObjectAttribute.ConstructorArguments)
        {
            if (arg is { Kind: TypedConstantKind.Enum, Value: not null })
            {
                NamingConventionByType = (NamingConvention)arg.Value;
                break;
            }
        }

        Constructors = symbol.InstanceConstructors
            .Where(x => !x.IsImplicitlyDeclared) // remove empty ctor(struct always generate it), record's clone ctor
            .ToArray();

        UnionMetas = symbol.GetAttributes()
            .Where(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, references.YamlObjectUnionAttribute))
            .Where(x => x.ConstructorArguments.Length == 2)
            .Select(
                x => new UnionMeta(
                    (string)x.ConstructorArguments[0].Value!,
                    (INamedTypeSymbol)x.ConstructorArguments[1].Value!))
            .ToArray();
    }

    public bool IsPartial()
    {
        return Syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
    }

    public bool IsNested()
    {
        return Syntax.Parent is TypeDeclarationSyntax;
    }

    private MemberMeta[] GetSerializeMembers()
    {
        return memberMetas ??= Symbol.GetAllMembers() // iterate includes parent type
            .Where(x => x is (IFieldSymbol or IPropertySymbol) and { IsStatic: false, IsImplicitlyDeclared: false })
            .Where(
                x =>
                {
                    if (x.ContainsAttribute(references.YamlIgnoreAttribute)) return false;
                    if (x.DeclaredAccessibility != Accessibility.Public &&
                        !x.ContainsAttribute(references.YamlMemberAttribute)) return false;

                    if (x is IPropertySymbol p)
                    {
                        // set only can't be serializable member
                        if (p.GetMethod == null && p.SetMethod != null) return false;
                        if (p.IsIndexer) return false;
                    }
                    return true;
                })
            .Select((x, i) => new MemberMeta(x, references, i, NamingConventionByType))
            .OrderBy(x => x.Order)
            .ToArray();
    }
}