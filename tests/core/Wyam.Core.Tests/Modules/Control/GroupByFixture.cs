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
using Wyam.Core.Modules.Metadata;
using Wyam.Testing;
using Wyam.Testing.Modules;
using Wyam.Common.Execution;

namespace Wyam.Core.Tests.Modules.Control
{
    [TestFixture]
    [NonParallelizable]
    public class GroupByFixture : BaseFixture
    {
        public class ExecuteTests : GroupByFixture
        {
            [Test]
            public void SetsCorrectMetadata()
            {
                // Given
                List<int> groupKey = new List<int>();
                Engine engine = new Engine();
                CountModule count = new CountModule("A")
                {
                    AdditionalOutputs = 7
                };
                GroupBy groupBy = new GroupBy((d, c) => d.Get<int>("A") % 3, count);
                Execute gatherData = new Execute(
                    (d, c) =>
                    {
                        groupKey.Add(d.Get<int>(Keys.GroupKey));
                        return null;
                    },
                    false);
                engine.Pipelines.Add(groupBy, gatherData);

                // When
                engine.Execute();

                // Then
                CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, groupKey);
            }

            [Test]
            public void SetsDocumentsInMetadata()
            {
                // Given
                List<IList<string>> content = new List<IList<string>>();
                Engine engine = new Engine();
                CountModule count = new CountModule("A")
                {
                    AdditionalOutputs = 7
                };
                GroupBy groupBy = new GroupBy((d, c) => d.Get<int>("A") % 3, count);
                OrderBy orderBy = new OrderBy((d, c) => d.Get<int>(Keys.GroupKey));
                Execute gatherData = new Execute(
                    (d, c) =>
                    {
                        content.Add(d.Get<IList<IDocument>>(Keys.GroupDocuments).Select(x => x.Content).ToList());
                        return null;
                    },
                    false);
                engine.Pipelines.Add(groupBy, orderBy, gatherData);

                // When
                engine.Execute();

                // Then
                Assert.AreEqual(3, content.Count);
                CollectionAssert.AreEquivalent(new[] { "3", "6" }, content[0]);
                CollectionAssert.AreEquivalent(new[] { "1", "4", "7" }, content[1]);
                CollectionAssert.AreEquivalent(new[] { "2", "5", "8" }, content[2]);
            }

            [Test]
            public void GroupByMetadataKey()
            {
                // Given
                List<int> groupKey = new List<int>();
                Engine engine = new Engine();
                CountModule count = new CountModule("A")
                {
                    AdditionalOutputs = 7
                };
                Core.Modules.Metadata.Meta meta = new Core.Modules.Metadata.Meta("GroupMetadata", (d, c) => d.Get<int>("A") % 3);
                GroupBy groupBy = new GroupBy("GroupMetadata", count, meta);
                Execute gatherData = new Execute(
                    (d, c) =>
                    {
                        groupKey.Add(d.Get<int>(Keys.GroupKey));
                        return null;
                    },
                    false);
                engine.Pipelines.Add(groupBy, gatherData);

                // When
                engine.Execute();

                // Then
                CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, groupKey);
            }

            [Test]
            public void GroupByMetadataKeyWithMissingMetadata()
            {
                // Given
                List<int> groupKey = new List<int>();
                Engine engine = new Engine();
                CountModule count = new CountModule("A")
                {
                    AdditionalOutputs = 7
                };
                Execute meta = new Execute(
                    (d, c) =>
                    {
                        int groupMetadata = d.Get<int>("A") % 3;
                        return groupMetadata == 0 ? d : c.GetDocument(d, new MetadataItems { { "GroupMetadata", groupMetadata } });
                    },
                    false);
                GroupBy groupBy = new GroupBy("GroupMetadata", count, meta);
                Execute gatherData = new Execute(
                    (d, c) =>
                    {
                        groupKey.Add(d.Get<int>(Keys.GroupKey));
                        return null;
                    },
                    false);
                engine.Pipelines.Add(groupBy, gatherData);

                // When
                engine.Execute();

                // Then
                CollectionAssert.AreEquivalent(new[] { 1, 2 }, groupKey);
            }

            [Test]
            public void ExcludesDocumentsThatDontMatchPredicate()
            {
                // Given
                List<int> groupKey = new List<int>();
                Engine engine = new Engine();
                CountModule count = new CountModule("A")
                {
                    AdditionalOutputs = 7
                };
                GroupBy groupBy = new GroupBy((d, c) => d.Get<int>("A") % 3, count)
                    .Where((d, c) => d.Get<int>("A") % 3 != 0);
                Execute gatherData = new Execute(
                    (d, c) =>
                    {
                        groupKey.Add(d.Get<int>(Keys.GroupKey));
                        return null;
                    },
                    false);
                engine.Pipelines.Add(groupBy, gatherData);

                // When
                engine.Execute();

                // Then
                CollectionAssert.AreEquivalent(new[] { 1, 2 }, groupKey);
            }
        }
    }
}
