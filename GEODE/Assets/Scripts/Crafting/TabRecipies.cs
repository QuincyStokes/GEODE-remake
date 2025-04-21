using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="NewTabRecipies", menuName = "ScriptableObject/TabRecipes")]
public class TabRecipies : ScriptableObject
{
   public List<CraftingRecipe> recipies;
}
