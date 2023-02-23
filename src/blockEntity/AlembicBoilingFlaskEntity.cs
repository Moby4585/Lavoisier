using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
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

        public bool isReacting = false;
        public RetortRecipe reactingRecipe;
        public int amountToReact = -1;
        public int amountReacted = 0;
        public int ticksSinceLastReacted = 0;

        public IAlembicEndContainer alembicEndContainer;

        public AssetLocation bubbleSound = new AssetLocation("lavoisier", "sounds/effect/bubbling");
        public AssetLocation tickSound = new AssetLocation("game", "tick");

        public List<string> apparatusComposition = new List<string>();

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
            if (Api.Side != EnumAppSide.Server) return;

            if (!isReacting) // Check recipes, start reaction
            {
                apparatusComposition = GetApparatusComposition(); // Used to set alembicEndContainer beforehand maybe? Apparently it's needed I guess?

                RetortRecipe recipe = RecipeSystem.matchRecipeRetort(Api.World, inventory[0].Itemstack, inventory[1].Itemstack, GetApparatusComposition().ToArray(), alembicEndContainer);

                if (recipe != null)
                {
                    // Checking stoechiometry
                    bool isStoechiometric = true;
                    if (!inventory[0].Empty)
                    {
                        isStoechiometric &= inventory[0].Itemstack.StackSize % recipe.liquidInput.ResolvedItemstack.StackSize == 0;
                        amountToReact = (int)(inventory[0].Itemstack.StackSize / recipe.liquidInput.ResolvedItemstack.StackSize);
                    }
                    if (!inventory[1].Empty)
                    {
                        
                        if (amountToReact != -1)
                        {
                            isStoechiometric &= inventory[1].Itemstack.StackSize % recipe.solidInput.ResolvedItemstack.StackSize == 0
                                && inventory[1].Itemstack.StackSize / recipe.solidInput.ResolvedItemstack.StackSize == amountToReact;
                        }
                        else
                        {
                            isStoechiometric &= inventory[1].Itemstack.StackSize % recipe.solidInput.ResolvedItemstack.StackSize == 0;
                            amountToReact = inventory[1].Itemstack.StackSize / recipe.solidInput.ResolvedItemstack.StackSize;
                        }
                    }
                    isStoechiometric &= alembicEndContainer?.checkStoechiometry(recipe) ?? true;

                    if (isStoechiometric && !inventory[2].Empty)
                    {
                        isReacting = true;
                        reactingRecipe = recipe; 
                    }
                }
            }
            else if (!inventory[2].Empty)// Handle reaction, only with oil lamp present
            {
                MarkDirty(true);

                if (Api.World.Rand.NextDouble() <= 0.5f)
                {
                    Api.World.PlaySoundAt(bubbleSound, Pos.X, Pos.Y, Pos.Z, randomizePitch: true, range: 16, volume: 0.5f);
                }

                RetortRecipe recipe = RecipeSystem.matchRecipeRetort(Api.World, inventory[0].Itemstack, inventory[1].Itemstack, GetApparatusComposition().ToArray(), alembicEndContainer);

                if (reactingRecipe == null)
                {
                    reactingRecipe = recipe;
                }

                if (recipe != reactingRecipe)
                {
                    isReacting = false;
                    alembicEndContainer?.stopDistilling();
                    commitRecipe();
                    return;
                }

                if (ticksSinceLastReacted < reactingRecipe.ticksPerItem)
                {
                    ticksSinceLastReacted++;
                }
                else
                {
                    ticksSinceLastReacted = 0;

                    amountReacted++;

                    bool shouldStillReact = true;
                    if (!inventory[0].Empty)
                    {
                        shouldStillReact &= (inventory[0].TakeOut(reactingRecipe.liquidInput.ResolvedItemstack.StackSize)?.StackSize ?? -1) > 0;
                        
                    }
                    if (!inventory[1].Empty)
                    {
                        shouldStillReact &= (inventory[1].TakeOut(reactingRecipe.solidInput.ResolvedItemstack.StackSize)?.StackSize ?? -1) > 0;
                    }

                    MarkDirty();
                    //shouldStillReact = amountReacted >= amountToReact;

                    if (!shouldStillReact) // Gérer la fin de la réaction : byproducts, reset des variables
                    {
                        commitRecipe();
                    }
                    else
                    {

                        if (alembicEndContainer != null) // Handle recipe with end container
                        {
                            alembicEndContainer.handleRecipe(reactingRecipe);
                        }
                        else // Handle self-contained recipe (only makes byproducts)
                        {

                        }
                    }
                }
            }
        }

        bool commitRecipe ()
        {
            ticksSinceLastReacted = 0;
            isReacting = false;


            if (reactingRecipe?.liquidByproduct != null)
            {
                ItemStack liquidByproduct = reactingRecipe.liquidByproduct.ResolvedItemstack.Clone();
                BlockLiquidContainerBase blcb = Block as BlockLiquidContainerBase;
                float itemsPerLitre = (Block as BlockLiquidContainerBase).GetContentProps(reactingRecipe.liquidByproduct.ResolvedItemstack)?.ItemsPerLitre ?? 100f;
                int maxCapacity = (int)((Block as BlockLiquidContainerBase).CapacityLitres * itemsPerLitre);
                liquidByproduct.StackSize = Math.Min(liquidByproduct.StackSize * amountReacted, maxCapacity);
                Inventory[0].Itemstack = liquidByproduct;
            }
            else
            {
                Inventory[0].Itemstack = null;
            }
            if (reactingRecipe?.solidByproduct != null)
            {
                ItemStack solidByproduct = reactingRecipe.solidByproduct.ResolvedItemstack.Clone();
                solidByproduct.StackSize = Math.Min(solidByproduct.StackSize * amountReacted, solidByproduct.Collectible.MaxStackSize);
                Inventory[1].Itemstack = solidByproduct;
            }
            else
            {
                Inventory[1].Itemstack = null;
            }

            amountToReact = -1;
            amountReacted = 0;
            reactingRecipe = null;
            MarkDirty(true);
            inventory.MarkSlotDirty(1);
            inventory.MarkSlotDirty(0);
            return true;
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

        public bool OnInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, bool onlyOilLamp = false)
        {
            ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

            hasOilLamp = (!inventory[2].Empty);
            
            if (hotbarSlot.Empty)
            {
                if (this.hasOilLamp)
                {
                    inventory[2].TryPutInto(Api.World, byPlayer.InventoryManager.ActiveHotbarSlot, 1);
                    (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

                    AssetLocation sound = inventory[2].Itemstack?.Block?.Sounds?.Place;
                    Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("game", "sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);

                }
                else if (!inventory[1].Empty)
                {
                    inventory[1].TryPutInto(Api.World, byPlayer.InventoryManager.ActiveHotbarSlot, inventory[1].Itemstack.StackSize);
                    Api.World.PlaySoundAt(new AssetLocation("game", "sounds/effect/squish1"), byPlayer.Entity, byPlayer, true, 16);
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

                            AssetLocation sound = hotbarSlot.Itemstack?.Block?.Sounds?.Place;
                            Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                        }

                        return true;
                    }
                }
            }

            if (onlyOilLamp) return true;

            if (!hotbarSlot.Empty && !(hotbarSlot.Itemstack.Collectible is BlockLiquidContainerTopOpened) && !hotbarSlot.Itemstack.Collectible.Code.Path.StartsWith("alembic"))
            {
                byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(Api.World, inventory[1], 1);
                Api.World.PlaySoundAt(new AssetLocation("game", "sounds/effect/squish1"), Pos.X, Pos.Y, Pos.Z, byPlayer, true, 16);
                return true;
            }
            return false;
        }

        public List<string> GetApparatusComposition()
        {
            

            List<string> apparatusComposition = new List<string>();
            apparatusComposition.Add(this.Block.Code.Path);

            // Check blocks around boiler
            BlockPos lastBlock = null;
            BlockPos lastValidBlock = null;
            if ((lastBlock = getNextComponent("north", this.Pos)) != null)
            {
                apparatusComposition.Add(Api.World.BlockAccessor.GetBlock(lastBlock).CodeWithoutParts(1));
                lastValidBlock = lastBlock;
            }
            else if ((lastBlock = getNextComponent("east", this.Pos)) != null)
            {
                apparatusComposition.Add(Api.World.BlockAccessor.GetBlock(lastBlock).CodeWithoutParts(1));
                lastValidBlock = lastBlock;
            }
            else if ((lastBlock = getNextComponent("south", this.Pos)) != null)
            {
                apparatusComposition.Add(Api.World.BlockAccessor.GetBlock(lastBlock).CodeWithoutParts(1));
                lastValidBlock = lastBlock;
            }
            else if ((lastBlock = getNextComponent("west", this.Pos)) != null)
            {
                apparatusComposition.Add(Api.World.BlockAccessor.GetBlock(lastBlock).CodeWithoutParts(1));
                lastValidBlock = lastBlock;
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
                    lastValidBlock = lastBlock;
                }
                else
                {
                    checkDone = true;
                }
            }
            if (lastValidBlock != null)
            {
                IAlembicEndContainer IAlembic;
                if ((IAlembic = Api.World.BlockAccessor.GetBlockEntity(lastValidBlock) as IAlembicEndContainer) != null) alembicEndContainer = IAlembic;
                else alembicEndContainer = null;
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

            isReacting = tree.TryGetBool("isReacting") ?? false;
            amountReacted = tree.TryGetInt("amountReacted") ?? 0;
            amountToReact = tree.TryGetInt("amountToReact") ?? -1;

            //reactingRecipe = JsonUtil.FromString<RetortRecipe>(tree.GetString("reactingRecipe", ""));

            base.FromTreeAttributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetBool("isReacting", isReacting);
            tree.SetInt("amountReacted", amountReacted);
            tree.SetInt("amountToReact", amountToReact);
            //tree.SetString("reactingRecipe", JsonUtil.ToString(reactingRecipe));

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
