using System;
using System.Collections.Generic;

namespace Fenix2GSX.GSX.Menu
{
    public class GsxMenuSequence
    {
        public virtual List<GsxMenuCommand> Commands { get; } = [];
        public virtual bool IsExecuting { get; set; } = false;
        public virtual bool IsSuccess { get; set; } = false;
        public virtual bool IgnoreGsxState { get; set; } = false;
        public virtual Action<GsxMenuSequence> CallbackCompleted { get; set; } = null;

        public virtual void Reset()
        {
            IsSuccess = false;
            IsExecuting = false;
        }
    }
}
