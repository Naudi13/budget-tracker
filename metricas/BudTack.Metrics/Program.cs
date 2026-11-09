using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Mide cada clase de C# de la aplicación: WMC, DIT, NOC, CBO, RFC, LCOM* e Índice de Mantenibilidad.
// Uso: dotnet run --project metricas/BudTack.Metrics -c Release -- <raíz del repositorio> <carpeta de salida>
// Requiere compilar antes src/api y src/db en Release. Las definiciones están en docs/metricas/fichas.md.

if (args.Length != 2)
{
    Console.Error.WriteLine("Uso: BudTack.Metrics <raíz del repositorio> <carpeta de salida>");
    return 2;
}

var root = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
Directory.CreateDirectory(output);

var apiBin = Path.Combine(root, "src/api/src/BudTack.Api/bin/Release/net10.0");
(string Name, string Folder, string Bin)[] projects =
[
    ("BudTack.Api", "src/api/src/BudTack.Api", apiBin),
    ("BudTack.Identity", "src/api/src/BudTack.Identity", apiBin),
    ("BudTack.Transactions", "src/api/src/BudTack.Transactions", apiBin),
    ("BudTack.Dashboard", "src/api/src/BudTack.Dashboard", apiBin),
    ("BudTack.Db.Migrations", "src/db/src/BudTack.Db.Migrations", Path.Combine(root, "src/db/src/BudTack.Db.Migrations/bin/Release/net10.0")),
];

var classes = new List<ClassMetrics>();
var methods = new List<MethodMetrics>();
foreach (var project in projects)
{
    var compilation = Meter.Compile(root, project.Name, project.Folder, project.Bin);
    // CS8795: los métodos [LoggerMessage] los completa un generador de código que aquí no se ejecuta.
    var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error && d.Id != "CS8795").ToList();
    if (errors.Count > 0)
    {
        foreach (var error in errors.Take(10))
        {
            Console.Error.WriteLine(error);
        }

        Console.Error.WriteLine($"{project.Name}: {errors.Count} errores de compilación; la medición no sería fiable.");
        return 1;
    }

    Meter.Measure(compilation, project.Name, classes, methods);
}

Csv.Write(
    Path.Combine(output, "ck_clases.csv"),
    "proyecto,clase,metodos,loc,wmc,max_vg,dit,noc,cbo,cbo_propio,rfc,lcom,halstead_v,mi",
    classes.OrderBy(c => c.Project, StringComparer.Ordinal).ThenBy(c => c.Name, StringComparer.Ordinal).Select(c => string.Join(",",
        c.Project, c.Name, Csv.N(c.Methods), Csv.N(c.Loc), Csv.N(c.Wmc), Csv.N(c.MaxVg), Csv.N(c.Dit), Csv.N(c.Noc), Csv.N(c.Cbo), Csv.N(c.OwnCbo), Csv.N(c.Rfc),
        Csv.D(c.Lcom), Csv.D(c.Volume), Csv.D(c.Mi))));
Csv.Write(
    Path.Combine(output, "vg_roslyn.csv"),
    "proyecto,clase,metodo,vg",
    methods.OrderBy(m => m.Project, StringComparer.Ordinal).ThenBy(m => m.Class, StringComparer.Ordinal).ThenBy(m => m.Name, StringComparer.Ordinal).Select(m => string.Join(",",
        m.Project, m.Class, m.Name, Csv.N(m.Vg))));

Console.WriteLine($"{classes.Count} clases y {methods.Count} métodos medidos.");
return 0;

internal sealed record ClassMetrics(
    string Project, string Name, int Methods, int Loc, int Wmc, int MaxVg, int Dit, int Noc, int Cbo, int OwnCbo, int Rfc, double? Lcom, double? Volume, double? Mi);

internal sealed record MethodMetrics(string Project, string Class, string Name, int Vg);

internal static class Csv
{
    public static string N(int value) => value.ToString(CultureInfo.InvariantCulture);

    public static string D(double? value) => value?.ToString("0.00", CultureInfo.InvariantCulture) ?? string.Empty;

