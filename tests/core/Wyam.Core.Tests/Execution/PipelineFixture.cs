using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Wyam.Common;
using Wyam.Common.Tracing;
using Wyam.Core.Execution;
using Wyam.Core.Modules.Control;
using Wyam.Testing;
using Wyam.Testing.Modules;
using Wyam.Common.Execution;

namespace Wyam.Core.Tests.Execution
{
    [TestFixture]
    [Parallelizable(ParallelScope.Self | ParallelScope.Children)]
    public class PipelineFixture : BaseFixture
    {
        public class ExecuteTests : PipelineFixture
        {
            [Test]
            public void ReprocessesPreviousDocumentsWithDistinctSources()
            {
                // Given
                Engine engine = new Engine();
                CountModule a = new CountModule("A")
                {
                    CloneSource = true,
                    AdditionalOutputs = 1
                };
                CountModule b = new CountModule("B")
                {
                    CloneSource = true,
                    AdditionalOutputs = 2
                };
                CountModule c = new CountModule("C")
                {
                    CloneSource = true,
                    AdditionalOutputs = 3
                };
                engine.Pipelines.Add("Count", a, b, c).WithProcessDocumentsOnce();

                // When
                engine.Execute();
                engine.Execute();

                // Then
                Assert.AreEqual(24, engine.Documents.FromPipeline("Count").Count());
                Assert.AreEqual(2, a.ExecuteCount);
                Assert.AreEqual(2, b.ExecuteCount);
                Assert.AreEqual(2, c.ExecuteCount);
                Assert.AreEqual(2, a.InputCount);
                Assert.AreEqual(4, b.InputCount);
                Assert.AreEqual(12, c.InputCount);
                Assert.AreEqual(4, a.OutputCount);
                Assert.AreEqual(12, b.OutputCount);
                Assert.AreEqual(48, c.OutputCount);
            }

            [Test]
            public void DoesNotProcessPreviousDocumentsWhenSameSource()
            {
                // Given
                Engine engine = new Engine();
                CountModule a = new CountModule("A")
                {
                    CloneSource = true,
                    AdditionalOutputs = 1
                };
                CountModule b = new CountModule("B")
                {
                    AdditionalOutputs = 2
                };
                CountModule c = new CountModule("C")
                {
                    AdditionalOutputs = 3
                };
                engine.Pipelines.Add("Count", a, b, c).WithProcessDocumentsOnce();

                // When
                engine.Execute();
                a.Value = 0; // Reset a.Value so output from a has same content
                engine.Execute();

                // Then
                Assert.AreEqual(24, engine.Documents.FromPipeline("Count").Count());
                Assert.AreEqual(2, a.ExecuteCount);
                Assert.AreEqual(2, b.ExecuteCount);
                Assert.AreEqual(2, c.ExecuteCount);
                Assert.AreEqual(2, a.InputCount);
                Assert.AreEqual(2, b.InputCount);
                Assert.AreEqual(6, c.InputCount);
                Assert.AreEqual(4, a.OutputCount);
                Assert.AreEqual(6, b.OutputCount);
                Assert.AreEqual(24, c.OutputCount);
            }

            [Test]
            public void ReprocessPreviousDocumentsWithDifferentContent()
            {
                // Given
                Engine engine = new Engine();
                CountModule a = new CountModule("A")
                {
                    CloneSource = true,
                    AdditionalOutputs = 1
                };
                CountModule b = new CountModule("B")
                {
                    AdditionalOutputs = 2
                };
                CountModule c = new CountModule("C")
                {
                    AdditionalOutputs = 3
                };
                engine.Pipelines.Add("Count", a, b, c).WithProcessDocumentsOnce();

                // When
                engine.Execute();
                engine.Execute();

                // Then
                Assert.AreEqual(24, engine.Documents.FromPipeline("Count").Count());
                Assert.AreEqual(2, a.ExecuteCount);
                Assert.AreEqual(2, b.ExecuteCount);
                Assert.AreEqual(2, c.ExecuteCount);
                Assert.AreEqual(2, a.InputCount);
                Assert.AreEqual(4, b.InputCount);
                Assert.AreEqual(12, c.InputCount);
                Assert.AreEqual(4, a.OutputCount);
                Assert.AreEqual(12, b.OutputCount);
                Assert.AreEqual(48, c.OutputCount);
            }

            [Test]
            public void SameSourceIsIgnoredIfAlreadySet()
            {
                // Given
                Engine engine = new Engine();
                CountModule a = new CountModule("A")
                {
                    CloneSource = true
                };
                CountModule b = new CountModule("A")
                {
                    CloneSource = true
                };
                engine.Pipelines.Add("Count", a, b);

                // When
                engine.Execute();

                // Then
                Assert.AreEqual(1, engine.Documents.FromPipeline("Count").Count());
                Assert.AreEqual(1, a.ExecuteCount);
                Assert.AreEqual(1, b.ExecuteCount);
                Assert.AreEqual(1, a.InputCount);
                Assert.AreEqual(1, b.InputCount);
                Assert.AreEqual(1, a.OutputCount);
                Assert.AreEqual(1, b.OutputCount);
            }

            [Test]
            public void SameSourceThrowsException()
            {
                // Given
                Engine engine = new Engine();
                CountModule a = new CountModule("A")
                {
                    CloneSource = true
                };
                CountModule b = new CountModule("B");
                CountModule c = new CountModule("A")
                {
                    CloneSource = true
                };
                engine.Pipelines.Add("Count", a, new Concat(b, c));

                // When, Then
                Assert.Throws<Exception>(() => engine.Execute());
            }

            [Test]
            public void DisposingPipelineDisposesModules()
            {
                // Given
                Engine engine = new Engine();
                DisposableCountModule a = new DisposableCountModule("A");
                DisposableCountModule b = new DisposableCountModule("B");
                CountModule c = new CountModule("C");
                engine.Pipelines.Add("Count", a, new Concat(b, c));

                // When
                engine.Dispose();

                // Then
                Assert.IsTrue(a.Disposed);
                Assert.IsTrue(b.Disposed);
            }

            private class DisposableCountModule : CountModule, IDisposable
            {
                public bool Disposed { get; private set; }

                public DisposableCountModule(string valueKey)
                    : base(valueKey)
                {
                }

                public void Dispose()
                {
                    Disposed = true;
                }
            }
        }
    }
}
