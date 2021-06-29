// The following environment variables need to be set for Build target:
// SIGNTOOL (to sign the executable)

// The following environment variables need to be set for Publish target:
// NUGET_API_KEY
// GITHUB_ACCESS_TOKEN

// The following environment variables need to be set for Publish-Chocolatey target:
// CHOCOLATEY_API_KEY

// Publishing workflow:
// - Update ReleaseNotes.md and RELEASE in develop branch
// - Run a normal build with Cake to set SolutionInfo.cs in the repo and run through unit tests (`build.cmd`)
// - Push to develop and fast-forward merge to master
// - Switch to master
// - Wait for CI to complete build and publish to MyGet
// - Run a local prerelease build of docs repo to verify release (`build -Script "prerelease.cake"` from docs folder)
// - Run a Publish build with Cake (`build -target Publish`)
// - No need to add a version tag to the repo - added by GitHub on publish
// - Switch back to develop branch
// - Add a blog post to docs repo about the release
// - Run a build on docs repo from CI to verify final release (first make sure NuGet Gallery has updated packages by searching for "wyam2")

#addin "Cake.FileHelpers"
#addin "Octokit"
#addin "System.Text.RegularExpressions"
#addin nuget:?package=Cake.Git
#tool "nuget:?package=NUnit.ConsoleRunner&version=3.12.0"
#tool "nuget:?package=NuGet.CommandLine&version=5.9.1"
#tool "nuget:?package=chocolatey&version=0.10.14"
#tool "AzurePipelines.TestLogger&version=1.1.0"

using Octokit;
using System.Text.RegularExpressions;

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
bool isNightlyBuild = Argument<bool>("nightly", false);
string gitTag = Argument<string>("gittag", string.Empty);

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

var isLocal = BuildSystem.IsLocalBuild;
var isRunningOnUnix = IsRunningOnUnix();
var isRunningOnWindows = IsRunningOnWindows();

var isAzurePipelines = BuildSystem.AzurePipelines.IsRunningOnAzurePipelines;
var isGitHubAction = BuildSystem.GitHubActions.IsRunningOnGitHubActions;

var isRunningOnBuildServer = isAzurePipelines || isGitHubAction;
var isPullRequest = false;
var pullRequestId = 0;
var pullRequestNumber = 0;
var buildId = "0";
var buildNumber = "0";
var branch = GitBranchCurrent(DirectoryPath.FromString(".")).FriendlyName;
var sha = GitBranchCurrent(DirectoryPath.FromString(".")).Tip.Sha;

//AZDO does the PR builds
if(isAzurePipelines)
{
    isPullRequest = BuildSystem.AzurePipelines.Environment.PullRequest.IsPullRequest;
    pullRequestId =  BuildSystem.AzurePipelines.Environment.PullRequest.Id;
    pullRequestNumber = BuildSystem.AzurePipelines.Environment.PullRequest.Number;
    buildId = BuildSystem.AzurePipelines.Environment.Build.Id.ToString();
    buildNumber =  BuildSystem.AzurePipelines.Environment.Build.Number;
    branch = BuildSystem.AzurePipelines.Environment.Repository.SourceBranchName;
    sha = BuildSystem.AzurePipelines.Environment.Repository.SourceVersion;
}
//GHA does the nightly and release builds
else if(isGitHubAction)
{
    isPullRequest = BuildSystem.GitHubActions.Environment.PullRequest.IsPullRequest;
    buildId = BuildSystem.GitHubActions.Environment.Workflow.RunId;
    buildNumber =  BuildSystem.GitHubActions.Environment.Workflow.RunNumber.ToString();
    branch = BuildSystem.GitHubActions.Environment.Workflow.Ref;
    sha = BuildSystem.GitHubActions.Environment.Workflow.Sha;
}

var versionPrefix = string.Empty;
var versionSuffix = string.Empty;
var semVersion = string.Empty;

