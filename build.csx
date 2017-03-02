#load "packages/simple-targets-csx.5.2.0/simple-targets.csx"

#r "System.Runtime.Serialization"
#r "System.Xml.Linq"

using System.Runtime.Serialization.Json;
using System.Xml;
using System.Xml.Linq;
using static SimpleTargets;

// options
var solutionName = "SelfInitializingFakes";
var frameworks = new[] { "net451", "netcoreapp1.0" };
var version = "0.1.0-beta001";

// solution file locations
var nuspecFiles = new [] { "src/SelfInitializingFakes.nuspec" };
var testProjectDirectories = new[] { "tests/SelfInitializingFakes.Tests" };
var mainProjectFile = "src/SelfInitializingFakes/project.json";
var solutionFile = "./" + solutionName + ".sln";
var versionInfoFile = "./src/VersionInfo.cs";

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
        if (!File.Exists(versionInfoFile) || versionContents != File.ReadAllText(versionInfoFile, Encoding.UTF8))
        {
            File.WriteAllText(versionInfoFile, versionContents, Encoding.UTF8);
        }
    });

targets.Add(
    "restore",
    () => Cmd(nuget, $"restore {solutionFile} -MSBuildVersion 14 -Verbosity quiet"));


targets.Add(
    "build",
    DependsOn("restore", "versionInfoFile", "logsDirectory"),
    () => Cmd(
        msBuild,
        $"{solutionFile} /p:Configuration=Release /nologo /m /v:m /nr:false " +
            $"/fl /flp:LogFile={logsDirectory}/msbuild.log;Verbosity=Detailed;PerformanceSummary"));

targets.Add(
    "pack",
    DependsOn("build", "outputDirectory"),
    () =>
    {
        var fakeItEasyVersion = GetDependencyVersion("FakeItEasy");
        foreach (var nuspecFile in nuspecFiles)
        {
            Cmd(nuget, $"pack {nuspecFile} -Version {version} -OutputDirectory {outputDirectory} -NoPackageAnalysis -Properties FakeItEasyVersion={fakeItEasyVersion}");
        }
    });

targets.Add(
    "test",
    DependsOn("build", "testsDirectory"),
    () =>
    {
        foreach (var testProjectDirectory in testProjectDirectories)
        {
            var outputBase = Path.GetFullPath(Path.Combine(testsDirectory, Path.GetFileName(testProjectDirectory)));

            foreach (var framework in frameworks)
            {
                Cmd(testProjectDirectory, "dotnet", $"test -c Release -f {framework} -nologo -xml {outputBase}-{framework}.xml");
            }
        }
    });

Run(Args, targets);

// helpers
public void Cmd(string fileName, string args)
{
    Cmd(".", fileName, args);
}

public void Cmd(string workingDirectory, string fileName, string args)
{
    using (var process = new Process())
    {
        process.StartInfo = new ProcessStartInfo
        {
            FileName = $"\"{fileName}\"",
            Arguments = args,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
        };

        var workingDirectoryMessage = workingDirectory == "." ? "" : $" in '{process.StartInfo.WorkingDirectory}'";
        Console.WriteLine($"Running '{process.StartInfo.FileName} {process.StartInfo.Arguments}'{workingDirectoryMessage}...");
        process.Start();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"The command exited with code {process.ExitCode}.");
        }
    }
}

public string GetDependencyVersion(string packageName)
{
    byte[] buffer = File.ReadAllBytes(mainProjectFile);
    XmlReader reader = JsonReaderWriterFactory.CreateJsonReader(buffer, new XmlDictionaryReaderQuotas());

    XElement root = XElement.Load(reader);
    return root.Element("dependencies").Element(packageName).Value;
}
