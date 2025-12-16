using ProjectME_BE.Domain.Common;

namespace ProjectME_BE.Domain.Model.RecurringTask;

public class RecurringTaskAggregate : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public List<DateTimeOffset> AssignedDates { get; private set; } = new();

    private RecurringTaskAggregate() { }

    public RecurringTaskAggregate(Guid userId, string name, List<DateTimeOffset> assignedDates)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        UserId = userId;
        Name = name;
        AssignedDates = assignedDates ?? new List<DateTimeOffset>();
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        Name = name;
    }

    public void AddAssignedDate(DateTimeOffset date)
    {
        if (!AssignedDates.Contains(date))
        {
            AssignedDates.Add(date);
        }
    }

    public void RemoveAssignedDate(DateTimeOffset date)
    {
        AssignedDates.Remove(date);
    }

    public void UpdateAssignedDates(List<DateTimeOffset> dates)
    {
        AssignedDates = dates ?? new List<DateTimeOffset>();
    }

    public void ClearAssignedDates()
    {
        AssignedDates.Clear();
    }
}
