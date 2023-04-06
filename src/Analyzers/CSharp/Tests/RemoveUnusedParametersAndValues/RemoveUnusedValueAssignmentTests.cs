﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedParametersAndValues;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;
using static Roslyn.Test.Utilities.TestHelpers;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnusedParametersAndValues
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpRemoveUnusedParametersAndValuesDiagnosticAnalyzer,
        CSharpRemoveUnusedValuesCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
    public class RemoveUnusedValueAssignmentTests : RemoveUnusedValuesTestsBase
    {
        public RemoveUnusedValueAssignmentTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        private protected override OptionsCollection PreferNone
            => Option(CSharpCodeStyleOptions.UnusedValueAssignment,
                   new CodeStyleOption2<UnusedValuePreference>(UnusedValuePreference.DiscardVariable, NotificationOption2.None));

        private protected override OptionsCollection PreferDiscard
            => Option(CSharpCodeStyleOptions.UnusedValueAssignment,
                   new CodeStyleOption2<UnusedValuePreference>(UnusedValuePreference.DiscardVariable, NotificationOption2.Suggestion));

        private protected override OptionsCollection PreferUnusedLocal
            => Option(CSharpCodeStyleOptions.UnusedValueAssignment,
                   new CodeStyleOption2<UnusedValuePreference>(UnusedValuePreference.UnusedLocalVariable, NotificationOption2.Suggestion));

        [Theory, CombinatorialData]
        public void TestStandardProperty(AnalyzerProperty property)
            => VerifyCS.VerifyStandardProperty(property);

        [Fact]
        public async Task Initialization_Suppressed()
        {
            var source =
@"class C
{
    int M()
    {
        int x = 1;
        x = 2;
        return x;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, UnusedValuePreference.DiscardVariable, NotificationOption2.None },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task Assignment_Suppressed()
        {
            var source =
@"class C
{
    int M()
    {
        int x;
        x = 1;
        x = 2;
        return x;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, UnusedValuePreference.DiscardVariable, NotificationOption2.None },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData(UnusedValuePreference.DiscardVariable)]
        [InlineData(UnusedValuePreference.UnusedLocalVariable)]
        public async Task Initialization_ConstantValue(object option)
        {
            var source =
@"class C
{
    int M()
    {
        int {|IDE0059:x|} = 1;
        x = 2;
        return x;
    }
}";
            var fixedSource =
@"class C
{
    int M()
    {
        int x = 2;
        return x;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/48070")]
        [InlineData(UnusedValuePreference.DiscardVariable)]
        [InlineData(UnusedValuePreference.UnusedLocalVariable)]
        public async Task Initialization_ConstantValue_DoNotCopyLeadingTriviaDirectives(object option)
        {
            var source =
@"class C {
    void M()
    {
        #region
        int value = 3;
        #endregion

        int? {|IDE0059:x|} = null;
        int y = value + value;
        
        x = y;
        System.Console.WriteLine(x);
    }
}";
            var fixedSource =
@"class C {
    void M()
    {
        #region
        int value = 3;

        #endregion
        int y = value + value;

        int? x = y;
        System.Console.WriteLine(x);
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task Initialization_ConstantValue_RemoveUnusedParametersSuppressed()
        {
            var source =
@"class C
{
    int M()
    {
        int {|IDE0059:x|} = 1;
        x = 2;
        return x;
    }
}";
            var fixedSource =
@"class C
{
    int M()
    {
        int x = 2;
        return x;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CodeStyleOptions2.UnusedParameters, UnusedParametersPreference.NonPublicMethods, NotificationOption2.None },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task Initialization_ConstantValue_RemoveUnusedParametersNotApplicable()
        {
            var source =
@"class C
{
    public int M(int {|IDE0060:z|})
    {
        int {|IDE0059:x|} = 1;
        x = 2;
        return x;
    }
}";
            var fixedSource =
@"class C
{
    public int M(int {|IDE0060:z|})
    {
        int x = 2;
        return x;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CodeStyleOptions2.UnusedParameters, UnusedParametersPreference.NonPublicMethods, NotificationOption2.Silent },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData(UnusedValuePreference.DiscardVariable)]
        [InlineData(UnusedValuePreference.UnusedLocalVariable)]
        public async Task Assignment_ConstantValue(object option)
        {
            var source =
@"class C
{
    int M()
    {
        int x;
        {|IDE0059:x|} = 1;
        x = 2;
        return x;
    }
}";
            var fixedSource =
@"class C
{
    int M()
    {
        int x;
        x = 2;
        return x;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData(UnusedValuePreference.DiscardVariable)]
        [InlineData(UnusedValuePreference.UnusedLocalVariable)]
        public async Task Assignment_ConstantValue_NoReads(object option)
        {
            var source =
@"class C
{
    void M()
    {
        int x;
        {|IDE0059:x|} = 1;
    }
}";
            var fixedSource =
@"class C
{
    void M()
    {
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task Assignment_NonConstantValue_NoReads_PreferDiscard()
        {
            var source =
@"class C
{
    void M()
    {
        int x;
        {|IDE0059:x|} = M2();
    }

    int M2() => 0;
}";
            var fixedSource =
@"class C
{
    void M()
    {
        _ = M2();
    }

    int M2() => 0;
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, UnusedValuePreference.DiscardVariable },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task Assignment_NonConstantValue_NoReads_PreferUnusedLocal()
        {
            var source =
@"class C
{
    void M()
    {
        int x;
        x = M2();
    }

    int M2() => 0;
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, UnusedValuePreference.UnusedLocalVariable },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData(UnusedValuePreference.DiscardVariable)]
        [InlineData(UnusedValuePreference.UnusedLocalVariable)]
        public async Task Initialization_NonConstantValue_ParameterReference(object option)
        {
            var source =
@"class C
{
    int M(int p)
    {
        int {|IDE0059:x|} = p;
        x = 2;
        return x;
    }
}";
            var fixedSource =
@"class C
{
    int M(int {|IDE0060:p|})
    {
        int x = 2;
        return x;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData(UnusedValuePreference.DiscardVariable)]
        [InlineData(UnusedValuePreference.UnusedLocalVariable)]
        public async Task Assignment_NonConstantValue_ParameterReference(object option)
        {
            var source =
@"class C
{
    int M(int p)
    {
        int x;
        {|IDE0059:x|} = p;
        x = 2;
        return x;
    }
}";
            var fixedSource =
@"class C
{
    int M(int {|IDE0060:p|})
    {
        int x;
        x = 2;
        return x;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        public async Task Initialization_NonConstantValue_LocalReference(
            [CombinatorialValues(UnusedValuePreference.DiscardVariable, UnusedValuePreference.UnusedLocalVariable)] object option,
            [CombinatorialValues(CodeFixTestBehaviors.None, CodeFixTestBehaviors.FixOne)] CodeFixTestBehaviors testBehaviors)
        {
            var source =
@"class C
{
    int M()
    {
        int local = 0;
        int {|IDE0059:x|} = local;
        x = 2;
        return x;
    }
}";
            var (fixedSource, iterations) = testBehaviors switch
            {
                CodeFixTestBehaviors.None =>
(@"class C
{
    int M()
    {
        int x = 2;
        return x;
    }
}", iterations: 2),
                CodeFixTestBehaviors.FixOne =>
(@"class C
{
    int M()
    {
        int {|IDE0059:local|} = 0;
        int x = 2;
        return x;
    }
}", iterations: 1),
                _ => throw ExceptionUtilities.Unreachable(),
            };

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedState = { Sources = { fixedSource }, MarkupHandling = MarkupMode.Allow },
                CodeFixTestBehaviors = testBehaviors,
                NumberOfIncrementalIterations = iterations,
                NumberOfFixAllIterations = iterations,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        public async Task Assignment_NonConstantValue_LocalReference(
            [CombinatorialValues(UnusedValuePreference.DiscardVariable, UnusedValuePreference.UnusedLocalVariable)] object option,
            [CombinatorialValues(CodeFixTestBehaviors.None, CodeFixTestBehaviors.FixOne)] CodeFixTestBehaviors testBehaviors)
        {
            var source =
@"class C
{
    int M()
    {
        int local = 0;
        int x;
        {|IDE0059:x|} = local;
        x = 2;
        return x;
    }
}";
            var (fixedSource, iterations) = testBehaviors switch
            {
                CodeFixTestBehaviors.None =>
(@"class C
{
    int M()
    {
        int x;
        x = 2;
        return x;
    }
}", iterations: 2),
                CodeFixTestBehaviors.FixOne =>
(@"class C
{
    int M()
    {
        int {|IDE0059:local|} = 0;
        int x;
        x = 2;
        return x;
    }
}", iterations: 1),
                _ => throw ExceptionUtilities.Unreachable(),
            };

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedState = { Sources = { fixedSource }, MarkupHandling = MarkupMode.Allow },
                CodeFixTestBehaviors = testBehaviors,
                NumberOfIncrementalIterations = iterations,
                NumberOfFixAllIterations = iterations,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData(UnusedValuePreference.DiscardVariable)]
        [InlineData(UnusedValuePreference.UnusedLocalVariable)]
        public async Task Initialization_NonConstantValue_DefaultExpression(object option)
        {
            var source =
@"struct C
{
    C M()
    {
        C {|IDE0059:c|} = default(C);
        c = new C();
        return c;
    }
}";
            var fixedSource =
@"struct C
{
    C M()
    {
        C c = new C();
        return c;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData(UnusedValuePreference.DiscardVariable)]
        [InlineData(UnusedValuePreference.UnusedLocalVariable)]
        public async Task Initialization_NonConstantValue_CastExpression(object option)
        {
            var source =
@"struct C
{
    C M(object obj)
    {
        C {|IDE0059:c|} = (C)obj;
        c = new C();
        return c;
    }
}";
            var fixedSource =
@"struct C
{
    C M(object {|IDE0060:obj|})
    {
        C c = new C();
        return c;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData(UnusedValuePreference.DiscardVariable)]
        [InlineData(UnusedValuePreference.UnusedLocalVariable)]
        public async Task Initialization_NonConstantValue_FieldReferenceWithThisReceiver(object option)
        {
            var source =
@"class C
{
    private int field;
    int M()
    {
        int {|IDE0059:x|} = field;
        x = 2;
        return x;
    }
}";
            var fixedSource =
@"class C
{
    private int field;
    int M()
    {
        int x = 2;
        return x;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData(UnusedValuePreference.DiscardVariable)]
        [InlineData(UnusedValuePreference.UnusedLocalVariable)]
        public async Task Assignment_NonConstantValue_FieldReferenceWithNullReceiver(object option)
        {
            var source =
@"class C
{
    private static int field;
    int M()
    {
        int x;
        {|IDE0059:x|} = field;
        x = 2;
        return x;
    }
}";
            var fixedSource =
@"class C
{
    private static int field;
    int M()
    {
        int x;
        x = 2;
        return x;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData(UnusedValuePreference.DiscardVariable, "_")]
        [InlineData(UnusedValuePreference.UnusedLocalVariable, "int unused")]
        public async Task Assignment_NonConstantValue_FieldReferenceWithReceiver(object option, string fix)
        {
            var source =
@"class C
{
    private int field;
    int M(C c)
    {
        int x;
        {|IDE0059:x|} = c.field;
        x = 2;
        return x;
    }
}";
            var fixedSource =
$@"class C
{{
    private int field;
    int M(C c)
    {{
        int x;
        {fix} = c.field;
        x = 2;
        return x;
    }}
}}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData(UnusedValuePreference.DiscardVariable, "_")]
        [InlineData(UnusedValuePreference.UnusedLocalVariable, "int unused")]
        public async Task Initialization_NonConstantValue_PropertyReference(object option, string fix)
        {
            var source =
@"class C
{
    private int Property { get { throw new System.Exception(); } }
    int M()
    {
        int x;
        {|IDE0059:x|} = Property;
        x = 2;
        return x;
    }
}";
            var fixedSource =
$@"class C
{{
    private int Property {{ get {{ throw new System.Exception(); }} }}
    int M()
    {{
        int x;
        {fix} = Property;
        x = 2;
        return x;
    }}
}}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData(UnusedValuePreference.DiscardVariable, "_")]
        [InlineData(UnusedValuePreference.UnusedLocalVariable, "int unused")]
        public async Task Initialization_NonConstantValue_MethodInvocation(object option, string fix)
        {
            var source =
@"class C
{
    int M()
    {
        int {|IDE0059:x|} = M2();
        x = 2;
        return x;
    }

    int M2() => 0;
}";
            var fixedSource =
$@"class C
{{
    int M()
    {{
        {fix} = M2();
        int x = 2;
        return x;
    }}

    int M2() => 0;
}}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task Initialization_NonConstantValue_PreferDiscard_CSharp6()
        {
            // Discard not supported in C# 6.0, so we fallback to unused local variable.
            var source =
@"class C
{
    int M()
    {
        int {|IDE0059:x|} = M2();
        x = 2;
        return x;
    }

    int M2() => 0;
}";
            var fixedSource =
@"class C
{
    int M()
    {
        int unused = M2();
        int x = 2;
        return x;
    }

    int M2() => 0;
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, UnusedValuePreference.DiscardVariable },
                },
                LanguageVersion = LanguageVersion.CSharp6,
            }.RunAsync();
        }

        [Theory]
        [InlineData(UnusedValuePreference.DiscardVariable, "_")]
        [InlineData(UnusedValuePreference.UnusedLocalVariable, "int unused")]
        public async Task Assignment_NonConstantValue_MethodInvocation(object option, string fix)
        {
            var source =
@"class C
{
    int M()
    {
        int x;
        {|IDE0059:x|} = M2();
        x = 2;
        return x;
    }

    int M2() => 0;
}";
            var fixedSource =
$@"class C
{{
    int M()
    {{
        int x;
        {fix} = M2();
        x = 2;
        return x;
    }}

    int M2() => 0;
}}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData(UnusedValuePreference.DiscardVariable)]
        [InlineData(UnusedValuePreference.UnusedLocalVariable)]
        public async Task Assignment_NonConstantValue_ImplicitConversion(object option)
        {
            var source =
@"class C
{
    int M(int {|IDE0060:x|}, short s)
    {
        {|IDE0059:x|} = s;
        x = 2;
        return x;
    }
}";
            var fixedSource =
@"class C
{
    int M(int {|IDE0060:x|}, short {|IDE0060:s|})
    {
        x = 2;
        return x;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData(UnusedValuePreference.DiscardVariable, "_")]
        [InlineData(UnusedValuePreference.UnusedLocalVariable, "int unused")]
        public async Task Assignment_NonConstantValue_UserDefinedConversion(object option, string fix)
        {
            var source =
@"class C
{
    int M(int {|IDE0060:x|}, C c)
    {
        {|IDE0059:x|} = (int)c;
        x = 2;
        return x;
    }

    public static explicit operator int(C {|IDE0060:c|})
    {
        return 0;
    }

    public static explicit operator C(int {|IDE0060:i|})
    {
        return default(C);
    }
}";
            var fixedSource =
$@"class C
{{
    int M(int {{|IDE0060:x|}}, C c)
    {{
        {fix} = (int)c;
        x = 2;
        return x;
    }}

    public static explicit operator int(C {{|IDE0060:c|}})
    {{
        return 0;
    }}

    public static explicit operator C(int {{|IDE0060:i|}})
    {{
        return default(C);
    }}
}}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        public async Task NestedAssignment_ConstantValue(
            [CombinatorialValues(UnusedValuePreference.DiscardVariable, UnusedValuePreference.UnusedLocalVariable)] object option,
            [CombinatorialValues(CodeFixTestBehaviors.None, CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck)] CodeFixTestBehaviors testBehaviors)
        {
            var source =
@"class C
{
    int M(int {|IDE0060:x|}, int {|IDE0060:y|})
    {
        {|IDE0059:y|} = {|IDE0059:x|} = 1;
        x = 2;
        return x;
    }
}";

            var (fixedSource, iterations) = ((UnusedValuePreference)option, testBehaviors) switch
            {
                (UnusedValuePreference.DiscardVariable, CodeFixTestBehaviors.None) =>
(@"class C
{
    int M(int {|IDE0060:x|}, int {|IDE0060:y|})
    {
        _ = 1;
        x = 2;
        return x;
    }
}", iterations: 2),
                (UnusedValuePreference.UnusedLocalVariable, CodeFixTestBehaviors.None) =>
(@"class C
{
    int M(int {|IDE0060:x|}, int {|IDE0060:y|})
    {
        x = 2;
        return x;
    }
}", iterations: 3),
                (_, CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck) =>
(@"class C
{
    int M(int {|IDE0060:x|}, int {|IDE0060:y|})
    {
        {|IDE0059:y|} = 1;
        x = 2;
        return x;
    }
}", iterations: 1),
                _ => throw ExceptionUtilities.Unreachable(),
            };

            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedState = { Sources = { fixedSource }, MarkupHandling = MarkupMode.Allow },
                CodeFixTestBehaviors = testBehaviors,
                NumberOfIncrementalIterations = iterations,
                NumberOfFixAllIterations = iterations,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            };

            if (testBehaviors.HasFlag(CodeFixTestBehaviors.FixOne))
            {
                test.DiagnosticSelector = diagnostics => diagnostics[1];
            }

            await test.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        public async Task NestedAssignment_NonConstantValue(
            [CombinatorialValues(UnusedValuePreference.DiscardVariable, UnusedValuePreference.UnusedLocalVariable)] object option,
            [CombinatorialValues(CodeFixTestBehaviors.None, CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck)] CodeFixTestBehaviors testBehaviors)
        {
            var source =
@"class C
{
    int M(int {|IDE0060:x|}, int {|IDE0060:y|})
    {
        {|IDE0059:y|} = {|IDE0059:x|} = M2();
        x = 2;
        return x;
    }

    int M2() => 0;
}";

            var fixedSource = ((UnusedValuePreference)option, testBehaviors) switch
            {
                (UnusedValuePreference.DiscardVariable, CodeFixTestBehaviors.None) =>
@"class C
{
    int M(int {|IDE0060:x|}, int {|IDE0060:y|})
    {
        _ = _ = M2();
        x = 2;
        return x;
    }

    int M2() => 0;
}",
                (UnusedValuePreference.DiscardVariable, CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck) =>
@"class C
{
    int M(int {|IDE0060:x|}, int {|IDE0060:y|})
    {
        {|IDE0059:y|} = _ = M2();
        x = 2;
        return x;
    }

    int M2() => 0;
}",
                (UnusedValuePreference.UnusedLocalVariable, CodeFixTestBehaviors.None) =>
@"class C
{
    int M(int {|IDE0060:x|}, int {|IDE0060:y|})
    {
        int unused1;
        int unused = unused1 = M2();
        x = 2;
        return x;
    }

    int M2() => 0;
}",
                (UnusedValuePreference.UnusedLocalVariable, CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck) =>
@"class C
{
    int M(int {|IDE0060:x|}, int {|IDE0060:y|})
    {
        int unused;
        {|IDE0059:y|} = unused = M2();
        x = 2;
        return x;
    }

    int M2() => 0;
}",
                _ => throw ExceptionUtilities.Unreachable(),
            };

            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedState = { Sources = { fixedSource }, MarkupHandling = MarkupMode.Allow },
                CodeFixTestBehaviors = testBehaviors,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            };

            if (testBehaviors.HasFlag(CodeFixTestBehaviors.FixOne))
            {
                test.DiagnosticSelector = diagnostics => diagnostics[1];
            }

            await test.RunAsync();
        }

        [Theory]
        [InlineData(UnusedValuePreference.DiscardVariable)]
        [InlineData(UnusedValuePreference.UnusedLocalVariable)]
        public async Task ReadAndWriteInSameExpression_MethodInvocation(object option)
        {
            var source =
@"class C
{
    int M()
    {
        int x = 1;
        x = M2(x);
        return x;
    }

    int M2(int x) => x;
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        public async Task IncrementOrDecrementOperator_ValueUsed_SameStatement(
            [CombinatorialValues("++", "--")] string @operator,
            bool applyAsPrefix,
            [CombinatorialValues(UnusedValuePreference.DiscardVariable, UnusedValuePreference.UnusedLocalVariable)] object option)
        {
            var (prefix, postfix) = applyAsPrefix ? (@operator, "") : ("", @operator);
            var source =
$@"class C
{{
    void M(int x)
    {{
        var {{|#0:y|}} = {prefix}x{postfix};
    }}
}}";

            var fixedSource = (UnusedValuePreference)option switch
            {
                UnusedValuePreference.UnusedLocalVariable => source,
                UnusedValuePreference.DiscardVariable =>
$@"class C
{{
    void M(int x)
    {{
        _ = {prefix}x{postfix};
    }}
}}",
                _ => throw ExceptionUtilities.Unreachable(),
            };

            var test = new VerifyCS.Test
            {
                TestState = { Sources = { source } },
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            };

            if ((UnusedValuePreference)option == UnusedValuePreference.DiscardVariable)
            {
                test.TestState.ExpectedDiagnostics.Add(
                    // /0/Test0.cs(5,13): info IDE0059: Unnecessary assignment of a value to 'y'
                    VerifyCS.Diagnostic("IDE0059").WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithArguments("y"));
            }

            await test.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        public async Task IncrementOrDecrementOperator_ValueUsed_LaterStatement(
            [CombinatorialValues("++", "--")] string @operator,
            bool applyAsPrefix,
            [CombinatorialValues(UnusedValuePreference.DiscardVariable, UnusedValuePreference.UnusedLocalVariable)] object option)
        {
            var (prefix, postfix) = applyAsPrefix ? (@operator, "") : ("", @operator);
            var source =
$@"class C
{{
    int M(int x)
    {{
        {prefix}x{postfix};
        return x;
    }}
}}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        public async Task IncrementOrDecrementOperator_ValueUnused(
            [CombinatorialValues("++", "--")] string @operator,
            bool applyAsPrefix,
            [CombinatorialValues(UnusedValuePreference.DiscardVariable, UnusedValuePreference.UnusedLocalVariable)] object option)
        {
            var (prefix, postfix) = applyAsPrefix ? (@operator, "") : ("", @operator);
            var source =
$@"class C
{{
    void M(int x)
    {{
        {prefix}{{|IDE0059:x|}}{postfix};
    }}
}}";
            var fixedSource =
@"class C
{
    void M(int {|IDE0060:x|})
    {
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        public async Task CompoundAssignmentOperator_ValueUsed_SameStatement(
            [CombinatorialValues("1" /*Constant*/, "M2()" /*Non-constant*/)] string rightHandSide,
            [CombinatorialValues(UnusedValuePreference.DiscardVariable, UnusedValuePreference.UnusedLocalVariable)] object option)
        {
            var source =
$@"class C
{{
    void M(int x)
    {{
        var {{|#0:y|}} = x += {rightHandSide};
    }}

    int M2() => 0;
}}";

            var fixedSource = (UnusedValuePreference)option switch
            {
                UnusedValuePreference.UnusedLocalVariable => source,
                UnusedValuePreference.DiscardVariable =>
$@"class C
{{
    void M(int x)
    {{
        _ = x += {rightHandSide};
    }}

    int M2() => 0;
}}",
                _ => throw ExceptionUtilities.Unreachable(),
            };

            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            };

            if ((UnusedValuePreference)option == UnusedValuePreference.DiscardVariable)
            {
                test.TestState.ExpectedDiagnostics.Add(
                    // /0/Test0.cs(5,13): info IDE0059: Unnecessary assignment of a value to 'y'
                    VerifyCS.Diagnostic("IDE0059").WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithArguments("y"));
            }

            await test.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        public async Task CompoundAssignmentOperator_ValueUsed_LaterStatement(
            [CombinatorialValues("1" /*Constant*/, "M2()" /*Non-constant*/)] string rightHandSide,
            [CombinatorialValues(UnusedValuePreference.DiscardVariable, UnusedValuePreference.UnusedLocalVariable)] object option)
        {
            var source =
$@"class C
{{
    int M(int x)
    {{
        x += {rightHandSide};
        return x;
    }}

    int M2() => 0;
}}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        public async Task CompoundLogicalOrOperator_ValueUsed_LaterStatement(
            [CombinatorialValues("true" /*Constant*/, "M2()" /*Non-constant*/)] string rightHandSide,
            [CombinatorialValues(UnusedValuePreference.DiscardVariable, UnusedValuePreference.UnusedLocalVariable)] object option)
        {
            var source =
$@"class C
{{
    bool M(bool x)
    {{
        x |= {rightHandSide} && {rightHandSide};
        return x;
    }}

    bool M2() => true;
}}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        public async Task CompoundLogicalOrOperator_ValueUsed_LaterStatement_02(
            [CombinatorialValues("true" /*Constant*/, "M2()" /*Non-constant*/)] string rightHandSide,
            [CombinatorialValues(UnusedValuePreference.DiscardVariable, UnusedValuePreference.UnusedLocalVariable)] object option)
        {
            var source =
$@"class C
{{
    bool M()
    {{
        bool x = false;
        x |= {rightHandSide} && {rightHandSide};
        return x;
    }}

    bool M2() => true;
}}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task CompoundAssignmentOperator_ValueNotUsed_ConstantValue(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(int x)
    {
        [|x|] += 1;
    }
}",
@"class C
{
    int M(int x)
    {
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "int unused")]
        public async Task CompoundAssignmentOperator_ValueNotUsed_NonConstantValue(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
$@"class C
{{
    int M(int x)
    {{
        [|x|] += M2();
    }}

    int M2() => 0;
}}",
$@"class C
{{
    int M(int x)
    {{
        {fix} = M2();
    }}

    int M2() => 0;
}}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NullCoalescing_ReadWrite(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    C M()
    {
        C [|x|] = M2();
        x = x ?? new C();
        return x;
    }

    C M2() => null;
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task LValueFlowCapture_Assignment_ControlFlowInAssignedTarget(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    C M(C y)
    {
        C [|x|] = M2();
        (x ?? y) = y;
        return x;
    }

    C M2() => null;
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "var unused")]
        public async Task LValueFlowCapture_Assignment_ControlFlowInAssignedValue_01(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    C M(C y, C z)
    {
        var [|x|] = M2();
        x = y ?? z;
        return x;
    }

    C M2() => null;
}",
$@"class C
{{
    C M(C y, C z)
    {{
        {fix} = M2();
        C x = y ?? z;
        return x;
    }}

    C M2() => null;
}}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task LValueFlowCapture_Assignment_ControlFlowInAssignedValue_02(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    C M(C y, C z)
    {
        C [|x|] = M2();
        x = y ?? (x ?? z);
        return x;
    }

    C M2() => null;
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task LValueFlowCapture_DeconstructionAssignment_ControlFlowInAssignedTarget(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    C M(C y)
    {
        C [|x|] = M2();
        ((x ?? y), _) = (y, y);
        return x;
    }

    C M2() => null;
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "var unused")]
        public async Task LValueFlowCapture_DeconstructionAssignment_ControlFlowInAssignedValue_01(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    C M(C y, C z)
    {
        var [|x|] = M2();
        (x, y) = (y ?? z, z);
        return x;
    }

    C M2() => null;
}",
$@"class C
{{
    C M(C y, C z)
    {{
        {fix} = M2();
        C x;
        (x, y) = (y ?? z, z);
        return x;
    }}

    C M2() => null;
}}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task LValueFlowCapture_DeconstructionAssignment_ControlFlowInAssignedValue_02(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    C M(C y, C z)
    {
        C [|x|] = M2();
        (x, y) = (y ?? x, z);
        return x;
    }

    C M2() => null;
}", optionName);
        }

        [Fact]
        public async Task Initialization_NonConstantValue_NoReferences_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int [|x|] = M2();
    }

    int M2() => 0;
}",
@"class C
{
    void M()
    {
        _ = M2();
    }

    int M2() => 0;
}", options: PreferDiscard);
        }

        [Fact]
        public async Task Initialization_NonConstantValue_NoReferences_PreferUnusedLocal()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int [|x|] = M2();
    }

    int M2() => 0;
}", new TestParameters(options: PreferUnusedLocal));
        }

        [Fact]
        public async Task Initialization_NonConstantValue_NoReadReferences_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int [|x|] = M2();
        x = 0;
    }

    int M2() => 0;
}",
@"class C
{
    void M()
    {
        _ = M2();
        int x = 0;
    }

    int M2() => 0;
}", options: PreferDiscard);
        }

        [Fact]
        public async Task Initialization_NonConstantValue_NoReadReferences_PreferUnusedLocal()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int [|x|] = M2();
        x = 0;
    }

    int M2() => 0;
}", new TestParameters(options: PreferUnusedLocal));
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Initialization_ConstantValue_FirstField(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int [|x|] = 1, y = 2;
        x = 2;
        return x;
    }
}",
@"class C
{
    int M()
    {
        int y = 2;
        int x = 2;
        return x;
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Initialization_ConstantValue_MiddleField(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int i = 0, [|x|] = 1, y = 2;
        x = 2;
        return x;
    }
}",
@"class C
{
    int M()
    {
        int i = 0, y = 2;
        int x = 2;
        return x;
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Initialization_ConstantValue_LastField(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int i = 0, y = 2, [|x|] = 1;
        x = 2;
        return x;
    }
}",
@"class C
{
    int M()
    {
        int i = 0, y = 2;
        int x = 2;
        return x;
    }
}", optionName);
        }

        [Fact]
        public async Task Initialization_NonConstantValue_FirstField_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int [|x|] = M2(), y = 2;
        x = 2;
        return x;
    }

    void M2() => 0;
}",
@"class C
{
    int M()
    {
        _ = M2();
        int y = 2;
        int x = 2;
        return x;
    }

    void M2() => 0;
}", options: PreferDiscard);
        }

        [Fact]
        public async Task Initialization_NonConstantValue_FirstField_PreferUnusedLocal()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int [|x|] = M2(), y = 2;
        x = 2;
        return x;
    }

    void M2() => 0;
}",
@"class C
{
    int M()
    {
        int unused = M2(), y = 2;
        int x = 2;
        return x;
    }

    void M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Fact]
        public async Task Initialization_NonConstantValue_MiddleField_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int i = 0, [|x|] = M2(), y = 2;
        x = 2;
        return x;
    }

    void M2() => 0;
}",
@"class C
{
    int M()
    {
        int i = 0;
        _ = M2();
        int y = 2;
        int x = 2;
        return x;
    }

    void M2() => 0;
}", options: PreferDiscard);
        }

        [Fact]
        public async Task Initialization_NonConstantValue_MiddleField_PreferUnusedLocal()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int i = 0, [|x|] = M2(), y = 2;
        x = 2;
        return x;
    }

    void M2() => 0;
}",
@"class C
{
    int M()
    {
        int i = 0, unused = M2(), y = 2;
        int x = 2;
        return x;
    }

    void M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Fact]
        public async Task Initialization_NonConstantValue_LastField_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int i = 0, y = 2, [|x|] = M2();
        x = 2;
        return x;
    }

    void M2() => 0;
}",
@"class C
{
    int M()
    {
        int i = 0, y = 2;
        _ = M2();
        int x = 2;
        return x;
    }

    void M2() => 0;
}", options: PreferDiscard);
        }

        [Fact]
        public async Task Initialization_NonConstantValue_LastField_PreferUnusedLocal()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int i = 0, y = 2, [|x|] = M2();
        x = 2;
        return x;
    }

    void M2() => 0;
}",
@"class C
{
    int M()
    {
        int i = 0, y = 2, unused = M2();
        int x = 2;
        return x;
    }

    void M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Assignment_BeforeUseAsOutArgument(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int x;
        [|x|] = 1;
        M2(out x);
        return x;
    }

    void M2(out int x) => x = 0;
}",
@"class C
{
    int M()
    {
        int x;
        M2(out x);
        return x;
    }

    void M2(out int x) => x = 0;
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NonRedundantAssignment_BeforeUseAsRefArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int x;
        [|x|] = 1;
        M2(ref x);
        return x;
    }

    void M2(ref int x) => x = 0;
}", optionName);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/40717")]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NonRedundantAssignment_AfterUseAsRefArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    static int Example()
    {
        int value = 1;

        Span<int> valueSpan = M(ref value);

        [|value = 2;|]

        return valueSpan[0];
    }

    static Span<int> M(ref int value)
    {
        return default;
    }
}", optionName);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/40483")]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NonRedundantAssignment_AfterUseAsRefArgument_02(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class P
{
    public ref bool f(ref bool z, ref bool q)
    {
        z = ref q;
        return ref z;
    }
}

class Q
{
    static void F()
    {
        bool a = true;
        bool b = false;
        ref var r = ref new P().f(ref a, ref b);
        [|b = true|];

        Console.WriteLine(r);
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NonRedundantAssignment_BeforeUseAsInArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int x;
        [|x|] = 1;
        M2(in x);
        return x;
    }

    void M2(in int x) { }
}", optionName);
        }

        [Fact]
        public async Task OutArgument_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int x;
        M2(out [|x|]);
        x = 1;
        return x;
    }

    void M2(out int x) => x = 0;
}",
@"class C
{
    int M()
    {
        int x;
        M2(out _);
        x = 1;
        return x;
    }

    void M2(out int x) => x = 0;
}", options: PreferDiscard);
        }

        [Fact]
        public async Task OutArgument_PreferUnusedLocal()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int x;
        M2(out [|x|]);
        x = 1;
        return x;
    }

    void M2(out int x) => x = 0;
}",
@"class C
{
    int M()
    {
        int x;
        int unused;
        M2(out unused);
        x = 1;
        return x;
    }

    void M2(out int x) => x = 0;
}", options: PreferUnusedLocal);
        }

        [Fact]
        public async Task OutVarArgument_ExpressionBody_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M() => M2(out var [|x|]);
    void M2(out int x) => x = 0;
}",
@"class C
{
    void M() => M2(out _);
    void M2(out int x) => x = 0;
}", options: PreferDiscard);
        }

        [Fact]
        public async Task OutArgument_NoReads_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int x;
        M2(out [|x|]);

        // Unrelated, unused local should not be removed.
        int unused;
    }

    void M2(out int x) => x = 0;
}",
@"class C
{
    void M()
    {
        M2(out _);

        // Unrelated, unused local should not be removed.
        int unused;
    }

    void M2(out int x) => x = 0;
}", options: PreferDiscard);
        }

        [Fact]
        public async Task OutArgument_NoReads_PreferUnusedLocal()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int x;
        M2(out [|x|]);
    }

    void M2(out int x) => x = 0;
}", options: PreferUnusedLocal);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "var unused")]
        public async Task OutDeclarationExpressionArgument(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        M2(out var [|x|]);
        x = 1;
        return x;
    }

    void M2(out int x) => x = 0;
}",
$@"class C
{{
    int M()
    {{
        M2(out {fix});
        int x = 1;
        return x;
    }}

    void M2(out int x) => x = 0;
}}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NonRedundantRefArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int M(int x)
    {
        M2(ref [|x|]);
        x = 1;
        return x;
    }

    void M2(ref int x) => x = 0;
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NonRedundantInArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int M(int x)
    {
        M2(in [|x|]);
        x = 1;
        return x;
    }

    void M2(in int x) { }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "unused")]
        public async Task DeconstructionDeclarationExpression(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        var ([|x|], y) = (1, 1);
        x = 1;
        return x;
    }
}",
$@"class C
{{
    int M()
    {{
        var ({fix}, y) = (1, 1);
        int x = 1;
        return x;
    }}
}}", optionName);
        }

        [Fact]
        public async Task DeconstructionAssignment_01_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int x, y;
        ([|x|], y) = (1, 1);
        x = 1;
        return x;
    }
}",
@"class C
{
    int M()
    {
        int x, y;
        (_, y) = (1, 1);
        x = 1;
        return x;
    }
}", options: PreferDiscard);
        }

        [Fact]
        public async Task DeconstructionAssignment_01_PreferUnusedLocal()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int x, y;
        ([|x|], y) = (1, 1);
        x = 1;
        return x;
    }
}",
@"class C
{
    int M()
    {
        int x, y;
        int unused;
        (unused, y) = (1, 1);
        x = 1;
        return x;
    }
}", options: PreferUnusedLocal);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task DeconstructionAssignment_02(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int [|x|] = 0, y = 0;
        (x, y) = (x, y);
        return x;
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "var unused")]
        public async Task TupleExpressionWithDeclarationExpressions(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        (var [|x|], var y) = (1, 1);
        x = 1;
        return x;
    }
}",
$@"class C
{{
    int M()
    {{
        ({fix}, var y) = (1, 1);
        int x = 1;
        return x;
    }}
}}", optionName);
        }

        [Fact]
        public async Task DeclarationPatternInSwitchCase_WithOnlyWriteReference_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object p)
    {
        switch (p)
        {
            case int [|x|]:
                x = 1;
                break;
        };
    }
}",
@"class C
{
    void M(object p)
    {
        switch (p)
        {
            case int _:
                int x = 1;
                break;
        };
    }
}", options: PreferDiscard, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8));
        }

        [Fact]
        public async Task DeclarationPatternInSwitchCase_WithOnlyWriteReference_PreferDiscard_CSharp9()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object p)
    {
        switch (p)
        {
            case int [|x|]:
                x = 1;
                break;
        };
    }
}",
@"class C
{
    void M(object p)
    {
        switch (p)
        {
            case int:
                int x = 1;
                break;
        };
    }
}", options: PreferDiscard, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp9));
        }

        [Theory]
        [CombinatorialData]
        public async Task DeclarationPatternInSwitchCase_WithOnlyWriteReference_PreferUnusedLocal(
            [CombinatorialValues(LanguageVersion.CSharp8, LanguageVersion.CSharp9)] LanguageVersion languageVersion)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(object p)
    {
        switch (p)
        {
            case int [|x|]:
                x = 1;
                break;
        };
    }
}", new TestParameters(options: PreferUnusedLocal, parseOptions: new CSharpParseOptions(languageVersion)));
        }

        [Theory]
        [CombinatorialData]
        public async Task DeclarationPatternInSwitchCase_WithOnlyWriteReference_TypePattern(
            [CombinatorialValues(CodeFixTestBehaviors.None, CodeFixTestBehaviors.FixOne)] CodeFixTestBehaviors testBehaviors)
        {
            var source =
@"class C
{
    void M(object p)
    {
        switch (p)
        {
            case int {|IDE0059:x|}:
                {|IDE0059:x|} = 1;
                break;
        };
    }
}";
            var (fixedSource, iterations) = testBehaviors switch
            {
                CodeFixTestBehaviors.None =>
(@"class C
{
    void M(object p)
    {
        switch (p)
        {
            case int:
                break;
        };
    }
}", iterations: 2),
                CodeFixTestBehaviors.FixOne =>
(@"class C
{
    void M(object p)
    {
        switch (p)
        {
            case int:
                int {|IDE0059:x|} = 1;
                break;
        };
    }
}", iterations: 1),
                _ => throw ExceptionUtilities.Unreachable(),
            };

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedState = { Sources = { fixedSource }, MarkupHandling = MarkupMode.Allow },
                LanguageVersion = LanguageVersion.CSharp9,
                CodeFixTestBehaviors = testBehaviors,
                NumberOfIncrementalIterations = iterations,
                NumberOfFixAllIterations = iterations,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, UnusedValuePreference.DiscardVariable },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task DeclarationPatternInIsPattern_WithNoReference_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object p)
    {
        if (p is C [|x|])
        {
        }
    }
}",
@"class C
{
    void M(object p)
    {
        if (p is C)
        {
        }
    }
}", options: PreferDiscard);
        }

        [Fact]
        public async Task DeclarationPatternInIsPattern_WithNoReference_PreferUnusedLocal()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(object p)
    {
        if (p is C [|x|])
        {
        }
    }
}", options: PreferUnusedLocal);
        }

        [Fact]
        public async Task DeclarationPatternInIsPattern_WithOnlyWriteReference_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object p)
    {
        if (p is C [|x|])
        {
            x = null;
        }
    }
}",
@"class C
{
    void M(object p)
    {
        if (p is C)
        {
            C x = null;
        }
    }
}", options: PreferDiscard);
        }

        [Fact]
        public async Task DeclarationPatternInIsPattern_WithOnlyWriteReference_PreferUnusedLocal()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(object p)
    {
        if (p is C [|x|])
        {
            x = null;
        }
    }
}", options: PreferUnusedLocal);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard), "C")]
        [InlineData(nameof(PreferUnusedLocal), "C unused")]
        public async Task DeclarationPatternInIsPattern_WithReadAndWriteReference(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object p)
    {
        if (p is C [|x|])
        {
            x = null;
            p = x;
        }
    }
}",
$@"class C
{{
    void M(object p)
    {{
        if (p is {fix})
        {{
            C x = null;
            p = x;
        }}
    }}
}}", optionName: optionName);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32271")]
        public async Task DeclarationPatternInRecursivePattern_WithNoReference_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object p1, object p2)
    {
        var isZero = (p1, p2) switch { (0, 0) => true, (int [|x1|], int x2) => false };
    }
}",
@"class C
{
    void M(object p1, object p2)
    {
        var isZero = (p1, p2) switch { (0, 0) => true, (int _, int x2) => false };
    }
}", options: PreferDiscard, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8));
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/32271")]
        [CombinatorialData]
        public async Task DeclarationPatternInRecursivePattern_WithNoReference_PreferUnusedLocal(
            [CombinatorialValues(LanguageVersion.CSharp8, LanguageVersion.CSharp9)] LanguageVersion languageVersion)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(object p1, object p2)
    {
        var isZero = (p1, p2) switch { (0, 0) => true, (int [|x1|], int x2) => false };
    }
}", options: PreferUnusedLocal, parseOptions: new CSharpParseOptions(languageVersion));
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/32271")]
        [CombinatorialData]
        public async Task DeclarationPatternInRecursivePattern_WithNoReference_TypePattern(
            [CombinatorialValues(CodeFixTestBehaviors.None, CodeFixTestBehaviors.FixOne)] CodeFixTestBehaviors testBehaviors)
        {
            var source =
@"class C
{
    bool M(object p1, object p2)
    {
        var isZero = (p1, p2) switch { (0, 0) => true, (int {|IDE0059:x1|}, int {|IDE0059:x2|}) => false };
        return isZero;
    }
}";
            var batchFixedSource =
@"class C
{
    bool M(object p1, object p2)
    {
        var isZero = (p1, p2) switch { (0, 0) => true, (int, int) => false };
        return isZero;
    }
}";
            var fixedSource = testBehaviors switch
            {
                CodeFixTestBehaviors.None => batchFixedSource,
                CodeFixTestBehaviors.FixOne =>
                @"class C
{
    bool M(object p1, object p2)
    {
        var isZero = (p1, p2) switch { (0, 0) => true, (int, int {|IDE0059:x2|}) => false };
        return isZero;
    }
}",
                _ => throw ExceptionUtilities.Unreachable()
            };

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedState = { Sources = { fixedSource }, MarkupHandling = MarkupMode.Allow },
                BatchFixedCode = batchFixedSource,
                LanguageVersion = LanguageVersion.CSharp9,
                CodeFixTestBehaviors = testBehaviors,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, UnusedValuePreference.DiscardVariable },
                },
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32271")]
        public async Task DeclarationPatternInRecursivePattern_WithOnlyWriteReference_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object p1, object p2)
    {
        var isZero = (p1, p2) switch { (0, 0) => true, (int [|x1|], int x2) => M2(out x1) };
    }

    bool M2(out int x)
    {
        x = 0;
        return false;
    }
}",
@"class C
{
    void M(object p1, object p2)
    {
        int x1;
        var isZero = (p1, p2) switch { (0, 0) => true, (int _, int x2) => M2(out x1) };
    }

    bool M2(out int x)
    {
        x = 0;
        return false;
    }
}", options: PreferDiscard, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8));
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/32271")]
        [CombinatorialData]
        public async Task DeclarationPatternInRecursivePattern_WithOnlyWriteReference_PreferUnusedLocal(
            [CombinatorialValues(LanguageVersion.CSharp8, LanguageVersion.CSharp9)] LanguageVersion languageVersion)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(object p1, object p2)
    {
        var isZero = (p1, p2) switch { (0, 0) => true, (int [|x1|], int x2) => M2(out x1) };
    }

    bool M2(out int x)
    {
        x = 0;
        return false;
    }
}", options: PreferUnusedLocal, parseOptions: new CSharpParseOptions(languageVersion));
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/32271")]
        [CombinatorialData]
        public async Task DeclarationPatternInRecursivePattern_WithOnlyWriteReference_TypePattern(
            [CombinatorialValues(CodeFixTestBehaviors.None, CodeFixTestBehaviors.FixOne)] CodeFixTestBehaviors testBehaviors)
        {
            var source =
@"class C
{
    bool M(object p1, object p2)
    {
        var isZero = (p1, p2) switch { (0, 0) => true, (int {|IDE0059:x1|}, int {|IDE0059:x2|}) => M2(out {|IDE0059:x1|}) };
        return isZero;
    }

    bool M2(out int x)
    {
        x = 0;
        return false;
    }
}";
            var batchFixedSource =
@"class C
{
    bool M(object p1, object p2)
    {
        var isZero = (p1, p2) switch { (0, 0) => true, (int, int) => M2(out _) };
        return isZero;
    }

    bool M2(out int x)
    {
        x = 0;
        return false;
    }
}";
            var fixedSource = testBehaviors switch
            {
                CodeFixTestBehaviors.None => batchFixedSource,
                CodeFixTestBehaviors.FixOne =>
@"class C
{
    bool M(object p1, object p2)
    {
        int x1;
        var isZero = (p1, p2) switch { (0, 0) => true, (int, int {|IDE0059:x2|}) => M2(out {|IDE0059:x1|}) };
        return isZero;
    }

    bool M2(out int x)
    {
        x = 0;
        return false;
    }
}",
                _ => throw ExceptionUtilities.Unreachable(),
            };

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedState = { Sources = { fixedSource }, MarkupHandling = MarkupMode.Allow },
                BatchFixedCode = batchFixedSource,
                LanguageVersion = LanguageVersion.CSharp9,
                CodeFixTestBehaviors = testBehaviors,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, UnusedValuePreference.DiscardVariable },
                },
            }.RunAsync();
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/32271")]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "unused")]
        public async Task DeclarationPatternInRecursivePattern_WithReadAndWriteReference(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object p1, object p2)
    {
        var isZero = (p1, p2) switch { (0, 0) => true, (int [|x1|], int x2) => M2(x1 = 0) && M2(x1) };
    }

    bool M2(int x)
    {
        return false;
    }
}",
$@"class C
{{
    void M(object p1, object p2)
    {{
        int x1;
        var isZero = (p1, p2) switch {{ (0, 0) => true, (int {fix}, int x2) => M2(x1 = 0) && M2(x1) }};
    }}

    bool M2(int x)
    {{
        return false;
    }}
}}", optionName: optionName, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8));
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/32271")]
        [InlineData(nameof(PreferDiscard), "")]
        [InlineData(nameof(PreferUnusedLocal), " unused")]
        public async Task DeclarationPatternInRecursivePattern_WithReadAndWriteReference_TypePatternxxxxxxxxxxxxxxxxxxxxxx(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object p1, object p2)
    {
        var isZero = (p1, p2) switch { (0, 0) => true, (int [|x1|], int x2) => M2(x1 = 0) && M2(x1) };
    }

    bool M2(int x)
    {
        return false;
    }
}",
$@"class C
{{
    void M(object p1, object p2)
    {{
        int x1;
        var isZero = (p1, p2) switch {{ (0, 0) => true, (int{fix}, int x2) => M2(x1 = 0) && M2(x1) }};
    }}

    bool M2(int x)
    {{
        return false;
    }}
}}", optionName: optionName, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp9));
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/32271")]
        [CombinatorialData]
        public async Task DeclarationPatternInRecursivePattern_WithReadAndWriteReference_TypePattern(
            [CombinatorialValues(UnusedValuePreference.DiscardVariable, UnusedValuePreference.UnusedLocalVariable)] object option,
            [CombinatorialValues(CodeFixTestBehaviors.None, CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck)] CodeFixTestBehaviors testBehaviors)
        {
            var source =
@"class C
{
    bool M(object p1, object p2)
    {
        var isZero = (p1, p2) switch { (0, 0) => true, (int {|IDE0059:x1|}, int {|#0:x2|}) => M2(x1 = 0) && M2(x1) };
        return isZero;
    }

    bool M2(int {|IDE0060:x|})
    {
        return false;
    }
}";

            var fixedSource = ((UnusedValuePreference)option, testBehaviors) switch
            {
                (UnusedValuePreference.DiscardVariable, CodeFixTestBehaviors.None) =>
@"class C
{
    bool M(object p1, object p2)
    {
        int x1;
        var isZero = (p1, p2) switch { (0, 0) => true, (int, int) => M2(x1 = 0) && M2(x1) };
        return isZero;
    }

    bool M2(int {|IDE0060:x|})
    {
        return false;
    }
}",
                (UnusedValuePreference.DiscardVariable, CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck) =>
@"class C
{
    bool M(object p1, object p2)
    {
        int x1;
        var isZero = (p1, p2) switch { (0, 0) => true, (int, int {|IDE0059:x2|}) => M2(x1 = 0) && M2(x1) };
        return isZero;
    }

    bool M2(int {|IDE0060:x|})
    {
        return false;
    }
}",
                (UnusedValuePreference.UnusedLocalVariable, _) =>
@"class C
{
    bool M(object p1, object p2)
    {
        int x1;
        var isZero = (p1, p2) switch { (0, 0) => true, (int unused, int x2) => M2(x1 = 0) && M2(x1) };
        return isZero;
    }

    bool M2(int {|IDE0060:x|})
    {
        return false;
    }
}",
                _ => throw ExceptionUtilities.Unreachable(),
            };

            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedState = { Sources = { fixedSource }, MarkupHandling = MarkupMode.Allow },
                LanguageVersion = LanguageVersion.CSharp9,
                CodeFixTestBehaviors = testBehaviors,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, (UnusedValuePreference)option },
                },
            };

            if ((UnusedValuePreference)option == UnusedValuePreference.DiscardVariable)
            {
                test.TestState.ExpectedDiagnostics.Add(
                    // /0/Test0.cs(5,69): info IDE0059: Unnecessary assignment of a value to 'x2'
                    VerifyCS.Diagnostic("IDE0059").WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithArguments("x2"));
            }

            await test.RunAsync();
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_WithInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        Action lambda = () =>
        {
            var x = p;
        };

        [|p|] = null;
        lambda();
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_WithInvocation_DefinedAtStart(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        void LocalFunction()
        {
            var x = p;
        }

        [|p|] = null;
        LocalFunction();
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_WithInvocation_DefinedAtEnd(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        [|p|] = null;
        LocalFunction();

        void LocalFunction()
        {
            var x = p;
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_WithoutInvocation(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        [|p|] = null;
        Action lambda = () =>
        {
            var x = p;
        };
    }
}",
@"using System;

class C
{
    void M(object p)
    {
        Action lambda = () =>
        {
            var x = p;
        };
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_WithoutInvocation_DefinedAtStart(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        void LocalFunction()
        {
            var x = p;
        }
        [|p|] = null;
    }
}",
@"using System;

class C
{
    void M(object p)
    {
        void LocalFunction()
        {
            var x = p;
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_WithoutInvocation_DefinedAtEnd(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        [|p|] = null;
        void LocalFunction()
        {
            var x = p;
        }
    }
}",
@"using System;

class C
{
    void M(object p)
    {
        void LocalFunction()
        {
            var x = p;
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NotUseInLambda_WithInvocation(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        Action lambda = () =>
        {
        };
        [|p|] = null;
        lambda();
    }
}",
@"using System;

class C
{
    void M(object p)
    {
        Action lambda = () =>
        {
        };
        lambda();
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NotUseInLocalFunction_WithInvocation(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        [|p|] = null;
        LocalFunction();
        void LocalFunction()
        {
        }
    }
}",
@"using System;

class C
{
    void M(object p)
    {
        LocalFunction();
        void LocalFunction()
        {
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NotUseInLambda_WithoutInvocation(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        [|p|] = null;
        Action lambda = () =>
        {
        };
    }
}",
@"using System;

class C
{
    void M(object p)
    {
        Action lambda = () =>
        {
        };
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NotUseInLocalFunction_WithoutInvocation(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        [|p|] = null;
        void LocalFunction()
        {
        }
    }
}",
@"using System;

class C
{
    void M(object p)
    {
        void LocalFunction()
        {
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task RedundantWriteInLambda_WithInvocation(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        Action lambda = () =>
        {
            [|p|] = null;
        };
        lambda();
    }
}",
@"using System;

class C
{
    void M(object p)
    {
        Action lambda = () =>
        {
        };
        lambda();
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task RedundantWriteInLocalFunction_WithInvocation(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        LocalFunction();
        void LocalFunction()
        {
            [|p|] = null;
        }
    }
}",
@"using System;

class C
{
    void M(object p)
    {
        LocalFunction();
        void LocalFunction()
        {
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task WriteThenReadInLambda_WithInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        Action lambda = () =>
        {
            [|p|] = null;
            var x = p;
        };
        lambda();
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task WriteThenReadInLocalFunction_WithInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        LocalFunction();
        void LocalFunction()
        {
            [|p|] = null;
            var x = p;
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task RedundantWriteInLambda_WithoutInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        Action lambda = () =>
        {
            [|p|] = null;
        };
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task RedundantWriteInLocalFunction_WithoutInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        void LocalFunction()
        {
            [|p|] = null;
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_Nested(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        Action outerLambda = () =>
        {
            Action innerLambda = () =>
            {
                var x = p;
            };

            innerLambda();
        });

        [|p|] = null;
        outerLambda();
    }

    void M2(Action a) => a();
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_NestedLocalFunction(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        [|p|] = null;
        OuterLocalFunction();

        void OuterLocalFunction()
        {
            InnerLocalFunction();

            void InnerLocalFunction()
            {
                var x = p;
            }
        });
    }

    void M2(Action a) => a();
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_NestedLocalFunction(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, Action<Action> outerDelegate)
    {
        [|p|] = null;
        outerDelegate(() =>
        {
            InnerLocalFunction();
            void InnerLocalFunction()
            {
                var x = p;
            }
        });
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_NestedLambda(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, Action<Action> myDelegate)
    {
        [|p|] = null;
        OuterLocalFunction();

        void OuterLocalFunction()
        {
            myDelegate(() =>
            {
                var x = p;
            });
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInNestedLambda_InvokedInOuterFunction(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, Action myDelegate)
    {
        [|p|] = null;
        OuterLocalFunction();
        myDelegate();

        void OuterLocalFunction()
        {
            myDelegate = () =>
            {
                var x = p;
            };
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInNestedLocalFunction_InvokedInOuterFunction(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, Action myDelegate)
    {
        [|p|] = null;
        OuterLocalFunction();
        myDelegate();

        void OuterLocalFunction()
        {
            myDelegate = NestedLocalFunction;
            void NestedLocalFunction()
            {
                var x = p;
            }
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_ArgumentToLambda(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, Action<object> myDelegate)
    {
        [|p|] = null;
        myDelegate(p);
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_ArgumentToLambda_02(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    Action<int> M(object p, Action<object> myDelegate)
    {
        [|p|] = null;
        return d => { myDelegate(0); };
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_PassedAsArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        [|p|] = null;
        M2(() =>
        {
            var x = p;
        });
    }

    void M2(Action a) => a();
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_PassedAsArgument_02(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    public C(bool flag)
    {
        Flag = flag;
    }

    public bool Flag { get; }
    public static bool M()
    {
        bool flag = true;
        var c = Create(() => flag);

        M2(c);
        [|flag|] = false;
        return M2(c);
    }

    private static C Create(Func<bool> isFlagTrue) { return new C(isFlagTrue()); }
    private static bool M2(C c) => c.Flag;
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_PassedAsArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        [|p|] = null;
        M2(LocalFunction);

        void LocalFunction()
        {
            var x = p;
        }
    }

    void M2(Action a) => a();
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_PassedAsArgument_CustomDelegate(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

public delegate void MyAction();

class C
{
    void M(object p)
    {
        [|p|] = null;
        M2(() =>
        {
            var x = p;
        });
    }

    void M2(MyAction a) => a();
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_PassedAsArgument_CustomDelegate(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

public delegate void MyAction();

class C
{
    void M(object p)
    {
        [|p|] = null;
        M2(LocalFunction);

        void LocalFunction()
        {
            var x = p;
        }
    }

    void M2(MyAction a) => a();
}", optionName);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/31744")]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UnusedInExpressionTree_PassedAsArgument(string optionName)
        {
            // Currently we bail out of analysis in presence of expression trees.
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Linq.Expressions;

class C
{
    public static void M1()
    {
        object [|p|] = null;
        M2(x => x.M3());
    }

    private static C M2(Expression<Func<C, int>> a) { return null; }
    private int M3() { return 0; }
}", optionName);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/31744")]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task ReadInExpressionTree_PassedAsArgument(string optionName)
        {
            // Currently we bail out of analysis in presence of expression trees.
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Linq.Expressions;

class C
{
    public static void M1()
    {
        object [|p|] = null;
        M2(x => x.M3(p));
    }

    private static C M2(Expression<Func<C, int>> a) { return null; }
    private int M3(object o) { return 0; }
}", optionName);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/31744")]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task OnlyWrittenInExpressionTree_PassedAsArgument(string optionName)
        {
            // Currently we bail out of analysis in presence of expression trees.
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Linq.Expressions;

class C
{
    public static void M1()
    {
        object [|p|] = null;
        M2(x => x.M3(out p));
    }

    private static C M2(Expression<Func<C, int>> a) { return null; }
    private int M3(out object o) { o = null; return 0; }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_PassedAsArgument_CastFromDelegateType(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        Action a = () =>
        {
            var x = p;
        };

        object o = a;
        [|p|] = null;
        M2(o);
    }

    void M2(object a) => ((Action)a)();
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_PassedAsArgument_CastFromDelegateType(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        object o = (Action)LocalFunction;
        [|p|] = null;
        M2(o);

        void LocalFunction()
        {
            var x = p;
        }
    }

    void M2(object a) => ((Action)a)();
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_DelegateCreationPassedAsArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        [|p|] = null;
        M2(new Action(() =>
        {
            var x = p;
        }));
    }

    void M2(Action a) => a();
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_DelegateCreationPassedAsArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        [|p|] = null;
        M2(new Action(LocalFunction));

        void LocalFunction()
        {
            var x = p;
        }
    }

    void M2(Action a) => a();
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_DelegatePassedAsArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        Action local = () =>
        {
            var x = p;
        };

        [|p|] = null;
        M2(local);
    }

    void M2(Action a) => a();
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_DelegatePassedAsArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        Action local = LocalFunction;
        [|p|] = null;
        M2(local);

        void LocalFunction()
        {
            var x = p;
        }
    }

    void M2(Action a) => a();
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task WrittenInLambda_DelegatePassedAsArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, object p2)
    {
        Action local = () =>
        {
            p = p2;
        };

        [|p|] = null;
        M2(local);

        var x = p;
    }

    void M2(Action a) => a();
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task WrittenInLocalFunction_DelegatePassedAsArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, object p2)
    {
        Action local = LocalFunction;
        [|p|] = null;
        M2(local);
        var x = p;

        void LocalFunction()
        {
            p = p2;
        }
    }

    void M2(Action a) => a();
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task WrittenInLambdaAndLocalFunctionTargets_DelegatePassedAsArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, object p2)
    {
        Action lambda = () =>
        {
            p = p2;
        };

        Action myDelegate;
        if (p2 != null)
        {
            myDelegate = lambda;
        }
        else
        {
            myDelegate = LocalFunction;
        }

        [|p|] = null;
        M2(myDelegate);

        var x = p;

        void LocalFunction()
        {
            p = p2;
        }
    }

    void M2(Action a) => a();
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_ReturnedDelegateCreation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    Action M(object p)
    {
        [|p|] = null;
        return new Action(() =>
        {
            var x = p;
        });
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_ReturnedDelegateCreation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    Action M(object p)
    {
        [|p|] = null;
        return new Action(LocalFunction);

        void LocalFunction()
        {
            var x = p;
        };
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_ReturnedDelegate(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    Action M(object p)
    {
        Action local = () =>
        {
            var x = p;
        };

        [|p|] = null;
        return local;
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_ReturnedDelegate(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    Action M(object p)
    {
        [|p|] = null;
        return LocalFunction;

        void LocalFunction()
        {
            var x = p;
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_InvokedDelegate_ControlFlow(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, bool flag)
    {
        Action local1 = () =>
        {
            var x = p;
        };

        Action local2 = () => { };

        [|p|] = null;
        var y = flag ? local1 : local2;
        y();
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_InvokedDelegate_ControlFlow(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, bool flag)
    {
        [|p|] = null;
        (flag ? LocalFunction1 : (Action)LocalFunction2)();

        void LocalFunction1()
        {
            var x = p;
        }

        void LocalFunction2()
        {
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_LambdaAndLocalFunctionTargets(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, bool flag, bool flag2)
    {
        Action lambda = () =>
        {
            var x = p;
        };

        [|p|] = null;
        var y = flag ? lambda : (flag2 ? (Action)LocalFunction : M2);
        y();

        void LocalFunction() { }
    }

    void M2() { }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NotUsed_LambdaAndLocalFunctionTargets(string optionName)
        {
            // Below should be changed to verify diagnostic/fix once we
            // perform points-to-analysis for accurate delegate target tracking.
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, bool flag, bool flag2)
    {
        Action lambda = () =>
        {
        };

        [|p|] = null;
        var y = flag ? lambda : (flag2 ? (Action)LocalFunction : M2);
        y();

        void LocalFunction() { }
    }

    void M2() { }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_LambdaAndLocalFunctionTargets_ThroughLocalsAndParameters(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, bool flag, bool flag2, Action param)
    {
        Action lambda = () =>
        {
            var x = p;
        };

        [|p|] = null;

        Action y;
        if (flag)
        {
            if (flag2)
            {
                y = (Action)LocalFunction;
            }
            else
            {
                y = M2;
            }
        }
        else
        {
            y = null;
            if (flag2)
            {
                param = lambda;
            }
            else
            {
                param = M2;
            }
        }

        Action z;
        if (y != null)
        {
            z = y;
        }
        else
        {
            z = param;
        }

        z();

        void LocalFunction() { }
    }

    void M2() { }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NotUsed_LambdaAndLocalFunctionTargets_ThroughLocalsAndParameters(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, bool flag, bool flag2, Action param)
    {
        Action lambda = () =>
        {
        };

        [|p|] = null;

        Action y;
        if (flag)
        {
            if (flag2)
            {
                y = (Action)LocalFunction;
            }
            else
            {
                y = M2;
            }
        }
        else
        {
            y = null;
            if (flag2)
            {
                param = lambda;
            }
            else
            {
                param = M2;
            }
        }

        Action z;
        if (y != null)
        {
            z = y;
        }
        else
        {
            z = param;
        }

        z();

        void LocalFunction() { }
    }

    void M2() { }
}",
@"using System;

class C
{
    void M(object p, bool flag, bool flag2, Action param)
    {
        Action lambda = () =>
        {
        };
        Action y;
        if (flag)
        {
            if (flag2)
            {
                y = (Action)LocalFunction;
            }
            else
            {
                y = M2;
            }
        }
        else
        {
            y = null;
            if (flag2)
            {
                param = lambda;
            }
            else
            {
                param = M2;
            }
        }

        Action z;
        if (y != null)
        {
            z = y;
        }
        else
        {
            z = param;
        }

        z();

        void LocalFunction() { }
    }

    void M2() { }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInLambda_UsedAfterInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    int M(int x)
    {
        Action a = () =>
        {
            [|x|] = 1;
        };
        a();

        return x;
    }
}
", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInLocalFunction_UsedAfterInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    int M(int x)
    {
        a();

        return x;

        void a()
        {
            [|x|] = 1;
        }
    }
}
", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInLambda_UsedAfterSecondInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    int M(int x)
    {
        Action a = () =>
        {
            [|x|] = 1;
        };

        a();
        a();

        return x;
    }
}
", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInLocalFunction_UsedAfterSecondInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    int M(int x)
    {
        a();
        a();

        return x;

        void a()
        {
            [|x|] = 1;
        }
    }
}
", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInLambda_MayBeUsedAfterOneOfTheInvocations(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    int M(int x, bool flag, bool flag2)
    {
        Action a = () =>
        {
            [|x|] = 1;
        };

        a();

        if (flag)
        {
            a();
            if (flag2)
            {
                return x;
            }
        }

        return 0;
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInLocalFunction_MayBeUsedAfterOneOfTheInvocations(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    int M(int x, bool flag, bool flag2)
    {
        a();

        if (flag)
        {
            a();
            if (flag2)
            {
                return x;
            }
        }

        return 0;

        void a()
        {
            [|x|] = 1;
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInLambda_NotUsedAfterInvocation(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        Action a = () =>
        {
            [|x|] = 1;
        };
        a();
    }
}
",
@"using System;

class C
{
    void M(int x)
    {
        Action a = () =>
        {
        };
        a();
    }
}
", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInLocalFunction_NotUsedAfterInvocation(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        a();

        void a()
        {
            [|x|] = 1;
        }
    }
}",
@"using System;

class C
{
    void M(int x)
    {
        a();

        void a()
        {
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_WithRecursiveInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        void LocalFunction()
        {
            var x = p;
            LocalFunction();
        }

        [|p|] = null;
        LocalFunction();
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NotUseInLocalFunction_WithRecursiveInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        void LocalFunction()
        {
            LocalFunction();
        }

        [|p|] = null;
        LocalFunction();
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Lambda_WithNonReachableExit(string optionName)
        {
            // We bail out from analysis for delegate passed an argument.
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        Action throwEx = () =>
        {
            throw new Exception();
        };

        [|p|] = null;
        M2(throwEx);
    }

    void M2(Action a) { }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Lambda_WithMultipleInvocations(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        Action lambda = () =>
        {
            var x = p;
            [|p|] = null;   // This write is read on next invocation of lambda.
        };

        M2(lambda);
    }

    void M2(Action a)
    {
        a();
        a();
    }
}", optionName);
        }

        [Fact]
        public async Task UnusedValue_DelegateTypeOptionalParameter_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        var [|x|] = M2();
    }

    C M2(Action c = null) => null;
}",
@"using System;

class C
{
    void M()
    {
        _ = M2();
    }

    C M2(Action c = null) => null;
}", options: PreferDiscard);
        }

        [Fact]
        public async Task UnusedValue_DelegateTypeOptionalParameter_PreferUnusedLocal()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        var [|x|] = M2();
    }

    C M2(Action c = null) => null;
}", options: PreferUnusedLocal);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_NestedInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        LocalFunction();

        bool LocalFunction2()
        {
            return true;
        }

        void LocalFunction()
        {
            object [|p|] = null;
            if (LocalFunction2())
            {
            }

            if (p != null)
            {
            }
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "unused")]
        public async Task DeclarationPatternInSwitchCase_WithReadAndWriteReferences(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object p)
    {
        switch (p)
        {
            case int [|x|]:
                x = 1;
                p = x;
                break;
        }
    }
}",
$@"class C
{{
    void M(object p)
    {{
        switch (p)
        {{
            case int {fix}:
                int x = 1;
                p = x;
                break;
        }}
    }}
}}", optionName, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8));
        }

        [Fact]
        public async Task CatchClause_ExceptionVariable_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        try
        {
        }
        catch (Exception [|ex|])
        {
        }
    }
}",
@"using System;

class C
{
    void M(object p)
    {
        try
        {
        }
        catch (Exception)
        {
        }
    }
}", options: PreferDiscard);
        }

        [Fact]
        public async Task CatchClause_ExceptionVariable_PreferUnusedLocal_01()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        try
        {
        }
        catch (Exception [|ex|])
        {
        }
    }
}", options: PreferUnusedLocal);
        }

        [Fact]
        public async Task CatchClause_ExceptionVariable_PreferUnusedLocal_02()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        try
        {
        }
        catch (Exception [|ex|])
        {
            ex = null;
            var x = ex;
        }
    }
}",
@"using System;

class C
{
    void M(object p)
    {
        try
        {
        }
        catch (Exception unused)
        {
            Exception ex = null;
            var x = ex;
        }
    }
}", options: PreferUnusedLocal);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedOutsideTry_UsedOnlyInCatchClause(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        [|x|] = 0;
        try
        {
        }
        catch (Exception)
        {
            var y = x;
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedOutsideTry_UsedOnlyInCatchFilter(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        [|x|] = 0;
        try
        {
        }
        catch (Exception) when (x != 0)
        {
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedOutsideTry_UsedOnlyInFinally(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        [|x|] = 0;
        try
        {
        }
        finally
        {
            var y = x;
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInsideTry_UsedOnlyInCatchClause(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        try
        {
            [|x|] = 0;
        }
        catch (Exception)
        {
            var y = x;
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInsideNestedBlockInTry_UsedOnlyInCatchClause(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        try
        {
            if (x > 0)
            {
                [|x|] = 0;
            }
        }
        catch (Exception)
        {
            var y = x;
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInCatchClause_UsedAfterTryCatch(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        try
        {
        }
        catch (Exception)
        {
            [|x|] = 0;
        }

        var y = x;
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInNestedCatchClause_UsedInOuterFinally(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        try
        {
            try
            {
            }
            catch (Exception)
            {
                [|x|] = 0;
            }
        }
        finally
        {
            var y = x;
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInCatchClause_UsedInFinally(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        try
        {
        }
        catch (Exception)
        {
            [|x|] = 0;
        }
        finally
        {
            var y = x;
        }
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInCatchFilter_UsedAfterTryCatch(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        try
        {
        }
        catch (Exception) when (M2(out [|x|]))
        {
        }

        var y = x;
    }

    bool M2(out int x) { x = 0; return true; }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInFinally_UsedAfterTryFinally(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        try
        {
        }
        finally
        {
            [|x|] = 0;
        }

        var y = x;
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInNestedFinally_UsedInOuterFinally(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        try
        {
            try
            {
            }
            finally
            {
                [|x|] = 0;
            }
        }
        finally
        {
            var y = x;
        }
    }
}", optionName);
        }

        [Fact]
        public async Task IfElse_AssignedInCondition_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool flag)
    {
        int x;
        if (M2(out [|x|]))
        {
            x = 2;
        }
        else
        {
            x = 3;
        }
    }

    bool M2(out int x) => x = 0;
}",
@"class C
{
    void M(bool flag)
    {
        int x;
        if (M2(out _))
        {
            x = 2;
        }
        else
        {
            x = 3;
        }
    }

    bool M2(out int x) => x = 0;
}", options: PreferDiscard);
        }

        [Fact]
        public async Task IfElse_DeclaredInCondition_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool flag)
    {
        if (M2(out var [|x|]))
        {
            x = 2;
        }
        else
        {
            x = 3;
        }
    }

    bool M2(out int x) => x = 0;
}",
@"class C
{
    void M(bool flag)
    {
        int x;
        if (M2(out _))
        {
            x = 2;
        }
        else
        {
            x = 3;
        }
    }

    bool M2(out int x) => x = 0;
}", options: PreferDiscard);
        }

        [Fact]
        public async Task IfElseAssignedInCondition_ReadAfter_PreferUnusedLocal()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(bool flag)
    {
        int x;
        if (M2(out [|x|]))
        {
            x = 2;
        }
        else
        {
            x = 3;
        }

        return x;
    }

    bool M2(out int x) => x = 0;
}",
@"class C
{
    int M(bool flag)
    {
        int x;
        int unused;
        if (M2(out unused))
        {
            x = 2;
        }
        else
        {
            x = 3;
        }

        return x;
    }

    bool M2(out int x) => x = 0;
}", options: PreferUnusedLocal);
        }

        [Fact]
        public async Task IfElse_AssignedInCondition_NoReads_PreferUnusedLocal()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool flag)
    {
        int x;
        if (M2(out [|x|]))
        {
            x = 2;
        }
        else
        {
            x = 3;
        }
    }

    bool M2(out int x) => x = 0;
}", new TestParameters(options: PreferUnusedLocal));
        }

        [Fact]
        public async Task IfElse_DeclaredInCondition_ReadAfter_PreferUnusedLocal()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(bool flag)
    {
        if (M2(out var [|x|]))
        {
            x = 2;
        }
        else
        {
            x = 3;
        }

        return x;
    }

    bool M2(out int x) => x = 0;
}",
@"class C
{
    int M(bool flag)
    {
        int x;
        if (M2(out var unused))
        {
            x = 2;
        }
        else
        {
            x = 3;
        }

        return x;
    }

    bool M2(out int x) => x = 0;
}", options: PreferUnusedLocal);
        }

        [Fact]
        public async Task IfElse_DeclaredInCondition_NoReads_PreferUnusedLocal()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool flag)
    {
        if (M2(out var [|x|]))
        {
            x = 2;
        }
        else
        {
            x = 3;
        }
    }

    bool M2(out int x) => x = 0;
}", new TestParameters(options: PreferUnusedLocal));
        }

        [Theory]
        // Simple if-else.
        [InlineData("x = 1;", "x = 2;")]
        // Nested if-else.
        [InlineData("if(flag) { x = 1; } else { x = 2; }",
                    "x = 3;")]
        // Multiple nested paths.
        [InlineData("if(flag) { x = 1; } else { x = 2; }",
                    "if(flag) { x = 3; } else { x = 4; }")]
        // Nested if-elseif-else.
        [InlineData("if(flag) { x = 1; } else if(flag2) { x = 2; } else { x = 3; }",
                    "if(flag) { x = 5; } else { x = 6; }")]
        //Multi-level nesting.
        [InlineData(@"if(flag) { x = 1; } else { if(flag2) { if(flag3) { x = 2; } else { x = 3; } } else { x = 4; } }",
                    @"x = 5;")]
        public async Task IfElse_OverwrittenInAllControlFlowPaths(string ifBranchCode, string elseBranchCode)
        {
            await TestInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    int M(bool flag, bool flag2, bool flag3)
    {{
        int [|x|] = 1;
        if (flag4)
        {{
            {ifBranchCode}
        }}
        else
        {{
            {elseBranchCode}
        }}

        return x;
    }}
}}",
$@"class C
{{
    int M(bool flag, bool flag2, bool flag3)
    {{
        int x;
        if (flag4)
        {{
            {ifBranchCode}
        }}
        else
        {{
            {elseBranchCode}
        }}

        return x;
    }}
}}");
        }

        [Theory]
        // Overwrite missing in if path.
        [InlineData(";", "x = 2;")]
        // Overwrite missing in else path.
        [InlineData("x = 2;", "")]
        // Overwrite missing in nested else path.
        [InlineData("if(flag) { x = 1; }",
                    "x = 2;")]
        // Overwrite missing in multiple nested paths.
        [InlineData("if(flag) { x = 1; }",
                    "if(flag) { x = 2; }")]
        // Overwrite missing with nested if-elseif-else.
        [InlineData("if(flag) { x = 1; } else if(flag2) { x = 2; }",
                    "if(flag) { x = 3; } else { x = 4; }")]
        // Overwrite missing in one path with multi-level nesting.
        [InlineData(@"if(flag) { x = 1; } else { if(flag2) { if(flag3) { x = 2; } } else { x = 3; } }",
                    @"x = 4;")]
        public async Task IfElse_OverwrittenInSomeControlFlowPaths(string ifBranchCode, string elseBranchCode)
        {
            await TestMissingInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    int M(bool flag, bool flag2, bool flag3)
    {{
        int [|x|] = 1;
        if (flag4)
        {{
            {ifBranchCode}
        }}
        else
        {{
            {elseBranchCode}
        }}

        return x;
    }}
}}");
        }

        [Theory]
        // Overitten in condition when true, overwritten in else code block when false.
        [InlineData("flag && M2(out x)", ";", "x = 2;")]
        // Overitten in condition when false, overwritten in if code block when true.
        [InlineData("flag || M2(out x)", "x = 2;", ";")]
        public async Task IfElse_Overwritten_CodeInOneBranch_ConditionInOtherBranch(string condition, string ifBranchCode, string elseBranchCode)
        {
            await TestInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    int M(bool flag)
    {{
        int [|x|] = 1;
        if ({condition})
        {{
            {ifBranchCode}
        }}
        else
        {{
            {elseBranchCode}
        }}

        return x;
    }}

    bool M2(out int x) {{ x = 0; return true; }}
    int M3() => 0;
}}",
$@"class C
{{
    int M(bool flag)
    {{
        int x;
        if ({condition})
        {{
            {ifBranchCode}
        }}
        else
        {{
            {elseBranchCode}
        }}

        return x;
    }}

    bool M2(out int x) {{ x = 0; return true; }}
    int M3() => 0;
}}");
        }

        [Theory]
        // Overwrite missing in condition when left of || is true.
        [InlineData("flag || M2(out x)")]
        // Overwrite missing in condition when left of && is true.
        [InlineData("flag && M2(out x)")]
        // Overwrite missing in condition when left of || is true, but both both sides of && have an overwrite.
        [InlineData("flag || M2(out x) && (x = M3()) > 0")]
        public async Task IfElse_MayBeOverwrittenInCondition_LogicalOperators(string condition)
        {
            await TestMissingInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    int M(bool flag)
    {{
        int [|x|] = 1;
        if ({condition})
        {{
        }}
        else
        {{
        }}

        return x;
    }}

    bool M2(out int x) {{ x = 0; return true; }}
    int M3() => 0;
}}");
        }

        [Theory]
        [InlineData("M2(out x) || flag")]
        [InlineData("M2(out x) && flag")]
        [InlineData("M2(out x) || M2(out x)")]
        [InlineData("M2(out x) && M2(out x)")]
        [InlineData("flag && M2(out x) || (x = M3()) > 0")]
        [InlineData("(flag || M2(out x)) && (x = M3()) > 0")]
        [InlineData("M2(out x) && flag || (x = M3()) > 0")]
        [InlineData("flag && M2(out x) || (x = M3()) > 0 && flag")]
        public async Task IfElse_OverwrittenInCondition_LogicalOperators(string condition)
        {
            await TestInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    int M(bool flag)
    {{
        int [|x|] = 1;
        if ({condition})
        {{
        }}
        else
        {{
        }}

        return x;
    }}

    bool M2(out int x) {{ x = 0; return true; }}
    int M3() => 0;
}}",
        $@"class C
{{
    int M(bool flag)
    {{
        int x;
        if ({condition})
        {{
        }}
        else
        {{
        }}

        return x;
    }}

    bool M2(out int x) {{ x = 0; return true; }}
    int M3() => 0;
}}");
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task ElselessIf(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int M(bool flag)
    {
        int [|x|] = 1;
        if (flag)
        {
            x = 1;
        }

        return x;
    }
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UnusedDefinition_NotFlagged_InUnreachableBlock(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int x;
        if (true)
        {
            x = 0;
        }
        else
        {
            [|x|] = 1;
        }

        return x;
    }
}

    bool M2(out int x) { x = 0; return true; }
}", optionName);
        }

        [Fact]
        public async Task SwitchCase_UnusedValueWithOnlyWrite_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(int flag)
    {
        switch(flag)
        {
            case 0:
                int [|x|] = M2();
                return 0;

            default:
                return flag;
        }
    }

    int M2() => 0;
}",
@"class C
{
    int M(int flag)
    {
        switch(flag)
        {
            case 0:
                _ = M2();
                return 0;

            default:
                return flag;
        }
    }

    int M2() => 0;
}", options: PreferDiscard);
        }

        [Fact]
        public async Task SwitchCase_UnusedValueWithOnlyWrite_PreferUnusedLocal()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int M(int flag)
    {
        switch(flag)
        {
            case 0:
                int [|x|] = M2();
                return 0;

            default:
                return flag;
        }
    }

    int M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task SwitchCase_UnusedConstantValue_WithReadsAndWrites(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(int flag)
    {
        switch(flag)
        {
            case 0:
                int [|x|] = 0;
                x = 1;
                return x;

            default:
                return flag;
        }
    }

    int M2() => 0;
}",
@"class C
{
    int M(int flag)
    {
        switch(flag)
        {
            case 0:
                int x = 1;
                return x;

            default:
                return flag;
        }
    }

    int M2() => 0;
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "int unused")]
        public async Task SwitchCase_UnusedNonConstantValue_WithReadsAndWrites(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(int flag)
    {
        switch(flag)
        {
            case 0:
                int [|x|] = M2();
                x = 1;
                return x;

            default:
                return flag;
        }
    }

    int M2() => 0;
}",
$@"class C
{{
    int M(int flag)
    {{
        switch(flag)
        {{
            case 0:
                {fix} = M2();
                int x = 1;
                return x;

            default:
                return flag;
        }}
    }}

    int M2() => 0;
}}", optionName);
        }

        [Theory]
        // For loop, assignment in body, read on back edge.
        [InlineData("for(i = 1; i < 10; i--)",
                        "M2(x); [|x|] = 1;")]
        // While loop, assignment in body, read on back edge.
        [InlineData("while(i++ < 10)",
                        "M2(x); [|x|] = 1;")]
        // Do loop, assignment in body, read on back edge.
        [InlineData("do",
                        "M2(x); [|x|] = 1;",
                    "while(i++ < 10);")]
        // Continue, read on back edge.
        [InlineData("while(i++ < 10)",
                        "M2(x); [|x|] = 1; if (flag) continue; x = 2;")]
        // Break.
        [InlineData(@"x = 0;
                      while(i++ < 10)",
                         "[|x|] = 1; if (flag) break; x = 2;")]
        // Assignment before loop, no overwrite on path where loop is never entered.
        [InlineData(@"[|x|] = 1;
                      while(i++ < 10)",
                         "x = 2;")]
        public async Task Loops_Overwritten_InSomeControlFlowPaths(
            string loopHeader, string loopBody, string? loopFooter = null)
        {
            await TestMissingInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    int M(int i, int x, bool flag)
    {{
        {loopHeader}
        {{
            {loopBody}
        }}
        {loopFooter ?? string.Empty}

        return x;
    }}

    void M2(int x) {{ }}
}}");
        }

        [Theory]
        // For loop, assignment in body, re-assigned on back edge before read in loop and re-assigned at loop exit.
        [InlineData("for(i = 1; i < 10; i--)",
                        "x = 1; M2(x); [|x|] = 2;",
                    "x = 3;",
                    // Fixed code.
                    "for(i = 1; i < 10; i--)",
                        "x = 1; M2(x);",
                    "x = 3;")]
        // While loop, assignment in body, re-assigned on condition before read in loop and re-assigned at loop exit.
        [InlineData("while(i++ < (x = 10))",
                        "M2(x); [|x|] = 2;",
                    "x = 3;",
                    // Fixed code.
                    "while(i++ < (x = 10))",
                        "M2(x);",
                    "x = 3;")]
        // Assigned before loop, Re-assigned in continue, break paths and loop exit.
        [InlineData(@"[|x|] = 1;
                      i = 1;
                      while(i++ < 10)",
                        @"if(flag)
                            { x = 2; continue; }
                          else if(i < 5)
                            { break; }
                          else
                            { x = 3; }
                          M2(x);",
                      "x = 4;",
                    // Fixed code.
                    @"i = 1;
                      while(i++ < 10)",
                        @"if(flag)
                            { x = 2; continue; }
                          else if(i < 5)
                            { break; }
                          else
                            { x = 3; }
                          M2(x);",
                      "x = 4;")]
        public async Task Loops_Overwritten_InAllControlFlowPaths(
            string loopHeader, string loopBody, string loopFooter,
            string fixedLoopHeader, string fixedLoopBody, string fixedLoopFooter)
        {
            await TestInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    int M(int i, int x, bool flag)
    {{
        {loopHeader}
        {{
            {loopBody}
        }}
        {loopFooter}

        return x;
    }}

    void M2(int x) {{ }}
}}",
$@"class C
{{
    int M(int i, int x, bool flag)
    {{
        {fixedLoopHeader}
        {{
            {fixedLoopBody}
        }}
        {fixedLoopFooter}

        return x;
    }}

    void M2(int x) {{ }}
}}");
        }

        [Fact]
        public async Task FixAll_NonConstantValue_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public C()
    {
        // Different code block
        int x = M2();
    }

    int M(bool flag)
    {
        // Trigger diagostic
        {|FixAllInDocument:int x = M2()|};

        // Unused out assignment
        M2(out x);

        // Used Assignment
        x = 0;
        System.Console.WriteLine(x);

        // Unused constant assignment.
        // Not fixed as we have a different code fix 'Remove redundant assignment'
        x = 1;

        // Unused initialization with only def/use in nested block.
        // Declaration for 'y' should be moved inside the if block.
        int y = M2();
        if (flag)
        {
            y = 2;
            System.Console.WriteLine(y);
        }
        else
        {
        }

        x = M2();
        return x;
    }

    bool M2(out int x) { x = 0; return true; }
    int M2() => 0;
}",
@"class C
{
    public C()
    {
        // Different code block
        _ = M2();
    }

    int M(bool flag)
    {
        // Trigger diagostic
        _ = M2();

        // Unused out assignment
        M2(out _);

        // Used Assignment
        int x = 0;
        System.Console.WriteLine(x);

        // Unused constant assignment.
        // Not fixed as we have a different code fix 'Remove redundant assignment'
        x = 1;

        // Unused initialization with only def/use in nested block.
        // Declaration for 'y' should be moved inside the if block.
        _ = M2();
        if (flag)
        {
            int y = 2;
            System.Console.WriteLine(y);
        }
        else
        {
        }

        x = M2();
        return x;
    }

    bool M2(out int x) { x = 0; return true; }
    int M2() => 0;
}", options: PreferDiscard);
        }

        [Fact]
        public async Task FixAll_NonConstantValue_PreferUnusedLocal()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public C()
    {
        // Different code block
        int x = M2();
        x = 0;
        System.Console.WriteLine(x);
    }

    int M(bool flag)
    {
        // Trigger diagostic
        {|FixAllInDocument:int x = M2()|};

        // Unused out assignment
        M2(out x);

        // Used Assignment, declaration for 'x' should move here
        x = 0;
        System.Console.WriteLine(x);

        // Unused constant assignment.
        // Not fixed as we have a different code fix 'Remove redundant assignment'
        x = 1;

        // Unused initialization with only def/use in nested block.
        // Declaration for 'y' should be moved inside the if block.
        int y = M2();
        if (flag)
        {
            y = 2;
            System.Console.WriteLine(y);
        }
        else
        {
        }

        x = M2();
        return x;
    }

    bool M2(out int x) { x = 0; return true; }
    int M2() => 0;
}",
@"class C
{
    public C()
    {
        // Different code block
        int unused = M2();
        int x = 0;
        System.Console.WriteLine(x);
    }

    int M(bool flag)
    {
        // Trigger diagostic
        int unused = M2();
        int unused1;

        // Unused out assignment
        M2(out unused1);

        // Used Assignment, declaration for 'x' should move here
        int x = 0;
        System.Console.WriteLine(x);

        // Unused constant assignment.
        // Not fixed as we have a different code fix 'Remove redundant assignment'
        x = 1;

        // Unused initialization with only def/use in nested block.
        // Declaration for 'y' should be moved inside the if block.
        int unused2 = M2();
        if (flag)
        {
            int y = 2;
            System.Console.WriteLine(y);
        }
        else
        {
        }

        x = M2();
        return x;
    }

    bool M2(out int x) { x = 0; return true; }
    int M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task FixAll_ConstantValue_RemoveRedundantAssignments(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public C()
    {
        // Different code block
        int x = 1;
    }

    int M(bool flag, int p)
    {
        // Trigger diagostic
        {|FixAllInDocument:int x = 0|};

        // Unused assignment from parameter, should be removed.
        x = p;

        // Unused assignment from local, should be removed.
        int local = 3;
        x = local;

        // Used assignment, declaration for 'x' should move here
        x = 0;
        System.Console.WriteLine(x);

        // Unused non-constant 'out' assignment
        // Not fixed as we have a different code fix 'Use discard' for it.
        M2(out x);

        // Unused initialization with only def/use in nested block.
        // Declaration for 'y' should be moved inside the if block.
        int y = 1;
        if (flag)
        {
            y = 2;
            System.Console.WriteLine(y);
        }
        else
        {
        }

        x = M2();
        return x;
    }

    bool M2(out int x) { x = 0; return true; }
    int M2() => 0;
}",
@"class C
{
    public C()
    {
        // Different code block
    }

    int M(bool flag, int p)
    {

        // Unused assignment from parameter, should be removed.

        // Unused assignment from local, should be removed.
        int local = 3;

        // Trigger diagostic
        // Used assignment, declaration for 'x' should move here
        int x = 0;
        System.Console.WriteLine(x);

        // Unused non-constant 'out' assignment
        // Not fixed as we have a different code fix 'Use discard' for it.
        M2(out x);
        if (flag)
        {
            // Unused initialization with only def/use in nested block.
            // Declaration for 'y' should be moved inside the if block.
            int y = 2;
            System.Console.WriteLine(y);
        }
        else
        {
        }

        x = M2();
        return x;
    }

    bool M2(out int x) { x = 0; return true; }
    int M2() => 0;
}", optionName);
        }

        [Fact]
        public async Task FixAll_MoveMultipleVariableDeclarations_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(bool flag, int p)
    {
        // Multiple unused variable declarations (x and y) moved below to start of if-else block
        // Used declaration (z1) and evaluation (_ = M2()) retained.
        // Completely unused declaration (z2) removed.
        {|FixAllInDocument:int x = 0|};
        int z1 = 1, _ = M2(), y = 0, z2 = 2;

        if (flag)
        {
            x = 1;
            y = 1;
        }
        else
        {
            x = 2;
            y = 2;
        }

        return x + y + z1;
    }

    int M2() => 0;
}",
@"class C
{
    int M(bool flag, int p)
    {
        int z1 = 1;
        _ = M2();

        // Multiple unused variable declarations (x and y) moved below to start of if-else block
        // Used declaration (z1) and evaluation (_ = M2()) retained.
        // Completely unused declaration (z2) removed.
        int x;
        int y;
        if (flag)
        {
            x = 1;
            y = 1;
        }
        else
        {
            x = 2;
            y = 2;
        }

        return x + y + z1;
    }

    int M2() => 0;
}", options: PreferDiscard);
        }

        [Fact]
        public async Task FixAll_MoveMultipleVariableDeclarations_PreferUnusedLocal()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(bool flag, int p)
    {
        // Multiple unused variable declarations (x and y) moved below to start of if-else block
        // Used declaration (z1) and evaluation (_ = M2()) retained.
        // Completely unused declaration (z2) removed.
        {|FixAllInDocument:int x = 0|};
        int z1 = 1, _ = M2(), y = 0, z2 = 2;

        if (flag)
        {
            x = 1;
            y = 1;
        }
        else
        {
            x = 2;
            y = 2;
        }

        return x + y + z1;
    }

    int M2() => 0;
}",
@"class C
{
    int M(bool flag, int p)
    {
        int z1 = 1, _ = M2();

        // Multiple unused variable declarations (x and y) moved below to start of if-else block
        // Used declaration (z1) and evaluation (_ = M2()) retained.
        // Completely unused declaration (z2) removed.
        int x;
        int y;
        if (flag)
        {
            x = 1;
            y = 1;
        }
        else
        {
            x = 2;
            y = 2;
        }

        return x + y + z1;
    }

    int M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Fact]
        public async Task NonConstantValue_Trivia_PreferDiscard_01()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        // C1
        [|int x = M2()|], y = M2();   // C2
        // C3

        return y;
    }

    int M2() => 0;
}",
@"class C
{
    int M()
    {
        // C1
        _ = M2();
        // C1
        int y = M2();   // C2
        // C3

        return y;
    }

    int M2() => 0;
}", options: PreferDiscard);
        }

        [Fact]
        public async Task NonConstantValue_Trivia_PreferDiscard_02()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        /*C1*/
        /*C2*/[|int/*C3*/ /*C4*/x/*C5*/ = /*C6*/M2()|]/*C7*/, y/*C8*/ = M2()/*C9*/;   // C10
        /*C11*/

        return y;
    }

    int M2() => 0;
}",
@"class C
{
    int M()
    {
        /*C1*/
        /*C2*//*C3*/ /*C4*/
        _/*C5*/ = /*C6*/M2()/*C7*/;
        /*C1*/
        /*C2*/
        int/*C3*/ /*C4*/y/*C8*/ = M2()/*C9*/;   // C10
        /*C11*/

        return y;
    }

    int M2() => 0;
}", options: PreferDiscard);
        }

        [Fact]
        public async Task NonConstantValue_Trivia_PreferUnusedLocal_01()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        // C1
        [|int x = M2()|], y = M2();   // C2
        // C3

        // C4
        x = 1;
        return x + y;
    }

    int M2() => 0;
}",
@"class C
{
    int M()
    {
        // C1
        int unused = M2(), y = M2();   // C2
        // C3

        // C4
        int x = 1;
        return x + y;
    }

    int M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Fact]
        public async Task NonConstantValue_Trivia_PreferUnusedLocal_02()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        /*C1*/
        /*C2*/[|int/*C3*/ /*C4*/x/*C5*/ = /*C6*/M2()|]/*C7*/, y/*C8*/ = M2()/*C9*/;   // C10
        /*C11*/

        // C12
        x = 1;
        return x + y;
    }

    int M2() => 0;
}",
@"class C
{
    int M()
    {
        /*C1*/
        /*C2*/
        int/*C3*/ /*C4*/unused/*C5*/ = /*C6*/M2()/*C7*/, y/*C8*/ = M2()/*C9*/;   // C10
        /*C11*/

        // C12
        int x = 1;
        return x + y;
    }

    int M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task ConstantValue_Trivia_01(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        // C1
        [|int x = 0|], y = M2();   // C2
        // C3

        // C4
        x = 1;
        return x + y;
    }

    int M2() => 0;
}",
@"class C
{
    int M()
    {
        // C1
        int y = M2();   // C2
        // C3

        // C4
        int x = 1;
        return x + y;
    }

    int M2() => 0;
}", optionName);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task ConstantValue_Trivia_02(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        /*C1*/
        /*C2*/[|int/*C3*/ /*C4*/x/*C5*/ = /*C6*/0|]/*C7*/, y/*C8*/ = M2()/*C9*/;   // C10
        /*C11*/

        // C12
        x = 1;
        return x + y;
    }

    int M2() => 0;
}",
@"class C
{
    int M()
    {
        /*C1*/
        /*C2*/
        int/*C3*/ /*C4*/y/*C8*/ = M2()/*C9*/;   // C10
        /*C11*/

        // C12
        int x = 1;
        return x + y;
    }

    int M2() => 0;
}", optionName);
        }

        [Fact]
        public async Task ExistingDiscardDeclarationInLambda_UseOutsideLambda()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        int [|x|] = M2();
        Action a = () =>
        {
            var _ = M2();
        };

        a();
    }

    int M2() => 0;
}",
@"using System;

