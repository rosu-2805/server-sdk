using System;
using System.Linq;
using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Model;

namespace Morph.Server.Sdk.Mappers
{
    internal static class WorkflowResultDetailsMapper
    {
        public static WorkflowResultDetails FromDto(WorkflowResultDetailsDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            return new WorkflowResultDetails()
            {
                Errors = dto.Errors?.Select(WorkflowResultErrorInfoMapper.FromDto)?.ToList(),
                FinishedTime = DateTime.Parse(dto.FinishedTime),
                SpaceName = dto.SpaceName,
                JournalEntryId = dto.JournalEntryId,
                JournalEntryUrl = dto.JournalEntryUrl,
                Result = ParseWorkflowResultCode(dto.Result)
            };
        }

        private static WorkflowResultCode ParseWorkflowResultCode(string text)
        {
            switch (text)
            {
                case  "Success":
                    return WorkflowResultCode.Success;
                case "Failure":
                    return WorkflowResultCode.Failure;
                case "TimedOut":
                    return WorkflowResultCode.TimedOut;
                case "Canceled.By_User":
                    return WorkflowResultCode.CanceledByUser;
                default:
                    throw new Exception($"Not supported WorkflowResultCode '{text}'");
            }
        }
    }
}