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

#addin "nuget:https://api.nuget.org/v3/index.json?package=Cake.FileHelpers&version=4.0.1"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Octokit&version=0.50.0"
//#addin "nuget:https://api.nuget.org/v3/index.json?package=System.Text.RegularExpressions&version=4.3.1"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Cake.Incubator&version=6.0.0"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Cake.Git&version=1.1.0"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Cake.Sonar&version=1.1.25"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Microsoft.Extensions.FileSystemGlobbing&version=2.2.0.0"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Cake.FileSet&version=2.0.0"
//#addin "nuget:https://api.nuget.org/v3/index.json?package=GitHubActionsTestLogger&version=1.2.0"

#tool "nuget:https://api.nuget.org/v3/index.json?package=NuGet.CommandLine&version=5.9.1"
#tool "nuget:https://api.nuget.org/v3/index.json?package=NUnit.ConsoleRunner&version=3.12.0"
#tool "nuget:https://api.nuget.org/v3/index.json?package=OpenCover&version=4.7.1221"
#tool "nuget:https://api.nuget.org/v3/index.json?package=ReportGenerator&version=4.8.12"
#tool "nuget:https://api.nuget.org/v3/index.json?package=dotnet-sonarscanner&version=5.2.2"
#tool "nuget:https://api.nuget.org/v3/index.json?package=chocolatey&version=0.10.14"
#tool "nuget:https://api.nuget.org/v3/index.json?package=AzurePipelines.TestLogger&version=1.1.0"

using Octokit;
using System.Text.RegularExpressions;

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
string gitTag = EnvironmentVariable("GITHUB_TAG") ?? Argument<string>("tag", string.Empty);

var sonarUrl   = Argument<string>("sonar-url", "https://sonarcloud.io");
var sonarProjectKey = Argument<string>("sonar-key", "Wyam2_wyam");
var sonarProjectName = Argument<string>("sonar-name", "wyam");
var sonarOrganization = Argument<string>("sonar-org", "wyam2");
var sonarToken = EnvironmentVariable("SONAR_TOKEN") ?? Argument<string>("sonar-token", string.Empty);

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

var isLocal = BuildSystem.IsLocalBuild;
var isRunningOnUnix = IsRunningOnUnix();
var isRunningOnWindows = IsRunningOnWindows();

var isAzurePipelines = BuildSystem.AzurePipelines.IsRunningOnAzurePipelines;
var isGitHubAction = BuildSystem.GitHubActions.IsRunningOnGitHubActions;

var isRunningOnBuildServer = isAzurePipelines || isGitHubAction;
var isNightlyBuild = target == "Nightly";
var isCodeQualityBuild = (target == "CodeQuality" || target == "SonarQube");

//embed pdb when running locally and when running the nightly build from GHA
bool embedSymbols = (isLocal && string.IsNullOrEmpty(gitTag)) || (isGitHubAction && isNightlyBuild);

var isPullRequest = false;
var pullRequestId = 0;
var pullRequestNumber = 0;
var buildId = "0";
var buildNumber = "0";
var branch = "main";
var sha = "00000000";