class C
{
    void M()
    {
        _ = M2();
        Action a = () =>
        {
            _ = M2();
        };

        a();
    }

    int M2() => 0;
}", options: PreferDiscard);
        }

        [Fact]
        public async Task ExistingDiscardDeclarationInLambda_UseInsideLambda()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        Action a = () =>
        {
            int [|x|] = M2();
            var _ = M2();
        };

        a();
    }

    int M2() => 0;
}",
@"using System;

class C
{
    void M()
    {
        Action a = () =>
        {
            _ = M2();
            _ = M2();
        };

        a();
    }

    int M2() => 0;
}", options: PreferDiscard);
        }

        [Theory]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task ValueOverwrittenByOutVar_ConditionalAndExpression(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        int {|FixAllInDocument:x1|} = -1, x2 = -1;
        if (M2(x: out x1) &&
            M2(x: out x2))
        {
            x1 = 0;
            x2 = 0;
        }
        else
        {
            Console.WriteLine(x1);
        }

        Console.WriteLine(x1 + x2);
    }

    bool M2(out int x)
    {
        x = 0;
        return true;
    }
}",
@"using System;

class C
{
    void M()
    {
        int x2 = -1;
        int x1;
        if (M2(x: out x1) &&
            M2(x: out x2))
        {
            x1 = 0;
            x2 = 0;
        }
        else
        {
            Console.WriteLine(x1);
        }

        Console.WriteLine(x1 + x2);
    }

