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
    class AlembicDissolverEntity : BlockEntityLiquidContainer, IAlembicEndContainer
    {
        public override string InventoryClassName => "retortneck";

        public ItemStack lastReceivedDistillate;
        public float timeSinceLastDistillate = 9999999999999f;

        //public override InventoryBase Inventory => inventory;

        MeshData currentMesh;
        AlembicDissolver ownBlock;

        Vec3f spoutpos = new Vec3f(0.5f, 0.375f, 0.4375f);

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            ownBlock = Block as AlembicDissolver;
            if (Api.Side == EnumAppSide.Client)
            {
                currentMesh = GenMesh();
                MarkDirty(true);
                RegisterGameTickListener(clientTick, 200, Api.World.Rand.Next(50));
            }

            Matrixf mat = new Matrixf();
            mat
                .Translate(0.5f, 0, 0.5f)
                .RotateYDeg(Block.Shape.rotateY)
                .Translate(-0.5f, 0, -0.5f)
            ;

            spoutpos = mat.TransformVector(new Vec4f(0.5f, 0.375f, 0.4375f, 1)).XYZ;
        }

        private void clientTick(float dt)
        {
            ItemStack bucketStack = inventory[0].Itemstack;
            int color = 0;
            if (lastReceivedDistillate != null) color = lastReceivedDistillate.Collectible.GetRandomColor(Api as ICoreClientAPI, bucketStack);

            MarkDirty(true);

            Api.World.SpawnParticles(1f, color, Pos.ToVec3d().Add(spoutpos), Pos.ToVec3d().Add(spoutpos), new Vec3f(), new Vec3f(), 0.08f, 1, 0.15f, EnumParticleModel.Quad);

        }

        public AlembicDissolverEntity()
        {
            inventory = new InventoryGeneric(2, null, null);

            inventory.SlotModified += inventory_SlotModified;
        }

        void inventory_SlotModified(int slotModified)
        {
            if (inventory[0].Empty)
            {
                if (inventory[1].Itemstack?.Collectible.IsLiquid() ?? false) {
                    inventory[1].TryPutInto(Api.World, inventory[0], quantity: 99999);
                }
            }
            MarkDirty(true);
        }
        
        public bool OnBlockInteractStart(IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot handslot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (handslot.Empty && !inventory[1].Empty)
            {
                Api.World.PlaySoundAt(new AssetLocation("game", "sounds/effect/squish1"), byPlayer.Entity, byPlayer, true, 16);

                if (!byPlayer.InventoryManager.TryGiveItemstack(inventory[1].Itemstack.Clone(), true))
                {
                    Api.World.SpawnItemEntity(inventory[1].Itemstack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }
                inventory[1].Itemstack = null;
                MarkDirty(true);
                handslot.MarkDirty();
                return true;
            }
            
            return false;
        }

        internal MeshData GenMesh()
        {
            if (ownBlock == null) return null;

            MeshData mesh = ownBlock.GenMesh(Api as ICoreClientAPI, GetContent(), Pos);

            if (mesh.CustomInts != null)
            {
                for (int i = 0; i < mesh.CustomInts.Count; i++)
                {
                    mesh.CustomInts.Values[i] |= 1 << 27; // Disable water wavy
                    mesh.CustomInts.Values[i] |= 1 << 26; // Enabled weak foam
                }
            }

            return mesh;
        }

        #region ITerrainMeshPool imp to get bucket mesh
        public void AddMeshData(MeshData data, int lodlevel = 1)
        {
            if (data == null) return;
            //bucketMeshTmp.AddMeshData(data);
        }

        public void AddMeshData(MeshData data, ColorMapData colormapdata, int lodlevel = 1)
        {
            if (data == null) return;
            //bucketMeshTmp.AddMeshData(data);
        }
        #endregion

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            mesher.AddMeshData(currentMesh);
            return true;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            if (Api?.Side == EnumAppSide.Client)
            {
                currentMesh = GenMesh();
                MarkDirty(true);
            }
             
            lastReceivedDistillate = (ItemStack)tree.GetItemstack("lastReceivedDistillate");
            lastReceivedDistillate?.ResolveBlockOrItem(worldForResolving);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetItemstack("lastReceivedDistillate", lastReceivedDistillate);
        }

        bool IAlembicEndContainer.handleRecipe(RetortRecipe recipe)
        {
            //if (!recipe.product.Resolve(Api.World, "Resolving retort neck product")) return false;

            AssetLocation sound = new AssetLocation("game", "sounds/environment/drip2");
            Api.World.PlaySoundAt(sound, Pos.X, Pos.Y, Pos.Z, randomizePitch: true);

            if (Api.Side != EnumAppSide.Server) { return false; }

            ItemStack fromStack = recipe.product.ResolvedItemstack.Clone();

            if (recipe.endInput.ResolvedItemstack.Collectible != inventory[0].Itemstack.Collectible) return false;

            ItemStack takenInput = inventory[0].TakeOut(recipe.endInput.ResolvedItemstack.Clone().StackSize);

            if (takenInput.StackSize == recipe.endInput.ResolvedItemstack.Clone().StackSize)
            {
                if (inventory[1].Empty)
                {
                    inventory[1].Itemstack = fromStack.Clone();
                }
                else if (inventory[1].Itemstack.Collectible == fromStack.Collectible)
                {
                    inventory[1].Itemstack.StackSize = Math.Min(inventory[1].Itemstack.Collectible.MaxStackSize, inventory[1].Itemstack.StackSize + fromStack.StackSize);
                    MarkDirty(true);
                }
                return true;
            }

            return false;
        }

        public string getCustomItem()
        {
            return inventory[0].Itemstack?.Collectible.Code.ToString() ?? "";
        }

        public void stopDistilling()
        {
            lastReceivedDistillate = null;
            MarkDirty(true);
        }

        public bool checkStoechiometry(RetortRecipe recipe)
        {
            if (recipe.endInput == null)
            {
                return inventory[0].Empty;
            }

            if (inventory[0].Empty) return false;

            if ((recipe.endInput.ResolvedItemstack.StackSize % inventory[0].Itemstack.StackSize) == 0)
            {
                return true;
            }

            return true;
        }
    }
}
