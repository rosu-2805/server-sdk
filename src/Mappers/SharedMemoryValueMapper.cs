using System;
using System.Collections.Generic;
using System.Globalization;
using Morph.Server.Sdk.Dto.SharedMemory;
using Morph.Server.Sdk.Model.SharedMemory;

namespace Morph.Server.Sdk.Mappers
{
    internal class SharedMemoryValueMapper
    {
        public static SharedMemoryValue MapFromDto(SharedMemoryValueDto dto)
        {
            switch (dto.TypeCode)
            {
                case SharedMemoryValueDto.TypeCodes.Error:
                    return SharedMemoryValue.NewError(dto.Contents);
                case SharedMemoryValueDto.TypeCodes.Nothing:
                    return SharedMemoryValue.NewNothing();
                case SharedMemoryValueDto.TypeCodes.Boolean:
                    return SharedMemoryValue.NewBoolean(bool.Parse(dto.Contents));
                case SharedMemoryValueDto.TypeCodes.Text:
                    return SharedMemoryValue.NewText(dto.Contents);
                case SharedMemoryValueDto.TypeCodes.Number:
                    return SharedMemoryValue.NewNumber(decimal.Parse(dto.Contents, CultureInfo.InvariantCulture));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static SharedMemoryValueDto MapToDto(SharedMemoryValue value)
        {
            switch (value)
            {
                case SharedMemoryValue.Error error:
                    return new SharedMemoryValueDto
                    {
                        Contents = error.Message,
                        TypeCode = SharedMemoryValueDto.TypeCodes.Error
                    };
                case SharedMemoryValue.Nothing _:
                    return new SharedMemoryValueDto
                    {
                        Contents = string.Empty,
                        TypeCode = SharedMemoryValueDto.TypeCodes.Nothing
                    };
                case SharedMemoryValue.Boolean boolean:
                    return new SharedMemoryValueDto
                    {
                        Contents = boolean.Value.ToString(),
                        TypeCode = SharedMemoryValueDto.TypeCodes.Boolean
                    };
                case SharedMemoryValue.Text text:
                    return new SharedMemoryValueDto
                    {
                        Contents = text.Value,
                        TypeCode = SharedMemoryValueDto.TypeCodes.Text
                    };
                case SharedMemoryValue.Number number:
                    return new SharedMemoryValueDto
                    {
                        Contents = number.Value.ToString(CultureInfo.InvariantCulture),
                        TypeCode = SharedMemoryValueDto.TypeCodes.Number
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        public static SharedMemoryListResponse MapFromDto(SharedMemoryListResponseDto arg)
        {
            var result = new SharedMemoryListResponse
            {
                Items = new List<SharedMemoryListRecord>(),
                HasMore = arg.HasMore
            };
            
            foreach (var item in arg.Items)
            {
                result.Items.Add(new SharedMemoryListRecord
                {
                    Key = item.Key,
                    Value = MapFromDto(item.Value),
                    Author = item.Author,
                    Modified = DateTime.Parse(item.Modified, CultureInfo.InvariantCulture)
                });
            }
            
            return result;
        }

        public static string MapOverwriteBehavior(OverwriteBehavior overwriteBehavior)
        {
            switch (overwriteBehavior)
            {
                case OverwriteBehavior.Overwrite:
                    return SetSharedMemoryValueDto.BehaviorCodes.Overwrite;
                case OverwriteBehavior.Fail:
                    return SetSharedMemoryValueDto.BehaviorCodes.ThrowIfExists;
                case OverwriteBehavior.DoNothing:
                    return SetSharedMemoryValueDto.BehaviorCodes.IgnoreIfExists;
                default:
                    throw new ArgumentOutOfRangeException(nameof(overwriteBehavior), overwriteBehavior, null);
            }
        }
    }
}