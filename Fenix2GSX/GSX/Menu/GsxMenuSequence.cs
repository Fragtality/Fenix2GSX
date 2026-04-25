using System;
using System.Collections.Generic;

namespace Fenix2GSX.GSX.Menu
{
    public class GsxMenuSequence(List<GsxMenuCommand> commands = null)
    {
        public virtual List<GsxMenuCommand> Commands { get; } = commands ?? [];
        public virtual bool IsExecuting { get; set; } = false;
        public virtual bool IsSuccess { get; set; } = false;
        public virtual bool IgnoreGsxState { get; set; } = false;

        public virtual void Reset()
        {
            IsSuccess = false;
            IsExecuting = false;
        }
    }
}