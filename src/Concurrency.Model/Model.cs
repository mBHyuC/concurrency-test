namespace Concurrency.Model
{
    using System;
    using System.ComponentModel.DataAnnotations;
    public abstract class Entity
    {
        public int Id { get; set; }
    }

    public enum ProcessState
    {
        ToProcess,
        InProcess,
        DoneProcessOK,
        DoneProcessERROR
    }

    public class WorkItem : Entity
    {
        public WorkItem()
        {
            State = ProcessState.ToProcess;
        }

        [Required]
        public string Name { get; set; }

        [Required]
        public ProcessState State { get; set; } = ProcessState.ToProcess;
        [Timestamp] public byte[] Timestamp { get; set; }
    }

    public enum RunState
    {
        InitTask,
        GeneratorRunningTask,
        ProcessorRunningTask,
        DoneTask,
        AbortTask,
        PauseTask
    }

    public class Run : Entity
    {
        [Required]
        public DateTimeOffset Start { get; set; }

        public DateTimeOffset End { get; set; }

        [Required]
        public RunState Status { get; set; } = RunState.InitTask;

        public string Name { get; set; }

        [Required]
        public string Guid { get; set; }
    }

}
