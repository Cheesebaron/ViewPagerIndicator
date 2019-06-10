#tool nuget:?package=GitVersion.CommandLine&version=4.0.0
#tool nuget:?package=vswhere&version=2.6.7
#addin nuget:?package=Cake.Figlet&version=1.3.0

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var verbosityArg = Argument("verbosity", "Minimal");
var outputDirArg = Argument("outputDir", "./artifacts");
var verbosity = Verbosity.Minimal;

var sln = new FilePath("./ViewPagerIndicator.sln");
var outputDir = new DirectoryPath(outputDirArg);

var isRunningOnPipelines = TFBuild.IsRunningOnAzurePipelines;

GitVersion versionInfo = null;

Setup(context => {
    versionInfo = context.GitVersion(new GitVersionSettings {
        UpdateAssemblyInfo = true,
        OutputType = GitVersionOutput.Json
    });

    if (isRunningOnPipelines)
    {
        var buildNumber = AppVeyor.Environment.Build.Number;
        TFBuild.Commands.UpdateBuildNumber(versionInfo.InformationalVersion
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
    .Does(() =>  {

    var settings = GetDefaultBuildSettings()
        .WithProperty("DebugSymbols", "True")
        .WithProperty("DebugType", "Full")
        .WithProperty("Version", versionInfo.SemVer)
        .WithProperty("PackageVersion", versionInfo.SemVer)
        .WithProperty("InformationalVersion", versionInfo.InformationalVersion)
        .WithProperty("NoPackageAnalysis", "True")
        .WithTarget("Build");

//     settings.BinaryLogger = new MSBuildBinaryLogSettings 
//     {
//         Enabled = true,
//         FileName = "viewpagerindicator.binlog"
//     };

    MSBuild(sln, settings);
});

Task("Package")
    .IsDependentOn("Build")
    .Does(() => 
{
    var nugetContent = new List<NuSpecContent>();

    var binDir = "./Library/bin/" + configuration;
    var files = GetFiles(binDir + "/*ViewPagerIndicator.dll") + GetFiles(binDir + "/*ViewPagerIndicator.pdb");
    foreach(var dll in files)
    {
        Information("File: {0}", dll.ToString());
        nugetContent.Add(new NuSpecContent
        {
            Target = "lib/MonoAndroid90",
            Source = dll.ToString()
        });
    }

    var nugetSettings = new NuGetPackSettings {
        Id = "ViewPagerIndicator",
        Title = "ViewPagerIndicator for Xamarin.Android",
        Authors = new [] { "Tomasz Cielecki" },
        Owners = new [] { "cheesebaron" },
        IconUrl = new Uri("http://i.imgur.com/V3983YY.png"),
        ProjectUrl = new Uri("https://github.com/Cheesebaron/ViewPagerIndicator"),
        LicenseUrl = new Uri("https://raw.githubusercontent.com/Cheesebaron/ViewPagerIndicator/master/LICENSE"),
        Copyright = "Copyright (c) Tomasz Cielecki",
        Tags = new [] { "viewpagerindicator", "viewpager", "android", "xamarin", "indicator", "pager" },
        Description = "A port of ViewPagerIndicator for Xamarin.Android. A highly customizable indicator for ViewPager.",
        RequireLicenseAcceptance = false,
        Version = versionInfo.NuGetVersion,
        Symbols = false,
        NoPackageAnalysis = true,
        OutputDirectory = outputDir,
        Verbosity = NuGetVerbosity.Detailed,
        Files = nugetContent,
        BasePath = "./",
        Dependencies = new NuSpecDependency[] {
            new NuSpecDependency { Id = "Xamarin.Android.Support.ViewPager", Version = "28.0.0.1" }
        }
    };

    NuGetPack(nugetSettings);
});

Task("UploadArtifacts")
    .WithCriteria(() => isRunningOnPipelines)
    .Does(() => 
{
    TFBuild.Commands.UploadArtifactDirectory(outputDir);
});

Task("Default")
    .IsDependentOn("Clean")
    .IsDependentOn("ResolveBuildTools")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("Package");

RunTarget(target);

MSBuildSettings GetDefaultBuildSettings()
{
    var settings = new MSBuildSettings 
    {
        Configuration = configuration,
        ToolPath = msBuildPath,
        Verbosity = verbosity,
        ArgumentCustomization = args => args.Append("/m"),
        ToolVersion = MSBuildToolVersion.VS2019
    };

    return settings;
}
