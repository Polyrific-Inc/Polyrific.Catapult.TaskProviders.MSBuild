// Copyright (c) Polyrific, Inc 2018. All rights reserved.

using System.Threading.Tasks;

namespace Polyrific.Catapult.Plugins.MSBuild
{
    public interface IBuilder
    {
        /// <summary>
        /// Build the source code
        /// </summary>
        /// <param name="slnLocation">Location of the solution file</param>
        /// <param name="csprojLocation">Location of the csproj file</param>
        /// <param name="buildOutputLocation">Location of the build output</param>
        /// <param name="configuration">Build configuration (default is Debug)</param>
        /// <param name="msBuildLocation">Location of the msbuild.exe. Leave as null to use default value</param>
        /// <returns>Error message</returns>
        Task<string> Build(string slnLocation, string csprojLocation, string buildOutputLocation, string configuration = "Debug", string msBuildLocation = null);

        /// <summary>
        /// Create build artifact
        /// </summary>
        /// <param name="buildOutputLocation">Location of the build output</param>
        /// <param name="destinationArtifact">Name of the artifact package, including the complete location</param>
        /// <returns>Error message</returns>
        Task<string> CreateArtifact(string buildOutputLocation, string destinationArtifact);
    }
}
