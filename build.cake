//////////////////////////////////////////////////////////////////////
// ADDINS
//////////////////////////////////////////////////////////////////////

#addin "Cake.FileHelpers"

//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////

#tool GitVersion.CommandLine
#tool GitLink
#tool xunit.runner.console&version=2.3.1
#tool vswhere

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Package");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// should MSBuild & GitLink treat any errors as warnings.
var treatWarningsAsErrors = false;

// Get whether or not this is a local build.
var local = BuildSystem.IsLocalBuild;
var isRunningOnWindows = IsRunningOnWindows();
var isRunningOnAppVeyor = AppVeyor.IsRunningOnAppVeyor;

// Parse release notes.
var releaseNotes = ParseReleaseNotes("RELEASENOTES.md");

// Get version.
var version = releaseNotes.Version.ToString();
var epoch = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

var semVersion = string.Format("{0}.{1}", version, epoch);

// Define directories. (msbuild /pack requires absolute path)
var artifactDirectory = MakeAbsolute(File("./artifacts/")).ToString();
var solutionFile = new FilePath("src/Cake.AndroidAppManifest.sln");

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup((context) =>
{
    Information("Building version {0} of Cake.AndroidAppManifest.", semVersion);
    Information("isRunningOnAppVeyor: {0}", isRunningOnAppVeyor);
	Information ("Running on Windows: {0}", isRunningOnWindows);
});

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

FilePath msBuildPath;
Task("ResolveBuildTools")
    .WithCriteria(() => isRunningOnWindows)
    .Does(() => 
{
    var vsWhereSettings = new VSWhereLatestSettings
    {
        IncludePrerelease = true
    };
    
    var vsLatest = VSWhereLatest();
    msBuildPath = (vsLatest == null)
        ? null
        : vsLatest.CombineWithFilePath("./MSBuild/15.0/Bin/MSBuild.exe");
});

Task("Build")
    .IsDependentOn("ResolveBuildtools")
    .IsDependentOn("RestorePackages")
    .IsDependentOn("UpdateAppVeyorBuildNumber")
    .Does (() =>
{
    Information("Building {0}", solutionFile);

    var settings = new MSBuildSettings()
        .SetConfiguration(configuration)
        .WithProperty("NoWarn", "1591") // ignore missing XML doc warnings
        .WithProperty("TreatWarningsAsErrors", treatWarningsAsErrors.ToString())
        .SetVerbosity(Verbosity.Minimal)
        .SetNodeReuse(false);

    if (msBuildPath != null)
        settings.ToolPath = msBuildPath;

    MSBuild(solutionFile, settings);
});

Task("UpdateAppVeyorBuildNumber")
    .WithCriteria(() => isRunningOnAppVeyor)
    .Does(() =>
{
    AppVeyor.UpdateBuildVersion(semVersion);
});

Task("RestorePackages").Does (() =>
{
    NuGetRestore(solutionFile);
});

Task("RunUnitTests")
    .IsDependentOn("Build")
    .Does(() =>
{
    XUnit2("./src/Cake.AndroidAppManifest.Tests/bin/Release/Cake.AndroidAppManifest.Tests.dll", new XUnit2Settings {
        OutputDirectory = artifactDirectory,
        XmlReport = true
    });
});

Task("Package")
    .IsDependentOn("Build")
    .IsDependentOn("RunUnitTests")
    .Does (() =>
{
    // switched to msbuild-based nuget creation
    // see here for parameters: https://docs.microsoft.com/en-us/nuget/schema/msbuild-targets
    var settings = new MSBuildSettings()
        .SetConfiguration(configuration)
        .WithTarget("pack")
        .WithProperty("IncludeSymbols", "True")
        .WithProperty("PackageVersion", version)
        .WithProperty("PackageOutputPath", artifactDirectory);

        settings.Properties.Add("PackageReleaseNotes", new List<string>(releaseNotes.Notes));

    if (msBuildPath != null)
        settings.ToolPath = msBuildPath;

    MSBuild ("./src/Cake.AndroidAppManifest/Cake.AndroidAppManifest.csproj", settings);
});

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
