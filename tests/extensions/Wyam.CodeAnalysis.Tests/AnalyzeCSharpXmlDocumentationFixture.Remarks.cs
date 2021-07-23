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
            public void Remarks_OnMethod_WithParamRefElement()
            {
                // Given
                // Given
                string code = @"
                    namespace Foo
                    {
                        class Calculator
                        {
                            /// <remarks>
                            /// Calculates the sum of <paramref name=""a""/> and <paramref name=""b"" />
                            /// </remarks>
                            public int Sum(int a, int b)
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
                IDocument methodDocument = GetMember(results, "Calculator", "Sum");
                methodDocument["Remarks"].ShouldBe("\n    Calculates the sum of <span name=\"a\" class=\"paramref\">a</span> and <span name=\"b\" class=\"paramref\">b</span>\n    ");
            }
            
            [Test]
            public void Remarks_OnGenericEvent_WithGenericSeeCrefElement()
            {
                // Given
                // Given
                string code = @"
                    namespace Foo
                    {
                        class Bar<T1>
                        {
                            /// <summary>
                            /// Event with generic argument.
                            /// </summary>
                            /// <remarks>The <see cref=""Delegate"">delegate</see> is
                            /// <see cref=""EventHandler{T}""/> of
                            /// generic type argument <typeparamref name=""T1""/>.
                            /// </remarks>
                            public event EventHandler<T1> MyEvent
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
                IDocument eventDocument = GetMember(results, "Bar", "MyEvent");
                eventDocument.ShouldSatisfyAllConditions(
                    d => d["Summary"].ShouldBe("\n    Event with generic argument.\n    "),
                    d => d["Remarks"]
                        .ShouldBe("The <code>Delegate</code> is\n    <code>EventHandler&lt;T&gt;</code> of\n    generic type argument <span name=\"T1\" class=\"typeparamref\">T1</span>.\n    "));
            }
            
            [Test]
            [TestCase("note", "<div class=\"note\">\n    Some note\n    </div>", TestName = "Remarks_OnMethod_WithNoteElement")]
            [TestCase("tip", "<div class=\"tip\">\n    Some note\n    </div>", TestName = "Remarks_OnMethod_WithTipNoteElement")]
            [TestCase("implement", "<div class=\"implement\">\n    Some note\n    </div>", TestName = "Remarks_OnMethod_WithImplementNoteElement")]
            [TestCase("caller", "<div class=\"caller\">\n    Some note\n    </div>", TestName = "Remarks_OnMethod_WithCallerNoteElement")]
            [TestCase("inherit", "<div class=\"inherit\">\n    Some note\n    </div>", TestName = "Remarks_OnMethod_WithInheritNoteElement")]
            [TestCase("caution", "<div class=\"caution\">\n    Some note\n    </div>", TestName = "Remarks_OnMethod_WithCautionNoteElement")]
            [TestCase("warning", "<div class=\"warning\">\n    Some note\n    </div>", TestName = "Remarks_OnMethod_WithWarningNoteElement")]
            [TestCase("important", "<div class=\"important\">\n    Some note\n    </div>", TestName = "Remarks_OnMethod_WithImportantNoteElement")]
            [TestCase("security", "<div class=\"security\">\n    Some note\n    </div>", TestName = "Remarks_OnMethod_WithSecurityNoteElement")]
            public void Remarks_OnMethod_WithNoteElement(string noteType, string noteRendered)
            {
                // Given
                string code = $@"
                    namespace Foo
                    {{
                        /// <summary>
                        /// Some summary
                        /// </summary>
                        class Green
                        {{
                            /// <remarks>
                            /// Some remarks
                            /// <note type=""{noteType}"">
                            /// Some note
                            /// </note>
                            /// More remarks
                            /// </remarks>
                            public void Method()
                        }}
                    }}
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetMember(results, "Green", "Method")["Remarks"].ShouldBe($"\n    Some remarks\n    {noteRendered}\n    More remarks\n    ");
            }
        }
    }
}