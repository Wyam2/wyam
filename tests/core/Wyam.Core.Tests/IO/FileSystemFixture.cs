﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wyam.Common.IO;
using Wyam.Core.IO;
using Wyam.Testing;
using Wyam.Testing.IO;

namespace Wyam.Core.Tests.IO
{
    [TestFixture]
    [Parallelizable(ParallelScope.Self | ParallelScope.Children)]
    public class FileSystemFixture : BaseFixture
    {
        public class ConstructorTests : FileSystemFixture
        {
            [Test]
            public void AddsDefaultInputPath()
            {
                // Given, When
                FileSystem fileSystem = new FileSystem();

                // Then
                CollectionAssert.AreEquivalent(new[] { "theme", "input" }, fileSystem.InputPaths.Select(x => x.FullPath));
            }
        }

        public class RootPathTests : FileSystemFixture
        {
            [Test]
            public void SetThrowsForNullValue()
            {
                // Given
                FileSystem fileSystem = new FileSystem();

                // When, Then
                Assert.Throws<ArgumentNullException>(() => fileSystem.RootPath = null);
            }

            [Test]
            public void SetThrowsForRelativePath()
            {
                // Given
                FileSystem fileSystem = new FileSystem();

                // When, Then
                Assert.Throws<ArgumentException>(() => fileSystem.RootPath = "foo");
            }

            [Test]
            public void CanSet()
            {
                // Given
                FileSystem fileSystem = new FileSystem();

                // When
                fileSystem.RootPath = "/foo/bar";

                // Then
                Assert.AreEqual("/foo/bar", fileSystem.RootPath.FullPath);
            }
        }

        public class OutputPathTests : FileSystemFixture
        {
            [Test]
            public void SetThrowsForNullValue()
            {
                // Given
                FileSystem fileSystem = new FileSystem();

                // When, Then
                Assert.Throws<ArgumentNullException>(() => fileSystem.OutputPath = null);
            }

            [Test]
            public void CanSet()
            {
                // Given
                FileSystem fileSystem = new FileSystem();

                // When
                fileSystem.OutputPath = "/foo/bar";

                // Then
                Assert.AreEqual("/foo/bar", fileSystem.OutputPath.FullPath);
            }
        }

        public class GetInputFileTests : FileSystemFixture
        {
            [Test]
            [TestCase("foo.txt", "/a/b/c/foo.txt")]
            [TestCase("bar.txt", "/a/x/bar.txt")]
            [TestCase("baz.txt", "/a/y/baz.txt")]
            [TestCase("/a/b/c/foo.txt", "/a/b/c/foo.txt")]
            [TestCase("/z/baz.txt", "/z/baz.txt")]
            public void ReturnsInputFile(string input, string expected)
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                fileSystem.RootPath = "/a";
                fileSystem.InputPaths.Add("b/c");
                fileSystem.InputPaths.Add("b/d");
                fileSystem.InputPaths.Add("x");
                fileSystem.InputPaths.Add("y");
                fileSystem.FileProviders.Add(NormalizedPath.DefaultFileProvider.Scheme, GetFileProvider());

                // When
                IFile result = fileSystem.GetInputFile(input);

                // Then
                Assert.AreEqual(expected, result.Path.FullPath);
            }

            [Test]
            public void ReturnsInputFileAboveInputDirectory()
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                fileSystem.RootPath = "/a";
                fileSystem.InputPaths.Add("x/t");
                fileSystem.FileProviders.Add(NormalizedPath.DefaultFileProvider.Scheme, GetFileProvider());

                // When
                IFile result = fileSystem.GetInputFile("../bar.txt");

                // Then
                Assert.AreEqual("/a/x/bar.txt", result.Path.FullPath);
            }

            [Test]
            public void ReturnsInputFileWhenInputDirectoryAboveRoot()
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                fileSystem.RootPath = "/a/b";
                fileSystem.InputPaths.Add("../x");
                fileSystem.FileProviders.Add(NormalizedPath.DefaultFileProvider.Scheme, GetFileProvider());

                // When
                IFile result = fileSystem.GetInputFile("bar.txt");

