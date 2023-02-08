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
    class AlembicRetortNeckEntity : BlockEntityLiquidContainer
    {
        public override string InventoryClassName => "retortneck";

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            //loadMesh();

            //RegisterGameTickListener(OnGameTick, 50);
        }

        public AlembicRetortNeckEntity()
        {
            inventory = new InventoryGeneric(3, null, null);

            //inventory.SlotModified += inventory_SlotModified;
        }
    }
}
