using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Model
{
    public class SpacesList
    {
        public List<SpaceListItem> Items { get; internal set; } = new List<SpaceListItem>();
    }

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
