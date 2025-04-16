using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents a task's status within a section, combining section, task, status and question details.
/// This model makes it easier to track task progress without needing multiple database queries.
/// </summary>
public class TaskItemStatusSection : ITaskItemStatus, ISection
{
    // Section Table
    public Guid SectionId { get; set; }
    public required string SectionName { get; set; }
    public int SectionOrderNumber { get; set; }

    // Task Table
    public Guid TaskId { get; set; }
    public required string TaskName { get; set; }
    public required string TaskNameUrl { get; set; }
    public int TaskOrderNumber { get; set; }

    // Task Status Table
    public Guid TaskStatusId { get; set; }
    public TaskStatusEnum Status { get; set; }

    // Question Table
    public required string QuestionNameUrl { get; set; }
}