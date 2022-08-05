using Cake.Core;
using Cake.Frosting;
using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using System.IO;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .Run(args);
    }
}

public class BuildContext : FrostingContext
{
    public string MsBuildConfiguration { get; set; }
    public string Framework { get; set; }
    public string ProjectPath { get; set; }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        MsBuildConfiguration = context.Argument("configuration", "Debug");
        Framework = context.Argument("framework", "net6.0");
        ProjectPath = context.Argument("project", "../src/examples/QuickImGuiNET.Example.Veldrid/");
    }
}

[TaskName("Clean")]
public sealed class CleanTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.CleanDirectory($"{context.ProjectPath}/bin");
        context.CleanDirectory($"{context.ProjectPath}/obj");
    }
}

[TaskName("Build")]
[IsDependentOn(typeof(CleanTask))]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetBuild(context.ProjectPath, new DotNetBuildSettings
        {
            Configuration = context.MsBuildConfiguration,
            Framework = context.Framework,
            OutputDirectory = $"{context.ProjectPath}/bin",
            Verbosity = DotNetVerbosity.Minimal
        });
    }
}

[IsDependentOn(typeof(BuildTask))]
public sealed class Default : FrostingTask
{
}
