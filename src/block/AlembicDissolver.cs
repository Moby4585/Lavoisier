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
    public class AlembicDissolver : Vintagestory.GameContent.BlockLiquidContainerTopOpened
    {

        public static AssetLocation blockSelfNorth = new AssetLocation("lavoisier:alembicdissolver-north");

        public override bool AllowHeldLiquidTransfer => false;

        public override bool CanDrinkFrom => false;

        //public AssetLocation bubbleSound = new AssetLocation("game", "effect/bubbling");

        public override float CapacityLitres => 3f;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            AlembicDissolverEntity be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as AlembicDissolverEntity;

            if (base.OnBlockInteractStart(world, byPlayer, blockSel)) return true;

            if (be != null)
            {
                bool handled = be.OnBlockInteractStart(byPlayer, blockSel);                
                if (handled) return true;
            }
            
            return false;
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            return new ItemStack(world.GetBlock(blockSelfNorth), 1);
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            string info = base.GetPlacedBlockInfo(world, pos, forPlayer);

            AlembicDissolverEntity be = world.BlockAccessor.GetBlockEntity(pos) as AlembicDissolverEntity;

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
                    if (be.Inventory[1].Itemstack.Collectible.IsLiquid())
                    {
                        info += "\n" + Lang.Get("{0} litres of {1}", ((float)be.Inventory[1].Itemstack.StackSize) / GetContainableProps(be.Inventory[1].Itemstack).ItemsPerLitre, be.Inventory[1].Itemstack.GetName());
                    }
                    else
                    {
                        info += "\n" + Lang.Get("{0}x{1}", be.Inventory[1].Itemstack.StackSize, be.Inventory[1].Itemstack.GetName());
                    }
                }
            }

            return info;
        }
    }
}
