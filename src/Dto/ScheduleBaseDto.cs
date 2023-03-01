using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    public class ScheduleBaseDto
    {
        [DataMember(Name="scheduleAsText")]
        public string ScheduleAsText { get; set; } = string.Empty;
        [DataMember(Name="scheduleType")]
        public string ScheduleType { get; set; }
    }
}