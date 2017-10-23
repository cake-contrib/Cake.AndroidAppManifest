//////////////////////////////////////////////////////////////////////
// ADDINS
//////////////////////////////////////////////////////////////////////

#addin "Cake.FileHelpers"

//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////

#tool GitVersion.CommandLine
#tool GitLink
#tool xunit.runner.console&version=2.3.0

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// should MSBuild & GitLink treat any errors as warnings.
var treatWarningsAsErrors = false;

// Get whether or not this is a local build.
var local = BuildSystem.IsLocalBuild;
var isRunningOnUnix = IsRunningOnUnix();
var isRunningOnWindows = IsRunningOnWindows();

var isRunningOnAppVeyor = AppVeyor.IsRunningOnAppVeyor;
var isPullRequest = AppVeyor.Environment.PullRequest.IsPullRequest;

// Parse release notes.
var releaseNotes = ParseReleaseNotes("RELEASENOTES.md");

// Get version.
var version = releaseNotes.Version.ToString();
var epoch = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
var gitSha = GitVersion().Sha;

var semVersion = string.Format("{0}.{1}", version, epoch);

// Define directories.
var artifactDirectory = "./artifacts/";

// Define global marcos.
Action Abort = () => { throw new Exception("a non-recoverable fatal error occurred."); };

Action<string> RestorePackages = (solution) =>
{
    NuGetRestore(solution);
};

Action<string, string> Package = (nuspec, basePath) =>
{
    CreateDirectory(artifactDirectory);

    Information("Packaging version {0} in {1} using {2} as the BasePath.", version, nuspec, basePath);

    NuGetPack(nuspec, new NuGetPackSettings {
        Authors                  = new [] { "Geoffrey Huntley, jzeferino" },
        Owners                   = new [] { "ghuntley" },

        ProjectUrl               = new Uri("https://github.com/cake-contrib/Cake.AndroidAppManifest"),
        IconUrl                  = new Uri("https://cdn.rawgit.com/cake-contrib/graphics/a5cf0f881c390650144b2243ae551d5b9f836196/png/cake-contrib-medium.png"),
        LicenseUrl               = new Uri("https://opensource.org/licenses/MIT"),
        Copyright                = "Copyright (c) Geoffrey Huntley",
        RequireLicenseAcceptance = false,

        Version                  = version,
        Tags                     = new [] {  "Cake", "Script", "Build", "Xamarin", "Android" },
        ReleaseNotes             = new List<string>(releaseNotes.Notes),

        Symbols                  = true,
        Verbosity                = NuGetVerbosity.Detailed,
        OutputDirectory          = artifactDirectory,
        BasePath                 = basePath,
    });
};

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup((context) =>
{
    Information("Building version {0} of Cake.AndroidAppManifest.", semVersion);
});

Teardown((context) =>
{
    // Executed AFTER the last task.
});

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Build")
    .IsDependentOn("RestorePackages")
    .IsDependentOn("UpdateAssemblyInfo")
    .Does (() =>
{
    Action<string> build = (filename) =>
    {
        var solution = System.IO.Path.Combine("./src/", filename);

        // UWP (project.json) needs to be restored before it will build.
        RestorePackages(solution);

        Information("Building {0}", solution);

        MSBuild(solution, new MSBuildSettings()
            .SetConfiguration(configuration)
            .WithProperty("NoWarn", "1591") // ignore missing XML doc warnings
            .WithProperty("TreatWarningsAsErrors", treatWarningsAsErrors.ToString())
            .SetVerbosity(Verbosity.Minimal)
            .SetNodeReuse(false));
    };

    build("Cake.AndroidAppManifest.sln");
});

Task("UpdateAppVeyorBuildNumber")
    .WithCriteria(() => isRunningOnAppVeyor)
    .Does(() =>
{
    AppVeyor.UpdateBuildVersion(semVersion);
});

Task("UpdateAssemblyInfo")
    .IsDependentOn("UpdateAppVeyorBuildNumber")
    .Does (() =>
{
    var file = "./src/CommonAssemblyInfo.cs";

    CreateAssemblyInfo(file, new AssemblyInfoSettings {
        Product = "Cake.AndroidAppManifest",
        Version = version,
        FileVersion = version,
        InformationalVersion = semVersion,
        Copyright = "Copyright (c) Geoffrey Huntley"
    });
});

Task("RestorePackages").Does (() =>
{
    RestorePackages("./src/Cake.AndroidAppManifest.sln");
});

Task("RunUnitTests")
    .IsDependentOn("Build")
    .Does(() =>
{
    XUnit2("./src/Cake.AndroidAppManifest.Tests/bin/Release/Cake.AndroidAppManifest.Tests.dll", new XUnit2Settings {
        OutputDirectory = artifactDirectory,
        XmlReportV1 = false,
        NoAppDomain = true
    });
});

Task("Package")
    .IsDependentOn("Build")
    .IsDependentOn("RunUnitTests")
    .Does (() =>
{
    Package("./src/Cake.AndroidAppManifest.nuspec", "./src/Cake.AndroidAppManifest");
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////


//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget("Package");