//There are cases when Cake.Git fails due to LibGit2Sharp / LibGit2Sharp.NativeBinaries type initialization errors
//TODO: Replace this try-catch with libgit2sharp when https://github.com/libgit2/libgit2sharp/issues/1904 is fixed
try{
    //ugly hack to get the correct branch name - Cake.Git does not get it right since the git repo is in detached mode
    if(!string.IsNullOrEmpty(gitTag))
    {
        throw new Exception("Repository is in 'detached HEAD' state. Git log must be run");
    }

    branch = GitBranchCurrent(DirectoryPath.FromString(".")).FriendlyName;
    sha = GitBranchCurrent(DirectoryPath.FromString(".")).Tip.Sha;
}
catch(Exception ex)
{
    Warning($"Cake.Git failed to retrieve current branch and sha because {ex}");
    Information("Using git log to retrieve current branch and sha because Cake.Git failed");

    var procSettings = new ProcessSettings{ 
        Arguments = ProcessArgumentBuilder.FromString(@"log -n 1 --pretty=format:""%D, %H, %h"""),
        WorkingDirectory = DirectoryPath.FromString("."),
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    using(var process = StartAndReturnProcess("git", procSettings))
    {
        process.WaitForExit();
        int exitCode = process.GetExitCode();
        if(exitCode != 0)
        {
            throw new CakeException($"git {procSettings.Arguments.RenderSafe()} returned ({exitCode}) because {process.GetStandardError()}");
        }
        List<string> output = process.GetStandardOutput().ToList();
        if(output is null || output.Count() == 0)
        {
            throw new CakeException($"git {procSettings.Arguments.RenderSafe()} returned no output to be parsed");
        }

        //if it's a normal commit: HEAD -> main, tag: v3.0.0-rc1, origin/main, 57fcb522f939e5c9eda05141f7f184384e868458, 57fcb522
        //if it's a tag          : HEAD, tag: v3.0.0-rc1, origin/main, main, 57fcb522f939e5c9eda05141f7f184384e868458, 57fcb522
        string[] shards = output.ElementAt(0).Split(new string[] {"HEAD -> ", ", "}, StringSplitOptions.RemoveEmptyEntries);
        branch = shards.Length == 5 ? shards[0] : shards[3]; //a tag has 6 shards, a normal commit has 5
        sha    = shards.Length == 5 ? shards[3] : shards[4];
    }
}

//AZDO does the PR builds
if(isAzurePipelines)
{
    isPullRequest = BuildSystem.AzurePipelines.Environment.PullRequest.IsPullRequest;
    pullRequestId =  BuildSystem.AzurePipelines.Environment.PullRequest.Id;
    pullRequestNumber = BuildSystem.AzurePipelines.Environment.PullRequest.Number;
    buildId = BuildSystem.AzurePipelines.Environment.Build.Id.ToString();
    buildNumber =  BuildSystem.AzurePipelines.Environment.Build.Number;
}
//GHA does the nightly and release builds
else if(isGitHubAction)
{
    isPullRequest = BuildSystem.GitHubActions.Environment.PullRequest.IsPullRequest;
    if(isPullRequest)
    {
        //On PRs, GITHUB_REF takes the format refs/pull/:prNumber/merge
        string prNumber = EnvironmentVariable<string>("GITHUB_REF", "0").Replace("refs/pull/:", string.Empty).Replace("/merge", string.Empty);
        int.TryParse(prNumber, out pullRequestId);
    }
    buildId = BuildSystem.GitHubActions.Environment.Workflow.RunId;
    buildNumber =  BuildSystem.GitHubActions.Environment.Workflow.RunNumber.ToString();
}

var versionPrefix = string.Empty;
var versionSuffix = string.Empty;
var semVersion = string.Empty;

var releaseNotes = ParseReleaseNotes("./ReleaseNotes.md");
//git tag was not provided from command line => not a release so take the release notes version
if(string.IsNullOrEmpty(gitTag))
{
    versionPrefix = releaseNotes.Version.ToString();

    if(isPullRequest)
    {
        versionSuffix = $"pr.{pullRequestId}";
    }
    else if(isNightlyBuild)
    {
        versionSuffix = $"nightly.{DateTime.Now.ToString("yyyyMMdd")}";
    }
    else
    {
        versionSuffix= $"pre.{buildNumber}";
    }

    string abbrevSha = sha;
    if(sha.Length > 9) abbrevSha = sha.Substring(0, 9);
    
    semVersion = $"{versionPrefix}-{versionSuffix}+{abbrevSha}";
}
else
{
    //extract numeric version from git tag
    Regex regEx = new Regex(@"(?:(\d+)\.)?(?:(\d+)\.\d+)", RegexOptions.Compiled);
    Match version = regEx.Match(gitTag);
    if(version.Success)
    {
        versionPrefix = version.Value;
        if(gitTag.Contains("-"))
        {
            versionSuffix = gitTag.Substring(gitTag.IndexOf("-") +1);
        }

        semVersion =  $"{versionPrefix}-{versionSuffix}";
    }
}
var informalVersion = $"{semVersion}.Branch.{branch}.Sha.{sha}";

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
    versionPrefix: {versionPrefix}
    versionSuffix: {versionSuffix}
    informalVersion: {informalVersion}
");

//Just to have the proper MSBuild props when building and testing locally
if (FileExists(".local.Build.props"))
{
    var file = File(".local.Build.props");
    XmlPoke(file, "/Project/PropertyGroup/SourceRevisionId", sha);
    XmlPoke(file, "/Project/PropertyGroup/Version", semVersion);
    XmlPoke(file, "/Project/PropertyGroup/VersionPrefix", versionPrefix);
    XmlPoke(file, "/Project/PropertyGroup/VersionSuffix", versionSuffix);
}

//AssemblyVersion and FileVersion default to the value of $(Version) without the suffix. For example, if $(Version) is 1.2.3-beta.4, then the value would be 1.2.3. - see https://docs.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#assemblyinfo-properties
//ContinuousIntegrationBuild prop. is required for SourceLink (deterministic build), see https://github.com/clairernovotny/DeterministicBuilds and https://devblogs.microsoft.com/nuget/introducing-source-code-link-for-nuget-packages/
var msBuildSettings = new DotNetCoreMSBuildSettings()
    .WithProperty("ContinuousIntegrationBuild", "true")//cake build should always be deterministic
    .WithProperty("SourceRevisionId", sha)
    .WithProperty("RepositoryBranch", branch)
    .SetVersionPrefix(versionPrefix)
    .SetConfiguration(configuration);

if(embedSymbols)
{
    msBuildSettings.WithProperty("DebugType", "embedded");
}
else
{
    msBuildSettings.WithProperty("DebugType", "portable");
}
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
var reportsDir = MakeAbsolute(Directory("./reports"));

CreateDirectory(buildResultDir);
CreateDirectory(nugetRoot);
CreateDirectory(chocoRoot);
CreateDirectory(binDir);
CreateDirectory(reportsDir);

var zipFile = $"Wyam2-v{semVersion}.zip";

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
        DeleteFiles($"{reportsDir}/*.*");
    });

