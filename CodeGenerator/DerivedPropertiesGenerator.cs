using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGenerator;

[Generator]
public class DerivedPropertiesGenerator : IIncrementalGenerator
{
	private const string DefinitionsNamespace = "Definitions";
	private const string ClassName = $"{DefinitionsNamespace}.DerivedProperties";
	private const string AttributeName = $"{DefinitionsNamespace}.HasDerivedPropertyAttribute";

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var classDeclarations = context
			.SyntaxProvider
			.CreateSyntaxProvider(static (s, _) => GetClassesWithAttributes(s), static (ctx, _) => FilterOnlyNeeded(ctx))
			.Where(static m => m is not null)!;

		var combination = context.CompilationProvider.Combine(classDeclarations.Collect());
		context.RegisterSourceOutput(combination, static (spc, source) => Execute(source.Item1, source.Item2, spc));
	}

	private static bool GetClassesWithAttributes(SyntaxNode node) => node is ClassDeclarationSyntax m && m.AttributeLists.Any();

	private static ClassDeclarationSyntax FilterOnlyNeeded(GeneratorSyntaxContext context)
	{
		var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
		foreach (var attributeSyntax in classDeclarationSyntax.AttributeLists.SelectMany(x => x.Attributes))
		{
			if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
			{
				continue;
			}

			var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
			var fullName = attributeContainingTypeSymbol.ToDisplayString();

			if (fullName == AttributeName)
			{
				return classDeclarationSyntax;
			}
		}

		return null;
	}

	private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
	{
		if (classes.IsDefaultOrEmpty)
		{
			return;
		}

		var hasDerivedPropertyAttributeType = compilation.GetTypeByMetadataName(AttributeName);
		var derivedPropertiesType = compilation.GetTypeByMetadataName(ClassName);

		if (hasDerivedPropertyAttributeType == null || derivedPropertiesType == null)
		{
			return;
		}

		foreach (var classDeclarationSyntax in classes.Distinct())
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
			if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
			{
				continue;
			}

			var className = classSymbol.Name;
			var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
			
			var sourceCode = new StringBuilder($@"using System;

namespace {namespaceName};

public partial class {className}
{{
");
			var attributes = classSymbol
				.GetAttributes()
				.Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, hasDerivedPropertyAttributeType));

			foreach (var attribute in attributes)
			{
				var name = (string)attribute.ConstructorArguments[0].Value!;
				var property = (IPropertySymbol)derivedPropertiesType.GetMembers(name).Single();
				var type = property.Type.OriginalDefinition.ToDisplayString();
				var definition = property.GetMethod!.DeclaringSyntaxReferences.SingleOrDefault()?.GetSyntax().ToString();
				sourceCode.AppendLine($"	public {type} {name} {definition};");
			}

			sourceCode.AppendLine("}");
			context.AddSource($"{className}.g.cs", sourceCode.ToString());
		}
	}
}