                // Then
                Assert.AreEqual("/a/x/bar.txt", result.Path.FullPath);
            }

            [Test]
            public void ReturnsInputFileWhenInputDirectoryAndFileAscend()
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                fileSystem.RootPath = "/a/b";
                fileSystem.InputPaths.Add("../x/y");
                fileSystem.FileProviders.Add(NormalizedPath.DefaultFileProvider.Scheme, GetFileProvider());

                // When
                IFile result = fileSystem.GetInputFile("../bar.txt");

                // Then
                Assert.AreEqual("/a/x/bar.txt", result.Path.FullPath);
            }
        }

        public class GetInputDirectoryTests : FileSystemFixture
        {
            [Test]
            public void ReturnsVirtualInputDirectoryForRelativePath()
            {
                // Given
                FileSystem fileSystem = new FileSystem();

                // When
                IDirectory result = fileSystem.GetInputDirectory("A/B/C");

                // Then
                Assert.IsInstanceOf<VirtualInputDirectory>(result);
                Assert.AreEqual("A/B/C", result.Path.FullPath);
            }

            [Test]
            public void ReturnsVirtualInputDirectoryForAscendingPath()
            {
                // Given
                FileSystem fileSystem = new FileSystem();

                // When
                IDirectory result = fileSystem.GetInputDirectory("../A/B/C");

                // Then
                Assert.IsInstanceOf<VirtualInputDirectory>(result);
                Assert.AreEqual("../A/B/C", result.Path.FullPath);
            }

            [Test]
            public void ReturnsVirtualInputDirectoryForNullPath()
            {
                // Given
                FileSystem fileSystem = new FileSystem();

                // When
                IDirectory result = fileSystem.GetInputDirectory();

                // Then
                Assert.IsInstanceOf<VirtualInputDirectory>(result);
                Assert.AreEqual(".", result.Path.FullPath);
            }

            [Test]
            public void ReturnsDirectoryForAbsolutePath()
            {
                // Given
                FileSystem fileSystem = new FileSystem();

                // When
                IDirectory result = fileSystem.GetInputDirectory("/A/B/C");

                // Then
                Assert.AreEqual("/A/B/C", result.Path.FullPath);
            }
        }

        public class GetInputDirectoriesTests : FileSystemFixture
        {
            [Test]
            public void ReturnsCombinedInputDirectories()
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                fileSystem.RootPath = "/a";
                fileSystem.InputPaths.Add("b/c");
                fileSystem.InputPaths.Add("b/d");
                fileSystem.InputPaths.Add("x");
                fileSystem.InputPaths.Add("y");
                fileSystem.InputPaths.Add("../z");
                fileSystem.FileProviders.Add(NormalizedPath.DefaultFileProvider.Scheme, GetFileProvider());

                // When
                IEnumerable<IDirectory> result = fileSystem.GetInputDirectories();

                // Then
                CollectionAssert.AreEquivalent(
                    new[]
                    {
                        "/a/theme",
                        "/a/input",
                        "/a/b/c",
                        "/a/b/d",
                        "/a/x",
                        "/a/y",
                        "/z"
                    },
                    result.Select(x => x.Path.FullPath));
            }
        }

        public class GetContainingInputPathTests : FileSystemFixture
        {
            [Test]
            public void ThrowsForNullPath()
            {
                // Given
                FileSystem fileSystem = new FileSystem();

                // When, Then
                Assert.Throws<ArgumentNullException>(() => fileSystem.GetContainingInputPath(null));
            }

            [TestCase("/a/b/c/foo.txt", "/a/b")]
            [TestCase("/a/x/bar.txt", "/a/x")]
            [TestCase("/a/x/baz.txt", "/a/x")]
            [TestCase("/z/baz.txt", null)]
            [TestCase("/a/b/c/../e/foo.txt", "/a/b")]
            [TestCase("/a/b/c", "/a/b")]
            [TestCase("/a/x", "/a/x")]
            public void ShouldReturnContainingPathForAbsolutePath(string path, string expected)
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                fileSystem.RootPath = "/a";
                fileSystem.InputPaths.Add("b");
                fileSystem.InputPaths.Add("x");
                fileSystem.FileProviders.Add(string.Empty, GetFileProvider());

                // When
                DirectoryPath inputPathFromFilePath = fileSystem.GetContainingInputPath(new FilePath(path));
                DirectoryPath inputPathFromDirectoryPath = fileSystem.GetContainingInputPath(new DirectoryPath(path));

                // Then
                Assert.AreEqual(expected, inputPathFromFilePath?.FullPath);
                Assert.AreEqual(expected, inputPathFromDirectoryPath?.FullPath);
            }

            [TestCase("/a/b/c/foo.txt", "/a/y/../b")]
            [TestCase("/a/x/bar.txt", "/a/y/../x")]
            [TestCase("/a/x/baz.txt", "/a/y/../x")]
            [TestCase("/z/baz.txt", null)]
            [TestCase("/a/b/c/../e/foo.txt", "/a/y/../b")]
            public void ShouldReturnContainingPathForInputPathAboveRootPath(string path, string expected)
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                fileSystem.RootPath = "/a/y";
                fileSystem.InputPaths.Add("../b");
                fileSystem.InputPaths.Add("../x");
                fileSystem.FileProviders.Add(string.Empty, GetFileProvider());

                // When
                DirectoryPath inputPathFromFilePath = fileSystem.GetContainingInputPath(new FilePath(path));
                DirectoryPath inputPathFromDirectoryPath = fileSystem.GetContainingInputPath(new DirectoryPath(path));

                // Then
                Assert.AreEqual(expected, inputPathFromFilePath?.FullPath);
                Assert.AreEqual(expected, inputPathFromDirectoryPath?.FullPath);
            }

            [TestCase("c/foo.txt", "/a/b")]
            [TestCase("bar.txt", "/a/x")]
            [TestCase("baz.txt", null)]
            [TestCase("z/baz.txt", null)]
            [TestCase("c/../e/foo.txt", null)]
            [TestCase("c/e/../foo.txt", "/a/b")]
            public void ShouldReturnContainingPathForRelativeFilePath(string path, string expected)
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                fileSystem.RootPath = "/a";
                fileSystem.InputPaths.Add("b");
                fileSystem.InputPaths.Add("x");
                fileSystem.FileProviders.Add(NormalizedPath.DefaultFileProvider.Scheme, GetFileProvider());

                // When
                DirectoryPath inputPath = fileSystem.GetContainingInputPath(new FilePath(path));

                // Then
                Assert.AreEqual(expected, inputPath?.FullPath);
            }

            [TestCase("c", "/a/b")]
            [TestCase("z", "/a/y")]
            [TestCase("r", null)]
            [TestCase("c/e", null)]
            public void ShouldReturnContainingPathForRelativeDirectoryPath(string path, string expected)
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                fileSystem.RootPath = "/a";
                fileSystem.InputPaths.Add("b");
                fileSystem.InputPaths.Add("y");
                fileSystem.FileProviders.Add(NormalizedPath.DefaultFileProvider.Scheme, GetFileProvider());

                // When
                DirectoryPath inputPath = fileSystem.GetContainingInputPath(new DirectoryPath(path));

                // Then
                Assert.AreEqual(expected, inputPath?.FullPath);
            }

            [Test]
            public void ShouldReturnContainingPathWhenOtherInputPathStartsTheSame()
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                fileSystem.RootPath = "/a";
                fileSystem.InputPaths.Add("yz");
                fileSystem.InputPaths.Add("y");
                TestFileProvider fileProvider = (TestFileProvider)GetFileProvider();
                fileProvider.AddDirectory("yz");
                fileProvider.AddDirectory("y");
                fileProvider.AddFile("/a/yz/baz.txt");
                fileSystem.FileProviders.Add(NormalizedPath.DefaultFileProvider.Scheme, fileProvider);

                // When
                DirectoryPath inputPath = fileSystem.GetContainingInputPath(new FilePath("baz.txt"));

                // Then
                Assert.AreEqual("/a/yz", inputPath?.FullPath);
            }
        }

        public class GetFileProviderTests : FileSystemFixture
        {
            [Test]
            public void ThrowsForNullPath()
            {
                // Given
                FileSystem fileSystem = new FileSystem();

                // When, Then
                Assert.Throws<ArgumentNullException>(() => fileSystem.GetFileProvider(null));
            }

            [Test]
            public void ThrowsForRelativeDirectoryPath()
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                DirectoryPath relativePath = new DirectoryPath("A/B/C");

                // When, Then
                Assert.Throws<ArgumentException>(() => fileSystem.GetFileProvider(relativePath));
            }

            [Test]
            public void ThrowsForRelativeFilePath()
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                FilePath relativePath = new FilePath("A/B/C.txt");

                // When, Then
                Assert.Throws<ArgumentException>(() => fileSystem.GetFileProvider(relativePath));
            }

            [Test]
            public void ReturnsDefaultProviderForDirectoryPath()
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                IFileProvider defaultProvider = new TestFileProvider();
                IFileProvider fooProvider = new TestFileProvider();
                fileSystem.FileProviders.Add(NormalizedPath.DefaultFileProvider.Scheme, defaultProvider);
                fileSystem.FileProviders.Add("foo", fooProvider);
                DirectoryPath path = new DirectoryPath("/a/b/c");

                // When
                IFileProvider result = fileSystem.GetFileProvider(path);

                // Then
                Assert.AreEqual(defaultProvider, result);
            }

            [Test]
            public void ReturnsDefaultProviderForFilePath()
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                IFileProvider defaultProvider = new TestFileProvider();
                IFileProvider fooProvider = new TestFileProvider();
                fileSystem.FileProviders.Add(NormalizedPath.DefaultFileProvider.Scheme, defaultProvider);
                fileSystem.FileProviders.Add("foo", fooProvider);
                FilePath path = new FilePath("/a/b/c.txt");

                // When
                IFileProvider result = fileSystem.GetFileProvider(path);

                // Then
                Assert.AreEqual(defaultProvider, result);
            }

            [Test]
            public void ReturnsOtherProviderForDirectoryPath()
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                IFileProvider defaultProvider = new TestFileProvider();
                IFileProvider fooProvider = new TestFileProvider();
                fileSystem.FileProviders.Add(NormalizedPath.DefaultFileProvider.Scheme, defaultProvider);
                fileSystem.FileProviders.Add("foo", fooProvider);
                DirectoryPath path = new DirectoryPath("foo", "/a/b/c");

                // When
                IFileProvider result = fileSystem.GetFileProvider(path);

                // Then
                Assert.AreEqual(fooProvider, result);
            }

            [Test]
            public void ReturnsOtherProviderForFilePath()
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                IFileProvider defaultProvider = new TestFileProvider();
                IFileProvider fooProvider = new TestFileProvider();
                fileSystem.FileProviders.Add(NormalizedPath.DefaultFileProvider.Scheme, defaultProvider);
                fileSystem.FileProviders.Add("foo", fooProvider);
                FilePath path = new FilePath("foo", "/a/b/c.txt");

                // When
                IFileProvider result = fileSystem.GetFileProvider(path);

                // Then
                Assert.AreEqual(fooProvider, result);
            }

            [Test]
            public void ThrowsIfProviderNotFoundForDirectoryPath()
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                DirectoryPath path = new DirectoryPath("foo", "/a/b/c");

                // When, Then
                Assert.Throws<KeyNotFoundException>(() => fileSystem.GetFileProvider(path));
            }

            [Test]
            public void ThrowsIfProviderNotFoundForFilePath()
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                FilePath path = new FilePath("foo", "/a/b/c.txt");

                // When, Then
                Assert.Throws<KeyNotFoundException>(() => fileSystem.GetFileProvider(path));
            }
        }

        public class GetFilesTests : FileSystemFixture
        {
            [Test]
            public void ShouldThrowForNullDirectory()
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                fileSystem.FileProviders.Add(NormalizedPath.DefaultFileProvider.Scheme, GetFileProvider());

                // When, Then
                Assert.Throws<ArgumentNullException>(() => fileSystem.GetFiles((IDirectory)null, "/"));
            }

            [Test]
            public void ShouldThrowForNullPatterns()
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                fileSystem.FileProviders.Add(NormalizedPath.DefaultFileProvider.Scheme, GetFileProvider());
                IDirectory dir = fileSystem.GetDirectory("/");

                // When, Then
                Assert.Throws<ArgumentNullException>(() => fileSystem.GetFiles(dir, null));
            }

            [Test]
            public void ShouldNotThrowForNullPattern()
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                fileSystem.FileProviders.Add(NormalizedPath.DefaultFileProvider.Scheme, GetFileProvider());
                IDirectory dir = fileSystem.GetDirectory("/");

                // When
                IEnumerable<IFile> results = fileSystem.GetFiles(dir, null, "**/foo.txt");

                // Then
                CollectionAssert.AreEquivalent(new[] { "/a/b/c/foo.txt" }, results.Select(x => x.Path.FullPath));
            }

            [TestCase("/", new[] { "/a/b/c/foo.txt" }, new[] { "/a/b/c/foo.txt" }, true)]
            [TestCase("/", new[] { "a/b/c/foo.txt" }, new[] { "/a/b/c/foo.txt" }, true)]
            [TestCase("/", new[] { "a/b/c/foo.txt", "a/b/c/foo.txt" }, new[] { "/a/b/c/foo.txt" }, true)]
            [TestCase("/a", new[] { "/a/b/c/foo.txt" }, new[] { "/a/b/c/foo.txt" }, true)]
            [TestCase("/a", new[] { "a/b/c/foo.txt" }, new string[] { }, true)]
            [TestCase("/", new[] { "!/a/b/c/foo.txt" }, new string[] { }, true)]
            [TestCase("/a", new[] { "a/b/c/foo.txt", "!/a/b/c/foo.txt" }, new string[] { }, true)]
            [TestCase("/a", new[] { "a/b/c/foo.txt", "a/b/c/foo.txt", "!/a/b/c/foo.txt" }, new string[] { }, true)]
            [TestCase("/a", new[] { "a/b/c/foo.txt", "!/a/b/c/foo.txt", "!/a/b/c/foo.txt" }, new string[] { }, true)]
            [TestCase("/", new[] { "**/foo.txt" }, new[] { "/a/b/c/foo.txt" }, true)]
            [TestCase("/", new[] { "**/foo.txt", "/a/x/bar.txt" }, new[] { "/a/b/c/foo.txt", "/a/x/bar.txt" }, true)]
            [TestCase("/", new[] { "**/foo.txt", "/a/x/baz.txt" }, new[] { "/a/b/c/foo.txt" }, true)]
            [TestCase("/", new[] { "**/foo.txt", "!/a/b/c/foo.txt" }, new string[] { }, true)]
            [TestCase("/", new[] { "**/foo.txt", "!/a/x/baz.txt" }, new[] { "/a/b/c/foo.txt" }, true)]
            [TestCase("/", new[] { "**/foo.txt", "!**/foo.txt" }, new string[] { }, true)]
            [TestCase("/", new[] { "**/foo.txt", "!**/bar.txt" }, new[] { "/a/b/c/foo.txt" }, true)]
            [TestCase("/", new[] { "/**/foo.txt" }, new[] { "/a/b/c/foo.txt" }, true)]
            [TestCase("/", new[] { "/a/b/c/d/../foo.txt" }, new[] { "/a/b/c/foo.txt" }, true)]
            [TestCase("/a", new[] { "a/b/c/foo.txt", "!/a/b/c/d/../foo.txt" }, new string[] { }, true)]
            [TestCase("/", new[] { "qwerty:///**/*.txt" }, new[] { "/q/werty.txt" }, false)]
            [TestCase("/", new[] { "qwerty|/**/*.txt" }, new[] { "/q/werty.txt" }, true)]
            [TestCase("/", new[] { "qwerty:///|/**/*.txt" }, new[] { "/q/werty.txt" }, false)]
            [TestCase("/", new[] { "/q/werty.txt" }, new string[] { }, true)]
            [TestCase("qwerty|/", new[] { "/q/werty.txt" }, new string[] { }, true)]
            [TestCase("qwerty:///|/", new[] { "/q/werty.txt" }, new string[] { }, true)]
            [TestCase("qwerty:///", new[] { "/q/werty.txt" }, new string[] { }, true)]
            [TestCase("/", new[] { "qwerty|/q/werty.txt" }, new[] { "/q/werty.txt" }, true)]
            [TestCase("/", new[] { "qwerty:///|/q/werty.txt" }, new[] { "/q/werty.txt" }, false)]
            [TestCase("/", new[] { "qwerty:///q/werty.txt" }, new[] { "/q/werty.txt" }, false)]
            public void ShouldReturnExistingFiles(string directory, string[] patterns, string[] expected, bool reverseSlashes)
            {
                // TestContext.Out.WriteLine($"Patterns: {string.Join(",", patterns)}");

                // Given
                FileSystem fileSystem = new FileSystem();
                fileSystem.FileProviders.Add(NormalizedPath.DefaultFileProvider.Scheme, GetFileProvider());
                TestFileProvider alternateProvider = new TestFileProvider();
                alternateProvider.AddDirectory("/");
                alternateProvider.AddDirectory("/q");
                alternateProvider.AddFile("/q/werty.txt");
                fileSystem.FileProviders.Add("qwerty", alternateProvider);
                IDirectory dir = fileSystem.GetDirectory(directory);

                // When
                IEnumerable<IFile> results = fileSystem.GetFiles(dir, patterns);

                // Then
                CollectionAssert.AreEquivalent(expected, results.Select(x => x.Path.FullPath));

                if (reverseSlashes)
                {
                    // When
                    results = fileSystem.GetFiles(dir, patterns.Select(x => x.Replace("/", "\\")));

                    // Then
                    CollectionAssert.AreEquivalent(expected, results.Select(x => x.Path.FullPath));
                }
            }
        }

        private IFileProvider GetFileProvider()
        {
            TestFileProvider fileProvider = new TestFileProvider();

            fileProvider.AddDirectory("/");
            fileProvider.AddDirectory("/a");
            fileProvider.AddDirectory("/a/b");
            fileProvider.AddDirectory("/a/b/c");
            fileProvider.AddDirectory("/a/b/d");
            fileProvider.AddDirectory("/a/x");
            fileProvider.AddDirectory("/a/y");
            fileProvider.AddDirectory("/a/y/z");

            fileProvider.AddFile("/a/b/c/foo.txt");
            fileProvider.AddFile("/a/x/bar.txt");

            return fileProvider;
        }
    }
}
