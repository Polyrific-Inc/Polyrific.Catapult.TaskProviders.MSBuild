// Copyright (c) Polyrific, Inc 2018. All rights reserved.

using Microsoft.Extensions.Logging;
using Polyrific.Catapult.Plugins.MSBuild.Helpers;
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;

namespace Polyrific.Catapult.Plugins.MSBuild
{
    public class Builder : IBuilder
    {
        private const string DefaultMsBuildLocation = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\msbuild.exe";

        private static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        private ILogger _logger;

        public Builder(ILogger logger)
        {
            logger.LogInformation("Initiating Catapult build.");

            _logger = logger;
        }

        public async Task<string> Build(string slnLocation, string csprojLocation, string buildOutputLocation, string configuration = "Debug", string msBuildLocation = null)
        {
            _logger.LogInformation("Building artifact using MS Build.");

            var restoreResult = await ExecuteNugetRestore(slnLocation);
            _logger.LogDebug(restoreResult);

            if (!string.IsNullOrEmpty(restoreResult))
                return restoreResult;

            var buildResult = await ExecuteMsBuild(slnLocation, csprojLocation, buildOutputLocation, configuration, msBuildLocation);

            if (!string.IsNullOrEmpty(buildResult))
            {
                return buildResult;
            }

            return "";
        }

        public Task<string> CreateArtifact(string buildOutputLocation, string destinationArtifact)
        {
            var errorMessage = "";

            var extension = Path.GetExtension(destinationArtifact);
            if (string.IsNullOrEmpty(extension) || !extension.Equals(".zip", StringComparison.InvariantCultureIgnoreCase))
                destinationArtifact = Path.ChangeExtension(destinationArtifact, "zip");

            var dir = Path.GetDirectoryName(destinationArtifact);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (File.Exists(destinationArtifact))
                File.Delete(destinationArtifact);

            try
            {
                ZipFile.CreateFromDirectory(buildOutputLocation, destinationArtifact, CompressionLevel.Fastest, false);
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
            }

            return Task.FromResult(errorMessage);
        }
        
        private async Task<string> ExecuteMsBuild(string solutionLocation, string csprojLocation, string buildFolder, string configuration, string msBuildLocation)
        {
            _logger.LogDebug("Running MsBuild.exe");
            
            Directory.CreateDirectory(buildFolder);

            var args = $@"""{csprojLocation}"" /p:DeployOnBuild=true /p:WebPublishMethod=FileSystem /p:DeployDefaultTarget=WebPublish /p:SkipInvalidConfigurations=true /p:publishUrl=""{buildFolder}\\"" /p:Configuration={configuration} /p:SolutionDir=""{Path.GetDirectoryName(solutionLocation)}""";

            return (await CommandHelper.Execute(msBuildLocation ?? DefaultMsBuildLocation, args, _logger)).error;
        }

        private async Task<string> ExecuteNugetRestore(string solutionFile)
        {
            _logger.LogDebug("Restoring Nuget Packages");
            var args = $"restore \"{solutionFile}\"";

            var nugetLocation = Path.Combine(AssemblyDirectory, "Tools/nuget.exe");
            return (await CommandHelper.Execute(nugetLocation, args, _logger)).error;
        }
    }
}