    bool M2(out int x)
    {
        x = 0;
        return true;
    }
}", optionName);
        }

        [Theory]
        [InlineData("var")]
        [InlineData("int")]
        public async Task UnusedOutVariableDeclaration_PreferDiscard(string typeName)
        {
            await TestInRegularAndScriptAsync(
$@"class C
{{
    void M()
    {{
        if (M2(out {typeName} [|x|]))
        {{
        }}
    }}

    bool M2(out int x)
    {{
        x = 0;
        return true;
    }}
}}",
@"class C
{
    void M()
    {
        if (M2(out _))
        {
        }
    }

    bool M2(out int x)
    {
        x = 0;
        return true;
    }
}", options: PreferDiscard);
        }

        [Fact]
        public async Task UnusedOutVariableDeclaration_MethodOverloads_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        if (M2(out int [|x|]))
        {
        }
    }

    bool M2(out int x)
    {
        x = 0;
        return true;
    }

    bool M2(out char x)
    {
        x = 'c';
        return true;
    }
}",
@"class C
{
    void M()
    {
        if (M2(out int _))
        {
        }
    }

    bool M2(out int x)
    {
        x = 0;
        return true;
    }

    bool M2(out char x)
    {
        x = 'c';
        return true;
    }
}", options: PreferDiscard);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/31583")]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task MissingImports(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        List<int> [|x|] = null;
    }
}", optionName);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/31583")]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UsedAssignment_ConditionalPreprocessorDirective(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"#define DEBUG

class C
{
    int M()
    {
        int [|x|] = 0;
#if DEBUG
        x = 1;
#endif
        return x;
    }
}", optionName);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/32855")]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task RefLocalInitialization(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class Test
{
  int[] data = { 0 };

  void Method()
  {
    ref int [|target|] = ref data[0];
    target = 1;
  }
}", optionName);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/32855")]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task RefLocalAssignment(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class Test
{
  int[] data = { 0 };

  int Method()
  {
    ref int target = ref data[0];
    [|target|] = 1;
    return data[0];
  }
}", optionName);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/32903")]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task DelegateCreationWrappedInATuple_UsedInReturnedLambda(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

public class C
{
    private (int, int) createTuple() => (1, 1);

    public (Func<int>, bool) M()
    {
        var ([|value1, value2|]) = createTuple();

        int LocalFunction() => value1 + value2;

        return (LocalFunction, true);
    }
}", optionName);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32923")]
        public async Task UnusedLocal_ForEach()
        {
            await TestDiagnosticsAsync(
@"using System;

public struct S
{
    public Enumerator GetEnumerator() => throw new NotImplementedException();

    public struct Enumerator
    {
        public Enumerator(S sequence) => throw new NotImplementedException();
        public int Current => throw new NotImplementedException();
        public bool MoveNext() => throw new NotImplementedException();
    }
}

class C
{
    void M(S s)
    {
        foreach (var [|x|] in s)
        {
        }
    }
}", new TestParameters(options: PreferDiscard, retainNonFixableDiagnostics: true),
    Diagnostic("IDE0059"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60030")]
        public async Task UnusedLocal_ForEach_TopLevelStatement()
        {
            await TestMissingInRegularAndScriptAsync(
@"var items = new[] { new { x = 1 } };

foreach (var [|item|] in items)
{
}", PreferDiscard, new CSharpParseOptions(LanguageVersion.CSharp9));
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/32923")]
        [InlineData("_", nameof(PreferDiscard))]
        [InlineData("_", nameof(PreferUnusedLocal))]
        [InlineData("_1", nameof(PreferDiscard))]
        [InlineData("_1", nameof(PreferUnusedLocal))]
        public async Task UnusedLocal_SpecialName_01(string variableName, string optionName)
        {
            await TestDiagnosticMissingAsync(
$@"using System;

public struct S
{{
    public Enumerator GetEnumerator() => throw new NotImplementedException();

    public struct Enumerator
    {{
        public Enumerator(S sequence) => throw new NotImplementedException();
        public int Current => throw new NotImplementedException();
        public bool MoveNext() => throw new NotImplementedException();
    }}
}}

class C
{{
    void M(S s)
    {{
        foreach (var [|{variableName}|] in s)
        {{
        }}
    }}
}}", new TestParameters(options: GetOptions(optionName), retainNonFixableDiagnostics: true));
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/32923")]
        [InlineData("_", nameof(PreferDiscard))]
        [InlineData("_", nameof(PreferUnusedLocal))]
        [InlineData("_3", nameof(PreferDiscard))]
        [InlineData("_3", nameof(PreferUnusedLocal))]
        public async Task UnusedLocal_SpecialName_02(string variableName, string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
$@"using System;

public class C
{{
    public void M(int p)
    {{
        var [|{variableName}|] = p;
    }}
}}", optionName);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32959")]
        public async Task UsedVariable_BailOutOnSemanticError()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int [|x|] = 1;
        
        // CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type.
        Invoke<string>(() => x);

        T Invoke<T>(Func<T> a) { return a(); }
    }
}", options: PreferDiscard);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32959")]
        public async Task UnusedVariable_BailOutOnSemanticError()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int [|x|] = 1;
        
        // CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type.
        Invoke<string>(() => 0);

        T Invoke<T>(Func<T> a) { return a(); }
    }
}", options: PreferDiscard);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32946")]
        public async Task DelegateEscape_01()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    Action[] M()
    {
        var [|j|] = 0;
        return new Action[1] { () => Console.WriteLine(j) };
    }
}", options: PreferDiscard);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32946")]
        public async Task DelegateEscape_02()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    Action[] M(Action[] actions)
    {
        var [|j|] = 0;
        actions[0] = () => Console.WriteLine(j);
        return actions;
    }
}", options: PreferDiscard);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32946")]
        public async Task DelegateEscape_03()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    Action[,] M(Action[,] actions)
    {
        var [|j|] = 0;
        actions[0, 0] = () => Console.WriteLine(j);
        return actions;
    }
}", options: PreferDiscard);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32946")]
        public async Task DelegateEscape_04()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class C
{
    List<Action> M()
    {
        var [|j|] = 0;
        return new List<Action> { () => Console.WriteLine(j) };
    }
}", options: PreferDiscard);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32946")]
        public async Task DelegateEscape_05()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class C
{
    List<Action> M()
    {
        var [|j|] = 0;
        var list = new List<Action>();
        list.Add(() => Console.WriteLine(j));
        return list;
    }
}", options: PreferDiscard);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32924")]
        public async Task DelegateEscape_06()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        int [|j|] = 0;
        Console.CancelKeyPress += (s, e) => e.Cancel = j != 0;
    }
}", options: PreferDiscard);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32924")]
        public async Task DelegateEscape_07()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        int [|j|] = 0;
        Console.CancelKeyPress += LocalFunctionHandler;
        return;

        void LocalFunctionHandler(object s, ConsoleCancelEventArgs e) => e.Cancel = j != 0;
    }
}", options: PreferDiscard);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32856")]
        public async Task RedundantAssignment_IfStatementParent()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int j)
    {
        if (M2())
            [|j|] = 0;
    }

    bool M2() => true;
}",
@"class C
{
    void M(int j)
    {
        if (M2())
            _ = 0;
    }

    bool M2() => true;
}", options: PreferDiscard);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32856")]
        public async Task RedundantAssignment_LoopStatementParent()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int j, int[] array)
    {
        for (int i = 0; i < array.Length; i++)
            [|j|] = i;
    }
}",
@"class C
{
    void M(int j, int[] array)
    {
        for (int i = 0; i < array.Length; i++)
            _ = i;
    }
}", options: PreferDiscard);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40336")]
        public async Task RedundantAssignment_ForStatementVariableDeclarationConstant()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        for (int [|i|] = 0; ; ) 
        {
        }
    }
}",
@"class C
{
    void M()
    {
        for (; ; ) 
        {
        }
    }
}", options: PreferDiscard);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40336")]
        public async Task RedundantAssignment_ForStatementVariableDeclarationMethod()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int GetValue() => 0;

    void M()
    {
        for (int [|i|] = GetValue(); ; )
        {
        }
    }
}",
@"class C
{
    int GetValue() => 0;

    void M()
    {
        for (int _ = GetValue(); ; )
        {
        }
    }
}", options: PreferDiscard);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40336")]
        public async Task RedundantAssignment_ForStatementVariableDeclarationStaticMethod()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    static int GetValue() => 0;

    void M()
    {
        for (int [|i|] = GetValue(); ; )
        {
        }
    }
}",
@"class C
{
    static int GetValue() => 0;

    void M()
    {
        for (int _ = GetValue(); ; )
        {
        }
    }
}", options: PreferDiscard);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40336")]
        public async Task RedundantAssignment_ForStatementVariableDeclarationInsideUsedLambda()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        Action a = () =>
        {
            for (int [|i|] = 0; ; )
            {
            }
        };
        a();
    }
}",
@"using System;

