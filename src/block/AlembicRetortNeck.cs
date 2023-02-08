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
    class AlembicRetortNeck : Vintagestory.GameContent.BlockLiquidContainerBase
    {
        public override float CapacityLitres => 1f;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (base.OnBlockInteractStart(world, byPlayer, blockSel)) return true;

            /*AlembicBoilingFlaskEntity be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as AlembicBoilingFlaskEntity;
            if (be != null)
            {
                bool handled = be.OnInteract(world, byPlayer, blockSel);
                if (handled) return true;
            }*/

            return false;
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            return new ItemStack(this, 1);
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            string info = base.GetPlacedBlockInfo(world, pos, forPlayer);

            return info;
        }
    }
}
