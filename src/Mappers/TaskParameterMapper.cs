using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Mappers
{
    

    internal static class TaskParameterMapper
    {
        public static TaskParameterDto Parse(TaskParameterBase value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var result = new TaskParameterDto()
            {
                Name = value.Name
            };
            switch (value)
            {
                case TaskStringParameter str:
                    result.Value = str.Value as string;
                    break;
                case TaskDateParameter st:
                    DateTime dt = Convert.ToDateTime(st.Value);
                    result.Value = dt.ToString("yyyy-MM-dd");
                    break;
                default: throw new NotImplementedException($"{value.ToString()} is not supported");
            }
            return result;
            
        }

       

    }
}
