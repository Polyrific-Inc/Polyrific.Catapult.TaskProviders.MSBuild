using Newtonsoft.Json;
using Polyrific.Catapult.Plugins.Core.Configs;
using Polyrific.Catapult.Plugins.MSBuild.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Polyrific.Catapult.Plugins.MSBuild.UnitTests
{
    public class BuildProviderTests
    {
        [Fact]
        public async void Build_Success()
        {
            var workingLocation = Path.Combine(AppContext.BaseDirectory, "working", "20180817.1");

            if (Directory.Exists(workingLocation))
                Directory.Delete(workingLocation, true);

            Directory.CreateDirectory(workingLocation);

            await GenerateTestProject("TestProject", Path.Combine(workingLocation, "src"));

            var taskConfig = new BuildTaskConfig
            {
                SourceLocation = "src",
                OutputArtifactLocation = "artifact",
                WorkingLocation = workingLocation
            };

            var provider = new Program(new string[] { GetArgString("main", "TestProject", taskConfig, new Dictionary<string, string>()) });

            var result = await provider.Build();

            Assert.Equal("", result.errorMessage);
            Assert.True(File.Exists(result.outputArtifact));
        }

        private async Task GenerateTestProject(string projectName, string outputLocation)
        {
            await CommandHelper.Execute("dotnet", $"new sln -n {projectName} -o \"{outputLocation}\"");
            await CommandHelper.Execute("dotnet", $"new console -n {projectName} -o \"{Path.Combine(outputLocation, projectName)}\" --target-framework-override net45");
            await CommandHelper.Execute("dotnet", $"sln \"{Path.Combine(outputLocation, $"{projectName}.sln")}\" add \"{Path.Combine(outputLocation, projectName, $"{projectName}.csproj")}\"");
        }

        private string GetArgString(string process, string projectName, BuildTaskConfig taskConfig, Dictionary<string, string> additionalConfigs)
        {
            var dict = new Dictionary<string, object>
            {
                {"process", process},
                {"project", projectName},
                {"config", taskConfig},
                {"additional", additionalConfigs}
            };

            return JsonConvert.SerializeObject(dict);
        }
    }
}