class C
{
    void M()
    {
        Action a = () =>
        {
            for (; ; )
            {
            }
        };
        a();
    }
}", options: PreferDiscard);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40336")]
        public async Task RedundantAssignment_ForStatementVariableDeclarationInsideUnusedLambda()
        {
            //NOTE: Currently the diagnostic is only reported on the outer unused variable a. 
            await TestDiagnosticMissingAsync(
@"using System;

class C
{
    void M()
    {
        Action a = () =>
        {
            for (int [|i|] = 0; ; )
            {
            }
        };
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33299")]
        public async Task NullCoalesceAssignment_01()
        {
            await TestMissingInRegularAndScriptWithAllOptionsAsync(
@"class C
{
    public static void M(C x)
    {
        [|x|] = M2();
        x ??= new C();
    }

    private static C M2() => null;
}
", parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33299")]
        public async Task NullCoalesceAssignment_02()
        {
            await TestMissingInRegularAndScriptWithAllOptionsAsync(
@"class C
{
    public static C M(C x)
    {
        [|x|] ??= new C();
        return x;
    }
}
", parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33299")]
        public async Task NullCoalesceAssignment_03()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public static void M(C x)
    {
        [|x|] ??= new C();
    }
}
",
@"class C
{
    public static void M(C x)
    {
        _ = x ?? new C();
    }
}
", optionName: nameof(PreferDiscard), parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33299")]
        public async Task NullCoalesceAssignment_04()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public static C M(C x)
    {
        return [|x|] ??= new C();
    }
}
",
@"class C
{
    public static C M(C x)
    {
        return x ?? new C();
    }
}
", optionName: nameof(PreferDiscard), parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33299")]
        public async Task NullCoalesceAssignment_05()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public static C M(C x)
        => [|x|] ??= new C();
}
",
@"class C
{
    public static C M(C x)
        => x ?? new C();
}
", optionName: nameof(PreferDiscard), parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8));
        }

        [Fact, WorkItem(32856, "https://github.com/dotnet/roslyn/issues/33312")]
        public async Task RedundantAssignment_WithLeadingAndTrailingComment()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        // This is a comment before the variable assignment.
        // It has two lines.
        [|int foo = 0;|] // Trailing comment.
        if (true)
        {
            foo = 1;
        }
        System.Console.WriteLine(foo);
    }
}",
@"class C
{
    void M()
    {
        // This is a comment before the variable assignment.
        // It has two lines.
        int foo;
        if (true)
        {
            foo = 1;
        }
        System.Console.WriteLine(foo);
    }
}", options: PreferUnusedLocal);
        }

        [Fact, WorkItem(32856, "https://github.com/dotnet/roslyn/issues/33312")]
        public async Task MultipleRedundantAssignment_WithLeadingAndTrailingComment()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        // This is a comment before the variable assignment.
        // It has two lines.
        {|FixAllInDocument:int unused = 0, foo = 0, bar = 0;|} // Trailing comment.
        if (true)
        {
            foo = 1;
            bar = 1;
        }
        System.Console.WriteLine(foo);
        System.Console.WriteLine(bar);
    }
}",
@"class C
{
    void M()
    {
        // This is a comment before the variable assignment.
        // It has two lines.
        int foo;
        int bar;
        if (true)
        {
            foo = 1;
            bar = 1;
        }
        System.Console.WriteLine(foo);
        System.Console.WriteLine(bar);
    }
}", options: PreferUnusedLocal);
        }

        [Fact, WorkItem(32856, "https://github.com/dotnet/roslyn/issues/33312")]
        public async Task MultipleRedundantAssignment_WithInnerComment()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        {|FixAllInDocument:int unused = 0, /*Comment*/foo = 0, /*Another comment*/ bar = 0;|}
        if (true)
        {
            foo = 1;
        }
        System.Console.WriteLine(foo);
    }
}",
@"class C
{
    void M()
    {
        int foo;
        if (true)
        {
            foo = 1;
        }
        System.Console.WriteLine(foo);
    }
}", options: PreferUnusedLocal);
        }

        [Fact, WorkItem(32856, "https://github.com/dotnet/roslyn/issues/33312")]
        public async Task DeclarationPatternInSwitchCase_WithTrivia_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object p)
    {
        switch (p)
        {
            case /*Inline trivia*/ int [|x|]:
                // Other trivia
                x = 1;
                break;
        };
    }
}",
@"class C
{
    void M(object p)
    {
        switch (p)
        {
            case /*Inline trivia*/ int _:
                // Other trivia
                int x = 1;
                break;
        };
    }
}", options: PreferDiscard, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8));
        }

        [Theory, WorkItem(32856, "https://github.com/dotnet/roslyn/issues/33312")]
        [CombinatorialData]
        public async Task DeclarationPatternInSwitchCase_WithTrivia_PreferUnusedLocal(
            [CombinatorialValues(LanguageVersion.CSharp8, LanguageVersion.CSharp9)] LanguageVersion languageVersion)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(object p)
    {
        switch (p)
        {
            case /*Inline trivia*/ int [|x|]:
                // Other trivia
                x = 1;
                break;
        };
    }
}", PreferUnusedLocal, parseOptions: new CSharpParseOptions(languageVersion));
        }

        [Theory, WorkItem(32856, "https://github.com/dotnet/roslyn/issues/33312")]
        [CombinatorialData]
        public async Task DeclarationPatternInSwitchCase_WithTrivia_TypePattern(
            [CombinatorialValues(CodeFixTestBehaviors.None, CodeFixTestBehaviors.FixOne)] CodeFixTestBehaviors testBehaviors)
        {
            var source =
@"class C
{
    void M(object p)
    {
        switch (p)
        {
            case /*Inline trivia*/ int {|IDE0059:x|}:
                // Other trivia
                {|IDE0059:x|} = 1;
                break;
        };
    }
}";
            var (fixedSource, iterations) = testBehaviors switch
            {
                CodeFixTestBehaviors.None =>
(@"class C
{
    void M(object p)
    {
        switch (p)
        {
            case /*Inline trivia*/ int:
                // Other trivia
                break;
        };
    }
}", iterations: 2),
                CodeFixTestBehaviors.FixOne =>
(@"class C
{
    void M(object p)
    {
        switch (p)
        {
            case /*Inline trivia*/ int:
                // Other trivia
                int {|IDE0059:x|} = 1;
                break;
        };
    }
}", iterations: 1),
                _ => throw ExceptionUtilities.Unreachable(),
            };

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedState = { Sources = { fixedSource }, MarkupHandling = MarkupMode.Allow },
                LanguageVersion = LanguageVersion.CSharp9,
                CodeFixTestBehaviors = testBehaviors,
                NumberOfIncrementalIterations = iterations,
                NumberOfFixAllIterations = iterations,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, UnusedValuePreference.DiscardVariable },
                },
            }.RunAsync();
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/33949")]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UsedInArgumentAfterAnArgumentWithControlFlow(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class A
{
    public static void M(int? x)
    {
        A [|a|] = new A();
        a = M2(x ?? 1, a);
    }

    private static A M2(int? x, A a)
    {
        return a;
    }
}", optionName);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/33949")]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task CompoundAssignmentWithControlFlowInValue(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class A
{
    public static void M(int? x)
    {
        int [|a|] = 1;
        a += M2(x ?? 1);
    }

    private static int M2(int? x) => 0;
}", optionName);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/33843")]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UsedValueWithUsingStatementAndLocalFunction(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    private class Disposable : IDisposable { public void Dispose() { } }
    public int M()
    {
        var result = 0;
        void append() => [|result|] += 1; // IDE0059 for 'result'
        using (var a = new Disposable())
            append();
        return result;
    }
}", optionName);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/33843")]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UsedValueWithUsingStatementAndLambda(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    private class Disposable : IDisposable { public void Dispose() { } }
    public int M()
    {
        var result = 0;
        Action append = () => [|result|] += 1; // IDE0059 for 'result'
        using (var a = new Disposable())
            append();
        return result;
    }
}", optionName);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/33843")]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UsedValueWithUsingStatementAndLambda_02(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    private class Disposable : IDisposable { public void Dispose() { } }
    public int M()
    {
        var result = 0;
        Action appendLambda = () => [|result|] += 1;
        void appendLocalFunction() => appendLambda();
        Action appendDelegate = appendLocalFunction;
        using (var a = new Disposable())
            appendDelegate();
        return result;
    }
}", optionName);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/33843")]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UsedValueWithUsingStatementAndLambda_03(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    private class Disposable : IDisposable { public void Dispose() { } }
    public int M()
    {
        var result = 0;
        void appendLocalFunction() => [|result|] += 1;
        Action appendLambda = () => appendLocalFunction();
        Action appendDelegate = appendLambda;
        using (var a = new Disposable())
            appendDelegate();
        return result;
    }
}", optionName);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/33937")]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInCatchUsedInFinally_ThrowInCatch(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

public static class Program
{
    public static void Test()
    {
        var exceptionThrown = false;
        try
        {
            throw new Exception();
        }
        catch
        {
            // The `exceptionThrown` token is incorrectly greyed out in the IDE
            // IDE0059 Value assigned to 'exceptionThrown' is never used
            [|exceptionThrown|] = true;
            throw;
        }
        finally
        {
            // Breakpoint on this line is hit and 'true' is printed
            Console.WriteLine(exceptionThrown);
        }
    }
}", optionName);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/33937")]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInCatchUsedInFinally_NoThrowInCatch(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

public static class Program
{
    public static void Test()
    {
        var exceptionThrown = false;
        try
        {
            throw new Exception();
        }
        catch
        {
            [|exceptionThrown|] = true;
        }
        finally
        {
            Console.WriteLine(exceptionThrown);
        }
    }
}", optionName);
        }

        [Fact]
        public async Task DoesNotUseLocalFunctionName_PreferUnused()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int [|x|] = M2();
        x = 2;
        return x;

        void unused() { }
    }

    int M2() => 0;
}",
@"class C
{
    int M()
    {
        int unused1 = M2();
        int x = 2;
        return x;

        void unused() { }
    }

    int M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Fact]
        public async Task CanUseLocalFunctionParameterName_PreferUnused()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int [|x|] = M2();
        x = 2;
        return x;

        void MLocal(int unused) { }
    }

    int M2() => 0;
}",
@"class C
{
    int M()
    {
        int unused = M2();
        int x = 2;
        return x;

        void MLocal(int unused) { }
    }

    int M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Fact]
        public async Task DoesNotUseLambdaFunctionParameterNameWithCSharpLessThan8_PreferUnused()
        {
            await TestInRegularAndScriptAsync(
@"
using System;
class C
{
    int M()
    {
        int [|x|] = M2();
        x = 2;
        Action<int> myLambda = unused => { };

        return x;
    }

    int M2() => 0;
}",
@"
using System;
class C
{
    int M()
    {
        int unused1 = M2();
        int x = 2;
        Action<int> myLambda = unused => { };

        return x;
    }

    int M2() => 0;
}", options: PreferUnusedLocal, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7_3));
        }

        [Fact]
        public async Task CanUseLambdaFunctionParameterNameWithCSharp8_PreferUnused()
        {
            await TestInRegularAndScriptAsync(
@"
using System;
class C
{
    int M()
    {
        int [|x|] = M2();
        x = 2;
        Action<int> myLambda = unused => { };

        return x;
    }

    int M2() => 0;
}",
@"
using System;
class C
{
    int M()
    {
        int unused = M2();
        int x = 2;
        Action<int> myLambda = unused => { };

        return x;
    }

    int M2() => 0;
}", options: PreferUnusedLocal, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33464")]
        public async Task UsingDeclaration()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    void M()
    {
        using var [|x|] = new C();
    }
}", options: PreferDiscard,
    parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33464")]
        public async Task UsingDeclarationWithInitializer()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C : IDisposable
{
    public int P { get; set; }
    public void Dispose()
    {
    }

    void M()
    {
        using var [|x|] = new C() { P = 1 };
    }
}", options: PreferDiscard,
    parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37709")]
        public async Task RefParameter_WrittenBeforeThrow()
        {
            await TestDiagnosticMissingAsync(
@"using System;

class C
{
    public void DoSomething(ref bool p)
    {
        if (p)
        {
            [|p|] = false;
            throw new ArgumentException(string.Empty);
        }
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37709")]
        public async Task OutParameter_WrittenBeforeThrow()
        {
            await TestDiagnosticMissingAsync(
@"using System;

class C
{
    public void DoSomething(out bool p, bool x)
    {
        if (x)
        {
            [|p|] = false;
            throw new ArgumentException(string.Empty);
        }
        else
        {
            p = true;
        }
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37871")]
        public async Task RefParameter_RefAssignmentFollowedByAssignment()
        {
            await TestDiagnosticMissingAsync(
@"using System;

class C
{
    delegate ref int UnsafeAdd(ref int source, int elementOffset);
    static UnsafeAdd MyUnsafeAdd;
    
    static void T1(ref int param)
    {
        [|param|] = ref MyUnsafeAdd(ref param, 1);
        param = default;
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37871")]
        public async Task RefParameter_RefConditionalAssignment()
        {
            await TestDiagnosticMissingAsync(
@"using System;

class C
{
    delegate ref int UnsafeAdd(ref int source, int elementOffset);
    static UnsafeAdd MyUnsafeAdd;

    static void T1(ref int param, bool flag)
    {
        [|param|] = flag ? ref MyUnsafeAdd(ref param, 1) : ref MyUnsafeAdd(ref param, 2);
        param = default;
    }
}");
        }

        [Fact]
        public async Task LocalFunction_OutParameter_UsedInCaller()
        {
            await TestDiagnosticMissingAsync(
@"
public class C
{
    public void M()
    {
        if (GetVal(out var [|value|]))
        {
            var x = value;
        }

        bool GetVal(out string val)
        {
            val = string.Empty;
            return true;
        }
    }
}");
        }

        [Fact]
        public async Task TupleMember_UsedAfterContinueBranch()
        {
            await TestDiagnosticMissingAsync(
@"
using System;
using System.Collections.Generic;

public class Test
{
    void M(List<(int, int)> list)
    {
        foreach (var (x, [|y|]) in list)
        {
            if (x != 0)
            {
                continue;
            }

            Console.Write(y);
        }
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38640")]
        public async Task DeclarationPatternInSwitchExpressionArm_UsedLocal()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    string M(object obj)
    {
        return obj switch
        {
            int [|p2|] => p2.ToString(),
            _ => ""NoMatch""
        };
    }
}", new TestParameters(options: PreferDiscard, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8)));
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/38640")]
        [CombinatorialData]
        public async Task DeclarationPatternInSwitchExpressionArm_UnusedLocal_PreferUnusedLocal(
            [CombinatorialValues(LanguageVersion.CSharp8, LanguageVersion.CSharp9)] LanguageVersion languageVersion)
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    string M(object obj)
    {
        return obj switch
        {
            int [|p2|] => ""Int"",
            _ => ""NoMatch""
        };
    }
}", new TestParameters(options: PreferUnusedLocal, parseOptions: new CSharpParseOptions(languageVersion)));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40499")]
        public async Task LocalUsedWithPropertySubPattern()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    public object P { get; }
    void M()
    {
        C [|c|] = new C();
        var x = c is { P : int i };
    }
}", new TestParameters(options: PreferDiscard, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8)));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40499")]
        public async Task UnusedLocalDefinedInPropertySubPattern_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public object P { get; }
    void M(C c)
    {
        var x = c is { P : int [|i|] };
    }
}",
@"class C
{
    public object P { get; }
    void M(C c)
    {
        var x = c is { P : int _ };
    }
}", options: PreferDiscard, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40499")]
        public async Task UnusedLocalDefinedInPropertySubPattern_TypePattern()
        {
            var source =
@"class C
{
    public object P { get; }
    bool M(C c)
    {
        var x = c is { P : int {|IDE0059:i|} };
        return x;
    }
}";
            var fixedSource =
@"class C
{
    public object P { get; }
    bool M(C c)
    {
        var x = c is { P : int };
        return x;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                LanguageVersion = LanguageVersion.CSharp9,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, UnusedValuePreference.DiscardVariable },
                },
            }.RunAsync();
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/40499")]
        [CombinatorialData]
        public async Task UnusedVarLocalDefinedInPropertySubPattern_PreferDiscard(
            [CombinatorialValues(LanguageVersion.CSharp8, LanguageVersion.CSharp9)] LanguageVersion languageVersion)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public object P { get; }
    void M(C c)
    {
        var x = c is { P : var [|i|] };
    }
}",
@"class C
{
    public object P { get; }
    void M(C c)
    {
        var x = c is { P : _ };
    }
}", options: PreferDiscard, parseOptions: new CSharpParseOptions(languageVersion));
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/40499")]
        [CombinatorialData]
        public async Task UnusedLocalDefinedInPropertySubPattern_PreferUnusedLocal(
            [CombinatorialValues(LanguageVersion.CSharp8, LanguageVersion.CSharp9)] LanguageVersion languageVersion)
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    public object P { get; }
    void M(C c)
    {
        var x = c is { P : int [|i|] };
    }
}", new TestParameters(options: PreferUnusedLocal, parseOptions: new CSharpParseOptions(languageVersion)));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38640")]
        public async Task DeclarationPatternInSwitchExpressionArm_UnusedLocal_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    string M(object obj)
    {
        return obj switch
        {
            int [|p2|] => ""Int"",
            _ => ""NoMatch""
        };
    }
}",
@"class C
{
    string M(object obj)
    {
        return obj switch
        {
            int _ => ""Int"",
            _ => ""NoMatch""
        };
    }
}", options: PreferDiscard, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38640")]
        public async Task DeclarationPatternInSwitchExpressionArm_UnusedLocal_TypePattern()
        {
            var source =
@"class C
{
    string M(object obj)
    {
        return obj switch
        {
            int {|IDE0059:p2|} => ""Int"",
            _ => ""NoMatch""
        };
    }
}";
            var fixedSource =
@"class C
{
    string M(object obj)
    {
        return obj switch
        {
            int => ""Int"",
            _ => ""NoMatch""
        };
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                LanguageVersion = LanguageVersion.CSharp9,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, UnusedValuePreference.DiscardVariable },
                },
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39344")]
        public async Task AssignmentInTry_UsedInFinally_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(
@"using System;

class C
{
    void M(int i)
    {
        bool b = false;
        try
        {
            if (i == 0)
            {
                [|b|] = true;
            }
        }
        finally
        {
            if (!b)
            {
                Console.WriteLine(i);
            }
        }
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39755")]
        public async Task AssignmentInTry_UsedInFinally_NoDiagnostic_02()
        {
            await TestDiagnosticMissingAsync(
@"using System;

class C
{
    void M()
    {
        IntPtr a = (IntPtr)1;
        try
        {
            var b = a;

            if (Some(a))
                [|a|] = IntPtr.Zero;
        }
        finally
        {
            if (a != IntPtr.Zero)
            {

            }
        }
    }

    bool Some(IntPtr a) => true;
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39755")]
        public async Task AssignmentInTry_NotUsedInFinally_Diagnostic()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int i)
    {
        bool b = false;
        try 
        { 
            if (i == 0)
            {
                [|b|] = true;
            }
        }
        finally 
        {
        }
    }
}",
@"using System;

class C
{
    void M(int i)
    {
        bool b = false;
        try 
        { 
            if (i == 0)
            {
            }
        }
        finally 
        {
        }
    }
}", options: PreferDiscard);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38507")]
        public async Task TestCodeFixTitleForBlockBodyRedundantCompoundAssignmentReturn()
        {
            var source = @"
class C
{
    C M(C x)
    {
        return [|x ??= M2()|];
    }

    C M2() => new C();
}
";

            await TestExactActionSetOfferedAsync(source, new[] { CodeFixesResources.Remove_redundant_assignment });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38507")]
        public async Task TestCodeFixTitleForExpressionBodyRedundantCompoundAssignmentReturn()
        {
            var source = @"
class C
{
    C M(C x) => [|x ??= M2()|];

    C M2() => new C();
}
";
            await TestExactActionSetOfferedAsync(source, new[] { CodeFixesResources.Remove_redundant_assignment });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38507")]
        public async Task TestCodeFixTitleForPatternMatching()
        {
            var source = @"
class C
{
    void M()
    {
        var c = M2();
        if [|(c is object obj)|]
        {
        }
    }

    C M2() => new C();
}
";

            await TestExactActionSetOfferedAsync(source, new[] { CodeFixesResources.Remove_redundant_assignment });
        }

        [Fact, WorkItem(38507, "https://github.com/dotnet/roslyn/issues/46251")]
        public async Task TestCodeFixForAllInDocumentForNestedDiagnostic()
        {
            var source = @"
using System;
namespace ConsoleApp
{
	public static class ConsoleApp
    {
		public static void Main(string[] args)
        {
            {|FixAllInDocument:Foo(() => { Bar(); return true; })|};
        }

        public static bool Foo(Func<bool> func)
        {
            return func. Invoke();
        }

        public static bool Bar()
        {
            return true;
        }
	}
}";
            var expected = @"
using System;
namespace ConsoleApp
{
	public static class ConsoleApp
    {
		public static void Main(string[] args)
        {
            _ = Foo(() => { _ = Bar(); return true; });
        }

        public static bool Foo(Func<bool> func)
        {
            return func. Invoke();
        }

        public static bool Bar()
        {
            return true;
        }
	}
}";
            await TestInRegularAndScriptAsync(source, expected, options: PreferDiscard).ConfigureAwait(false);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45768")]
        public async Task UnusedVarPattern_PartOfCase()
        {
            await TestInRegularAndScriptAsync(
@"static class Program
{
    public static void Main()
    {
        switch (string.Empty.Length)
        {
            case var [|i|] when string.Empty.Length switch { var y => y > 0 }:
            {
                break;
            }
        }
    }
}",
@"static class Program
{
    public static void Main()
    {
        switch (string.Empty.Length)
        {
            case var _ when string.Empty.Length switch { var y => y > 0 }:
            {
                break;
            }
        }
    }
}", options: PreferDiscard);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45768")]
        public async Task UnusedVarPattern_PartOfIs()
        {
            await TestInRegularAndScriptAsync(
@"static class Program
{
    public static void Main()
    {
        if (string.Empty.Length is var [|x|])
        {
        }
    }
}",
@"static class Program
{
    public static void Main()
    {
        if (string.Empty.Length is var _)
        {
        }
    }
}", options: PreferDiscard);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45768")]
        public async Task UnusedVarPattern_TestTrivia()
        {
            await TestInRegularAndScriptAsync(
@"static class Program
{
    public static void Main()
    {
        if (string.Empty.Length is var [|/*1*/x/*2*/|])
        {
        }
    }
}",
@"static class Program
{
    public static void Main()
    {
        if (string.Empty.Length is var /*1*/_/*2*/)
        {
        }
    }
}", options: PreferDiscard);
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/57650")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_WithInvocationOutsideLocalScope(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        Action act = null;
        {
            var[| capture |] = new object();
            act = () => capture.ToString();
        }
        act();
    }
}", optionName);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/64291")]
        public async Task TestImplicitObjectCreationInInitialization()
        {
            var source =
@"class C
{
    void M()
    {
        C {|IDE0059:c|} = new();
    }
}";
            var fixedSource =
@"class C
{
    void M()
    {
        _ = new C();
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, UnusedValuePreference.DiscardVariable },
                },
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64291")]
        public async Task TestImplicitObjectCreationInAssignment()
        {
            var source =
@"class C
{
    void M(C c)
    {
        System.Console.WriteLine(c);
        {|IDE0059:c|} = new();
    }
}";
            var fixedSource =
@"class C
{
    void M(C c)
    {
        System.Console.WriteLine(c);
        _ = new C();
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, UnusedValuePreference.DiscardVariable },
                },
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66573")]
        public async Task TestPropertyPatternAssignment1()
        {
            var source = """
                class C
                {
                    void M(object obj)
                    {
                        if (obj is string { } {|IDE0059:str|})
                        {

                        }
                    }
                }
                """;

            var fixedSource = """
                class C
                {
                    void M(object obj)
                    {
                        if (obj is string { })
                        {

                        }
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, UnusedValuePreference.DiscardVariable },
                },
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66573")]
        public async Task TestPropertyPatternAssignment2()
        {
            var source = """
                class C
                {
                    void M(object obj)
                    {
                        if (obj is string { Length: > 0 } {|IDE0059:str|})
                        {

                        }
                    }
                }
                """;

            var fixedSource = """
                class C
                {
                    void M(object obj)
                    {
                        if (obj is string { Length: > 0 })
                        {

                        }
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, UnusedValuePreference.DiscardVariable },
                },
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66573")]
        public async Task TestPropertyPatternAssignment3()
        {
            var source = """
                class C
                {
                    void M(object obj)
                    {
                        if (obj is string { Length: { } {|IDE0059:length|} })
                        {

                        }
                    }
                }
                """;

            var fixedSource = """
                class C
                {
                    void M(object obj)
                    {
                        if (obj is string { Length: { } })
                        {

                        }
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, UnusedValuePreference.DiscardVariable },
                },
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66573")]
        public async Task TestPropertyPatternAssignment4()
        {
            var source = """
                class C
                {
                    void M(object obj)
                    {
                        if (obj is string { Length: { } {|IDE0059:length|} } {|IDE0059:str|})
                        {

                        }
                    }
                }
                """;

            var fixedSource = """
                class C
                {
                    void M(object obj)
                    {
                        if (obj is string { Length: { } })
                        {

                        }
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, UnusedValuePreference.DiscardVariable },
                },
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66573")]
        public async Task TestListPatternAssignment1()
        {
            var source = """
                class C
                {
                    void M(string s)
                    {
                        if (s is [] {|IDE0059:str|})
                        {

                        }
                    }
                }
                """;

            var fixedSource = """
                class C
                {
                    void M(string s)
                    {
                        if (s is [])
                        {

                        }
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, UnusedValuePreference.DiscardVariable },
                },
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66573")]
        public async Task TestListPatternAssignment2()
        {
            var source = """
                class C
                {
                    void M(string[] ss)
                    {
                        if (ss is [[] {|IDE0059:str|}])
                        {

                        }
                    }
                }
                """;

            // Formatter bug tracked in https://github.com/dotnet/roslyn/issues/67516
            var fixedSource = """
                class C
                {
                    void M(string[] ss)
                    {
                        if (ss is [ []])
                        {

                        }
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, UnusedValuePreference.DiscardVariable },
                },
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66573")]
        public async Task TestListPatternAssignment3()
        {
            var source = """
                class C
                {
                    void M(string[] ss)
                    {
                        if (ss is [[] {|IDE0059:str|}] {|IDE0059:strings|})
                        {

                        }
                    }
                }
                """;

            // Formatter bug tracked in https://github.com/dotnet/roslyn/issues/67516
            var fixedSource = """
                class C
                {
                    void M(string[] ss)
                    {
                        if (ss is [ []])
                        {

                        }
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options =
                {
                    { CSharpCodeStyleOptions.UnusedValueAssignment, UnusedValuePreference.DiscardVariable },
                },
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
            }.RunAsync();
        }
    }
}
