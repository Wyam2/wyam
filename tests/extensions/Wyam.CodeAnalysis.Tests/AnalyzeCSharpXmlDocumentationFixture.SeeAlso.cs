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
            [TestCase(@"<seealso cref=""Red""/>", @"<code><a href=""/Foo/Red/index.html"">Red</a></code>", TestName = "SeeAlso_OnClass_WithCref")]
            [TestCase(@"<seealso href=""link"">link_title</seealso>", @"<a href=""link"">link_title</a>", TestName = "SeeAlso_OnClass_WithHref")]
            public void SeeAlso_OnClass(string seeAlsoTag, string seeAlsoTagRendered)
            {
                // Given
                string code = $@"
                    namespace Foo
                    {{
                        /// {seeAlsoTag}
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
                GetResult(results, "Green").Get<IReadOnlyList<string>>("SeeAlso")[0].ShouldBe(seeAlsoTagRendered);
            }
        }
    }
}