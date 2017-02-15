#load "packages/simple-targets-csharp.4.0.0/simple-targets-csharp.csx"

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static SimpleTargets;

// options
var solutionName = "SelfInitializingFakes";
var frameworks = new[] { "net451", "netcoreapp1.0" };
var version = "0.1.0-beta001";

// solution file locations
var nuspecs = new [] { "src/SelfInitializingFakes.nuspec" };
var testProjectDirs = new[] { "tests/SelfInitializingFakes.Tests" };
var solution = "./" + solutionName + ".sln";

// tool locations
var msBuild = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}/MSBuild/14.0/Bin/msbuild.exe";
var nuget = "./.nuget/NuGet.exe";
var xunit = "./packages/xunit.runner.console.2.1.0/tools/xunit.console.exe";

// artifact locations
var logsDirectory = "./artifacts/logs";
var outputDirectory = "./artifacts/output";
var testsDirectory = "./artifacts/tests";


// targets
var targets = new TargetDictionary();

targets.Add("default", DependsOn("pack", "test"));

targets.Add("outputDirectory", () => Directory.CreateDirectory(outputDirectory));

targets.Add("logsDirectory", () => Directory.CreateDirectory(logsDirectory));

targets.Add("testsDirectory", () => Directory.CreateDirectory(testsDirectory));

targets.Add("versionInfoFile",
    () =>
    {
        var assemblyVersion = version.Split('-')[0];
        var assemblyFileVersion = assemblyVersion;
        var assemblyInformationalVersion = version;
        var versionContents =
$@"using System.Reflection;

[assembly: AssemblyVersion(""{assemblyVersion}"")]
[assembly: AssemblyFileVersion(""{assemblyFileVersion}"")]
[assembly: AssemblyInformationalVersion(""{assemblyInformationalVersion}"")]
";

        File.WriteAllText("src/VersionInfo.cs", versionContents, Encoding.UTF8);
    });

targets.Add(
    "build",
    DependsOn("versionInfoFile", "logsDirectory"),
    () => Cmd(
        msBuild,
        $"{solution} /p:Configuration=Release /nologo /m /v:m /nr:false " +
            $"/fl /flp:LogFile={logsDirectory}/msbuild.log;Verbosity=Detailed;PerformanceSummary"));

targets.Add(
    "pack",
    DependsOn("build", "outputDirectory"),
    () =>
    {
        foreach (var nuspec in nuspecs)
        {
            Cmd(nuget, $"pack {nuspec} -Version {version} -OutputDirectory {outputDirectory} -NoPackageAnalysis");
        }
    });

targets.Add(
    "test",
    DependsOn("build", "testsDirectory"),
    () =>
    {
        foreach (var testDir in testProjectDirs)
        {
            var outputBase = Path.GetFullPath(Path.Combine(testsDirectory, Path.GetFileName(testDir)));

            foreach (var framework in frameworks)
            {
                RunDotNet(testDir, "test", $"-c Release -f {framework} -nologo -xml {outputBase}-{framework}.xml");
            }
        }
    });

Run(Args, targets);

// helpers
public static void Cmd(string fileName, string args)
{
    using (var process = new Process())
    {
        process.StartInfo = new ProcessStartInfo { FileName = $"\"{fileName}\"", Arguments = args, UseShellExecute = false, };
        Console.WriteLine($"Running '{process.StartInfo.FileName} {process.StartInfo.Arguments}'...");
        process.Start();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"The command exited with code {process.ExitCode}.");
        }
    }
}

public static void RunDotNet(string workingDirectory, string command, string args="")
{
    using (var process = new Process())
    {
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = command + " " + args,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false
        };

        Console.WriteLine($"Running '{process.StartInfo.FileName} {process.StartInfo.Arguments}' in '{process.StartInfo.WorkingDirectory}'...");
        process.Start();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"The command exited with code {process.ExitCode}.");
        }
    }
}
