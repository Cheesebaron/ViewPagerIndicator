#module nuget:https://api.nuget.org/v3/index.json?package=Cake.DotNetTool.Module&version=0.4.0
#tool dotnet:https://api.nuget.org/v3/index.json?package=GitVersion.Tool&version=5.3.7
#tool nuget:https://api.nuget.org/v3/index.json?package=vswhere&version=2.8.4
#addin nuget:https://api.nuget.org/v3/index.json?package=Cake.Figlet&version=1.3.1

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var verbosityArg = Argument("verbosity", "Minimal");
var outputDirArg = Argument("outputDir", "./artifacts");
var verbosity = Verbosity.Minimal;

var gitVersionLog = new FilePath("./gitversion.log");

var sln = new FilePath("./ViewPagerIndicator.sln");
var outputDir = new DirectoryPath(outputDirArg);

var isRunningOnPipelines = AzurePipelines.IsRunningOnAzurePipelines || AzurePipelines.IsRunningOnAzurePipelinesHosted;

GitVersion versionInfo = null;

Setup(context => 
{
    versionInfo = context.GitVersion(new GitVersionSettings 
    {
        UpdateAssemblyInfo = true,
        OutputType = GitVersionOutput.Json,
        LogFilePath = gitVersionLog.MakeAbsolute(context.Environment)
    });

    if (isRunningOnPipelines)
    {
        var buildNumber = AzurePipelines.Environment.Build.Number;
        var informationalVersion = versionInfo.InformationalVersion;

        var invalidChars = new char[] { '"', '/', ':', '<', '>', '\\', '|', '?', '@', '*' };
        foreach (var invalidChar in invalidChars)
            informationalVersion = informationalVersion.Replace(invalidChar, '.');

        AzurePipelines.Commands.UpdateBuildNumber(informationalVersion
            + "-" + buildNumber);
    }

    var cakeVersion = typeof(ICakeContext).Assembly.GetName().Version.ToString();

    Information(Figlet("ViewPagerIndicator"));
    Information("Building version {0}, ({1}, {2}) using version {3} of Cake.",
        versionInfo.SemVer,
        configuration,
        target,
        cakeVersion);

    verbosity = (Verbosity) Enum.Parse(typeof(Verbosity), verbosityArg, true);
});

Task("Clean").Does(() =>
{
    CleanDirectories("./**/bin");
    CleanDirectories("./**/obj");
    CleanDirectories(outputDir.FullPath);

    EnsureDirectoryExists(outputDir);
});

FilePath msBuildPath;
Task("ResolveBuildTools")
    .WithCriteria(() => IsRunningOnWindows())
    .Does(() => 
{
    var vsWhereSettings = new VSWhereLatestSettings
    {
        IncludePrerelease = true,
        Requires = "Component.Xamarin"
    };
    
    var vsLatest = VSWhereLatest(vsWhereSettings);
    msBuildPath = (vsLatest == null)
        ? null
        : vsLatest.CombineWithFilePath("./MSBuild/Current/Bin/MSBuild.exe");

    if (msBuildPath != null)
        Information("Found MSBuild at {0}", msBuildPath.ToString());
});

Task("Restore")
    .IsDependentOn("ResolveBuildTools")
    .Does(() => 
{
    var settings = GetDefaultBuildSettings()
        .WithTarget("Restore");
    MSBuild(sln, settings);
});

Task("Build")
    .IsDependentOn("ResolveBuildTools")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() =>
{
    var settings = GetDefaultBuildSettings()
        .WithProperty("DebugSymbols", "True")
        .WithProperty("DebugType", "Full")
        .WithProperty("Version", versionInfo.SemVer)
        .WithProperty("PackageVersion", versionInfo.SemVer)
        .WithProperty("InformationalVersion", versionInfo.InformationalVersion)
        .WithProperty("NoPackageAnalysis", "True")
        .WithTarget("Build");

    MSBuild(sln, settings);
});

Task("CopyArtifacts")
    .IsDependentOn("Build")
    .Does(() => 
{
    var nugetFiles = GetFiles("Library/bin/" + configuration + "/**/*.nupkg");
    CopyFiles(nugetFiles, outputDir);
    CopyFileToDirectory(gitVersionLog, outputDir);
});

Task("UploadArtifacts")
    .IsDependentOn("CopyArtifacts")
    .WithCriteria(() => isRunningOnPipelines)
    .Does(() => 
{
    AzurePipelines.Commands.UploadArtifactDirectory(outputDir);
});

Task("Default")
    .IsDependentOn("Clean")
    .IsDependentOn("ResolveBuildTools")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("UploadArtifacts");

RunTarget(target);

MSBuildSettings GetDefaultBuildSettings()
{
    var settings = new MSBuildSettings 
    {
        Configuration = configuration,
        ToolPath = msBuildPath,
        Verbosity = verbosity,
        ToolVersion = MSBuildToolVersion.VS2019
    };

    return settings;
}
