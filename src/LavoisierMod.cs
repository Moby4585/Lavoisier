using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace lavoisier
{
    class LavoisierMod : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterBlockClass("AlembicBoilingFlask", typeof(AlembicBoilingFlask));
            api.RegisterBlockEntityClass("AlembicBoilingFlaskEntity", typeof(AlembicBoilingFlaskEntity));
        }
    }
}