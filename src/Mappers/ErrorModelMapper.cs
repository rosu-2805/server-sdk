using Morph.Server.Sdk.Dto.Errors;
using Morph.Server.Sdk.Model;

namespace Morph.Server.Sdk.Mappers
{
    internal static class ErrorModelMapper
    {
        public static ErrorModel MapFromDto (Error error)
        {
            if(error == null)
            {
                return null;
            }
            return new ErrorModel
            {
                Code = error.code,
                Message = error.message
            };
        }
    }

}
