using AspectInjector.Broker;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace AspectInjector.Analyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ArgumentAttributeAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(
                Rules.ArgumentMustBePartOfAdvice
                , Rules.ArgumentIsAlwaysNull
                , Rules.ArgumentHasInvalidType
                );

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
        }

        private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
        {
            var attr = context.ContainingSymbol.GetAttributes().FirstOrDefault(a => a.ApplicationSyntaxReference.Span == context.Node.Span);

            if (attr == null || attr.AttributeClass.ToDisplayString() != WellKnown.AdviceArgumentType)
                return;

            var param = context.ContainingSymbol as IParameterSymbol;
            if (param == null)
                return;

            var location = context.Node.GetLocation();

            var adviceattr = param.ContainingSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass.ToDisplayString() == WellKnown.AdviceType);

            if (adviceattr == null)
                context.ReportDiagnostic(Diagnostic.Create(Rules.ArgumentMustBePartOfAdvice, location, param.ContainingSymbol.Name));

            if (attr.AttributeConstructor == null)
                return;

            var source = (Advice.Argument.Source)attr.ConstructorArguments[0].Value;


            if (source == Advice.Argument.Source.Arguments && param.Type.ToDisplayString() != "object[]")
                context.ReportDiagnostic(Diagnostic.Create(Rules.ArgumentHasInvalidType, location, param.Name, $"object[]"));

            if (source == Advice.Argument.Source.Instance && param.Type.SpecialType != SpecialType.System_Object)
                context.ReportDiagnostic(Diagnostic.Create(Rules.ArgumentHasInvalidType, location, param.Name, $"object"));

            if (source == Advice.Argument.Source.Method && param.Type.ToDisplayString() != WellKnown.MethodBase)
                context.ReportDiagnostic(Diagnostic.Create(Rules.ArgumentHasInvalidType, location, param.Name, WellKnown.MethodBase));

            if (source == Advice.Argument.Source.Name && param.Type.SpecialType != SpecialType.System_String)
                context.ReportDiagnostic(Diagnostic.Create(Rules.ArgumentHasInvalidType, location, param.Name, "string"));

            if (source == Advice.Argument.Source.ReturnType && param.Type.ToDisplayString() != WellKnown.Type)
                context.ReportDiagnostic(Diagnostic.Create(Rules.ArgumentHasInvalidType, location, param.Name, WellKnown.Type));

            if (source == Advice.Argument.Source.ReturnValue && param.Type.SpecialType != SpecialType.System_Object)
                context.ReportDiagnostic(Diagnostic.Create(Rules.ArgumentHasInvalidType, location, param.Name, "object"));

            if (source == Advice.Argument.Source.Target && param.Type.ToDisplayString() != "System.Func<object[],object>")
                context.ReportDiagnostic(Diagnostic.Create(Rules.ArgumentHasInvalidType, location, param.Name, "System.Func<object[],object>"));

            if (source == Advice.Argument.Source.Type && param.Type.ToDisplayString() != WellKnown.Type)
                context.ReportDiagnostic(Diagnostic.Create(Rules.ArgumentHasInvalidType, location, param.Name, WellKnown.Type));


            if (adviceattr.AttributeConstructor == null)
                return;

            var adviceType = (Advice.Type)adviceattr.ConstructorArguments[0].Value;

            if (source == Advice.Argument.Source.Target && adviceType != Advice.Type.Around)
                context.ReportDiagnostic(Diagnostic.Create(Rules.ArgumentIsAlwaysNull, location, param.Name, $"for '{adviceType}' advice"));

            if (source == Advice.Argument.Source.ReturnValue && adviceType != Advice.Type.After)
                context.ReportDiagnostic(Diagnostic.Create(Rules.ArgumentIsAlwaysNull, location, param.Name, $"for '{adviceType}' advice"));
        }
    }
}
