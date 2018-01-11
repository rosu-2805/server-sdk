using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Model
{
    
    public class TaskParameterBase
    {
        internal const string dateFormat = "yyyy-MM-dd";

        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public TaskParameterType ParameterType { get;  set; } = TaskParameterType.Text;
        public string Note { get; set; }

        public DateTime? DateValue
        {
            get
            {
                if (ParameterType == TaskParameterType.Date)
                {
                    DateTime dt;
                    bool res = DateTime.TryParseExact(Value, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
                    if (res)
                        return dt;
                }

                return null;
            }

            set
            {
                if(ParameterType == TaskParameterType.Date)
                {
                    if(value != null)
                    {
                        Value = value.Value.ToString(dateFormat);
                    }
                    else
                    {
                        Value = string.Empty;
                    }
                }
                else
                {
                    throw new NotImplementedException("This parameter is not a Date type");
                }
            }
        }

    }


    public sealed class TaskStringParameter : TaskParameterBase
    {
        public TaskStringParameter() : base()
        {

        }
        public TaskStringParameter(string name, string value) : this()
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Parameter name is empty", nameof(name));
            }

            this.Name = name;
            this.Value = value;
            this.ParameterType = TaskParameterType.Text;           
        }
    }

    public sealed class TaskFileParameter : TaskParameterBase
    {
        public TaskFileParameter() : base()
        {

        }
        public TaskFileParameter(string name, string value) : this()
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Parameter name is empty", nameof(name));
            }
            this.Name = name;
            this.Value = value;
            this.ParameterType = TaskParameterType.FilePath;
        }
    }

    public sealed class TaskDateParameter : TaskParameterBase
    {
        public TaskDateParameter() : base()
        {

        }
        public TaskDateParameter(string name, DateTime? value) : this()
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Parameter name is empty", nameof(name));
            }

            this.Name = name;
            if (value.HasValue)
            {
                this.Value = value.Value.ToString(dateFormat);
            }
            else
            {
                this.Value = string.Empty;
            }
            this.ParameterType = TaskParameterType.Date;
        }

        

       
    }
}
