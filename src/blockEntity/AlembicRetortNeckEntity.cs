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
    class AlembicRetortNeckEntity : BlockEntityContainer, IAlembicEndContainer
    {
        public override string InventoryClassName => "retortneck";

        public ItemStack lastReceivedDistillate;
        public float timeSinceLastDistillate = 9999999999999f;

        public InventoryGeneric inventory;
        public override InventoryBase Inventory => inventory;

        MeshData currentMesh;
        AlembicRetortNeck ownBlock;

        MeshData bucketMesh;
        MeshData bucketMeshTmp;

        Vec3f spoutpos = new Vec3f(0.5f, 0.375f, 0.4375f);

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            ownBlock = Block as AlembicRetortNeck;
            if (Api.Side == EnumAppSide.Client)
            {
                currentMesh = GenMesh();
                MarkDirty(true);
                RegisterGameTickListener(clientTick, 200, Api.World.Rand.Next(50));

                if (!inventory[0].Empty && bucketMesh == null) genBucketMesh();
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

            if (inventory[0].Empty) // Ideally to be removed ; patches a bug where bowls would keep being displayed after being removed
            {
                bucketMesh?.Clear();
            }
            MarkDirty(true);

            Api.World.SpawnParticles(1f, color, Pos.ToVec3d().Add(spoutpos), Pos.ToVec3d().Add(spoutpos), new Vec3f(), new Vec3f(), 0.08f, 1, 0.15f, EnumParticleModel.Quad);

            /*long msPassed = Api.World.ElapsedMilliseconds - lastReceivedDistillateTotalMs;
            if (msPassed < 10000)
            {
                int color = lastReceivedDistillate.Collectible.GetRandomColor(Api as ICoreClientAPI, lastReceivedDistillate);
                var droppos = Pos.ToVec3d().Add(spoutPos);

                if (!inventory[0].Empty)
                {
                    Api.World.SpawnParticles(0.5f, ColorUtil.ToRgba(64, 255, 255, 255), steamposmin, steamposmax, new Vec3f(-0.1f, 0.2f, -0.1f), new Vec3f(0.1f, 0.3f, 0.1f), 1.5f, 0, 0.25f, EnumParticleModel.Quad);
                    Api.World.SpawnParticles(1f, color, droppos, droppos, new Vec3f(), new Vec3f(), 0.08f, 1, 0.15f, EnumParticleModel.Quad);
                }
                else
                {
                    Api.World.SpawnParticles(0.33f, color, droppos, droppos, new Vec3f(), new Vec3f(), 0.08f, 1, 0.15f, EnumParticleModel.Quad);
                }
            }*/
        }

        public AlembicRetortNeckEntity()
        {
            inventory = new InventoryGeneric(1, null, null);

            inventory.SlotModified += inventory_SlotModified;
        }

        void inventory_SlotModified(int slotModified)
        {
            genBucketMesh();
            if (inventory[0].Empty)
            {
                bucketMesh?.Clear();
            }
            MarkDirty(true);
        }

        protected virtual ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
        {
            return null;
        }
        
        public bool OnBlockInteractStart(IPlayer byPlayer, BlockSelection blockSel)
        {

            if (blockSel.SelectionBoxIndex < 2)
            {
                ItemSlot handslot = byPlayer.InventoryManager.ActiveHotbarSlot;
                ItemStack handStack = handslot.Itemstack;

                if (handslot.Empty && !inventory[0].Empty)
                {
                    AssetLocation sound = inventory[0].Itemstack?.Block?.Sounds?.Place;
                    Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);

                    if (!byPlayer.InventoryManager.TryGiveItemstack(inventory[0].Itemstack.Clone(), true))
                    {
                        Api.World.SpawnItemEntity(inventory[1].Itemstack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                    }
                    inventory[0].Itemstack = null;
                    bucketMesh?.Clear();
                    MarkDirty(true);
                    handslot.MarkDirty();
                    return true;
                }

                else if (handStack != null && handStack.Collectible is BlockLiquidContainerTopOpened blockLiqCont && blockLiqCont.CapacityLitres >= 0.1f && blockLiqCont.CapacityLitres <= 1 && inventory[0].Empty)
                {
                    bool moved = handslot.TryPutInto(Api.World, inventory[0], 1) > 0;
                    if (moved)
                    {
                        AssetLocation sound = inventory[0].Itemstack?.Block?.Sounds?.Place;
                        Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                        handslot.MarkDirty();
                        genBucketMesh();
                        MarkDirty(true);
                        
                    }
                    return true;
                }
            }

            return false;
        }

        private void genBucketMesh()
        {
            if (Api == null || inventory == null) return;
            if (inventory.Count < 1 || inventory[0].Empty || Api.Side == EnumAppSide.Server) return;

            var stack = inventory[0].Itemstack;
            var meshSource = stack.Collectible as IContainedMeshSource;
            if (meshSource != null)
            {
                bucketMeshTmp = meshSource.GenMesh(stack, (Api as ICoreClientAPI).BlockTextureAtlas, Pos);
                // Liquid mesh part
                bucketMeshTmp.CustomInts = new CustomMeshDataPartInt(bucketMeshTmp.FlagsCount);
                bucketMeshTmp.CustomInts.Count = bucketMeshTmp.FlagsCount;
                bucketMeshTmp.CustomInts.Values.Fill(0x4000000); // light foam only

                bucketMeshTmp.CustomFloats = new CustomMeshDataPartFloat(bucketMeshTmp.FlagsCount * 2);
                bucketMeshTmp.CustomFloats.Count = bucketMeshTmp.FlagsCount * 2;

                bucketMesh = bucketMeshTmp
                    .Clone()
                    .Translate(1.5f / 16f, 0, 0)
                    .Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, GameMath.PIHALF + Block.Shape.rotateY * GameMath.DEG2RAD, 0)
                ;
            }
            else
            {
                bucketMesh?.Clear();
            }
        }
        internal MeshData GenMesh()
        {
            if (ownBlock == null) return null;

            ICoreClientAPI capi = Api as ICoreClientAPI;
            MeshData mesh = capi.TesselatorManager.GetDefaultBlockMesh(ownBlock);

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
            bucketMeshTmp.AddMeshData(data);
        }

        public void AddMeshData(MeshData data, ColorMapData colormapdata, int lodlevel = 1)
        {
            if (data == null) return;
            bucketMeshTmp.AddMeshData(data);
        }
        #endregion

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            mesher.AddMeshData(currentMesh);
            mesher.AddMeshData(bucketMesh);
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
             
            if (worldForResolving.Side == EnumAppSide.Client)
            {
                genBucketMesh();
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
        }

        bool IAlembicEndContainer.handleRecipe(RetortRecipe recipe)
        {
            //if (!recipe.product.Resolve(Api.World, "Resolving retort neck product")) return false;

            if (Api.Side != EnumAppSide.Server) { return false; }
            ItemStack fromStack = recipe.product.ResolvedItemstack.Clone();

            //Handle dropping product into the bowl
            ItemStack bucketStack = inventory[0].Itemstack;
            if (bucketStack != null)
            {
                if (bucketStack.Collectible as BlockLiquidContainerTopOpened != null)
                {
                    BlockLiquidContainerBase bucket = bucketStack.Collectible as BlockLiquidContainerBase;
                    int litresTransferred = (inventory[0].Itemstack.Collectible as BlockLiquidContainerBase).TryPutLiquid(inventory[0].Itemstack, fromStack, 9999f);
                    if (litresTransferred > 0f )
                    {
                        /*ItemStack content = (Inventory[0].Itemstack.Collectible as BlockLiquidContainerBase).GetContent(Inventory[0].Itemstack);
                        content.StackSize += litresTransferred;
                        (Inventory[0].Itemstack.Collectible as BlockLiquidContainerBase).SetContent(Inventory[0].Itemstack, content);*/
                        lastReceivedDistillate = fromStack;
                        //genBucketMesh();
                        
                        //inventory[0].MarkDirty();
                        MarkDirty(true);
                        return true;
                    }
                    else
                    {
                        //lastReceivedDistillate = null;
                        return true;
                    }
                } 
            }
            else
            {
                //if (bucketMesh != null) bucketMesh.Clear();
                //MarkDirty(true);
                //lastReceivedDistillate = null;
            }
            return false;
        }

        public string getCustomItem()
        {
            return "";
        }

        public void stopDistilling()
        {
            lastReceivedDistillate = null;
            MarkDirty(true);
        }

        public bool checkStoechiometry(RetortRecipe recipe)
        {
            return true;
        }
    }
}