var releaseNotes = ParseReleaseNotes("./ReleaseNotes.md");
//git tag was not provided from command line => not a release so take the release notes version
if(string.IsNullOrEmpty(gitTag))
{
    versionPrefix = releaseNotes.Version.ToString();

    if(releaseNotes.RawVersionLine.ToLowerInvariant().Contains("unreleased"))
    {
        versionSuffix = $"build";
        versionSuffix += isAzurePipelines 
                        ? $"z.{buildNumber}"
                        : (isGitHubAction ? $"gh.{buildId}" : string.Empty);
    }
    if(isNightlyBuild)
    {
        versionSuffix = $"nightly.{DateTime.Now.ToString("yyyyMMdd")}";
    }

    semVersion = $"{versionPrefix}-{versionSuffix.Replace('.', '-')}";
}
else
{
    //extract numeric version from git tag
    Regex regEx = new Regex(@"(?:(\d+)\.)?(?:(\d+)\.\d+)", RegexOptions.Compiled);
    Match version = regEx.Match(gitTag);
    if(version.Success)
    {
        semVersion = version.Value;
        versionPrefix = version.Value;
    }
}
var informalVersion = semVersion + $"-{branch}-{sha}";

Information($@"Build information
---------------------------------------
    isLocal: {isLocal}
    isNightly: {isNightlyBuild}
    isAzurePipelines: {isAzurePipelines}
    isGitHubAction: {isGitHubAction}
    isPullRequest: {isPullRequest}
    pullRequestId: {pullRequestId}
    pullRequestNumber: {pullRequestNumber}
    buildId: {buildId}
    buildNumber: {buildNumber}
    branch: {branch}
    sha: {sha}
    git tag: {gitTag}
    semVersion: {semVersion}
    informalVersion: {informalVersion}
");

//AssemblyVersion and FileVersion default to the value of $(Version) without the suffix. For example, if $(Version) is 1.2.3-beta.4, then the value would be 1.2.3. - see https://docs.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#assemblyinfo-properties
var msBuildSettings = new DotNetCoreMSBuildSettings()
    .WithProperty("Product", "Wyam2")
    .WithProperty("Copyright", $"Copyright {DateTime.Now.Year} \xa9 Wyam2 Contributors")
    .WithProperty("SourceRevisionId", sha)
    //.WithProperty("VersionPrefix", versionPrefix)
    //.WithProperty("VersionSuffix", versionSuffix)
    //.WithProperty("Version", semVersion)
    //.WithProperty("Configuration", configuration)
    //.SetVersion(semVersion)
    .SetVersionPrefix(versionPrefix)
    .SetConfiguration(configuration);

if(string.IsNullOrEmpty(versionPrefix))
{
    msBuildSettings.SetVersion(semVersion);
};
if(!string.IsNullOrEmpty(versionSuffix))
{
    msBuildSettings.SetVersionSuffix(versionSuffix);
};

var buildDir = Directory("./src/clients/Wyam/bin") + Directory(configuration);
var buildResultDir = Directory("./build");
var nugetRoot = buildResultDir + Directory("nuget");
var chocoRoot = buildResultDir + Directory("choco");
var binDir = buildResultDir + Directory("bin");

CreateDirectory(buildResultDir);
CreateDirectory(nugetRoot);
CreateDirectory(chocoRoot);
CreateDirectory(binDir);

var zipFile = "Wyam-v" + semVersion + ".zip";

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(context =>
{
    Information("Building version {0} of Wyam.", semVersion);
});

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
    {
        CleanDirectories(new DirectoryPath[] { buildDir, buildResultDir, binDir, nugetRoot, chocoRoot });
    });

Task("Restore-Packages")
    .Does(() =>
    {
        DotNetCoreRestore("./Wyam.sln", new DotNetCoreRestoreSettings
        {
            MSBuildSettings = msBuildSettings
        });
    });

Task("Build")
    .IsDependentOn("Restore-Packages")
    .Does(() =>
    {
        DotNetCoreBuild("./Wyam.sln", new DotNetCoreBuildSettings
        {
            Configuration = configuration,
            NoRestore = true,
            MSBuildSettings = msBuildSettings
        });
    });

Task("Publish-Wyam-Client")
    .IsDependentOn("Build")
    .Does(() =>
    {
        DotNetCorePublish("./src/clients/Wyam/Wyam.csproj", new DotNetCorePublishSettings
        {
            Configuration = configuration,
            NoBuild = true,
            NoRestore = true,
            MSBuildSettings = msBuildSettings
        });
    });

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .DoesForEach(GetFiles("./tests/**/*.csproj"), project =>
    {
        DotNetCoreTestSettings testSettings = new DotNetCoreTestSettings()
        {
            NoBuild = true,
            NoRestore = true,
            Configuration = configuration
        };
        if (isRunningOnBuildServer)
        {
            testSettings.Filter = "TestCategory!=ExcludeFromBuildServer";
            if(isAzurePipelines)
            {
                testSettings.Logger = "AzurePipelines";
                testSettings.TestAdapterPath = GetDirectories($"./tools/AzurePipelines.TestLogger.*/contentFiles/any/any").First();
            }
        }

        Information($"Running tests in {project}");
        DotNetCoreTest(MakeAbsolute(project).ToString(), testSettings);
    })
    .DeferOnError();

