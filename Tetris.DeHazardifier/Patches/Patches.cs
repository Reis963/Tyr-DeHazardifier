using System;
using System.Collections;
using System.Reflection;
using EFT;
using EFT.Interactive;
using SPT.Reflection.Patching;
using UnityEngine;

namespace HazardPatches
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PatchTargetAttribute : Attribute
    {
        public Type TargetType { get; }
        public string MethodName { get; }
        public Type ReturnType { get; }
        public Type[] Parameters { get; }

        public PatchTargetAttribute(Type targetType, string methodName, Type returnType, params Type[] parameters)
        {
            TargetType = targetType;
            MethodName = methodName;
            ReturnType = returnType;
            Parameters = parameters;
        }
    }

    public abstract class DisableDamagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var target = GetType().GetCustomAttribute<PatchTargetAttribute>();
            var method = target.TargetType.GetMethod(target.MethodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                null, target.Parameters, null);
            if (method == null || method.ReturnType != target.ReturnType || method.ContainsGenericParameters)
                throw new MissingMethodException(target.TargetType.FullName, target.MethodName);
            return method;
        }

        // Unity callers pass the result directly to StartCoroutine.
        protected static IEnumerator EmptyCoroutine()
        {
            yield break;
        }
    }

    [PatchTarget(typeof(Minefield), nameof(Minefield.FireCoroutine), typeof(IEnumerator), typeof(IPlayer))]
    public class MinefieldCoroutinePatch : DisableDamagePatch
    {
        [PatchPrefix]
        public static bool Prefix(ref IEnumerator __result)
        {
            __result = EmptyCoroutine();
            return false;
        }
    }

    [PatchTarget(typeof(Minefield), nameof(Minefield.DealExplosionDamage), typeof(void), typeof(IPlayer), typeof(float), typeof(bool), typeof(bool))]
    public class MinefieldDamagePatch : DisableDamagePatch
    {
        [PatchPrefix]
        public static bool Prefix() => false;
    }

    [PatchTarget(typeof(MinefieldView), nameof(MinefieldView.MinefieldOnPlayerShotEvent), typeof(void), typeof(IObserverToPlayerBridge), typeof(BorderZone), typeof(float), typeof(bool))]
    public class MinefieldViewTriggerPatch : DisableDamagePatch
    {
        [PatchPrefix]
        public static bool Prefix() => false;
    }

    [PatchTarget(typeof(BarbedWire), nameof(BarbedWire.ProceedDamage), typeof(void), typeof(IObserverToPlayerBridge), typeof(BodyPartCollider))]
    public class BarbedWireDamagePatch : DisableDamagePatch
    {
        [PatchPrefix]
        public static bool Prefix() => false;
    }

    [PatchTarget(typeof(BarbedWire), nameof(BarbedWire.AddPenalty), typeof(void), typeof(IObserverToPlayerBridge))]
    public class BarbedWireSpeedPenaltyPatch : DisableDamagePatch
    {
        [PatchPrefix]
        public static bool Prefix() => false;
    }

    [PatchTarget(typeof(SniperImitator), nameof(SniperImitator.method_1), typeof(IEnumerator), typeof(float), typeof(IObserverToPlayerBridge), typeof(Vector3), typeof(Quaternion), typeof(bool))]
    public class SniperImitatorDamagePatch : DisableDamagePatch
    {
        [PatchPrefix]
        public static bool Prefix(ref IEnumerator __result)
        {
            __result = EmptyCoroutine();
            return false;
        }
    }

    [PatchTarget(typeof(SniperImitator), nameof(SniperImitator.OnPlayerShotEvent), typeof(void), typeof(IObserverToPlayerBridge), typeof(BorderZone), typeof(float), typeof(bool))]
    public class SniperImitatorShootPatch : DisableDamagePatch
    {
        [PatchPrefix]
        public static bool Prefix() => false;
    }

    [PatchTarget(typeof(SniperFiringZone), nameof(SniperFiringZone.Shoot), typeof(void), typeof(IPlayer), typeof(bool), typeof(bool), typeof(bool))]
    public class SniperFiringZoneShootPatch : DisableDamagePatch
    {
        [PatchPrefix]
        public static bool Prefix() => false;
    }

    [PatchTarget(typeof(SniperFiringZone), nameof(SniperFiringZone.FireCoroutine), typeof(IEnumerator), typeof(IPlayer))]
    public class SniperFiringZoneCoroutinePatch : DisableDamagePatch
    {
        [PatchPrefix]
        public static bool Prefix(ref IEnumerator __result)
        {
            __result = EmptyCoroutine();
            return false;
        }
    }

    [PatchTarget(typeof(FlameDamageTrigger), nameof(FlameDamageTrigger.ProceedDamage), typeof(void), typeof(IObserverToPlayerBridge), typeof(BodyPartCollider))]
    public class FlameDamageTriggerPatch : DisableDamagePatch
    {
        [PatchPrefix]
        public static bool Prefix() => false;
    }
}

