// Copyright (c) Polyrific, Inc 2018. All rights reserved.

using Polyrific.Catapult.Plugins.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Polyrific.Catapult.Plugins.MsBuild
{
    public class Program : BuildProvider
    {
        private readonly IBuilder _builder;

        public override string Name => "Polyrific.Catapult.Plugins.MsBuild";

        public Program() : base(new string[0])
        {
        }

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

            var buildConfiguration = "Release";
            if (AdditionalConfigs != null && AdditionalConfigs.ContainsKey("Configuration") && !string.IsNullOrEmpty(AdditionalConfigs["Configuration"]))
                buildConfiguration = AdditionalConfigs["Configuration"];

            var buildOutputLocation = Path.Combine(Config.WorkingLocation, "publish");

            var artifactLocation = "artifact";
            if (!string.IsNullOrEmpty(Config.OutputArtifactLocation))
                artifactLocation = Config.OutputArtifactLocation;
            if (!Path.IsPathRooted(artifactLocation))
                artifactLocation = Path.Combine(Config.WorkingLocation, artifactLocation);
            
            var error = await _builder.Build(slnLocation, buildOutputLocation, buildConfiguration);
            if (!string.IsNullOrEmpty(error))
                return ("", null, error);

            var destinationArtifact = Path.Combine(artifactLocation, $"{ProjectName}.zip");
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