Task("Copy-Wyam-Client")
    .IsDependentOn("Publish-Wyam-Client")
    .Does(() =>
    {
        CopyDirectory(buildDir.Path.FullPath + "/netcoreapp2.1/publish", binDir);
        CopyFiles(new FilePath[] { "LICENSE", "README.md", "ReleaseNotes.md" }, binDir);
    });

Task("Zip-Wyam-Client")
    .IsDependentOn("Copy-Wyam-Client")
    .Does(() =>
    {
        var zipPath = buildResultDir + File(zipFile);
        var files = GetFiles(binDir.Path.FullPath + "/**/*");
        Zip(binDir, zipPath, files);
    });

Task("Create-Wyam-Packages")
    .IsDependentOn("Build")
    .IsDependentOn("Publish-Wyam-Client")
    .Does(() =>
    {        
        // Get the set of projects to package
        List<FilePath> projects = new List<FilePath>(GetFiles("./src/**/*.csproj"));
        
        // Package all nuspecs
        foreach (var project in projects)
        {
            DotNetCorePack(
                MakeAbsolute(project).ToString(),
                new DotNetCorePackSettings
                {
                    Configuration = configuration,
                    NoBuild = true,
                    NoRestore = true,
                    OutputDirectory = nugetRoot,
                    MSBuildSettings = msBuildSettings
                });
        }
    });

Task("Create-Theme-Packages")
    .WithCriteria(() => isRunningOnWindows)
    .Does(() =>
    {        
        // All themes must be under the themes folder in a NameOfRecipe/NameOfTheme subfolder
        var themeDirectories = GetDirectories("./themes/*/*");
        
        // Package all themes
        foreach (var themeDirectory in themeDirectories)
        {
            string[] segments = themeDirectory.Segments;
            string id = "Wyam2." + segments[segments.Length - 2] + "." + segments[segments.Length - 1];
            NuGetPack(new NuGetPackSettings
            {
                Id = id,
                Version = semVersion,
                Title = id,
                Authors = new [] { "Simona Avornicesei", "Wyam2", "and contributors" },
                Description = "A theme for the Wyam2 " + segments[segments.Length - 2] + " recipe.",
                ProjectUrl = new Uri("https://wyam2.github.io"),
                IconUrl = new Uri("https://github.com/Wyam2/assets/raw/master/logo-square-64.png"),
                LicenseUrl = new Uri("https://github.com/Wyam2/assets/raw/master/LICENSE"),
                Copyright = $"Copyright {DateTime.Now.Year} © Wyam2 Contributors",
                Tags = new [] { "Wyam", "Wyam2", "Theme", "Static", "StaticContent", "StaticSite", "Documentation" },
                RequireLicenseAcceptance = false,
                Symbols = false,
                Repository = new NuGetRepository {
                    Type = "git",
                    Url = "https://github.com/Wyam2/Wyam.git"
                },
                Files = new []
                {
                    new NuSpecContent 
                    { 
                        Source = "**/*",
                        Target = "content"
                    }                     
                },
                BasePath = themeDirectory,
                OutputDirectory = nugetRoot
            });
        }
    });
    
Task("Create-AllModules-Package")
    .IsDependentOn("Build")
    .WithCriteria(() => isRunningOnWindows)
    .Does(() =>
    {        
        var nuspec = GetFiles("./src/extensions/Wyam.All/*.nuspec").FirstOrDefault();
        if (nuspec == null)
        {            
            throw new InvalidOperationException("Could not find all modules nuspec.");
        }
        
        // Add dependencies for all module libraries
        List<FilePath> nuspecs = new List<FilePath>(GetFiles("./src/extensions/**/*.nuspec"));
        nuspecs.RemoveAll(x => x.GetDirectory().GetDirectoryName() == "Wyam.All");
        List<NuSpecDependency> dependencies = new List<NuSpecDependency>(
            nuspecs
                .Select(x => new NuSpecDependency
                    {
                        Id = x.GetDirectory().GetDirectoryName(),
                        Version = semVersion
                    })
        );
        
        // Pack the all modules package
        NuGetPack(nuspec, new NuGetPackSettings
        {
            Version = semVersion,
            Copyright = $"Copyright {DateTime.Now.Year} © Wyam2 Contributors",
            BasePath = nuspec.GetDirectory(),
            OutputDirectory = nugetRoot,
            Symbols = false,
            Dependencies = dependencies
        });
    });
    
