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
    class AlembicBoilingFlaskEntity : BlockEntityLiquidContainer
    {
        public override string InventoryClassName => "boiler";

        public bool hasOilLamp;

        public static AssetLocation oilLampBlock = new AssetLocation("oillamp-up");
        MeshData oilLampMesh;

        // Inventory : 0 - liquide, 1 - solide, 2 - lampe

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            loadMesh();

            RegisterGameTickListener(OnGameTick, 50);
        }

        public AlembicBoilingFlaskEntity()
        {
            inventory = new InventoryGeneric(3, null, null);

            inventory.SlotModified += inventory_SlotModified;
        }

        public void OnGameTick(float dt)
        {
            
        }

        void inventory_SlotModified(int slotId)
        {
            if (slotId == 2)
            {
                if (Api?.Side == EnumAppSide.Client)
                {
                    loadMesh();
                }
                MarkDirty(true);
            }
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            dsc.AppendLine("Has oil lamp: ");
            dsc.Append((!inventory[2].Empty).ToString());
            dsc.Append("\n");

            ItemSlot slot = inventory[0];

            if (slot.Empty)
            {
                dsc.AppendLine(Lang.Get("Empty"));
            }
            else
            {
                dsc.AppendLine(Lang.Get("Contents: {0}x{1}", slot.Itemstack.StackSize, slot.Itemstack.GetName()));
            }

            

            base.GetBlockInfo(forPlayer, dsc);
        }

        public bool OnInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

            hasOilLamp = (!inventory[2].Empty);
            
            if (hotbarSlot.Empty)
            {
                if (this.hasOilLamp)
                {
                    inventory[2].TryPutInto(Api.World, byPlayer.InventoryManager.ActiveHotbarSlot, 1);
                    (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                    
                }
                else if (!inventory[1].Empty)
                {
                    inventory[1].TryPutInto(Api.World, byPlayer.InventoryManager.ActiveHotbarSlot, inventory[1].Itemstack.StackSize);
                }
                return true;
            }
            else if (hotbarSlot.Itemstack.Block != null)
            {
                if (hotbarSlot.Itemstack.Block.Code.BeginsWith("game", "oillamp"))
                {
                    if (!this.hasOilLamp)
                    {
                        if (byPlayer.InventoryManager.ActiveHotbarSlot.CanTakeFrom(hotbarSlot))
                        {
                            byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(Api.World, inventory[2], 1);
                            (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                            
                        }

                        return true;
                    }
                }
            }
            if (!hotbarSlot.Empty && !(hotbarSlot.Itemstack.Collectible is BlockLiquidContainerTopOpened) && !hotbarSlot.Itemstack.Collectible.Code.Path.StartsWith("alembic"))
            {
                byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(Api.World, inventory[1], 1);
                return true;
            }
            return false;
        }

        /*private void TryPut(ItemSlot fromSlot, ItemSlot intoSlot)
        {
            if (fromSlot.TryPutInto(Api.World, intoSlot, 1) > 0)
            {
                fromSlot.MarkDirty();
                MarkDirty(true);
            }
        }*/

        public List<string> GetApparatusComposition()
        {
            List<string> apparatusComposition = new List<string>();
            apparatusComposition.Add(this.Block.Code.Path);

            // Check blocks around boiler
            BlockPos lastBlock = null;
            if ((lastBlock = getNextComponent("north", this.Pos)) != null)
            {
                apparatusComposition.Add(Api.World.BlockAccessor.GetBlock(lastBlock).CodeWithoutParts(1));
            }
            else if ((lastBlock = getNextComponent("east", this.Pos)) != null)
            {
                apparatusComposition.Add(Api.World.BlockAccessor.GetBlock(lastBlock).CodeWithoutParts(1));
            }
            else if ((lastBlock = getNextComponent("south", this.Pos)) != null)
            {
                apparatusComposition.Add(Api.World.BlockAccessor.GetBlock(lastBlock).CodeWithoutParts(1));
            }
            else if ((lastBlock = getNextComponent("west", this.Pos)) != null)
            {
                apparatusComposition.Add(Api.World.BlockAccessor.GetBlock(lastBlock).CodeWithoutParts(1));
            }
            
            if (lastBlock == null)
            {
                return apparatusComposition;
            }

            bool checkDone = false;
            while (!checkDone)
            {
                lastBlock = getNextComponent(Api.World.BlockAccessor.GetBlock(lastBlock).Code.Path.Split('-').Last<string>(), (BlockPos)lastBlock);
                if (lastBlock != null)
                {
                    apparatusComposition.Add(Api.World.BlockAccessor.GetBlock(lastBlock).CodeWithoutParts(1));
                }
                else
                {
                    checkDone = true;
                }
            }
            return apparatusComposition;
        }

        public BlockPos getNextComponent(string facing, BlockPos currentPos)
        {
            BlockFacing side = BlockFacing.FromCode(facing).Opposite;
            BlockPos newPosSide = currentPos.AddCopy(side);
            BlockPos newPosTop = currentPos.AddCopy(0, 1, 0);
            Block block;
            if ((block = Api.World.BlockAccessor.GetBlock(newPosTop)).Code.Path.StartsWith("alembic"))
            {
                //return Api.World.BlockAccessor.GetBlock(newPos).CodeWithoutParts(1);
                if (block.CodeWithoutParts(1).EndsWith("vertical"))
                {
                    //return Api.World.BlockAccessor.GetBlock(newPosTop);
                    return newPosTop;
                }
            }
            if ((block = Api.World.BlockAccessor.GetBlock(newPosSide)).Code.Path.StartsWith("alembic"))
            {
                if (block.Code.Path.EndsWith("-" + facing) && !block.CodeWithoutParts(1).EndsWith("vertical"))
                {
                    //return Api.World.BlockAccessor.GetBlock(newPosSide);
                    return newPosSide;
                }
            }
            return null;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            if (Api != null) loadMesh();

            base.FromTreeAttributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
        }

        private void loadMesh()
        {
            if (Api.Side == EnumAppSide.Server) return;
            if (inventory[2].Empty)
            {
                oilLampMesh = null;
                return;
            }
            Block block = Api.World.GetBlock(oilLampBlock);
            ICoreClientAPI capi = Api as ICoreClientAPI;
            oilLampMesh = capi.TesselatorManager.GetDefaultBlockMesh(block);
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            loadMesh();
            
            mesher.AddMeshData(oilLampMesh);

            return base.OnTesselation(mesher, tessThreadTesselator);
        }
    }
}
