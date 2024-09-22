namespace BaseDamageModifiers
{
    using System.Collections.Generic;
    using UnityEngine;
    using BepInEx;
    using HarmonyLib;
    using System.Linq;
    using Discord;

    [BepInPlugin(GUID, NAME, VERSION)]
    public class BaseDamageModifiers : BaseUnityPlugin
    {
        public const string GUID = "com.ehaugw.basedamagemodifiers";
        public const string VERSION = "1.0.0";
        public const string NAME = "BaseDamageModifiers";

        public delegate void WeaponDamageModifier(Weapon weapon, DamageList original, ref DamageList result);
        public static WeaponDamageModifier WeaponDamageModifiers = delegate (Weapon weapon, DamageList original, ref DamageList result) { };

        public delegate void WeaponImpactModifier(Weapon weapon, float original, ref float result);
        public static WeaponImpactModifier WeaponImpactModifiers = delegate (Weapon weapon, float original, ref float result) { };

        internal void Awake()
        {
            var harmony = new Harmony(GUID);
            harmony.PatchAll();
        }

        //MANIPULATE IMPACT AT THE MOST BASIC LEVEL
        [HarmonyLib.HarmonyPatch(typeof(Weapon), "BaseImpact", MethodType.Getter)]
        public class Weapon_BaseImpact
        {
            [HarmonyPostfix]
            public static void Postfix(Weapon __instance, ref float __result)
            {
                float original = __result;
                WeaponImpactModifiers(__instance, original, ref __result);
            }
        }

        //This patch causes GetAttackImpact to calculate its impact based on Stats.Impact rather than using cached values. Overrides value from the original function entirely and may break other mods
        [HarmonyLib.HarmonyPatch(typeof(WeaponStats), nameof(WeaponStats.GetAttackImpact))]
        public class WeaponStats_GetAttackImpact
        {
            [HarmonyPostfix]
            public static void Postfix(WeaponStats __instance, int _attackID, ref float __result, Item ___m_item)
            {
                var weapon = (Weapon)___m_item;
                var currentType = weapon.Type;
                if (weapon?.OwnerCharacter?.Animator is Animator animator && animator.HasParameter("WeaponType"))
                {
                    currentType = (Weapon.WeaponType)animator.GetInteger("WeaponType");
                }
                var impactMult = 1f;
                if (WeaponStatData.WeaponBaseDataDict.Keys.Contains(currentType))
                {
                    var impactMultArray = WeaponStatData.WeaponBaseDataDict[currentType].ImpactMult;


                    if (_attackID < 0 || _attackID >= impactMultArray.Length)
                    {
                        _attackID = 0;
                    }

                    impactMult = impactMultArray[_attackID];
                }
                __result = weapon.Impact * impactMult;
            }
        }

        //ALTERNATE WEAPON DAMAMAGE AT THE MOST BASIC LEVEL
        [HarmonyLib.HarmonyPatch(typeof(Weapon), nameof(Weapon.Damage), MethodType.Getter)]
        public class Weapon_Damage
        {
            [HarmonyPostfix]
            public static void Postfix(Weapon __instance, ref DamageList __result)
            {
                var original = __result.Clone();
                __result = __result.Clone();
                WeaponDamageModifiers(__instance, original, ref __result);
            }
        }

        //This patch causes GetAttackDamage to calculate its damage based on Stats.Damage rather than using cached values. Overrides value from the original function entirely and may break other mods
        [HarmonyLib.HarmonyPatch(typeof(WeaponStats), nameof(WeaponStats.GetAttackDamage))]
        public class WeaponStats_GetAttackDamage
        {
            [HarmonyPostfix]
            public static void Postfix(WeaponStats __instance, int _attackID, ref IList<float> __result, Item ___m_item)
            {
                var weapon = (Weapon)___m_item;


                var currentType = weapon.Type;
                if (weapon?.OwnerCharacter?.Animator is Animator animator && animator.HasParameter("WeaponType"))
                {
                    currentType = (Weapon.WeaponType)animator.GetInteger("WeaponType");
                }

                var damageMult = new float[] { 1f, 1f, 1f, 1f, 1f };
                if (WeaponStatData.WeaponBaseDataDict.Keys.Contains(currentType))
                {
                    damageMult = WeaponStatData.WeaponBaseDataDict[currentType].DamageMult;
                }

                if (_attackID < 0 || _attackID >= damageMult.Length)
                {
                    _attackID = 0;
                }

                var damageList = weapon.Damage;
                for (int i = 0; i < damageList.Count && i < __result.Count; i++)
                {
                    __result[i] = damageList[i].Damage * damageMult[_attackID];
                }
            }
        }
    }
}