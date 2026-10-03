namespace Acorn.Core.Mapping;

/// <summary>Maps a data entity to its application model.</summary>
/// <typeparam name="TEntity">The source entity type.</typeparam>
/// <typeparam name="TModel">The destination model type.</typeparam>
internal interface IEntityModelMapper<TEntity, TModel>
{
  /// <summary>Maps an entity to its application model.</summary>
  /// <param name="entity">The source entity.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The mapped application model.</returns>
  Task<TModel> MapAsync(TEntity entity, CancellationToken cancellationToken = default);
}
