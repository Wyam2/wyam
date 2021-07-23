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
            public void RevisionHistory_SingleRevision_WithTextOnlyDescription()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <revisionHistory>
                        ///     <revision date=""21/07/2021"" version=""3.0.0.0"" author=""gituser"">Made Green super green</revision>
                        /// </revisionHistory>
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
                classDocument.ShouldSatisfyAllConditions(
                    d => d.List<RevisionComment>("RevisionHistory").Count.ShouldBe(1),
                    d => d.List<RevisionComment>("RevisionHistory")[0].Date.ShouldBe("21/07/2021"),
                    d => d.List<RevisionComment>("RevisionHistory")[0].Version.ShouldBe("3.0.0.0"),
                    d => d.List<RevisionComment>("RevisionHistory")[0].Author.ShouldBe("gituser"),
                    d => d.List<RevisionComment>("RevisionHistory")[0].Html.ShouldBe("Made Green super green"));
            }
            
            [Test]
            public void RevisionHistory_MultipleRevision_WithTextOnlyDescription()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <revisionHistory>
                        ///     <revision date=""22/05/2020"" version=""2.2.9.0"" author=""firstgituser"">Made Green</revision>
                        ///     <revision date=""21/07/2021"" version=""3.0.0.0"" author=""gituser"">Made Green super green</revision>
                        /// </revisionHistory>
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

                // Then they are also sorted descending by version
                IDocument classDocument = GetResult(results, "Green");
                classDocument.ShouldSatisfyAllConditions(
                    d => d.List<RevisionComment>("RevisionHistory").Count.ShouldBe(2),
                    d => d.List<RevisionComment>("RevisionHistory")[0].Version.ShouldBe("3.0.0.0"),
                    d => d.List<RevisionComment>("RevisionHistory")[0].Html.ShouldBe("Made Green super green"),
                    d => d.List<RevisionComment>("RevisionHistory")[1].Version.ShouldBe("2.2.9.0"),
                    d => d.List<RevisionComment>("RevisionHistory")[1].Html.ShouldBe("Made Green"));
            }
            
            [Test]
            public void RevisionHistory_WithVisibleAttributeFalse_IsEmpty()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <revisionHistory visible=""false"">
                        ///     <revision date=""21/07/2021"" version=""3.0.0.0"" author=""gituser"">Made Green super green</revision>
                        /// </revisionHistory>
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
                classDocument.List<RevisionComment>("RevisionHistory").Count.ShouldBe(0);
            }
            
            [Test]
            public void RevisionHistory_MultipleRevision_WithOneRevision_HavingVisibleAttributeFalse()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <revisionHistory>
                        ///     <revision date=""22/05/2020"" version=""2.2.9.0"" author=""firstgituser"">Made Green</revision>
                        ///     <revision date=""21/07/2021"" version=""3.0.0.0"" author=""gituser"" visible=""false"">Made Green super green</revision>
                        /// </revisionHistory>
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

                // Then they are also sorted descending by version
                IDocument classDocument = GetResult(results, "Green");
                classDocument.ShouldSatisfyAllConditions(
                    d => d.List<RevisionComment>("RevisionHistory").Count.ShouldBe(1),
                    d => d.List<RevisionComment>("RevisionHistory")[0].Version.ShouldBe("2.2.9.0"));
            }
            
            [Test]
            public void RevisionHistory_MultipleRevision_WithComplexDescription()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <revisionHistory>
                        ///     <revision date=""22/05/2020"" version=""2.2.9.0"" author=""firstgituser"">Made Green</revision>
                        ///     <revision date=""21/07/2021"" version=""3.0.0.0"" author=""gituser"">
                        ///         Made <b>Green</b> super green 
                        ///         <para>Paragraph</para> <see cref=""Foo.Red"" />
                        ///     </revision>
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

                // Then they are also sorted descending by version
                IDocument classDocument = GetResult(results, "Green");
                classDocument.ShouldSatisfyAllConditions(
                    d => d.List<RevisionComment>("RevisionHistory").Count.ShouldBe(2),
                    d => d.List<RevisionComment>("RevisionHistory")[0].Version.ShouldBe("3.0.0.0"),
                    d => d.List<RevisionComment>("RevisionHistory")[0].Html.ShouldBe("\n            Made <strong>Green</strong> super green \n            <p>Paragraph</p> <code><a href=\"/Foo/Red/index.html\">Red</a></code>\n        "),
                    d => d.List<RevisionComment>("RevisionHistory")[1].Version.ShouldBe("2.2.9.0"),
                    d => d.List<RevisionComment>("RevisionHistory")[1].Html.ShouldBe("Made Green"));
            }
        }
    }
}