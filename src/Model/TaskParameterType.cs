using System.ComponentModel;

namespace Morph.Server.Sdk.Model
{
    public enum TaskParameterType
    {
        [Description("Text or number")]
        Text,
        [Description("File name")]
        FilePath,
        [Description("Date")]
        Date,
        [Description("Calculated")]
        Calculated,
        [Description("Folder path")]
        FolderPath,
    }


    

}
