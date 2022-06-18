﻿using System.Collections.Concurrent;
using System.Linq;
using AngleSharp.Html.Parser;
using NUnit.Framework;
using Wyam.Common.Documents;
using Wyam.Testing;
using Wyam.Testing.Documents;

namespace Wyam.Html.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.Self | ParallelScope.Children)]
    public class ValidateLinksFixture : BaseFixture
    {
        public class GatherLinksTests : ValidateLinksFixture
        {
            [TestCase("<a href=\"/foo/bar\">baz</a>", "/foo/bar")]
            [TestCase("<a href=\"/foo/bar.html\">baz</a>", "/foo/bar.html")]
            [TestCase("<a href=\"http://foo.com/bar\">baz</a>", "http://foo.com/bar")]
            [TestCase("<a href=\"http://foo.com/bar.html\">baz</a>", "http://foo.com/bar.html")]
            [TestCase("<img src=\"/foo/bar.png\"></img>", "/foo/bar.png")]
            [TestCase("<img src=\"http://foo/bar.png\"></img>", "http://foo/bar.png")]
            [TestCase("<script src=\"/foo/bar.js\"></script>", "/foo/bar.js")]
            [TestCase("<script src=\"http://foo.com/bar.js\"></script>", "http://foo.com/bar.js")]
            public void FindsLinksInBody(string tag, string link)
            {
                // Given
                IDocument document = new TestDocument($"<html><head></head><body>{tag}</body></html>");
                HtmlParser parser = new HtmlParser();
                ConcurrentDictionary<string, ConcurrentBag<(string Source, string OuterHtml)>> links =
                    new ConcurrentDictionary<string, ConcurrentBag<(string Source, string OuterHtml)>>();

                // When
                ValidateLinks.GatherLinks(document, parser, links);

                // Then
                Assert.That(links.Count, Is.EqualTo(1));
                Assert.That(links.First().Key, Is.EqualTo(link));
            }

            [TestCase("<link href=\"/foo/bar.css\" rel=\"stylesheet\" />", "/foo/bar.css")]
            [TestCase("<link href=\"http://foo.com/bar.css\" rel=\"stylesheet\" />", "http://foo.com/bar.css")]
            [TestCase("<link rel=\"icon\" href=\"/foo/favicon.ico\" type=\"image/x-icon\">", "/foo/favicon.ico")]
            [TestCase("<script src=\"/foo/bar.js\"></script>", "/foo/bar.js")]
            [TestCase("<script src=\"http://foo.com/bar.js\"></script>", "http://foo.com/bar.js")]
            public void FindsLinksInHead(string tag, string link)
            {
                // Given
                IDocument document = new TestDocument($"<html><head>{tag}</head><body></body></html>");
                HtmlParser parser = new HtmlParser();
                ConcurrentDictionary<string, ConcurrentBag<(string Source, string OuterHtml)>> links =
                    new ConcurrentDictionary<string, ConcurrentBag<(string Source, string OuterHtml)>>();

                // When
                ValidateLinks.GatherLinks(document, parser, links);

                // Then
                Assert.That(links.Count, Is.EqualTo(1));
                Assert.That(links.First().Key, Is.EqualTo(link));
            }
        }
    }
}
