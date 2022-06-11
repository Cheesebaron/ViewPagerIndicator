#tool dotnet:https://api.nuget.org/v3/index.json?package=GitVersion.Tool&version=5.10.3
#addin nuget:https://api.nuget.org/v3/index.json?package=Cake.Figlet&version=2.0.1

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var verbosityArg = Argument("verbosity", "Minimal");
var outputDirArg = Argument("outputDir", "./artifacts");
var verbosity = DotNetVerbosity.Minimal;

var gitVersionLog = new FilePath("./gitversion.log");

var sln = new FilePath("./ViewPagerIndicator.sln");
var outputDir = new DirectoryPath(outputDirArg);

var isGitHubActionsBuild = GitHubActions.IsRunningOnGitHubActions;

GitVersion versionInfo = null;

Setup(context => 
{
    versionInfo = context.GitVersion(new GitVersionSettings 
    {
        UpdateAssemblyInfo = true,
        OutputType = GitVersionOutput.Json,
        LogFilePath = gitVersionLog.MakeAbsolute(context.Environment)
    });

    var cakeVersion = typeof(ICakeContext).Assembly.GetName().Version.ToString();

    Information(Figlet("ViewPagerIndicator"));
    Information("Building version {0}, ({1}, {2}) using version {3} of Cake.",
        versionInfo.SemVer,
        configuration,
        target,
        cakeVersion);

    verbosity = (DotNetVerbosity) Enum.Parse(typeof(DotNetVerbosity), verbosityArg, true);
});

Task("Clean").Does(() =>
{
    CleanDirectories("./**/bin");
    CleanDirectories("./**/obj");
    CleanDirectories(outputDir.FullPath);

    EnsureDirectoryExists(outputDir);
});

Task("Restore")
    .Does(() => 
{
    DotNetRestore(sln.ToString());
});

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() =>
{
    var msBuildSettings = new DotNetMSBuildSettings
    {
        Version = versionInfo.SemVer,
        PackageVersion = versionInfo.SemVer,
        InformationalVersion = versionInfo.InformationalVersion,
        ContinuousIntegrationBuild = true
    };
    
    msBuildSettings = msBuildSettings
        .WithProperty("DebugSymbols", "True")
        .WithProperty("DebugType", "Portable");

    var settings = new DotNetBuildSettings
    {
        Configuration = configuration,
        Verbosity = verbosity,
        MSBuildSettings = msBuildSettings
    };

    DotNetBuild(sln.ToString(), settings);
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
    .WithCriteria(() => isGitHubActionsBuild)
    .Does(() => 
{
    GitHubActions.Commands.UploadArtifact(outputDir, "nugets");
});

Task("Default")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("UploadArtifacts");

RunTarget(target);
