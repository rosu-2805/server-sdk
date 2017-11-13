using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Model
{
    public abstract class TaskBaseParameter
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }


    public sealed class TaskStringParameter : TaskBaseParameter
    {
        public TaskStringParameter() : base()
        {

        }
        public TaskStringParameter(string name, string value) : this()
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("message", nameof(name));
            }

            this.Name = name;
            this.Value = value;
        }
    }

    public sealed class TaskDateParamter : TaskBaseParameter
    {
        public TaskDateParamter() : base()
        {

        }
        public TaskDateParamter(string name, DateTime value) : this()
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("message", nameof(name));
            }

            this.Name = name;
            this.Value = value;
        }
    }
}
