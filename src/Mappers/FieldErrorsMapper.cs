using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Dto.Commands;
using Morph.Server.Sdk.Dto.Errors;
using Morph.Server.Sdk.Model;
using Morph.Server.Sdk.Model.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Mappers
{
    internal static class FieldErrorsMapper
    {
        public static List<FieldError> MapFromDto(Error error)
        {
            var result = error.detais.Select(x => new FieldError
            {
                Field = x.target,
                Message = x.message,
                FieldErrorType = ParseFieldErrorType(x.code)
            });
            return result.ToList();
        }

        private static FieldErrorType ParseFieldErrorType(string code)
        {
            FieldErrorType e;
            if (Enum.TryParse(code, out e))
            {
                return e;
            }
            else
            {
                return FieldErrorType.OtherError;
            }
            
        }
       
    }
}
