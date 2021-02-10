using NLog;
using Sandbox.Engine.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.Network;
using VRageMath;

namespace Utils.Torch
{
    internal static class MultiplayerUtils
    {
        private static readonly ILogger _log = LogManager.GetCurrentClassLogger();

        public static void RaiseStaticEvent(MethodInfo eventInfo, object[] args, EndpointId endpoint = default, Vector3D? pos = null)
        {
            if (eventInfo == null)
                throw new ArgumentNullException(nameof(eventInfo));
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            var parameters = eventInfo.GetParameters();
            var validMethod = ValidRaiseGenericMethodOrDefault(parameters.Length);
            if (validMethod == default)
            {
                _log.Error($"RaiseStaticEvent method not found for method {eventInfo.DeclaringType.FullName} => {eventInfo.Name}");
                return;
            }

            validMethod = validMethod.MakeGenericMethod(parameters.Select(b => b.ParameterType).ToArray());

            var invokeArgs = new List<object> { MakeEventDelegate(eventInfo) };
            invokeArgs.AddRange(args);
            invokeArgs.Add(endpoint);
            invokeArgs.Add(pos);
            validMethod.Invoke(null, invokeArgs.ToArray());
        }

        private static MethodInfo ValidRaiseGenericMethodOrDefault(int count) => typeof(MyMultiplayer).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                                                                      .Where(b => b.IsGenericMethod && b.Name.StartsWith("RaiseStaticEvent") && b.GetGenericArguments().Length == count)
                                                                                                      .FirstOrDefault();

        private static Delegate MakeEventDelegate(MethodInfo eventInfo)
        {
            var parameters = eventInfo.GetParameters();
            var types = parameters.Select(b => b.ParameterType).ToArray();
            var actionType = Expression.GetActionType(types) ?? throw new NullReferenceException("Action type");
            var funcType = Expression.GetFuncType(typeof(IMyEventOwner), actionType) ?? throw new NullReferenceException("Func type");
            var eventOwnerParam = Expression.Parameter(typeof(IMyEventOwner), "_");
            return (Delegate)Convert.ChangeType(Expression.Lambda(Expression.Convert(Expression.Constant(eventInfo.CreateDelegate(actionType)), actionType), eventOwnerParam).Compile(), funcType);
        }
    }
}
