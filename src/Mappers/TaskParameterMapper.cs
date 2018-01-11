using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Mappers
{
    

    internal static class TaskParameterMapper
    {
        public static TaskParameterBase FromDto(TaskParameterResponseDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            var parameterType = ParseParameterType(dto.ParameterType);
            switch (parameterType)
            {
                case TaskParameterType.Text: return new TaskStringParameter(dto.Name, dto.Value) { Note = dto.Note };
                case TaskParameterType.Date:
                    DateTime dt;
                    DateTime? dtn = null;
                    bool res = DateTime.TryParseExact(dto.Value, TaskDateParameter.dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
                    if (res)
                        dtn = dt;
                    return new TaskDateParameter(dto.Name, dtn) { Note = dto.Note };
                case TaskParameterType.FilePath:
                    return new TaskFileParameter(dto.Name, dto.Value) { Note = dto.Note };
                default: throw new NotImplementedException("Specified parameter type is not implemented yet");
            }

           
        }

        private static TaskParameterType ParseParameterType(string value)
        {
            //fallback to text
            if (string.IsNullOrWhiteSpace(value))
                return TaskParameterType.Text;

            return (TaskParameterType)Enum.Parse(typeof(TaskParameterType), value, true);
        }

        public static TaskParameterRequestDto ToDto(TaskParameterBase value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var result = new TaskParameterRequestDto()
            {
                Name = value.Name,
                Value = value.Value               
                
            };


            //switch (value)
            //{
            //    case TaskStringParameter str:
            //        result.Value = str.Value as string;
            //        break;
            //    case TaskDateParameter st:
            //        DateTime dt = Convert.ToDateTime(st.Value);
            //        result.Value = dt.ToString("yyyy-MM-dd");
            //        break;
            //    default: throw new NotImplementedException($"{value.ToString()} is not supported");
            //}
            return result;
            
        }

       

    }
}