Task("Create-Tools-Package")
    .IsDependentOn("Publish-Wyam-Client")
    .WithCriteria(() => isRunningOnWindows)
    .Does(() =>
    {        
        var nuspec = GetFiles("./src/clients/Wyam/*.nuspec").FirstOrDefault();
        if (nuspec == null)
        {            
            throw new InvalidOperationException("Could not find tools nuspec.");
        }
        var pattern = string.Format("bin\\{0}\\netcoreapp2.1\\publish\\**\\*", configuration);  // This is needed to get around a Mono scripting issue (see #246, #248, #249)
        NuGetPack(nuspec, new NuGetPackSettings
        {
            Version = semVersion,
            BasePath = nuspec.GetDirectory(),
            OutputDirectory = nugetRoot,
            Symbols = false,
            Files = new [] 
            { 
                new NuSpecContent 
                { 
                    Source = pattern,
                    Target = "tools\\netcoreapp2.1"
                },
                new NuSpecContent
                {
                    Source = System.IO.Path.Combine(nuspec.GetDirectory().FullPath, "..\\..\\..\\assets\\wyam-square-128.png"),
                    Target = ""
                }
            }
        });
    });

Task("Create-Chocolatey-Package")
    .IsDependentOn("Copy-Wyam-Client")
    .WithCriteria(() => isRunningOnWindows)
    .Does(() => {
        var nuspecFile = GetFiles("./src/clients/Chocolatey/*.nuspec").FirstOrDefault();
        ChocolateyPack(nuspecFile, new ChocolateyPackSettings {
            Version = semVersion,
            OutputDirectory = chocoRoot.Path.FullPath,
            WorkingDirectory = buildResultDir.Path.FullPath
        });
    });
    
Task("Publish-AzureFeed")
    .IsDependentOn("Create-Packages")
    .WithCriteria(() => isAzurePipelines)
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isRunningOnWindows)
    .WithCriteria(() => branch == "main")
    .Does(() =>
    {
        // Get the access token
        var accessToken = EnvironmentVariable("SYSTEM_ACCESSTOKEN");
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new InvalidOperationException("Could not resolve SYSTEM_ACCESSTOKEN.");
        }

        // Add the authenticated feed source
        NuGetAddSource(
            "VSTS",
            "https://pkgs.dev.azure.com/Wyam2/_packaging/Wyam2/nuget/v3/index.json",
            new NuGetSourcesSettings
            {
                UserName = "VSTS",
                Password = accessToken
            });

        foreach (var nupkg in GetFiles(nugetRoot.Path.FullPath + "/*.nupkg"))
        {
            NuGetPush(nupkg, new NuGetPushSettings 
            {
                Source = "VSTS",
                ApiKey = "VSTS"
            });
        }
    });

Task("Publish-GitHubFeed")
    .IsDependentOn("Create-Packages")
    .WithCriteria(() => isGitHubAction)
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isRunningOnWindows)
    .WithCriteria(() => branch == "main")
    .Does(() =>
    {
        // Get the access token
        var accessToken = EnvironmentVariable("GH_ACCESS_TOKEN");
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new InvalidOperationException("Could not resolve GH_ACCESS_TOKEN.");
        }

        // Add the authenticated feed source
        NuGetAddSource(
            "GitHub",
            "https://nuget.pkg.github.com/Wyam2/index.json",
            new NuGetSourcesSettings
            {
                Password = accessToken
            });

        foreach (var nupkg in GetFiles(nugetRoot.Path.FullPath + "/*.nupkg"))
        {
            NuGetPush(nupkg, new NuGetPushSettings 
            {
                Source = "GitHub",
                ApiKey = accessToken
            });
        }
    });
    
Task("Publish-NuGetFeed")
    .IsDependentOn("Create-Packages")
    .WithCriteria(() => isRunningOnWindows)
    .WithCriteria(() => branch == "main")
    .Does(() =>
    {
        var apiKey = EnvironmentVariable("NUGET_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Could not resolve NuGet API key.");
        }

        foreach (var nupkg in GetFiles(nugetRoot.Path.FullPath + "/*.nupkg"))
        {
            NuGetPush(nupkg, new NuGetPushSettings 
            {
                ApiKey = apiKey,
                Source = "https://api.nuget.org/v3/index.json"
            });
        }
    });

