using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace lavoisier
{
    public class AlembicBoilingFlask : Vintagestory.GameContent.BlockLiquidContainerBase
    {

        //public override bool AllowHeldLiquidTransfer => true;

        //public AssetLocation tickSound = new AssetLocation("game", "tick");

        public override float CapacityLitres => 3f;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            
        }

        public override byte[] GetLightHsv(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
        {
            if (pos == null) return base.GetLightHsv(blockAccessor, pos, stack);

            AlembicBoilingFlaskEntity fe = blockAccessor.GetBlockEntity(pos) as AlembicBoilingFlaskEntity;
            if (fe != null) return ((!fe.Inventory[2].Empty) ? (new byte[] { 4, 2, 8 }) : base.GetLightHsv(blockAccessor, pos, stack));

            return base.GetLightHsv(blockAccessor, pos, stack);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (base.OnBlockInteractStart(world, byPlayer, blockSel)) return true;

            AlembicBoilingFlaskEntity be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as AlembicBoilingFlaskEntity;
            if (be != null)
            {
                bool handled = be.OnInteract(world, byPlayer, blockSel);                
                if (handled) return true;
            }
            
            return false;
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            return new ItemStack(this, 1);
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            string info = base.GetPlacedBlockInfo(world, pos, forPlayer);

            AlembicBoilingFlaskEntity be = world.BlockAccessor.GetBlockEntity(pos) as AlembicBoilingFlaskEntity;

            if (be != null)
            {
                //ItemSlot slot = be.Inventory[1];
                if (be.Inventory[1].Empty)
                {
                    //info += "\n" + Lang.Get("No solid");
                }
                else
                {
                    if (be.Inventory[0].Empty)
                    {
                        info = "Contents:";
                    }
                    info += "\n" + Lang.Get("{0}x{1}", be.Inventory[1].Itemstack.StackSize, be.Inventory[1].Itemstack.GetName());
                }
                if (!be.Inventory[2].Empty)
                {
                    info += ("\n\nHas oil lamp");
                    //info += "\n" + be.Inventory[2].Itemstack.Collectible.Code.ToString();
                }
                info += "\n\nWill create: ";
                string[] setup = be.GetApparatusComposition().ToArray<string>();
                info += Lang.Get("{0}", (RecipeSystem.matchRecipe(world, be.Inventory[0].Itemstack, be.Inventory[1].Itemstack, setup)?.product.ResolvedItemstack.GetName()) ?? "null");

                /*info += "\n\nSetup:\n";
                foreach (string s in be.GetApparatusComposition())
                {
                    info += s + "\n";
                }*/
                RetortRecipe recipe = RecipeSystem.retortRecipes[0];
                string key = "setup:";
                foreach (string s in recipe.setup)
                {
                    key += s + "+";
                }
                key += ";ingredients:";
                if ((recipe.liquidInput?.Resolve(world, "Retort liquid input")) ?? false)
                {
                    key += recipe.liquidInput.ResolvedItemstack.Collectible.Code + "+";
                }
                if ((recipe.solidInput?.Resolve(world, "Retort solid input")) ?? false)
                {
                    key += recipe.solidInput.ResolvedItemstack.GetName();
                }
                info += "\n" + key;
                //if (!retortRecipesDic.ContainsKey(key)) retortRecipesDic.Add(key, recipe);
            }

            return info;
        }
    }
}
