namespace PostHog.Library;

/// <summary>
/// Used to run tasks on a background thread in a way that is appropriate for the environment and testable.
/// </summary>
public interface ITaskScheduler
{
    /// <summary>
    /// Run the action on a background thread.
    /// </summary>
    /// <param name="action">The action to run in the background.</param>
    /// <param name="cancellationToken">The cancellation token that can be used to cancel the operation.</param>
    void Run(Action action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the task on a background thread.
    /// </summary>
    /// <param name="task">The task to run in the background.</param>
    /// <param name="cancellationToken">The cancellation token that can be used to cancel the operation.</param>
    /// <returns>The background <see cref="Task"/>.</returns>
    Task Run(Func<Task> task, CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="ITaskScheduler"/>
public class TaskRunTaskScheduler : ITaskScheduler
{
    /// <inheritdoc />
    public void Run(Action action, CancellationToken cancellationToken = default)
        => Task.Run(action, cancellationToken);

    /// <inheritdoc />
    public Task Run(Func<Task> task, CancellationToken cancellationToken = default)
        => Task.Run(task, cancellationToken);
}