using Torch.API;
using Torch.Managers;
using Torch.Managers.PatchManager;

namespace Utils.Torch
{
    internal sealed class UtilsPatchManager : Manager
    {
#pragma warning disable 649
        [Dependency(Ordered = false)]
        readonly PatchManager _patchMgr;
#pragma warning restore 649

        PatchContext _patchContext;

        UtilsPatchManager(ITorchBase torchInstance) : base(torchInstance)
        {
        }

        public static void Add(ITorchBase torch)
        {
            var mngr = new UtilsPatchManager(torch);
            torch.Managers.AddManager(mngr);
        }

        public override void Attach()
        {
            base.Attach();

            _patchContext = _patchMgr.AcquireContext();
            GameLoopObserver.Patch(_patchContext);
            PerformanceWarningApi.Patch(_patchContext);
        }

        public override void Detach()
        {
            base.Detach();

            _patchMgr.FreeContext(_patchContext);
        }
    }
}