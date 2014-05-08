﻿using System;
using System.Linq;
using MrCMS.Entities;
using MrCMS.Helpers;
using MrCMS.Website;
using Ninject;

namespace MrCMS.Tasks
{
    public class QueuedTask : SiteEntity, IHaveExecutionStatus
    {
        public virtual string Type { get; set; }
        public virtual Type GetTaskType()
        {
            return TypeHelper.GetGenericTypeByName(Type);
        }
        public virtual string Data { get; set; }
        public virtual TaskExecutionStatus Status { get; set; }
        public virtual int Tries { get; set; }
        public virtual int Priority { get; set; }
        public virtual DateTime? QueuedAt { get; set; }
        public virtual DateTime? StartedAt { get; set; }
        public virtual DateTime? CompletedAt { get; set; }
        public virtual DateTime? FailedAt { get; set; }

        public virtual string DisplayTypeName
        {
            get
            {
                var taskType = GetTaskType();
                if (taskType == null)
                    return Type;
                if (!taskType.IsGenericType)
                    return taskType.Name.BreakUpString();
                return taskType.Name.Remove(taskType.Name.IndexOf('`')).BreakUpString() + " - " +
                       string.Join(", ", taskType.GetGenericArguments().Select(type => type.Name.BreakUpString()));
            }
        }

        public virtual void OnStarting(IExecutableTask executableTask)
        {
            Status = TaskExecutionStatus.Executing;
            StartedAt = CurrentRequestData.Now;
            executableTask.OnStarting();
        }

        public virtual void OnSuccess(IExecutableTask executableTask)
        {
            Status = TaskExecutionStatus.Completed;
            CompletedAt = CurrentRequestData.Now;
            executableTask.OnSuccess();
        }

        public virtual void OnFailure(IExecutableTask executableTask, Exception exception)
        {
            executableTask.OnFailure(exception);
            if (Tries < 5)
                Status = TaskExecutionStatus.Pending;
            else
            {
                Status = TaskExecutionStatus.Failed;
                FailedAt = CurrentRequestData.Now;
                executableTask.OnFinalFailure(exception);
            }
            Tries++;
        }
    }
}