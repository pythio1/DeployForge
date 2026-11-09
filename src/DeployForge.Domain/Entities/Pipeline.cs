namespace DeployForge.Domain.Entities;

public sealed class Pipeline
{
    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<PipelineStep> Steps => _steps.AsReadOnly();

    private readonly List<PipelineStep> _steps = [];

    // EF Core использует этот конструктор
    // для восстановления объекта из базы данных.
    private Pipeline() { }

    public Pipeline(Guid projectId, string name)
    {
        if (projectId == Guid.Empty)
            throw new ArgumentException(
                "Project ID is required.",
                nameof(projectId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Pipeline name is required.",
                nameof(name));

        Id = Guid.NewGuid();
        ProjectId = projectId;
        Name = name.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }
    public void AddStep(PipelineStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        if (_steps.Any(existingStep => existingStep.Order == step.Order))
            throw new InvalidOperationException(
                $"A step with order {step.Order} already exists.");

        step.SetPipelineId(Id);

        _steps.Add(step);
    }
}
