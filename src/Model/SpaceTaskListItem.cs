using System;
using System.Collections.Generic;

namespace Morph.Server.Sdk.Model
{
    public class SpaceTasksListItem
    {
        public Guid Id { get; internal set; }
        public string TaskName { get; internal set; } = string.Empty;
        public string ProjectPath { get; internal set; } = string.Empty;
        public string Note { get; internal set; } = string.Empty;

        public TaskSchedule[] Schedules { get; internal set; } = Array.Empty<TaskSchedule>();
        //public List<TaskParameter> TaskParameters { get; internal set; } = new List<TaskParameter>();
        //public string StatusText { get; internal set; } = string.Empty;
        public bool Enabled { get; internal set; } = false;
        //public bool IsRunning { get; internal set; } = false;
        //public TaskState TaskState { get; internal set; } = TaskState.Disabled;
    }

    public sealed class SpaceTask : SpaceTasksListItem
    {
        public List<TaskParameterBase> TaskParameters { get; internal set; } = new List<TaskParameterBase>();
    }

    public sealed class TaskSchedule
    {
        public string ScheduleType { get; internal set; } = string.Empty;

        public string ScheduleDescription { get; internal set; } = String.Empty;
    }
}