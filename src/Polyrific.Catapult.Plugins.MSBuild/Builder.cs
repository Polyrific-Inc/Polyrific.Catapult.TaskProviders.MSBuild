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

        public async Task<string> Build(string slnLocation, string buildOutputLocation, string configuration = "Debug")
        {
            _logger.LogInformation("Building artifact using MS Build.");

            var restoreResult = await ExecuteNugetRestore(slnLocation);
            _logger.LogDebug(restoreResult);

            if (!string.IsNullOrEmpty(restoreResult))
                return restoreResult;

            var buildResult = await ExecuteMsBuild(slnLocation, buildOutputLocation, configuration);

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
        
        private async Task<string> ExecuteMsBuild(string solutionFile, string buildFolder, string configuration)
        {
            _logger.LogDebug("Running MsBuild.exe");
            var args = $@"msbuild ""{solutionFile}"" /p:OutputPath=""{buildFolder}"" /p:Configuration={configuration}";

            return (await CommandHelper.Execute("dotnet", args, _logger)).error;
        }

        private async Task<string> ExecuteNugetRestore(string solutionFile)
        {
            _logger.LogDebug("Restoring Nuget Packages");
            var args = $"restore {solutionFile}";

            var nugetLocation = Path.Combine(AssemblyDirectory, "Tools/nuget.exe");
            return (await CommandHelper.Execute(nugetLocation, args, _logger)).error;
        }
    }
}
