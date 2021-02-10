using Sandbox.Game.SessionComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Torch.Managers.PatchManager;
using Torch.Utils;
using VRage;

namespace Utils.Torch
{
    internal static class PerformanceWarningApi
    {
#pragma warning disable 649
        [ReflectedMethodInfo(typeof(MySessionComponentWarningSystem), "UpdateServerWarnings")]
        static readonly MethodInfo _updateServerWarningsMethod;

        [ReflectedMethodInfo(typeof(MySessionComponentWarningSystem), "OnUpdateWarnings")]
        static readonly MethodInfo _onUpdateWarningsMethod;
#pragma warning restore 649


        public static bool Enabled { get; set; }

        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(_updateServerWarningsMethod).Prefixes.Add(typeof(PerformanceWarningApi).GetMethod(nameof(UpdateServerWarningsHack), BindingFlags.Static | BindingFlags.NonPublic));
        }

        private static bool UpdateServerWarningsHack() => !Enabled;

        public static void Broadcast(IEnumerable<MySessionComponentWarningSystem.WarningData> collection)
        {
            MultiplayerUtils.RaiseStaticEvent(_onUpdateWarningsMethod, new[] { collection.ToList() });
        }
    }
}
