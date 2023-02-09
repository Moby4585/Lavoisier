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
                    key += recipe.solidInput.ResolvedItemstack.Collectible.Code;
                    hasAtLeastOneIngredient = true;
                }
                if (!retortRecipesDic.ContainsKey(key) && hasAtLeastOneIngredient) retortRecipesDic.Add(key, recipe);
            }
            hasRegisteredRecipes = true;
        }

        public static RetortRecipe matchRecipeRetort(IWorldAccessor world, ItemStack liquidInput, ItemStack solidInput, string[] setup)
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
                recipeKey += solidInput.Collectible.Code;
            }

            if (retortRecipesDic.ContainsKey(recipeKey))
            {
                if (retortRecipesDic[recipeKey].product.Resolve(world, "Retort product"))
                {
                    return retortRecipesDic[recipeKey];
                }
            }

            #region oldcode
            /*
            foreach (RetortRecipe recipe in retortRecipes)
            {
                if (!(recipe.product?.Resolve(world, "Retort recipe product")) ?? true) return null;



                #region oldcode
                /*bool isLiquidInputOK = true;
                bool isSolidInputOK = true;

                if (recipe.liquidInput != null)
                {
                    if (liquidInput == null) isLiquidInputOK = false;
                    else
                    {
                        if (recipe.liquidInput.Resolve(world, "Retort liquid input resolve"))
                        {
                            if (liquidInput.Collectible != recipe.liquidInput.ResolvedItemstack.Collectible 
                                || liquidInput.StackSize % recipe.liquidInput.ResolvedItemstack.StackSize != 0) isLiquidInputOK = false;
                        }
                    }
                }
                else
                {
                    if (liquidInput != null) isLiquidInputOK = false;
                }

                if (recipe.solidInput != null)
                {
                    if (solidInput == null) isSolidInputOK = false;
                    else
                    {
                        if (recipe.solidInput.Resolve(world, "Retort liquid input resolve"))
                        {
                            if (solidInput.Collectible != recipe.solidInput.ResolvedItemstack.Collectible 
                                || solidInput.StackSize % recipe.solidInput.ResolvedItemstack.StackSize != 0) isSolidInputOK = false;
                        }
                    }
                }
                else
                {
                    if (solidInput != null) isSolidInputOK = false;
                }

                if (liquidInput != null || (recipe.liquidInput?.Resolve(world, "Retort liquid input resolve") ?? false))
                {
                    if (!(liquidInput?.Collectible == recipe.liquidInput.ResolvedItemstack?.Collectible && liquidInput.StackSize % recipe.liquidInput.ResolvedItemstack?.StackSize == 0))
                    {
                        isLiquidInputOK = false;
                    }
                }

                if (solidInput != null || (recipe.solidInput?.Resolve(world, "Retort solid input resolve") ?? false))
                {
                    if (!(solidInput?.Collectible == recipe.solidInput.ResolvedItemstack?.Collectible && solidInput.StackSize % recipe.solidInput.ResolvedItemstack?.StackSize == 0))
                    {
                        isSolidInputOK = false;
                    }
                }*/

            /*if (liquidInput?.Collectible.Code == ((recipe.liquidInput.Resolve(world, "Retort liquid input resolve")) ? recipe.liquidInput.ResolvedItemstack.Collectible.Code : null)
                && solidInput?.Collectible.Code == ((recipe.solidInput.Resolve(world, "Retort liquid input resolve")) ? recipe.solidInput.ResolvedItemstack.Collectible.Code : null))
            {
                return recipe;
            }
            #endregion
            /*if (isLiquidInputOK && isSolidInputOK)
            {
                return recipe;
            }
        }
        //return retortRecipes[0];
        */
            #endregion
            return null;
        }
    }

    class RetortRecipe
    {
        public bool Enabled = true;
        public string code = "";
        public string[] setup;
        public float secondsPerItem = 0.1f;
        public JsonItemStack liquidInput;
        public JsonItemStack solidInput;
        public JsonItemStack product;
        public JsonItemStack liquidByproduct;
        public JsonItemStack solidByproduct;
    }
}
