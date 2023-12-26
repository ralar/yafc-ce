using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YAFC.Workspace.ProductionTable {
    internal class SetScrollPositionMessage {

        public SetScrollPositionMessage(float top) {
            Top = top;
        }

        public float Top { get; }
    }
}
