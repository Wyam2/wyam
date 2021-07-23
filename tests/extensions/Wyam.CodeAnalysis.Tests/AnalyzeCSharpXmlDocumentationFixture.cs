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
    [TestFixture]
    [Parallelizable(ParallelScope.Self | ParallelScope.Children)]
    public partial class AnalyzeCSharpXmlDocumentationFixture : AnalyzeCSharpBaseFixture
    {
        public partial class ExecuteTests : AnalyzeCSharpXmlDocumentationFixture
        {
            [Test]
            [TestCase(@"<see cref=""Red""/>", @"<code><a href=""/Foo/Red/index.html"">Red</a></code>", TestName = "OtherComment_WithSeeElement_Cref")]
            [TestCase(@"<see href=""link"">link_title</see>", @"<a href=""link"">link_title</a>", TestName = "OtherComment_WithSeeElement_Href")]
            [TestCase(@"<see langword=""keyword"" />", @"<code>keyword</code>", TestName = "OtherCommentWithSeeElement_Langword")]
            public void OtherComments_WithSeeElement(string seeTag, string seeTagRendered)
            {
                // Given
                string code = $@"
                    namespace Foo
                    {{
                        /// <bar>Check {seeTag} class</bar>
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
                Assert.AreEqual(
                    $@"Check {seeTagRendered} class",
                    GetResult(results, "Green").List<OtherComment>("BarComments")[0].Html);
            }
            
            [Test]
            public void OtherComments_MultipleOtherComments()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <bar>Circle</bar>
                        /// <bar>Square</bar>
                        /// <bar>Rectangle</bar>
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
                Assert.AreEqual(
                    3,
                    GetResult(results, "Green").List<OtherComment>("BarComments").Count);
                Assert.AreEqual(
                    "Circle",
                    GetResult(results, "Green").List<OtherComment>("BarComments")[0].Html);
                Assert.AreEqual(
                    "Square",
                    GetResult(results, "Green").List<OtherComment>("BarComments")[1].Html);
                Assert.AreEqual(
                    "Rectangle",
                    GetResult(results, "Green").List<OtherComment>("BarComments")[2].Html);
            }

            [Test]
            public void OtherComments_WithAttributes()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <bar a='x'>Circle</bar>
                        /// <bar a='y' b='z'>Square</bar>
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
                Assert.AreEqual(
                    1,
                    GetResult(results, "Green").List<OtherComment>("BarComments")[0].Attributes.Count);
                Assert.AreEqual(
                    "x",
                    GetResult(results, "Green").List<OtherComment>("BarComments")[0].Attributes["a"]);
                Assert.AreEqual(
                    2,
                    GetResult(results, "Green").List<OtherComment>("BarComments")[1].Attributes.Count);
                Assert.AreEqual(
                    "y",
                    GetResult(results, "Green").List<OtherComment>("BarComments")[1].Attributes["a"]);
                Assert.AreEqual(
                    "z",
                    GetResult(results, "Green").List<OtherComment>("BarComments")[1].Attributes["b"]);
            }
            
            [Test]
            public void OtherComments_OnMethod_WithParamRefElement()
            {
                // Given
                // Given
                string code = $@"
                    namespace Foo
                    {{
                        class Calculator
                        {{
                            /// <bar>Calculates the sum of <paramref name=""a""/> and <paramref name=""b"" /></bar>
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
                IDocument methodDocument = GetMember(results, "Calculator", "Sum");
                methodDocument.List<OtherComment>("BarComments")[0].Html.ShouldBe<string>("Calculates the sum of <span name=\"a\" class=\"paramref\">a</span> and <span name=\"b\" class=\"paramref\">b</span>");
            }
            
            [Test]
            public void OtherComments_SandcastleSpecificElements()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <event cref=""eventType"">description</event>
                        /// <preliminary>This method will be going away in the production release.</preliminary>
                        /// <threadsafety static=""true"" instance=""false"" />
                        /// <permission cref=""SecurityPermission"">
                        /// <see cref=""SecurityPermissionFlag.Execution"">Execution</see> privilege.
                        /// </permission>
                        /// <revisionHistory>
                        ///     <revision date=""21/07/2021"" version=""3.0.0.0"" author=""gituser"">Made Green super green</revision>
                        /// </revisionHistory>
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
                    d => d.List<OtherComment>("EventComments")[0].Html.ShouldBe("description"),
                    d => d.List<OtherComment>("PreliminaryComments")[0].Html.ShouldBe("This method will be going away in the production release."),
                    d => d.List<OtherComment>("ThreadsafetyComments").Count.ShouldBe(1),
                    d => d.List<OtherComment>("ThreadsafetyComments").Count.ShouldBe(1),
                    d => d.List<OtherComment>("ThreadsafetyComments")[0].Attributes["static"].ShouldBe("true"),
                    d => d.List<OtherComment>("ThreadsafetyComments")[0].Attributes["instance"].ShouldBe("false"),
                    d => d.List<RevisionComment>("RevisionHistory").Count.ShouldBe(1));
            }

            [Test]
            public void Include_ExternalFile()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <include file=""Included.xml"" path=""//Test/*"" />
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
                GetResult(results, "Green")["Summary"].ShouldBe("This is a included summary.");
            }
            
            /// <remarks>
            /// Example taken from <see href="https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/examples" />
            /// </remarks>
            [Test]
            public void Include_ExternalFile_WithXPath()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <include file=""Included_xml_tag.xml"" path=""MyDocs/MyMembers[@name='test']/*"" />
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
                GetResult(results, "Green")["Summary"].ShouldBe("The summary for this type.");
            }

            [Test]
            public void Inherit_FromBaseClass()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>This is a summary.</summary>
                        class Green
                        {
                        }

                        /// <inheritdoc />
                        class Blue : Green
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
                GetResult(results, "Blue")["Summary"].ShouldBe("This is a summary.");
            }

            [Test]
            public void Inherit_ImplicitInheritFromBaseClass_WhenModuleCalled_WithImplicitInheritDoc()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>This is a summary.</summary>
                        class Green
                        {
                        }

                        class Blue : Green
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp().WithImplicitInheritDoc();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Blue")["Summary"].ShouldBe("This is a summary.");
            }

            [Test]
            public void Inherit_FromCref()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>This is a summary.</summary>
                        class Green
                        {
                        }

                        /// <inheritdoc cref=""Green"" />
                        class Blue
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
                GetResult(results, "Blue")["Summary"].ShouldBe("This is a summary.");
            }

            [Test]
            public void Inherit_CircularInheritdoc()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>This is a summary.</summary>
                        /// <inheritdoc cref=""Blue"" />
                        class Green
                        {
                        }

                        /// <inheritdoc cref=""Green"" />
                        class Blue
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
                GetResult(results, "Blue")["Summary"].ShouldBe("This is a summary.");
            }

            [Test]
            public void Inherit_Recursive()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>This is a summary.</summary>
                        class Red
                        {
                        }

                        /// <inheritdoc cref=""Red"" />
                        class Green
                        {
                        }

                        /// <inheritdoc cref=""Green"" />
                        class Blue
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
                GetResult(results, "Blue")["Summary"].ShouldBe("This is a summary.");
            }

            [Test]
            public void Inherit_InheritDocDoesNotOverride_WhenExistingSummary()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>This is a summary.</summary>
                        class Green
                        {
                        }

                        /// <inheritdoc />
                        /// <summary>Blue summary.</summary>
                        class Blue : Green
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
                GetResult(results, "Blue")["Summary"].ShouldBe("Blue summary.");
            }

            [Test]
            public void Inherit_FromOverriddenMethod()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Green summary.</summary>
                        class Green
                        {
                            /// <summary>Base summary.</summary>
                            public virtual void Foo() {}
                        }

                        /// <summary>Blue summary.</summary>
                        class Blue : Green
                        {
                            /// <inheritdoc />
                            public override void Foo() {}
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetMember(results, "Blue", "Foo")["Summary"].ShouldBe("Base summary.");
            }

            [Test]
            public void Inherit_FromOverriddenMethodWithParams()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Green summary.</summary>
                        class Green
                        {
                            /// <param name=""a"">AAA</param>
                            /// <param name=""b"">BBB</param>
                            public virtual void Foo(string a, string b) {}
                        }

                        /// <summary>Blue summary.</summary>
                        class Blue : Green
                        {
                            /// <inheritdoc />
                            /// <param name=""b"">XXX</param>
                            public override void Foo(string a, string b) {}
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                IDocument methodDocument = GetMember(results, "Blue", "Foo");
                methodDocument.ShouldSatisfyAllConditions(
                    d => d.List<ReferenceComment>("Params")[0].Name.ShouldBe("b"),
                    d => d.List<ReferenceComment>("Params")[0].Html.ShouldBe("XXX"),
                    d => d.List<ReferenceComment>("Params")[1].Name.ShouldBe("a"),
                    d => d.List<ReferenceComment>("Params")[1].Html.ShouldBe("AAA"));
            }

            [Test]
            public void Inherit_FromInterface()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Green summary.</summary>
                        interface IGreen
                        {
                        }

                        /// <inheritdoc />
                        class Blue : IGreen
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
                GetResult(results, "Blue")["Summary"].ShouldBe("Green summary.");
            }

            [Test]
            public void Inherit_FromMultipleInterfaces()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Red summary.</summary>
                        interface IRed
                        {
                        }

                        interface IGreen
                        {
                        }

                        /// <inheritdoc />
                        class Blue : IGreen, IRed
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
                GetResult(results, "Blue")["Summary"].ShouldBe("Red summary.");
            }

            [Test]
            public void Inherit_FromMultipleInterfacesWithMultipleMatches()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Red summary.</summary>
                        interface IRed
                        {
                        }

                        /// <summary>Green summary.</summary>
                        interface IGreen
                        {
                        }

                        /// <inheritdoc />
                        class Blue : IGreen, IRed
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
                GetResult(results, "Blue")["Summary"].ShouldBe("Green summary.");
            }

            [Test]
            public void Inherit_FromImplementedMethod()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Green summary.</summary>
                        interface IGreen
                        {
                            /// <summary>Interface summary.</summary>
                            void Foo();
                        }

                        /// <summary>Blue summary.</summary>
                        class Blue : IGreen
                        {
                            /// <inheritdoc />
                            public void Foo() {}
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetMember(results, "Blue", "Foo")["Summary"].ShouldBe("Interface summary.");
            }

            [Test]
            public void Inherit_FromImplementedMethodIfOverride()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        public interface IGreen
                        {
                            /// <summary>Interface summary.</summary>
                            void Foo();
                        }

                        public abstract class Red : IGreen
                        {
                            public abstract void Foo();
                        }

                        public class Blue : Red
                        {
                            /// <inheritdoc />
                            public override void Foo() {}
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetMember(results, "Blue", "Foo")["Summary"].ShouldBe("Interface summary.");
            }

            [Test]
            public void Inherit_FromBaseMethod_IfOverrideAndInterface()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        public interface IGreen
                        {
                            /// <summary>Interface summary.</summary>
                            void Foo();
                        }

                        public abstract class Red : IGreen
                        {
                            /// <summary>Base summary.</summary>
                            public abstract void Foo();
                        }

                        public class Blue : Red
                        {
                            /// <inheritdoc />
                            public override void Foo() {}
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetMember(results, "Blue", "Foo")["Summary"].ShouldBe("Base summary.");
            }

            [Test]
            public void Inherit_FromImplementedMethod_IfIndirectOverride()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        public interface IGreen
                        {
                            /// <summary>Interface summary.</summary>
                            void Foo();
                        }

                        public abstract class Yellow : IGreen
                        {
                            public abstract void Foo();
                        }

                        public abstract class Red : Yellow
                        {
                        }

                        public class Blue : Red
                        {
                            /// <inheritdoc />
                            public override void Foo() {}
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetMember(results, "Blue", "Foo")["Summary"].ShouldBe("Interface summary.");
            }
        }
    }
}