Task("Restore-Packages")
    .Does(() =>
    {
        DotNetCoreRestore("./Wyam.sln");
    });

Task("Sonar-Begin")
    .Does(() => {

        //full debug if doing a code quality analysis
        msBuildSettings.ArgumentCustomization = arg => arg.AppendSwitch("/p:DebugType","=","Full");

        SonarBegin(new SonarBeginSettings{
            Url = sonarUrl,
            Organization = sonarOrganization,
            Key = sonarProjectKey,
            Name = sonarProjectName,
            Login = sonarToken,
            Branch = branch,
            Version = versionPrefix + (string.IsNullOrEmpty(gitTag) ? "-pre" : string.Empty),
            OpenCoverReportsPath = reportsDir.ToString() + "/*.opencover.xml",
            NUnitReportsPath = reportsDir.ToString() + "/*.Tests.xml"
        });
    });

Task("Build")
    .IsDependentOn("Clean")
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

Task("Run-Tests")
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

Task("Run-Tests-With-Coverage")
    .IsDependentOn("Build")
    .Does(() =>
    {
        bool success = true;

        DotNetCoreTestSettings testSettings = new DotNetCoreTestSettings()
        {
            NoBuild = true,
            NoRestore = true,
            NoLogo = true,
            Configuration = configuration
        };
        testSettings.Filter = "TestCategory!=ExcludeFromBuildServer"; //or else it will never complete

        if(isAzurePipelines)
        {
            testSettings.Logger = "AzurePipelines";
            testSettings.TestAdapterPath = GetDirectories($"./tools/AzurePipelines.TestLogger.*/contentFiles/any/any").First();
        };

        OpenCoverSettings openCoverSettings = new OpenCoverSettings
        {
            OldStyle = true, //ONLY use this option if you are encountering MissingMethodException like errors when the code is run under OpenCover
            MergeByHash = true,
            ArgumentCustomization = arg => arg.Append("-coverbytest:*.Tests.dll")
                                              .Append("-filter\":+[Wyam.*]* +[Cake.Wyam]* +[Wyam]* -[*Wyam*.Tests]*\"")
                                              .Append("-returntargetcode:100"),
            HandleExitCode = exitCode => 
            {
                if(exitCode > 100)
                {
                    Error($"OpenCover failed with code {(exitCode - 100)}");
                    return false;
                }
                else
                {
                    Verbose($"dotnet test returned code {exitCode}");
                    return true;
                }
            }
        };

        var testProjects = GetFiles("./tests/**/*.csproj");
        foreach (var project in testProjects)
        {
            try
            {
                string projectName = project.GetFilenameWithoutExtension().ToString();

                testSettings.ArgumentCustomization = arg => arg.Append($"-- NUnit.TestOutputXml=\"{reportsDir}\"");

                Information($"Running tests and calculating coverage in {project}");
                OpenCover(context => context.DotNetCoreTest(MakeAbsolute(project).ToString(), testSettings),
                           $@"{reportsDir}/{projectName}.opencover.xml", 
                          openCoverSettings);
            }
            catch (Exception ex)
            {
                success = false;
                Error("There was an error while running the tests", ex);
            }
        }

        if (success == false)
        {
            throw new CakeException("There was an error while running the tests");
        }
    });

