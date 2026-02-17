var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var solution = "./mp4-parser.slnx";

Task("Clean")
    .Does(() =>
{
    CleanDirectories($"./src/**/bin/{configuration}");
    CleanDirectories($"./src/**/obj");
    CleanDirectories($"./tests/**/bin/{configuration}");
    CleanDirectories($"./tests/**/obj");
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetRestore(solution);
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    DotNetBuild(solution, new DotNetBuildSettings
    {
        Configuration = configuration,
        NoRestore = true,
    });
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    DotNetTest(solution, new DotNetTestSettings
    {
        Configuration = configuration,
        NoRestore = true,
        NoBuild = true,
    });
});

Task("Default")
    .IsDependentOn("Test");

RunTarget(target);
