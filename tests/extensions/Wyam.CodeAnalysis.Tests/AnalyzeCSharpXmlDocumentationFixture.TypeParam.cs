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
            public void TypeParam_OnClass()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <typeparam name=""T1"">Generic argument number one.</typeparam>
                        public class GenericGreen<T1>
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
                GetResult(results, "GenericGreen").List<ReferenceComment>("TypeParams")[0].Name
                    .ShouldBe("T1");
            }
            
            [Test]
            public void TypeParam_OnProperty()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <typeparam name=""T1"">Generic argument number one.</typeparam>
                        public class GenericGreen<T1>
                        {
                            /// <summary>
                            /// Gets or sets an instance of <typeparamref name=""T1"" />
                            /// </summary>
                            public T1 Property{ get; set; }
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
                GetMember(results, "GenericGreen", "Property")["Summary"]
                    .ShouldBe("\n    Gets or sets an instance of <span name=\"T1\" class=\"typeparamref\">T1</span>\n    ");
            }
        }
    }
}