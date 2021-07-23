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
            public void Value_OnValueProperty()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        public class Green
                        {
                            /// <value>The value can be any string</value>
                            public string Property{ get; set; }
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
                GetMember(results, "Green", "Property")["Value"]
                    .ShouldBe("The value can be any string");
            }
            
            [Test]
            public void Value_OnGenericProperty()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        public class Green<T>
                        {
                            /// <value>The value is <typeparamref name=""T""/></value>
                            public T Property{ get; set; }
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
                GetMember(results, "Green", "Property")["Value"]
                    .ShouldBe("The value is <span name=\"T\" class=\"typeparamref\">T</span>");
            }
        }
    }
}