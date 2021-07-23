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
            public void Param_OnMethod()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Green
                        {
                            /// <param name=""bar"">comment</param>
                            void Go(string bar)
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
                IDocument methodDocument = GetMember(results, "Green", "Go");
                methodDocument.ShouldSatisfyAllConditions(
                    d => d.List<ReferenceComment>("Params")[0].Name.ShouldBe("bar"),
                    d => d.List<ReferenceComment>("Params")[0].Html.ShouldBe("comment"));
            }

            [Test]
            public void Param_MissingOnMethod()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Green
                        {
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
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetMember(results, "Green", "Go").List<ReferenceComment>("Params").ShouldBeEmpty();
            }
            
            [Test]
            public void Param_OnMethod_WithMultipleParams()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Green
                        {
                            /// <param name=""bar"">comment</param>
                            /// <param name=""zu"">other comment</param>
                            void Go(string bar, int zu)
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
                IDocument methodDocument = GetMember(results, "Green", "Go");
                methodDocument.ShouldSatisfyAllConditions(
                    d => d.List<ReferenceComment>("Params")[0].Name.ShouldBe("bar"),
                    d => d.List<ReferenceComment>("Params")[0].Html.ShouldBe("comment"),
                    d => d.List<ReferenceComment>("Params")[1].Name.ShouldBe("zu"),
                    d => d.List<ReferenceComment>("Params")[1].Html.ShouldBe("other comment"));
            }
        }
    }
}