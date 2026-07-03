namespace LooksRatingApi.Infrastructure.DeployMigrations
{
    public interface IDeployMigration
    {
        string Name { get; }

        /// <returns>true when migration finished and should be recorded in history.</returns>
        Task<bool> ApplyAsync(CancellationToken cancellationToken = default);
    }
}
