using FileEntity = Acorn.Core.Data.Entities.File;

namespace Acorn.Core.FileManagement.Upload;

internal sealed class FileRecipeRegistry
{
  private readonly IReadOnlyDictionary<string, IFileRecipe> _recipes;

  public FileRecipeRegistry(IEnumerable<IFileRecipe> recipes)
  {
    var recipeList = recipes.ToArray();
    var duplicateName = recipeList
      .GroupBy(recipe => recipe.Name, StringComparer.Ordinal)
      .FirstOrDefault(group => group.Count() > 1);

    if (duplicateName is not null)
      throw new InvalidOperationException($"Recipe name '{duplicateName.Key}' is registered more than once.");

    _recipes = recipeList.ToDictionary(recipe => recipe.Name, StringComparer.Ordinal);
  }

  public IReadOnlyList<IFileRecipe> GetApplicableRecipes(FileEntity parent)
    => _recipes.Values.Where(recipe => recipe.CanApply(parent)).ToArray();

  public IFileRecipe GetRequired(string name)
  {
    if (!_recipes.TryGetValue(name, out var recipe))
      throw new KeyNotFoundException($"No file recipe named '{name}' is registered.");

    return recipe;
  }
}
