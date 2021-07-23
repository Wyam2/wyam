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
            public void Permissions_OnMethod_SinglePermission()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Green
                        {
                            /// <permission cref=""SecurityPermission"">
                            /// <see cref=""SecurityPermissionFlag.Execution"">Execution</see> privilege.
                            /// </permission>
                            [SecurityPermission(SecurityAction.Demand, Execution = true)]
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
                    d => d.List<ReferenceComment>("Permissions").Count.ShouldBe(1),
                    d => d.List<ReferenceComment>("Permissions")[0].Html.ShouldBe("\n    <code>SecurityPermissionFlag.Execution</code> privilege.\n    "));
            }
            
            [Test]
            public void Permissions_OnMethod_MultiplePermissions()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Green
                        {
                            /// <permission cref=""SecurityPermission"">
                            /// <see cref=""SecurityPermissionFlag.Execution"">Execution</see> privilege.
                            /// </permission>
                            /// <permission cref=""ReflectionPermission"">
                            /// <see cref=""ReflectionPermissionFlag.MemberAccess"">Member access</see> privilege.
                            /// </permission>
                            [SecurityPermission(SecurityAction.Demand, Execution = true)]
                            [ReflectionPermission(SecurityAction.Demand, MemberAccess = true)]
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
                    d => d.List<ReferenceComment>("Permissions").Count.ShouldBe(2),
                    d => d.List<ReferenceComment>("Permissions")[0].Html.ShouldBe("\n    <code>SecurityPermissionFlag.Execution</code> privilege.\n    "),
                    d => d.List<ReferenceComment>("Permissions")[1].Html.ShouldBe("\n    <code>ReflectionPermissionFlag.MemberAccess</code> privilege.\n    "));
            }
        }
    }
}