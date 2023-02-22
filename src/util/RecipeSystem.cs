using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace lavoisier
{
    static class RecipeSystem
    {
        public static bool hasRegisteredRecipes = false;

        public static List<RetortRecipe> retortRecipes = new List<RetortRecipe>();

        public static Dictionary<string, RetortRecipe> retortRecipesDic = new Dictionary<string, RetortRecipe>();

        public static void LoadRetortRecipes(ICoreAPI api)
        {
            Dictionary<AssetLocation, JToken> files = api.Assets.GetMany<JToken>(api.Logger, "recipes/alembic/retort");
            int recipeQuantity = 0;

            foreach (var val in files)
            {
                if (val.Value is JObject)
                {
                    RetortRecipe rec = val.Value.ToObject<RetortRecipe>();
                    if (!rec.Enabled) continue;

                    //rec.Resolve(api.World, "mixing recipe " + val.Key);
                    //MixingRecipeRegistry.Registry.MixingRecipes.Add(rec);
                    retortRecipes.Add(rec);

                    recipeQuantity++;
                }
                if (val.Value is JArray)
                {
                    foreach (var token in (val.Value as JArray))
                    {
                        RetortRecipe rec = token.ToObject<RetortRecipe>();
                        if (!rec.Enabled) continue;

                        //rec.Resolve(api.World, "mixing recipe " + val.Key);
                        //MixingRecipeRegistry.Registry.MixingRecipes.Add(rec);
                        retortRecipes.Add(rec);

                        recipeQuantity++;
                    }
                }
            }

            api.World.Logger.Event("{0} retort recipes loaded", recipeQuantity);
            api.World.Logger.StoryEvent(Lang.Get("lavoisier:recipesloaded"));
        }

        public static void RegisterRetortRecipes(IWorldAccessor world)
        {
            foreach (RetortRecipe recipe in retortRecipes)
            {
                string key = "setup:";
                foreach (string s in recipe.setup)
                {
                    key += s + "+";
                }
                key += "ingredients:";
                bool hasAtLeastOneIngredient = false;
                if ((recipe.liquidInput?.Resolve(world, "Retort liquid input")) ?? false)
                {
                    key += recipe.liquidInput.ResolvedItemstack.Collectible.Code + "+";
                    hasAtLeastOneIngredient = true;
                }
                if ((recipe.solidInput?.Resolve(world, "Retort solid input")) ?? false)
                {
                    key += recipe.solidInput.ResolvedItemstack.Collectible.Code + "+";
                    hasAtLeastOneIngredient = true;
                }
                if ((recipe.endInput?.Resolve(world, "Retort additional input")) ?? false)
                {
                    key += recipe.endInput.ResolvedItemstack.Collectible.Code + "+";
                    hasAtLeastOneIngredient = true;
                }
                if ((recipe.product?.Resolve(world, "Retort product") ?? true)
                    /*&& (recipe.liquidByproduct?.Resolve(world, "Retort liquid byproduct") ?? true)
                    && (recipe.solidByproduct?.Resolve(world, "Retort solid byproduct") ?? true)*/)
                {
                    recipe.solidByproduct?.Resolve(world, "Retort solid byproduct");
                    recipe.liquidByproduct?.Resolve(world, "Retort liquid byproduct");
                    if (!retortRecipesDic.ContainsKey(key) && hasAtLeastOneIngredient) retortRecipesDic.Add(key, recipe);
                }
            }
            hasRegisteredRecipes = true;
        }

        public static RetortRecipe matchRecipeRetort(IWorldAccessor world, ItemStack liquidInput, ItemStack solidInput, string[] setup, IAlembicEndContainer container = null)
        {

            string recipeKey = "setup:";
            foreach (string s in setup)
            {
                recipeKey += s + "+";
            }
            recipeKey += "ingredients:";
            if (liquidInput != null)
            {
                recipeKey += liquidInput.Collectible.Code + "+";
            }
            if (solidInput != null)
            {
                recipeKey += solidInput.Collectible.Code + "+";
            }
            if (container != null)
            {
                recipeKey += container.getCustomItem() + "+"; // Used to handle the bubbler's content, for instance
            }

            if (retortRecipesDic.ContainsKey(recipeKey))
            {
                /*if (retortRecipesDic[recipeKey].product.Resolve(world, "Retort product"))
                {
                    return retortRecipesDic[recipeKey];
                }*/
                if (retortRecipesDic[recipeKey].product?.Resolve(world, "Retort product") ?? true)
                {
                    return retortRecipesDic[recipeKey];
                }
            }

            return null;
        }
    }

    public class RetortRecipe
    {
        public bool Enabled = true;
        public string code = "";
        public string[] setup;
        public int ticksPerItem = 1;
        public JsonItemStack liquidInput;
        public JsonItemStack solidInput;
        public JsonItemStack endInput;
        public JsonItemStack product;
        public JsonItemStack liquidByproduct;
        public JsonItemStack solidByproduct;
    }
}