    public static void Write(string path, string header, IEnumerable<string> rows)
    {
        var text = new StringBuilder(header).Append('\n');
        foreach (var row in rows)
        {
            text.Append(row).Append('\n');
        }

        File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
    }
}

internal static class Meter
{
    private static readonly SymbolDisplayFormat ShortName = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        genericsOptions: SymbolDisplayGenericsOptions.None);

    public static CSharpCompilation Compile(string root, string name, string folder, string bin)
    {
        var parse = new CSharpParseOptions(LanguageVersion.Latest);
        var projectFolder = Path.Combine(root, folder);
        var sources = Directory.EnumerateFiles(projectFolder, "*.cs", SearchOption.AllDirectories)
            .Where(IsSource)
            .Concat(Directory.EnumerateFiles(Path.Combine(projectFolder, "obj/Release/net10.0"), "*.GlobalUsings.g.cs"))
            .OrderBy(path => path, StringComparer.Ordinal);
        var trees = sources.Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), parse, path)).ToList();

        var runtime = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var aspNet = Directory.GetDirectories(Path.GetDirectoryName(runtime.Replace("Microsoft.NETCore.App", "Microsoft.AspNetCore.App", StringComparison.Ordinal))!, "10.*")
            .OrderBy(path => path, StringComparer.Ordinal)
            .Last();
        var references = Directory.EnumerateFiles(runtime, "*.dll")
            .Concat(Directory.EnumerateFiles(aspNet, "*.dll"))
            .Concat(Directory.EnumerateFiles(bin, "*.dll"))
            .Where(path => !Path.GetFileNameWithoutExtension(path).Equals(name, StringComparison.Ordinal))
            .Where(IsManaged)
            .GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path));

        var hasEntryPoint = trees.Any(tree => tree.GetCompilationUnitRoot().Members.Any(member => member is GlobalStatementSyntax));
        var kind = hasEntryPoint ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary;
        return CSharpCompilation.Create(name, trees, references, new CSharpCompilationOptions(kind));
    }

    public static void Measure(CSharpCompilation compilation, string project, List<ClassMetrics> classes, List<MethodMetrics> methods)
    {
        var declarations = compilation.SyntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().Where(d => d is ClassDeclarationSyntax or RecordDeclarationSyntax or StructDeclarationSyntax)
                .Select(d => (Declaration: d, Model: compilation.GetSemanticModel(tree))))
            .ToList();
        var types = declarations.Select(d => d.Model.GetDeclaredSymbol(d.Declaration)!).ToList();

        foreach (var (declaration, model) in declarations)
        {
            var type = model.GetDeclaredSymbol(declaration)!;
            var name = type.ToDisplayString(ShortName);
            var members = MethodLike(declaration).ToList();
            var vgs = members.Select(Complexity).ToList();
            for (var i = 0; i < members.Count; i++)
            {
                methods.Add(new MethodMetrics(project, name, MemberName(members[i]), vgs[i]));
            }

            var wmc = vgs.Sum();
            var (volume, loc) = HalsteadAndLoc(members);
            double? mi = members.Count == 0 || loc == 0 || volume <= 0
                ? null
                : Math.Min(100, Math.Max(0, 171 - (5.2 * Math.Log(volume)) - (0.23 * wmc) - (16.2 * Math.Log(loc))));

            var (cbo, ownCbo) = Coupling(declaration, model, type);
            classes.Add(new ClassMetrics(
                project, name, members.Count, loc, wmc, vgs.Count == 0 ? 0 : vgs.Max(),
                Depth(type),
                types.Count(t => SymbolEqualityComparer.Default.Equals(t.BaseType?.OriginalDefinition, type)),
                cbo,
                ownCbo,
                Response(declaration, model, type, members.Count),
                Cohesion(declaration, model, type),
                members.Count == 0 ? null : volume,
                mi));
        }
    }

    private static bool IsSource(string path)
    {
        var parts = path.Split('/', '\\');
        return !parts.Contains("bin", StringComparer.Ordinal) && !parts.Contains("obj", StringComparer.Ordinal);
    }

    private static bool IsManaged(string path)
    {
        try
        {
            System.Reflection.AssemblyName.GetAssemblyName(path);
            return true;
        }
        catch (BadImageFormatException)
        {
            return false;
        }
    }

    /// <summary>Descendientes del nodo, sin entrar en los tipos anidados, que se miden aparte.</summary>
    private static IEnumerable<SyntaxNode> Own(SyntaxNode node) =>
        node.DescendantNodes(child => child == node || child is not BaseTypeDeclarationSyntax);

    private static IEnumerable<SyntaxNode> MethodLike(TypeDeclarationSyntax declaration)
    {
        foreach (var member in declaration.Members)
        {
            switch (member)
            {
                case BaseMethodDeclarationSyntax { Body: not null } or BaseMethodDeclarationSyntax { ExpressionBody: not null }:
                    yield return member;
                    break;
                case PropertyDeclarationSyntax { ExpressionBody: not null }:
                case IndexerDeclarationSyntax { ExpressionBody: not null }:
                    yield return member;
                    break;
                case BasePropertyDeclarationSyntax { AccessorList: not null } property:
                    foreach (var accessor in property.AccessorList.Accessors.Where(a => a.Body is not null || a.ExpressionBody is not null))
                    {
                        yield return accessor;
                    }

                    break;
            }
        }
    }

    private static string MemberName(SyntaxNode member) => member switch
    {
        MethodDeclarationSyntax method => method.Identifier.Text,
        ConstructorDeclarationSyntax => ".ctor",
        OperatorDeclarationSyntax op => "operator " + op.OperatorToken.Text,
        ConversionOperatorDeclarationSyntax => "conversion",
        PropertyDeclarationSyntax property => property.Identifier.Text,
        AccessorDeclarationSyntax { Parent.Parent: PropertyDeclarationSyntax property } accessor => property.Identifier.Text + "." + accessor.Keyword.Text,
        _ => member.Kind().ToString(),
    };

    private static int Complexity(SyntaxNode member) => 1 + member.DescendantNodes().Count(IsDecision);

    private static bool IsDecision(SyntaxNode node) => node switch
    {
        IfStatementSyntax or WhileStatementSyntax or DoStatementSyntax or ForStatementSyntax or CommonForEachStatementSyntax => true,
        CatchClauseSyntax or CaseSwitchLabelSyntax or CasePatternSwitchLabelSyntax => true,
        ConditionalExpressionSyntax or ConditionalAccessExpressionSyntax => true,
        SwitchExpressionArmSyntax arm => arm.Pattern is not DiscardPatternSyntax,
        BinaryExpressionSyntax binary => binary.Kind() is SyntaxKind.LogicalAndExpression or SyntaxKind.LogicalOrExpression or SyntaxKind.CoalesceExpression,
        AssignmentExpressionSyntax assignment => assignment.IsKind(SyntaxKind.CoalesceAssignmentExpression),
        BinaryPatternSyntax => true,
        _ => false,
    };

    private static (double Volume, int Loc) HalsteadAndLoc(List<SyntaxNode> members)
    {
        var operators = new HashSet<string>(StringComparer.Ordinal);
        var operands = new HashSet<string>(StringComparer.Ordinal);
        var total = 0;
        var lines = new HashSet<(string, int)>();
        foreach (var token in members.SelectMany(m => m.DescendantTokens()))
        {
            total++;
            (token.IsKind(SyntaxKind.IdentifierToken) || token.Value is not null && !token.IsKeyword() ? operands : operators).Add(token.Text);
            var span = token.GetLocation().GetLineSpan();
            for (var line = span.StartLinePosition.Line; line <= span.EndLinePosition.Line; line++)
            {
                lines.Add((span.Path, line));
            }
        }

        var vocabulary = operators.Count + operands.Count;
        return (vocabulary < 2 ? 0 : total * Math.Log2(vocabulary), lines.Count);
    }

    private static int Depth(INamedTypeSymbol type)
    {
        if (type.IsValueType)
        {
            return 0;
        }

        var depth = 0;
        for (var current = type.BaseType; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            depth++;
        }

        return depth;
    }

    private static (int All, int Own) Coupling(TypeDeclarationSyntax declaration, SemanticModel model, INamedTypeSymbol type)
    {
        var coupled = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (var node in Own(declaration))
        {
            ISymbol? symbol = node switch
            {
                TypeSyntax or InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax or AttributeSyntax or MemberAccessExpressionSyntax => model.GetSymbolInfo(node).Symbol,
                _ => null,
            };
            var target = symbol switch
            {
                INamedTypeSymbol named => named,
                IMethodSymbol method => method.ContainingType,
                IPropertySymbol property => property.ContainingType,
                IFieldSymbol field => field.ContainingType,
                _ => null,
            };
            if (target is not null && Counts(target.OriginalDefinition, type))
            {
                coupled.Add(target.OriginalDefinition);
            }
        }

        return (coupled.Count, coupled.Count(symbol => symbol.ContainingNamespace.ToDisplayString().StartsWith("BudTack", StringComparison.Ordinal)));
    }

    private static bool Counts(INamedTypeSymbol target, INamedTypeSymbol self)
    {
        if (target.SpecialType != SpecialType.None || target.IsAnonymousType || target.TypeKind is TypeKind.Error or TypeKind.TypeParameter)
        {
            return false;
        }

        for (var current = target; current is not null; current = current.ContainingType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, self))
            {
                return false;
            }
        }

        var space = target.ContainingNamespace.ToDisplayString();
        return space != "System" && !space.StartsWith("System.", StringComparison.Ordinal);
    }

    private static int Response(TypeDeclarationSyntax declaration, SemanticModel model, INamedTypeSymbol type, int ownMethods)
    {
        var called = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (var node in Own(declaration).Where(n => n is InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax))
        {
            if (model.GetSymbolInfo(node).Symbol is IMethodSymbol method && !SymbolEqualityComparer.Default.Equals(method.ContainingType.OriginalDefinition, type))
            {
                called.Add(method.OriginalDefinition);
            }
        }

        return ownMethods + called.Count;
    }

    /// <summary>LCOM* de Henderson-Sellers. Sin dos métodos de instancia y un atributo no se puede calcular.</summary>
    private static double? Cohesion(TypeDeclarationSyntax declaration, SemanticModel model, INamedTypeSymbol type)
    {
        var attributes = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (var member in type.GetMembers())
        {
            if (member is IFieldSymbol { IsStatic: false, IsConst: false, IsImplicitlyDeclared: false } || member is IPropertySymbol { IsStatic: false } property && IsAuto(property))
            {
                attributes.Add(member);
            }
        }

        if (declaration is ClassDeclarationSyntax { ParameterList: not null } or StructDeclarationSyntax { ParameterList: not null })
        {
            foreach (var parameter in declaration.ParameterList!.Parameters)
            {
                attributes.Add(model.GetDeclaredSymbol(parameter)!);
            }
        }

        var instanceMethods = declaration.Members
            .Where(m => m is MethodDeclarationSyntax && !m.Modifiers.Any(SyntaxKind.StaticKeyword))
            .ToList();
        if (instanceMethods.Count < 2 || attributes.Count == 0)
        {
            return null;
        }

        var uses = attributes.ToDictionary(a => a, _ => 0, (IEqualityComparer<ISymbol>)SymbolEqualityComparer.Default);
        foreach (var method in instanceMethods)
        {
            var seen = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            foreach (var identifier in method.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                var symbol = model.GetSymbolInfo(identifier).Symbol?.OriginalDefinition;
                if (symbol is not null && uses.ContainsKey(symbol) && seen.Add(symbol))
                {
                    uses[symbol]++;
                }
            }
        }

        var m = (double)instanceMethods.Count;
        var lcom = (uses.Values.Average() - m) / (1 - m);
        return lcom == 0 ? 0 : lcom;
    }

    private static bool IsAuto(IPropertySymbol property) =>
        property.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).OfType<PropertyDeclarationSyntax>()
            .Any(p => p.ExpressionBody is null && p.AccessorList is not null && p.AccessorList.Accessors.All(a => a.Body is null && a.ExpressionBody is null));
}