Task("Publish-ChocolateyFeed")
    .IsDependentOn("Create-Chocolatey-Package")
    .WithCriteria(() => isRunningOnWindows)
    .WithCriteria(() => branch == "main")
    .Does(()=> 
    {
        var chocolateyApiKey = EnvironmentVariable("CHOCOLATEY_API_KEY");
        if (string.IsNullOrEmpty(chocolateyApiKey))
        {
            throw new InvalidOperationException("Could not resolve Chocolatey API key.");
        }

        foreach(var chocoPkg in GetFiles(chocoRoot.Path.FullPath + "/*.nupkg"))
        {
            ChocolateyPush(chocoPkg, new ChocolateyPushSettings {
                ApiKey = chocolateyApiKey,
                Source = "https://push.chocolatey.org/"
            });
        }
    });

Task("Publish-Release")
    .IsDependentOn("Zip-Wyam-Client")
    .WithCriteria(() => isRunningOnWindows)
    .WithCriteria(() => branch == "main")
    .WithCriteria(() => !string.IsNullOrEmpty(gitTag))
    .Does(() =>
    {
        var githubToken = EnvironmentVariable("GH_ACCESS_TOKEN");
        if (string.IsNullOrEmpty(githubToken))
        {
            throw new InvalidOperationException("Could not resolve Wyam GitHub token.");
        };
        
        var github = new GitHubClient(new ProductHeaderValue("Wyam2 wyam release"))
        {
            Credentials = new Credentials(githubToken)
        };
        var release = github.Repository.Release.Create("Wyam2", "Wyam", new NewRelease(gitTag) 
        {
            Name = gitTag,
            Body = string.Join(Environment.NewLine, releaseNotes.Notes) + Environment.NewLine + Environment.NewLine
                + @"### Please see https://wyam2.github.io/docs/usage/obtaining for important notes about downloading and installing.",
            TargetCommitish = sha
        }).Result; 
        
        var zipPath = buildResultDir + File(zipFile);
        using (var zipStream = System.IO.File.OpenRead(zipPath.Path.FullPath))
        {
            var releaseAsset = github.Repository.Release.UploadAsset(release, new ReleaseAssetUpload(zipFile, "application/zip", zipStream, null)).Result;
        }
    });

Task("Tag-Release-Documentation")
    .WithCriteria(() => branch == "main")
    .WithCriteria(() => !string.IsNullOrEmpty(gitTag))
    .Does(() =>
    {
        var githubToken = EnvironmentVariable("GH_ACCESS_TOKEN");
        if (string.IsNullOrEmpty(githubToken))
        {
            throw new InvalidOperationException("Could not resolve Wyam2 GitHub token.");
        };
        
        var github = new GitHubClient(new ProductHeaderValue("Wyam2 wyam tag docs"))
        {
            Credentials = new Credentials(githubToken)
        };

        var tag = new NewTag {
            Message = $"Wyam2 {gitTag} release",
            Tag = gitTag,
            Object = sha,
            Type = TaggedType.Commit,
            Tagger = new Committer("Wyam GitHub Actions", "wyam2.action@github.com", DateTimeOffset.Now)  
        };

        var result = github.Git.Tag.Create("Wyam2", "docs", tag).Result;
        Information("Created tag {0} for Wyam2 docs at {1}", result.Tag, result.Sha);
    });

    
//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Create-Packages")
    .IsDependentOn("Create-Wyam-Packages")
    .IsDependentOn("Create-Theme-Packages")   
    .IsDependentOn("Create-AllModules-Package")    
    .IsDependentOn("Create-Tools-Package")
    .IsDependentOn("Create-Chocolatey-Package");
    
Task("Package")
    .IsDependentOn("Run-Unit-Tests")
    .IsDependentOn("Zip-Wyam-Client")
    .IsDependentOn("Create-Packages");

Task("Default")
    .IsDependentOn("Package");

Task("CI")
    .IsDependentOn("Run-Unit-Tests")
    .IsDependentOn("Publish-AzureFeed");

//builds, generates the nuget packages and deploy them to GitHub feed
Task("Nightly")
    .IsDependentOn("Publish-GitHubFeed");

Task("Publish")
    .IsDependentOn("Publish-NuGetFeed")
    .IsDependentOn("Publish-ChocolateyFeed")
    .IsDependentOn("Publish-Release");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