Task("Coverage-Report")
    .Does(() => 
    {
        if(DirectoryExists($@"{reportsDir}/coverage_html"))
        {
            CleanDirectory($@"{reportsDir}/coverage_html");
        }
        else
        {
            CreateDirectory($@"{reportsDir}/coverage_html");
        }

        var coverageFiles = GetFileSet(
            patterns: new string[] { "*.opencover.xml" }, 
            caseSensitive: false,
            basePath: reportsDir
        );
        if(coverageFiles == null || coverageFiles.Count == 0)
        {
            Error("No coverage files found for generating coverage report");
            return;
        }

        ReportGenerator(GlobPattern.FromString($@"{reportsDir}/*.opencover.xml"), 
                        $@"{reportsDir}/coverage_html",
                        new ReportGeneratorSettings(){
                            HistoryDirectory = $@"{reportsDir}/coverage_history",
                            ReportTypes = {ReportGeneratorReportType.Html, ReportGeneratorReportType.HtmlSummary},
                            SourceDirectories = { Directory("./src")},
                            AssemblyFilters = {"+Wyam.*", "+Cake.Wyam", "+Wyam", "-*.Tests"},
                            ArgumentCustomization = arg => arg.AppendQuoted("-title: Wyam2 code coverage")
                                                              .AppendQuoted($"-tag:{semVersion}")
                        });
    });

