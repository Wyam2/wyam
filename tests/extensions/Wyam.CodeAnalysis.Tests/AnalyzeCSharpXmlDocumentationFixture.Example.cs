using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using NUnit.Framework.Internal;
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
            public void Example_WithPlainTextOnly()
            {
                // Given
                const string code = @"
                   namespace Foo
                   {
                       /// <example>
                       /// Lorem ipsum et dolor
                       /// </example>
                       class Green
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
                GetResult(results, "Green")["Example"].ShouldBe("\n    Lorem ipsum et dolor\n    ");
            }
            
            [Test]
            [TestCase("csharp", "language-csharp", TestName = "Example_WithCSharpCode_AndCSharpLanguageAttribute_CSharpLowercase")]
            [TestCase("CSharp", "language-csharp", TestName = "Example_WithCSharpCode_AndCSharpLanguageAttribute_CSharpAsIs")]
            public void Example_WithCSharpCode_AndCSharpLanguageAttribute(string language, string languageCssClass)
            {
                // Given
                string code = $@"
                   namespace Foo
                   {{
                       /// <example>
                       /// <code language=""{language}"">var i = 1;</code>
                       /// </example>
                       class Green
                       {{
                       }}
                   }}
               ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList(); // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Example"].ShouldBe($"\n    <pre><code class=\"{languageCssClass}\">var i = 1;</code></pre>\n    ");
            }
            
            [Test]
            [TestCase("Example_xml_code_source.xml", "&lt;project name=\"nant\" default=\"help\"&gt;\n    &lt;property name=\"hello\" value=\"you!\" /&gt;\n&lt;/project&gt;", TestName = "Example_WithCode_AndSourceAttribute_Xml")]
            [TestCase("Example_sql_code_source.sql", "SELECT *\nFROM sys.tables", TestName = "Example_WithCode_AndSourceAttribute_Sql")]
            [TestCase("Example_cs_code_source.cs", "var i = 1;", TestName = "Example_WithCode_AndSourceAttribute_CSharp")]
            public void Example_WithCode_AndSourceAttribute(string sourceFile, string codeRendered)
            {
                // Given
                string code = $@"
                   namespace Foo
                   {{
                       /// <example>
                       /// <code source=""{sourceFile}"" />
                       /// </example>
                       class Green
                       {{
                       }}
                   }}
               ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList(); // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Example"].ShouldBe($"\n    <pre><code>{codeRendered}</code></pre>\n    ");
            }
            
            [Test]
            [TestCase("Example_xml_code_source_regions.xml", "&lt;property name=\"hello\" value=\"you!\" /&gt;", "&lt;target name=\"hello\"&gt;\n    &lt;echo&gt;hello ${hello}&lt;/echo&gt;\n&lt;/target&gt;", TestName = "Example_WithCode_AndSource_AndRegionAttributes_Xml")]
            [TestCase("Example_sql_code_source_regions.sql", "SELECT *\nFROM  sys.tables", "SELECT *\nFROM sys.columns", TestName = "Example_WithCode_AndSource_AndRegionAttributes_Sql")]
            [TestCase("Example_cs_code_source_regions.cs", "var i = 1;", "var i = 2;", TestName = "Example_WithCode_AndSource_AndRegionAttributes_CSharp")]
            public void Example_WithCode_AndSource_AndRegionAttributes(string sourceFile, string example1Rendered, string example2Rendered)
            {
                // Given
                string code = $@"
                   namespace Foo
                   {{
                       /// <example>
                       /// <code source=""{sourceFile}"" region=""Example 1"" />
                       /// </example>
                       class Green
                       {{
                       }}
                        
                       /// <example>
                       /// <code source=""{sourceFile}"" region=""Example 2"" />
                       /// </example>
                       class Blue
                       {{
                       }}
                   }}
               ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList(); // Make sure to materialize the result list

                // Then
                IDocument greenDocument = GetResult(results, "Green");
                IDocument blueDocument = GetResult(results, "Blue");
                greenDocument["Example"].ShouldBe($"\n    <pre><code>{example1Rendered}</code></pre>\n    ");
                blueDocument["Example"].ShouldBe($"\n    <pre><code>{example2Rendered}</code></pre>\n    ");
            }
            
            [Test]
            [TestCase("Example_xml_code_source_regions.xml", "&lt;property name=\"hello\" value=\"you!\" /&gt;", "&lt;target name=\"hello\"&gt;\n    &lt;echo&gt;hello ${hello}&lt;/echo&gt;\n&lt;/target&gt;", TestName = "Example_WithMultipleCodeElements_Xml")]
            [TestCase("Example_sql_code_source_regions.sql", "SELECT *\nFROM  sys.tables", "SELECT *\nFROM sys.columns", TestName = "Example_WithMultipleCodeElements_Sql")]
            [TestCase("Example_cs_code_source_regions.cs", "var i = 1;", "var i = 2;", TestName = "Example_WithMultipleCodeElements_CSharp")]
            public void Example_WithMultipleCodeElements_WithSource_AndRegionAttributes(string sourceFile, string example1Rendered, string example2Rendered)
            {
                // Given
                string code = $@"
                   namespace Foo
                   {{
                       /// <example>
                       /// <code source=""{sourceFile}"" region=""Example 1"" />
                       /// some text
                       /// <code source=""{sourceFile}"" region=""Example 2"" />
                       /// <code language=""cs"">
                       /// var i = 1;
                       /// </code>
                       /// </example>
                       class Green
                       {{
                       }}
                   }}
               ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList(); // Make sure to materialize the result list

                // Then
                IDocument greenDocument = GetResult(results, "Green");
                greenDocument["Example"]
                    .ShouldBe($"\n    <pre><code>{example1Rendered}</code></pre>\n    some text\n    <pre><code>{example2Rendered}</code></pre>\n    <pre><code class=\"language-cs\">var i = 1;</code></pre>\n    ");
            }
            
            [Test]
            public void Example_WithCode_HavingCdataBlock()
            {
                // Given
                const string code = @"
                   namespace Foo
                   {
                       /// <example>
                       /// <code>
                       /// <![CDATA[
                       /// <foo>bar</foo>
                       /// ]]>
                       /// </code>
                       /// </example>
                       class Green
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
                GetResult(results, "Green")["Example"].ShouldBe("\n    <pre><code>&lt;foo&gt;bar&lt;/foo&gt;</code></pre>\n    ");
            }
            
            [Test]
            [TestCase("note", "<div class=\"note\">\n    Some note\n    </div>", TestName = "Example_WithCode_AndNoteElement")]
            [TestCase("tip", "<div class=\"tip\">\n    Some note\n    </div>", TestName = "Example_WithCode_AndTipNoteElement")]
            [TestCase("implement", "<div class=\"implement\">\n    Some note\n    </div>", TestName = "Example_WithCode_AndImplementNoteElement")]
            [TestCase("caller", "<div class=\"caller\">\n    Some note\n    </div>", TestName = "Example_WithCode_AndCallerNoteElement")]
            [TestCase("inherit", "<div class=\"inherit\">\n    Some note\n    </div>", TestName = "Example_WithCode_AndInheritNoteElement")]
            [TestCase("caution", "<div class=\"caution\">\n    Some note\n    </div>", TestName = "Example_WithCode_AndCautionNoteElement")]
            [TestCase("warning", "<div class=\"warning\">\n    Some note\n    </div>", TestName = "Example_WithCode_AndWarningNoteElement")]
            [TestCase("important", "<div class=\"important\">\n    Some note\n    </div>", TestName = "Example_WithCode_AndImportantNoteElement")]
            [TestCase("security", "<div class=\"security\">\n    Some note\n    </div>", TestName = "Example_WithCode_AndSecurityNoteElement")]
            public void Example_WithCode_AndNoteElement(string noteType, string noteRendered)
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
                            /// <example>
                            /// <code>
                            /// some code
                            /// </code>
                            /// More text
                            /// <note type=""{noteType}"">
                            /// Some note
                            /// </note>
                            /// </example>
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
                IDocument methodDocument = GetMember(results, "Green", "Method");
                methodDocument["Example"].ShouldBe($"\n    <pre><code>some code</code></pre>\n    More text\n    {noteRendered}\n    ");
            }
        }
    }
}