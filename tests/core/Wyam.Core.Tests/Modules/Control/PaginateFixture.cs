﻿using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wyam.Common.Documents;
using Wyam.Common.Meta;
using Wyam.Core.Documents;
using Wyam.Core.Execution;
using Wyam.Core.Meta;
using Wyam.Core.Modules.Control;
using Wyam.Core.Modules.Extensibility;
using Wyam.Testing;
using Wyam.Testing.Modules;
using Wyam.Common.Execution;
using Wyam.Common.Util;

namespace Wyam.Core.Tests.Modules.Control
{
    [TestFixture]
    [NonParallelizable]
    public class PaginateFixture : BaseFixture
    {
        public class ExecuteTests : PaginateFixture
        {
            [Test]
            public void PaginateSetsCorrectMetadata()
            {
                // Given
                List<int> currentPage = new List<int>();
                List<int> totalPages = new List<int>();
                List<int> totalItems = new List<int>();
                List<bool> hasNextPage = new List<bool>();
                List<bool> hasPreviousPage = new List<bool>();
                Engine engine = new Engine();
                CountModule count = new CountModule("A")
                {
                    AdditionalOutputs = 7
                };
                Paginate paginate = new Paginate(3, count);
                Execute gatherData = new Execute(
                    (d, c) =>
                    {
                        currentPage.Add(d.Get<int>(Keys.CurrentPage));
                        totalPages.Add(d.Get<int>(Keys.TotalPages));
                        totalItems.Add(d.Get<int>(Keys.TotalItems));
                        hasNextPage.Add(d.Bool(Keys.HasNextPage));
                        hasPreviousPage.Add(d.Bool(Keys.HasPreviousPage));
                        return null;
                    },
                    false);
                engine.Pipelines.Add(paginate, gatherData);

                // When
                engine.Execute();

                // Then
                CollectionAssert.AreEqual(new[] { 1, 2, 3 }, currentPage);
                CollectionAssert.AreEqual(new[] { 3, 3, 3 }, totalPages);
                CollectionAssert.AreEqual(new[] { 8, 8, 8 }, totalItems);
                CollectionAssert.AreEqual(new[] { true, true, false }, hasNextPage);
                CollectionAssert.AreEqual(new[] { false, true, true }, hasPreviousPage);
            }

            [Test]
            public void PaginateSetsDocumentsInMetadata()
            {
                // Given
                List<IList<string>> content = new List<IList<string>>();
                Engine engine = new Engine();
                CountModule count = new CountModule("A")
                {
                    AdditionalOutputs = 7
                };
                Paginate paginate = new Paginate(3, count);
                Execute gatherData = new Execute(
                    (d, c) =>
                    {
                        content.Add(d.Get<IList<IDocument>>(Keys.PageDocuments).Select(x => x.Content).ToList());
                        return null;
                    },
                    false);
                engine.Pipelines.Add(paginate, gatherData);

                // When
                engine.Execute();

                // Then
                Assert.AreEqual(3, content.Count);
                CollectionAssert.AreEqual(new[] { "1", "2", "3" }, content[0]);
                CollectionAssert.AreEqual(new[] { "4", "5", "6" }, content[1]);
                CollectionAssert.AreEqual(new[] { "7", "8" }, content[2]);
            }

            [Test]
            public void SetsPreviousAndNextDocuments()
            {
                // Given
                List<IList<string>> previousPages = new List<IList<string>>();
                List<IList<string>> nextPages = new List<IList<string>>();
                Engine engine = new Engine();
                CountModule count = new CountModule("A")
                {
                    AdditionalOutputs = 7
                };
                Paginate paginate = new Paginate(3, count);
                Execute gatherData = new Execute(
                    (d, c) =>
                    {
                        previousPages.Add(d.Document(Keys.PreviousPage)?.Get<IList<IDocument>>(Keys.PageDocuments).Select(x => x.Content).ToList());
                        nextPages.Add(d.Document(Keys.NextPage)?.Get<IList<IDocument>>(Keys.PageDocuments).Select(x => x.Content).ToList());
                        return null;
                    },
                    false);
                engine.Pipelines.Add(paginate, gatherData);

                // When
                engine.Execute();

                // Then
                Assert.AreEqual(3, previousPages.Count);
                Assert.AreEqual(3, nextPages.Count);
                CollectionAssert.AreEqual(null, previousPages[0]);
                CollectionAssert.AreEqual(new[] { "1", "2", "3" }, previousPages[1]);
                CollectionAssert.AreEqual(new[] { "4", "5", "6" }, previousPages[2]);
                CollectionAssert.AreEqual(new[] { "4", "5", "6" }, nextPages[0]);
                CollectionAssert.AreEqual(new[] { "7", "8" }, nextPages[1]);
                CollectionAssert.AreEqual(null, nextPages[2]);
            }

            [Test]
            public void ExcludesDocumentsThatFailPredicate()
            {
                // Given
                List<IList<string>> content = new List<IList<string>>();
                Engine engine = new Engine();
                CountModule count = new CountModule("A")
                {
                    AdditionalOutputs = 7
                };
                Paginate paginate = new Paginate(3, count).Where((doc, ctx) => doc.Content != "5");
                Execute gatherData = new Execute(
                    (d, c) =>
                    {
                        content.Add(d.Get<IList<IDocument>>(Keys.PageDocuments).Select(x => x.Content).ToList());
                        return null;
                    },
                    false);
                engine.Pipelines.Add(paginate, gatherData);

                // When
                engine.Execute();

                // Then
                Assert.AreEqual(3, content.Count);
                CollectionAssert.AreEqual(new[] { "1", "2", "3" }, content[0]);
                CollectionAssert.AreEqual(new[] { "4", "6", "7" }, content[1]);
                CollectionAssert.AreEqual(new[] { "8" }, content[2]);
            }
        }
    }
}
