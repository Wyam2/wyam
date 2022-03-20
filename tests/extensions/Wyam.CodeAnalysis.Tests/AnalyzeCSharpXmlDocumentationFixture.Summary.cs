using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using Shouldly;
using Wyam.Common.Documents;
using Wyam.Common.Meta;
using Wyam.Common.Modules;
using Wyam.Common.Execution;

namespace Wyam.CodeAnalysis.Tests
{
    public partial class AnalyzeCSharpXmlDocumentationFixture
    {
        public partial class ExecuteTests
        {
            [Test]
            public void Summary_OnClass_WithSingleLine()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>This is a summary.</summary>
                        class Green
                        {
                        }

                        /// <summary>This is another summary.</summary>
                        struct Red
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe("This is a summary.");
                GetResult(results, "Red")["Summary"].ShouldBe("This is another summary.");
            }

            [Test]
            public void Summary_OnClass_WithMultiLineText()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is a summary.
                        /// </summary>
                        class Green
                        {
                        }

                        /// <summary>
                        /// This is
                        /// another summary.
                        /// </summary>
                        struct Red
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe("\n    This is a summary.\n    ");
                GetResult(results, "Red")["Summary"].ShouldBe("\n    This is\n    another summary.\n    ");
            }

            [Test]
            public void Summary_OnClass_MultipleSummaryElements()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>This is a summary.</summary>
                        /// <summary>This is another summary.</summary>
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe("This is a summary.\nThis is another summary.");
            }

            [Test]
            public void Summary_OnClass_NoSummary()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe(string.Empty);
            }

            [Test]
            public void Summary_OnClass_WithCElement()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is <c>some code</c> in a summary.
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe("\n    This is <code>some code</code> in a summary.\n    ");
            }

            [Test]
            public void Summary_OnClass_WithCElement_AndInlineCssClass()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is <c class=""code"">some code</c> in a summary.
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe("\n    This is <code class=\"code\">some code</code> in a summary.\n    ");
            }

            [Test]
            public void Summary_OnClass_WithCElement_AndDeclaredCssClass()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is <c>some code</c> in a summary.
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp().WithCssClasses("code", "code");

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe("\n    This is <code class=\"code\">some code</code> in a summary.\n    ");
            }

            [Test]
            public void Summary_OnClass_WithCElement_AndInline_AndDeclaredCssClasses()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is <c class=""code"">some code</c> in a summary.
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp().WithCssClasses("code", "more-code");

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe("\n    This is <code class=\"code more-code\">some code</code> in a summary.\n    ");
            }

            [Test]
            public void Summary_OnClass_WithMultipleCElements()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is <c>some code</c> in <c>a</c> summary.
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe("\n    This is <code>some code</code> in <code>a</code> summary.\n    ");
            }

            [Test]
            public void Summary_OnClass_WithCodeElement()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is
                        /// <code>
                        /// with some code
                        /// </code>
                        /// a summary
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe("\n    This is\n    <pre><code>with some code</code></pre>\n    a summary\n    ");
            }

            [Test]
            public void Summary_OnClass_WithCodeElement_AndCElement()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is <c>some code</c> and
                        /// <code>
                        /// with some code
                        /// </code>
                        /// a summary
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"]
                    .ShouldBe("\n    This is <code>some code</code> and\n    <pre><code>with some code</code></pre>\n    a summary\n    ");
            }

            [Test]
            public void Summary_OnClass_WithMultipleCodeElements()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is
                        /// <code>
                        /// with some code
                        /// </code>
                        /// a summary
                        /// <code>
                        /// more code
                        /// </code>
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"]
                    .ShouldBe("\n    This is\n    <pre><code>with some code</code></pre>\n    a summary\n    <pre><code>more code</code></pre>\n    ");
            }

            [Test]
            public void Summary_OnPartialClasses()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is a summary repeated for each partial class
                        /// </summary>
                        partial class Green
                        {
                        }

                        /// <summary>
                        /// This is a summary repeated for each partial class
                        /// </summary>
                        partial class Green
                        {
                        }

                        /// <summary>
                        /// This is a summary repeated for each partial class
                        /// </summary>
                        partial class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe("\n    This is a summary repeated for each partial class\n    ");
            }
            
            [Test]
            public void Summary_OnClass_WithListElement_AndBulletType_WithOnlyStringItems()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is a summary.
                        /// <list type=""bullet"">
                        /// <item>x</item>
                        /// <item>y</item>
                        /// </list>
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe(
                    @"
                This is a summary.
                <ul>
                <li>x</li>
                <li>y</li>
                </ul>
                ".Replace("\r\n", "\n").Replace("                ", "    "));
            }

            [Test]
            public void Summary_OnClass_WithListElement_AndBulletType_WithListHeader()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is a summary.
                        /// <list type=""bullet"">
                        /// <listheader>
                        /// <term>A</term>
                        /// <description>a</description>
                        /// </listheader>
                        /// <item>
                        /// <term>X</term>
                        /// <description>x</description>
                        /// </item>
                        /// <item>
                        /// <term>Y</term>
                        /// <description>y</description>
                        /// </item>
                        /// </list>
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe(
                    @"
                This is a summary.
                <ul>
                <li>
                <span class=""term"">A</span><span> - </span>
                <span class=""description"">a</span>
                </li>
                <li>
                <span class=""term"">X</span><span> - </span>
                <span class=""description"">x</span>
                </li>
                <li>
                <span class=""term"">Y</span><span> - </span>
                <span class=""description"">y</span>
                </li>
                </ul>
                ".Replace("\r\n", "\n").Replace("                ", "    "));
            }

            [Test]
            public void Summary_OnClass_WithListElement_AndNumberType_WithOnlyStringItems()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is a summary.
                        /// <list type=""number"">
                        /// <item>X</item>
                        /// <item>Y</item>
                        /// </list>
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe(
                    @"
                This is a summary.
                <ol>
                <li>X</li>
                <li>Y</li>
                </ol>
                ".Replace("\r\n", "\n").Replace("                ", "    "));
            }
            
            [Test]
            public void Summary_OnClass_WithListElement_AndNumberType_WithListHeader()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is a summary.
                        /// <list type=""number"">
                        /// <listheader>
                        /// <term>A</term>
                        /// <description>a</description>
                        /// </listheader>
                        /// <item>
                        /// <term>X</term>
                        /// <description>x</description>
                        /// </item>
                        /// <item>
                        /// <term>Y</term>
                        /// <description>y</description>
                        /// </item>
                        /// </list>
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe(
                    @"
                This is a summary.
                <ol>
                <li>
                <span class=""term"">A</span><span> - </span>
                <span class=""description"">a</span>
                </li>
                <li>
                <span class=""term"">X</span><span> - </span>
                <span class=""description"">x</span>
                </li>
                <li>
                <span class=""term"">Y</span><span> - </span>
                <span class=""description"">y</span>
                </li>
                </ol>
                ".Replace("\r\n", "\n").Replace("                ", "    "));
            }
            
            [Test]
            public void Summary_OnClass_WithListElement_AndNumberType_AndStartAttribute()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is a summary.
                        /// <list type=""number"" start=""2"">
                        /// <listheader>
                        /// <term>A</term>
                        /// <description>a</description>
                        /// </listheader>
                        /// <item>
                        /// <term>X</term>
                        /// <description>x</description>
                        /// </item>
                        /// <item>
                        /// <term>Y</term>
                        /// <description>y</description>
                        /// </item>
                        /// </list>
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe(
                    @"
                This is a summary.
                <ol start=""2"">
                <li>
                <span class=""term"">A</span><span> - </span>
                <span class=""description"">a</span>
                </li>
                <li>
                <span class=""term"">X</span><span> - </span>
                <span class=""description"">x</span>
                </li>
                <li>
                <span class=""term"">Y</span><span> - </span>
                <span class=""description"">y</span>
                </li>
                </ol>
                ".Replace("\r\n", "\n").Replace("                ", "    "));
            }

            [Test]
            public void Summary_OnClass_WithListElement_AndTableType_With2Columns()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is a summary.
                        /// <list type=""table"">
                        /// <listheader>
                        /// <term>i</term>
                        /// <description>d</description>
                        /// </listheader>
                        /// <item>
                        /// <term>A</term>
                        /// <description>x</description>
                        /// </item>
                        /// <item>
                        /// <term>B</term>
                        /// <description>y</description>
                        /// </item>
                        /// </list>
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe(
                    @"
                This is a summary.
                <table class=""table""><thead><tr><th>i</th><th>d</th></tr></thead><tbody><tr>
                <td>A</td>
                <td>x</td>
                </tr><tr>
                <td>B</td>
                <td>y</td>
                </tr></tbody></table>
                ".Replace("\r\n", "\n").Replace("                ", "    "));
            }
            
            [Test]
            public void Summary_OnClass_WithListElement_AndTableType_WithMultipleColumns()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is a summary.
                        /// <list type=""table"">
                        /// <listheader>
                        /// <term>Column 1</term>
                        /// <term>Column 2</term>
                        /// <term>Column 3</term>
                        /// </listheader>
                        /// <item>
                        /// <term>R1, C1</term>
                        /// <term>R1, C2</term>
                        /// <term>R1, C3</term>
                        /// </item>
                        /// <item>
                        /// <description>R2, C1</description>
                        /// <description>R2, C2</description>
                        /// <description>R2, C3</description>
                        /// </item>
                        /// </list>
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe(
                    @"
                This is a summary.
                <table class=""table""><thead><tr><th>Column 1</th><th>Column 2</th><th>Column 3</th></tr></thead><tbody><tr>
                <td>R1, C1</td>
                <td>R1, C2</td>
                <td>R1, C3</td>
                </tr><tr>
                <td>R2, C1</td>
                <td>R2, C2</td>
                <td>R2, C3</td>
                </tr></tbody></table>
                ".Replace("\r\n", "\n").Replace("                ", "    "));
            }
            
            [Test]
            public void Summary_OnClass_WithListElement_AndDefinitionType()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is a summary.
                        /// <list type=""definition"">
                        ///     <item>
                        ///         <term>X</term>
                        ///         <description>x</description>
                        ///     </item>
                        ///     <item>
                        ///         <term>Y</term>
                        ///         <description>y</description>
                        ///     </item>
                        /// </list>
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                IDocument classDocument = GetResult(results, "Green");
                classDocument["Summary"].ShouldBe("\n    This is a summary.\n    <dl>\n        <dt class=\"term\">X</dt><dd class=\"description\">x</dd>\n        <dt class=\"term\">Y</dt><dd class=\"description\">y</dd>\n    </dl>\n    ");
            }

            [Test]
            public void Summary_OnClass_WithParaElements()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// <para>ABC</para>
                        /// <para>XYZ</para>
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe("\n    <p>ABC</p>\n    <p>XYZ</p>\n    ");
            }

            [Test]
            public void Summary_OnClass_WithParaElements_AndNestedCElement()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// <para>ABC</para>
                        /// <para>X<c>Y</c>Z</para>
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe("\n    <p>ABC</p>\n    <p>X<code>Y</code>Z</p>\n    ");
            }

            [Test]
            [TestCase(@"<see cref=""Red""/>", @"<code><a href=""/Foo/Red/index.html"">Red</a></code>", TestName = "Summary_OnClass_WithSeeElement_Cref")]
            [TestCase(@"<see href=""link"">link_title</see>", @"<a href=""link"">link_title</a>", TestName = "Summary_OnClass_WithSeeElement_Href")]
            [TestCase(@"<see langword=""keyword"" />", @"<code>keyword</code>", TestName = "Summary_OnClass_WithSeeElement_Langword")]
            public void Summary_OnClass_WithSeeElement(string seeTag, string seeTagRendered)
            {
                // Given
                string code = $@"
                    namespace Foo
                    {{
                        /// <summary>Check {seeTag} class</summary>
                        class Green
                        {{
                        }}

                        class Red
                        {{
                        }}
                    }}
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe($"Check {seeTagRendered} class");
            }

            [Test]
            public void Summary_OnClass_WithSeeCrefElement_WithNotFoundSymbol()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Check <see cref=""Blue""/> class</summary>
                        class Green
                        {
                        }

                        class Red
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe("Check <code>Blue</code> class");
            }

            [Test]
            public void Summary_OnClass_WithSeeCrefElement_WithNonCompilationGenericSymbol()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Check <see cref=""IEnumerable{string}""/> class</summary>
                        class Green
                        {
                        }

                        class Red
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe("Check <code>IEnumerable&lt;string&gt;</code> class");
            }

            [Test]
            public void Summary_OnClass_WithSeeCrefElementToMethod()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Check <see cref=""Red.Blue""/> method</summary>
                        class Green
                        {
                        }

                        class Red
                        {
                            void Blue()
                            {
                            }
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe("Check <code><a href=\"/Foo/Red/00F22A50.html\">Blue()</a></code> method");
            }

            [Test]
            public void Summary_OnClass_WithUnknownSeeCrefElement()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Check <see cref=""Red""/> class</summary>
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe("Check <code>Red</code> class");
            }

            [Test]
            public void Summary_OnClass_WithSeeAlsoElement()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Check this out <seealso cref=""Red""/></summary>
                        class Green
                        {
                        }

                        class Red
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                IDocument classDocument = GetResult(results, "Green");
                classDocument.ShouldSatisfyAllConditions(
                    d => d["Summary"].ShouldBe("Check this out "), // <seealso> should be removed from the summary and instead placed in the SeeAlso metadata
                    d => d.Get<IReadOnlyList<string>>("SeeAlso")[0].ShouldBe("<code><a href=\"/Foo/Red/index.html\">Red</a></code>"));
            }

            [Test]
            public void Summary_OnNamespace()
            {
                // Given
                const string code = @"
                    /// <summary>This is a summary.</summary>
                    namespace Foo
                    {
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Foo")["Summary"].ShouldBe("This is a summary.");
            }

            [Test]
            public void Summary_OnNamespace_WithNamespaceDocClass()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Green
                        {
                        }

                        /// <summary>This is a summary.</summary>
                        class NamespaceDoc
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Foo")["Summary"].ShouldBe("This is a summary.");
            }

            [Test]
            public void Summary_OnConstructor_WhenAnalyzeModuleIsCalled_WithDocsForImplicitSymbols()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Green
                        {
                            /// <summary>This is a summary.</summary>
                            Green() {}
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp()
                    .WhereSymbol(x => x is INamedTypeSymbol)
                    .WithDocsForImplicitSymbols();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green").Get<IReadOnlyList<IDocument>>("Constructors")[0]["Summary"]
                    .ShouldBe("This is a summary.");
            }

            [Test]
            public void Summary_OnConstructor_NoDocs_OnImplicitSymbols()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Green
                        {
                            /// <summary>This is a summary.</summary>
                            Green() {}
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp()
                    .WhereSymbol(x => x is INamedTypeSymbol);

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green").Get<IReadOnlyList<IDocument>>("Constructors")[0].ContainsKey("Summary")
                    .ShouldBeFalse();
            }

            [Test]
            public void Summary_OnClass_WithCdata()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// <![CDATA[
                        /// <foo>bar</foo>
                        /// ]]>
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe("\n    &lt;foo&gt;bar&lt;/foo&gt;\n    ");
            }

            [Test]
            [TestCase("public void", TestName = "Summary_OnPublicVoidMethod")]
            [TestCase("internal void", TestName = "Summary_OnInternalVoidMethod")]
            [TestCase("public static void", TestName = "Summary_OnPublicStaticVoidMethod")]
            [TestCase("protected void", TestName = "Summary_OnProtectedVoidMethod")]
            [TestCase("protected virtual void", TestName = "Summary_OnProtectedVirtualVoidMethod")]
            public void Summary_OnMethod(string keywords)
            {
                // Given
                string code = $@"
                    namespace Foo
                    {{
                        class Green
                        {{
                            /// <summary>This is a method summary</summary>
                            {keywords} Go()
                            {{
                            }}
                        }}
                    }}
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                IDocument methodDocument = GetMember(results, "Green", "Go");
                methodDocument["Summary"].ShouldBe("This is a method summary");
            }
            
            [Test]
            public void Summary_OnProperty()
            {
                // Given
                string code = $@"
                    namespace Foo
                    {{
                        class Green
                        {{
                            /// <summary>This is a property summary</summary>
                            public int Circle
                            {{
                                get;
                                set;
                            }}
                        }}
                    }}
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetMember(results, "Green", "Circle")["Summary"].ShouldBe("This is a property summary");
            }

            [Test]
            public void Summary_OnMethod_WithParamRefElement()
            {
                // Given
                // Given
                string code = $@"
                    namespace Foo
                    {{
                        class Calculator
                        {{
                            /// <summary>Calculates the sum of <paramref name=""a""/> and <paramref name=""b"" /></summary>
                            public int Sum(int a, int b)
                            {{
                            }}
                        }}
                    }}
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetMember(results, "Calculator", "Sum")["Summary"]
                    .ShouldBe("Calculates the sum of <span name=\"a\" class=\"paramref\">a</span> and <span name=\"b\" class=\"paramref\">b</span>");
            }
            
            [Test]
            [TestCase("note", "<div class=\"note\">\n    Some note\n    </div>", TestName = "Summary_OnClass_WithNoteElement")]
            [TestCase("tip", "<div class=\"tip\">\n    Some note\n    </div>", TestName = "Summary_OnClass_WithTipNoteElement")]
            [TestCase("implement", "<div class=\"implement\">\n    Some note\n    </div>", TestName = "Summary_OnClass_WithImplementNoteElement")]
            [TestCase("caller", "<div class=\"caller\">\n    Some note\n    </div>", TestName = "Summary_OnClass_WithCallerNoteElement")]
            [TestCase("inherit", "<div class=\"inherit\">\n    Some note\n    </div>", TestName = "Summary_OnClass_WithInheritNoteElement")]
            [TestCase("caution", "<div class=\"caution\">\n    Some note\n    </div>", TestName = "Summary_OnClass_WithCautionNoteElement")]
            [TestCase("warning", "<div class=\"warning\">\n    Some note\n    </div>", TestName = "Summary_OnClass_WithWarningNoteElement")]
            [TestCase("important", "<div class=\"important\">\n    Some note\n    </div>", TestName = "Summary_OnClass_WithImportantNoteElement")]
            [TestCase("security", "<div class=\"security\">\n    Some note\n    </div>", TestName = "Summary_OnClass_WithSecurityNoteElement")]
            public void Summary_OnClass_WithNoteElement(string noteType, string noteRendered)
            {
                // Given
                string code = $@"
                    namespace Foo
                    {{
                        /// <summary>
                        /// Some summary
                        /// <note type=""{noteType}"">
                        /// Some note
                        /// </note>
                        /// </summary>
                        class Green
                        {{
                        }}
                    }}
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe($"\n    Some summary\n    {noteRendered}\n    ");
            }
            
            /// <remarks>
            /// Known tags are b and i and their content is processed for XML comments.
            /// Unknown tags like u or html mark tag to not get their content processed for XML comments.
            /// </remarks>
            [Test]
            [TestCase("<b>bold</b>", "<strong>bold</strong>", TestName = "Summary_OnClass_WithBoldTextStylingElements")]
            [TestCase(@"<b><see cref=""Red""/></b>", "<strong><code><a href=\"/Foo/Red/index.html\">Red</a></code></strong>", TestName = "Summary_OnClass_WithBoldTextStylingElements_AndSeeCref")]
            [TestCase("<i>italic</i>", "<i>italic</i>", TestName = "Summary_OnClass_WithItalicsTextStylingElements")]
            [TestCase(@"<i><see cref=""Red""/></i>", "<i><code><a href=\"/Foo/Red/index.html\">Red</a></code></i>", TestName = "Summary_OnClass_WithItalicsTextStylingElements_AndSeeCref")]
            [TestCase("<u>underline</u>", "<u>underline</u>", TestName = "Summary_OnClass_WithUnderlineTextStylingElements")]
            [TestCase(@"<u><see cref=""Red""/></u>", "<u><see cref=\"T:Foo.Red\" /></u>", TestName = "Summary_OnClass_WithUnderlineTextStylingElements_AndSeeCref")]
            [TestCase("<mark>marked</mark>", "<mark>marked</mark>", TestName = "Summary_OnClass_WithMarkTextStylingElements")]
            [TestCase(@"<mark><see cref=""Red""/></mark>", "<mark><see cref=\"T:Foo.Red\" /></mark>", TestName = "Summary_OnClass_WithMarkTextStylingElements_AndSeeCref")]
            public void Summary_OnClass_WithHtmlTextStylingElements(string htmlStyling, string htmlRendered)
            {
                // Given
                string code = $@"
                    namespace Foo
                    {{
                        /// <summary>This is a {htmlStyling} summary.</summary>
                        class Green
                        {{
                        }}

                        /// <summary>This is another summary.</summary>
                        struct Red
                        {{
                        }}
                    }}
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Summary"].ShouldBe($"This is a {htmlRendered} summary.");
                GetResult(results, "Red")["Summary"].ShouldBe("This is another summary.");
            }
        }
    }
}