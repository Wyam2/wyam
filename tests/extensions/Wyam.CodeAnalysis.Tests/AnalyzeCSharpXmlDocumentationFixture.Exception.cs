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
            public void Exception_WithKnownCref()
            {
                // Given
                const string code = @"
                                namespace Foo
                                {
                                    class Green
                                    {
                                        /// <exception cref=""FooException"">Throws when null</exception>
                                        void Go()
                                        {
                                        }
                                    }
            
                                    class FooException : Exception
                                    {
                                    }
                                }
                            ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList(); // Make sure to materialize the result list

                // Then
                IDocument methodDocument = GetMember(results, "Green", "Go");
                methodDocument.ShouldSatisfyAllConditions(
                    d => d.List<ReferenceComment>("Exceptions")[0].Name.ShouldBe("FooException"),
                    d => d.List<ReferenceComment>("Exceptions")[0].Link.ShouldBe("<code><a href=\"/Foo/FooException/index.html\">FooException</a></code>"),
                    d => d.List<ReferenceComment>("Exceptions")[0].Html.ShouldBe("Throws when null"));
            }

            [Test]
            public void Exception_WithUnknownCref()
            {
                // Given
                const string code = @"
                                namespace Foo
                                {
                                    class Green
                                    {
                                        /// <exception cref=""FooException"">Throws when null</exception>
                                        void Go()
                                        {
                                        }
                                    }
                                }
                            ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList(); // Make sure to materialize the result list

                // Then
                IDocument methodDocument = GetMember(results, "Green", "Go");
                methodDocument.ShouldSatisfyAllConditions(
                    d => d.List<ReferenceComment>("Exceptions")[0].Name.ShouldBe("FooException"),
                    d => d.List<ReferenceComment>("Exceptions")[0].Link.ShouldBe("FooException"),
                    d => d.List<ReferenceComment>("Exceptions")[0].Html.ShouldBe("Throws when null"));
            }

            [Test]
            public void Exception_WithoutCref()
            {
                // Given
                const string code = @"
                                namespace Foo
                                {
                                    class Green
                                    {
                                        /// <exception>Throws when null</exception>
                                        void Go()
                                        {
                                        }
                                    }
                                }
                            ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList(); // Make sure to materialize the result list

                // Then
                IDocument methodDocument = GetMember(results, "Green", "Go");
                methodDocument.ShouldSatisfyAllConditions(
                    d => d.List<ReferenceComment>("Exceptions")[0].Name.ShouldBe(string.Empty),
                    d => d.List<ReferenceComment>("Exceptions")[0].Html.ShouldBe("Throws when null"));
            }

            [Test]
            public void Exception_MultipleExceptionElements()
            {
                // Given
                const string code = @"
                                namespace Foo
                                {
                                    class Green
                                    {
                                        /// <exception cref=""FooException"">Throws when null</exception>
                                        /// <exception cref=""BarException"">Throws for another reason</exception>
                                        void Go()
                                        {
                                        }
                                    }
            
                                    class FooException : Exception
                                    {
                                    }
                                }
                            ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList(); // Make sure to materialize the result list

                // Then
                IDocument methodDocument = GetMember(results, "Green", "Go");
                methodDocument.ShouldSatisfyAllConditions(
                    d => d.List<ReferenceComment>("Exceptions").Count.ShouldBe(2),
                    d => d.List<ReferenceComment>("Exceptions")[0].Link.ShouldBe("<code><a href=\"/Foo/FooException/index.html\">FooException</a></code>"),
                    d => d.List<ReferenceComment>("Exceptions")[0].Name.ShouldBe("FooException"),
                    d => d.List<ReferenceComment>("Exceptions")[0].Html.ShouldBe("Throws when null"),
                    d => d.List<ReferenceComment>("Exceptions")[1].Link.ShouldBe("BarException"),
                    d => d.List<ReferenceComment>("Exceptions")[1].Name.ShouldBe("BarException"),
                    d => d.List<ReferenceComment>("Exceptions")[1].Html.ShouldBe("Throws for another reason"));
            }
        }
    }
}