Task("Sonar-End")
    .Does(() => {
        SonarEnd(new SonarEndSettings{
            Login = sonarToken
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
                IconUrl = new Uri("https://github.com/Wyam2/assets/raw/master/logo-square-invert-64.png"),
                Icon = "logo-square-invert.png",
                LicenseUrl = new Uri("https://github.com/Wyam2/assets/raw/master/LICENSE"),
                Copyright = $"Copyright {DateTime.Now.Year} © Wyam2 Contributors",
                Tags = new [] { "Wyam", "Wyam2", "Theme", "Static", "StaticContent", "StaticSite", "Blog", "BlogEngine", "Documentation" },
                RequireLicenseAcceptance = false,
                Symbols = false,
                Repository = new NuGetRepository {
                    Type = "git",
                    Url = "https://github.com/Wyam2/Wyam.git",
                    Branch = branch,
                    Commit = sha

                },
                Files = new []
                {
                    new NuSpecContent 
                    { 
                        Source = "**/*",
                        Target = "content"
                    },
                    new NuSpecContent 
                    { 
                        Source = "../../../assets/logo-square-invert-128.png",
                        Target = "logo-square-invert.png"
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
        
        //TODO: Dynamically add dependencies for all module libraries
        List<FilePath> nuspecs = new List<FilePath>(GetFiles($"./src/extensions/**/*.{versionPrefix}-{versionSuffix}.nuspec"));
        foreach (var spec in nuspecs)
        {
            Verbose($"Found dependency {spec}");
        }
        nuspecs.RemoveAll(x => x.GetFilenameWithoutExtension().ToString().StartsWith("Wyam2.All"));
        List<NuSpecDependency> dependencies = new List<NuSpecDependency>(
            nuspecs
                .Select(x => 
                {
                    string dependencyId = x.GetFilenameWithoutExtension().ToString().Replace($".{versionPrefix}-{versionSuffix}", "");
                    Verbose($"Adding nuspec dependency {dependencyId} from {x}");
                    return new NuSpecDependency
                    {
                        Id = dependencyId,
                        Version = semVersion
                    };
                })
        );
        
        // Pack the all modules package
        NuGetPack(nuspec, new NuGetPackSettings
        {
            Version = semVersion,
            Copyright = $"Copyright {DateTime.Now.Year} © Wyam2 Contributors",
            BasePath = nuspec.GetDirectory(),
            OutputDirectory = nugetRoot,
            Symbols = false, //this is just a meta package (aka like openSUSE's patterns)
            Dependencies = dependencies,
            Repository = new NuGetRepository {
                Type = "git",
                Url = "https://github.com/Wyam2/Wyam.git",
                Branch = branch,
                Commit = sha

            },
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
            Symbols = !embedSymbols,
            ArgumentCustomization = arg => arg.Append("-SymbolPackageFormat snupkg")
                                              .Append($"-Properties EmbedUntrackedSources=true"),
            Icon = "logo-drop.png",
            Repository = new NuGetRepository {
                Type = "git",
                Url = "https://github.com/Wyam2/Wyam.git",
                Branch = branch,
                Commit = sha
            },
            Files = new [] 
            { 
                new NuSpecContent 
                { 
                    Source = pattern,
                    Target = "tools\\netcoreapp2.1"
                },
                new NuSpecContent
                {
                    Source = @"../../../assets/logo-drop-128.png",
                    Target = "logo-drop.png"
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
            Version = string.IsNullOrEmpty(gitTag) ? $"{versionPrefix}.{DateTime.Now.ToString("yyyyMMdd")}" : versionPrefix,
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
        //Get the user name
        var userName = EnvironmentVariable("GH_USERNAME");
        if (string.IsNullOrEmpty(userName))
        {
            throw new InvalidOperationException("Could not resolve GH_USERNAME.");
        }
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
                UserName = userName,
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
    .WithCriteria(() => !string.IsNullOrEmpty(gitTag))
    .Does(() =>
    {
        var apiKey = EnvironmentVariable("NUGET_API_KEY");
        var url = "https://api.nuget.org/v3/index.json";

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Could not resolve NuGet API key.");
        }

        foreach (var nupkg in GetFiles(nugetRoot.Path.FullPath + "/*.nupkg"))
        {
            NuGetPush(nupkg, new NuGetPushSettings 
            {
                ApiKey = apiKey,
                Source = url,
                SkipDuplicate = true
            });
        }
        foreach (var snupkg in GetFiles(nugetRoot.Path.FullPath + "/*.snupkg"))
        {
            NuGetPush(snupkg, new NuGetPushSettings 
            {
                ApiKey = apiKey,
                Source = url,
                SkipDuplicate = true
            });
        }
    });

Task("Publish-ChocolateyFeed")
    .IsDependentOn("Create-Chocolatey-Package")
    .WithCriteria(() => isRunningOnWindows)
    .WithCriteria(() => !string.IsNullOrEmpty(gitTag))
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
    .IsDependentOn("Create-Chocolatey-Package")
    .Does(() => { Information("Ran Create-Packages target"); });
    
Task("Package")
    .IsDependentOn("Build")
    .IsDependentOn("Zip-Wyam-Client")
    .IsDependentOn("Create-Packages")
    .Does(() => { Information("Ran Package target"); });

Task("Default")
    .IsDependentOn("Package")
    .Does(() => { Information("Ran Default target"); });

Task("CodeQuality")
    .IsDependentOn("Run-Tests-With-Coverage")
    .IsDependentOn("Coverage-Report")
    .Does(() => { Information("Ran CodeQuality target"); });

Task("SonarQube")
    .IsDependentOn("Sonar-Begin")
    .IsDependentOn("Run-Tests-With-Coverage")
    .IsDependentOn("Sonar-End")
    .Does(() => { Information("Ran SonarQube target"); });

Task("CI")
    .IsDependentOn("Run-Tests")
    .IsDependentOn("Publish-AzureFeed")
    .Does(() => { Information("Ran CI target"); });

//builds, generates the nuget packages and deploy them to GitHub feed
Task("Nightly")
    .IsDependentOn("Publish-GitHubFeed")
    .Does(() => { Information("Ran Nightly target"); });

Task("Release")
    .IsDependentOn("Package")
    .IsDependentOn("Publish-NuGetFeed")
    .IsDependentOn("Publish-ChocolateyFeed")
    .IsDependentOn("Publish-Release")
    .Does(() => { Information("Ran Publish target"); });

Task("Info")
    .Does(() => { Information("Ran Info target"); });

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
