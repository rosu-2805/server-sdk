using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    public sealed class ProjectDetailsInfoDto
    {
        [DataMember(Name="projectName")]
        public string ProjectName { get; set; }
        [DataMember(Name="projectPath")]
        public string ProjectPath { get; set; }
        [DataMember(Name="projectLastEdited")]
        public string ProjectLastEdited { get; set; }
        
    }
}