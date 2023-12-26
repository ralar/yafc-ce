using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YAFC.Workspace.ProductionTable {
    internal class SearchCancelMessage {
        public SearchCancelMessage(string reason) {
            Reason = reason;
        }

        public string Reason { get; }
    }
}
