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
    class AlembicRetortNeck : Vintagestory.GameContent.BlockContainer
    {
        //public override float CapacityLitres => 1f;

        public static AssetLocation blockSelfNorth = new AssetLocation("lavoisier:alembicretortneck-north");

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (base.OnBlockInteractStart(world, byPlayer, blockSel)) return true;

            AlembicRetortNeckEntity be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as AlembicRetortNeckEntity;
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
            string info = "Empty";// base.GetPlacedBlockInfo(world, pos, forPlayer);

            AlembicRetortNeckEntity be = world.BlockAccessor.GetBlockEntity(pos) as AlembicRetortNeckEntity;

            if (be != null)
            {
                if (!be.Inventory[0].Empty)
                {
                    ItemStack bucketStack = be.Inventory[0].Itemstack;

                    info = "Container: ";
                    if ((bucketStack.Collectible as BlockLiquidContainerTopOpened).GetContent(bucketStack) != null)
                    {
                        info += Lang.Get("{0} litres of {1}", (bucketStack.Collectible as BlockLiquidContainerTopOpened).GetCurrentLitres(bucketStack), (bucketStack.Collectible as BlockLiquidContainerTopOpened).GetContent(bucketStack).GetName());
                    }
                    else
                    {
                        info += "Empty";
                    }
                    //info += "\n" + be.lastReceivedDistillate?.Collectible.Code.ToString() ?? "no lrd";
                }
            }

            return info;
        }
    }
}
