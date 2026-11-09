namespace DeployForge.Domain.Entities;

public sealed class PipelineStep
{
    public Guid Id { get; private set; }

    public Guid PipelineId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int Order { get; private set; }

    public string Command { get; private set; } = string.Empty;

    private PipelineStep() { }

    public PipelineStep(
        string name,
        int order,
        string command)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Step name is required.",
                nameof(name));

        if (order < 1)
            throw new ArgumentException(
                "Step order must be greater than zero.",
                nameof(order));

        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException(
                "Step command is required.",
                nameof(command));

        Id = Guid.NewGuid();
        Name = name.Trim();
        Order = order;
        Command = command.Trim();
    }

    internal void SetPipelineId(Guid pipelineId)
    {
        if (pipelineId == Guid.Empty)
            throw new ArgumentException(
                "Pipeline ID is required.",
                nameof(pipelineId));

        PipelineId = pipelineId;
    }
}
