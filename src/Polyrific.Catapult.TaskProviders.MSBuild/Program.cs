// Copyright (c) Polyrific, Inc 2018. All rights reserved.

using Polyrific.Catapult.TaskProviders.Core;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Polyrific.Catapult.TaskProviders.MSBuild
{
    public class Program : BuildProvider
    {
        private const string TaskProviderName = "Polyrific.Catapult.TaskProviders.MSBuild";

        private readonly IBuilder _builder;

        public override string Name => TaskProviderName;

        public Program(string[] args) : base(args)
        {
            _builder = new Builder(Logger);
        }

        public override async Task<(string outputArtifact, Dictionary<string, string> outputValues, string errorMessage)> Build()
        {
            var slnLocation = Path.Combine(Config.SourceLocation ?? Config.WorkingLocation, $"{ProjectName}.sln");
            if (AdditionalConfigs != null && AdditionalConfigs.ContainsKey("SlnLocation") && !string.IsNullOrEmpty(AdditionalConfigs["SlnLocation"]))
                slnLocation = AdditionalConfigs["SlnLocation"];
            if (!Path.IsPathRooted(slnLocation))
                slnLocation = Path.Combine(Config.WorkingLocation, slnLocation);

            var csprojLocation = Path.Combine(Path.GetDirectoryName(slnLocation), ProjectName, $"{ProjectName}.csproj");
            if (AdditionalConfigs != null && AdditionalConfigs.ContainsKey("CsprojLocation") && !string.IsNullOrEmpty(AdditionalConfigs["CsprojLocation"]))
                csprojLocation = AdditionalConfigs["CsprojLocation"];
            if (!Path.IsPathRooted(csprojLocation))
                csprojLocation = Path.Combine(Path.GetDirectoryName(slnLocation), csprojLocation);

            var buildConfiguration = "Release";
            if (AdditionalConfigs != null && AdditionalConfigs.ContainsKey("Configuration") && !string.IsNullOrEmpty(AdditionalConfigs["Configuration"]))
                buildConfiguration = AdditionalConfigs["Configuration"];

            var projectName = Path.GetFileNameWithoutExtension(csprojLocation);
            var buildOutputLocation = Path.Combine(Config.WorkingLocation, "publish", projectName);

            var artifactLocation = "artifact";
            if (!string.IsNullOrEmpty(Config.OutputArtifactLocation))
                artifactLocation = Config.OutputArtifactLocation;
            if (!Path.IsPathRooted(artifactLocation))
                artifactLocation = Path.Combine(Config.WorkingLocation, artifactLocation);

            string msBuildLocation = null;
            if (AdditionalConfigs != null && AdditionalConfigs.ContainsKey("MSBuildLocation") && !string.IsNullOrEmpty(AdditionalConfigs["MSBuildLocation"]))
                msBuildLocation = AdditionalConfigs["MSBuildLocation"];

            var error = await _builder.Build(slnLocation, csprojLocation, buildOutputLocation, buildConfiguration, msBuildLocation);
            if (!string.IsNullOrEmpty(error))
                return ("", null, error);

            var destinationArtifact = Path.Combine(artifactLocation, $"{projectName}.zip");
            error = await _builder.CreateArtifact(buildOutputLocation, destinationArtifact);
            if (!string.IsNullOrEmpty(error))
                return ("", null, error);

            return (destinationArtifact, null, "");
        }

        public async static Task Main(string[] args)
        {
            var app = new Program(args);

            var result = await app.Execute();
            app.ReturnOutput(result);
        }
    }
}
