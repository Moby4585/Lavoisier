using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace lavoisier
{
    class LavoisierMod : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterBlockClass("AlembicBoilingFlask", typeof(AlembicBoilingFlask));
            api.RegisterBlockEntityClass("AlembicBoilingFlaskEntity", typeof(AlembicBoilingFlaskEntity));
            api.RegisterBlockClass("AlembicRetortNeck", typeof(AlembicRetortNeck));
            api.RegisterBlockEntityClass("AlembicRetortNeckEntity", typeof(AlembicRetortNeckEntity));
            api.RegisterBlockClass("AlembicDissolver", typeof(AlembicDissolver));
            api.RegisterBlockEntityClass("AlembicDissolverEntity", typeof(AlembicDissolverEntity));

            RecipeSystem.LoadRetortRecipes(api);
        }
    }
}