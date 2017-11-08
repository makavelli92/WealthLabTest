using System;
using System.Collections.Generic;
using System.Text;
using WealthLab;

namespace WealthLabTest
{
    class TestStrategyHelper : StrategyHelper
    {
        public override string Name => "PavlovTestStrategy";

        public override Guid ID => new System.Guid("af777a27-14c6-41b3-9d9e-c54966cdbc77");

        public override string Author => "Vladimir";

        public override Type WealthScriptType => typeof(TestStrategyScript);

        public override string Description => "Fractals";

        public override DateTime CreationDate => new DateTime(2017,10,17);

        public override DateTime LastModifiedDate => new DateTime(2017, 10, 17);
    }
}
