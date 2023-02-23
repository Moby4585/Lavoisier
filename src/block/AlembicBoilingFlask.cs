using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;

namespace lavoisier
{
    public class AlembicBoilingFlask : Vintagestory.GameContent.BlockLiquidContainerBase
    {

        //public override bool AllowHeldLiquidTransfer => true;

        //public AssetLocation bubbleSound = new AssetLocation("game", "effect/bubbling");

        public override float CapacityLitres => 3f;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        /*public override byte[] GetLightHsv(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
        {
            if (pos == null) return base.GetLightHsv(blockAccessor, pos, stack);

            AlembicBoilingFlaskEntity fe = blockAccessor.GetBlockEntity(pos) as AlembicBoilingFlaskEntity;
            if (fe != null) return ((!fe.Inventory[2].Empty) ? (new byte[] { 4, 2, 8 }) : base.GetLightHsv(blockAccessor, pos, stack));

            return base.GetLightHsv(blockAccessor, pos, stack);
        }*/

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            AlembicBoilingFlaskEntity be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as AlembicBoilingFlaskEntity;
            if (be?.isReacting ?? false) return be.OnInteract(world, byPlayer, blockSel, true);

            if (base.OnBlockInteractStart(world, byPlayer, blockSel)) return true;

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
                //if (!be.Inventory[0].Empty) info += be.Inventory[0].Itemstack.Collectible.Code;
                if (!be.Inventory[2].Empty)
                {
                    info += ("\nHas oil lamp");
                    //info += "\n" + be.Inventory[2].Itemstack.Collectible.Code.ToString();
                }

                if (be.isReacting)
                {
                    info += "\n\nReacting! Do not disturb!";
                    info += "\n" + ((int)((float)be.amountReacted / (float)be.amountToReact * 100f)).ToString() + "% reacted";
                }

                string[] setup = be.GetApparatusComposition().ToArray<string>();
                RetortRecipe rec;
                if ((rec = RecipeSystem.matchRecipeRetort(world, (be.Inventory[0]?.Itemstack) ?? null, (be.Inventory[1]?.Itemstack) ?? null, setup, be.alembicEndContainer ?? null)) != null) {
                    info += be.isReacting ? "\n\nCreating: " : "\n\nWill create: ";
                    info += Lang.Get("{0}", rec.product?.ResolvedItemstack.GetName() ?? "Byproducts");

                    if (be.Inventory[2].Empty) info += "\nAdd an oil lamp to " + (be.isReacting ? "resume" : "start") + " reacting";
                }

                /*AlembicRetortNeckEntity rtnEntity;
                if ((rtnEntity = be.alembicEndContainer as AlembicRetortNeckEntity) != null)
                {
                    info += "\n" + ((rtnEntity.Inventory[0].Itemstack?.Collectible as BlockLiquidContainerBase)?.GetContent(rtnEntity.Inventory[0].Itemstack)?.Collectible.Code.ToString()) ?? "";
                }
                info += "\nisReacting: " + be.isReacting.ToString() + " amountReacted: " + be.amountReacted.ToString() + " amountToReact: " + be.amountToReact.ToString();
                if (be.reactingRecipe != null) {
                    info += "\n\nLiquidInput: " + be.reactingRecipe.liquidInput?.ResolvedItemstack?.StackSize;
                    info += "\nSolidInput: " + be.reactingRecipe.solidInput?.ResolvedItemstack?.StackSize;
                    info += "\nOutput: " + be.reactingRecipe.product?.ResolvedItemstack?.StackSize;
                }*/
            }

            return info;
        }
    }
}
