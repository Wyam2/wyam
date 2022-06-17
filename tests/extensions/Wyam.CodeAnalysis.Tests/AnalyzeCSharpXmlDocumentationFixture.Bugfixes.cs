using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Wyam.Common.Documents;
using Wyam.Common.Execution;
using Wyam.Common.Modules;

namespace Wyam.CodeAnalysis.Tests
{
    public partial class AnalyzeCSharpXmlDocumentationFixture
    {
        public partial class ExecuteTests
        {
            /// <summary>
            /// Test case for https://github.com/Wyam2/wyam/issues/92 
            /// </summary>
            [Test]
            public void Issue92_Summary_OnClass_WithNote_AndTableList_With2Columns()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// Checks if a resource is available at runtime.
                        /// </summary>
                        /// <remarks>
                        ///   <para>
                        ///   The specified property is set to <see langword=""true"" /> if the 
                        ///   requested resource is available at runtime, and <see langword=""false"" /> 
                        ///   if the resource is not available.
                        ///   </para>
                        ///   <note>
                        ///   we advise you to use the following functions instead:
                        ///   </note>
                        ///   <list type=""table"">
                        ///     <listheader>
                        ///         <term>Function</term>
                        ///         <description>Description</description>
                        ///     </listheader>
                        ///     <item>
                        ///         <term><see cref=""Red.A"" /></term>
                        ///         <description>Determines whether the specified file exists.</description>
                        ///     </item>
                        ///     <item>
                        ///         <term><see cref=""Red.B"" /></term>
                        ///         <description>Determines whether the given path refers to an existing directory on disk.</description>
                        ///     </item>
                        ///     <item>
                        ///         <term><see cref=""Red.C"" /></term>
                        ///         <description>Checks whether the specified framework exists..</description>
                        ///     </item>
                        ///     <item>
                        ///         <term><see cref=""Red.D"" /></term>
                        ///         <description>Checks whether the SDK for the specified framework is installed.</description>
                        ///     </item>
                        ///   </list> 
                        /// </remarks>
                        class Green
                        { }

                        class Red
                        {
                            void A()
                            { }
                            void B()
                            { }
                            void C()
                            { }
                            void D()
                            { }
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
                Checks if a resource is available at runtime.
                ".Replace("\r\n", "\n").Replace("                ", "    "));
                GetResult(results, "Green")["Remarks"].ShouldBe(
                    @"
                <p>
                The specified property is set to <code>true</code> if the 
                requested resource is available at runtime, and <code>false</code> 
                if the resource is not available.
                </p>
                <div class=""note"">
                we advise you to use the following functions instead:
                </div>
                <table class=""table""><thead><tr><th>Function</th><th>Description</th></tr></thead><tbody><tr>
                      <td><code><a href=""/Foo/Red/69E7B8E6.html"">A()</a></code></td>
                      <td>Determines whether the specified file exists.</td>
                  </tr><tr>
                      <td><code><a href=""/Foo/Red/D3B6B17F.html"">B()</a></code></td>
                      <td>Determines whether the given path refers to an existing directory on disk.</td>
                  </tr><tr>
                      <td><code><a href=""/Foo/Red/4586B608.html"">C()</a></code></td>
                      <td>Checks whether the specified framework exists..</td>
                  </tr><tr>
                      <td><code><a href=""/Foo/Red/E613D296.html"">D()</a></code></td>
                      <td>Checks whether the SDK for the specified framework is installed.</td>
                  </tr></tbody></table> 
    ".Replace("\r\n", "\n").Replace("                ", "      "));
            }

            [Test]
            public void Issue92_Example_OnClass_WithBulletList_AndItems_HavingOnlyDescription()
            {
               // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <example>
                        ///   <list type=""bullet"">
                        ///   <item>
                        ///   <description>Full Name &lt;address@abcxyz.com&gt;</description>
                        ///   </item>
                        ///   <item>
                        ///   <description>&lt;address@abcxyz.com&gt; Full Name</description>
                        ///   </item>
                        ///   <item>
                        ///   <description>(Full Name) address@abcxyz.com</description>
                        ///   </item>
                        ///   <item>
                        ///   <description>address@abcxyz.com (Full Name)</description>
                        ///   </item>
                        ///   <item>
                        ///   <description>address@abcxyz.com</description>
                        ///   </item>
                        ///   </list>
                        /// </example>
                        class Green
                        { }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                GetResult(results, "Green")["Example"].ShouldBe(
                    @"
                <ul>
                <li>
                <span class=""description"">Full Name &lt;address@abcxyz.com&gt;</span>
                </li>
                <li>
                <span class=""description"">&lt;address@abcxyz.com&gt; Full Name</span>
                </li>
                <li>
                <span class=""description"">(Full Name) address@abcxyz.com</span>
                </li>
                <li>
                <span class=""description"">address@abcxyz.com (Full Name)</span>
                </li>
                <li>
                <span class=""description"">address@abcxyz.com</span>
                </li>
                </ul>
    ".Replace("\r\n", "\n").Replace("                ", "      "));
            }
        }
    }
}