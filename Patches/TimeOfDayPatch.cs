using HarmonyLib;
using UnityEngine;

namespace Random_Sell_Prices.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class TimeOfDayPatch
    {
        // MATH

        static bool hadPityDay = false;
        static float generatePrice(int daysUntilDeadline)
        {
            float minPercentage = RandomSellPrices.minPercentage.Value;
            float maxPercentage = RandomSellPrices.maxPercentage.Value;

            if (daysUntilDeadline <= 1 && !hadPityDay)
            {
                minPercentage = RandomSellPrices.pityPercentage.Value;
            }

            if (StartOfRound.Instance.companyBuyingRate >= RandomSellPrices.pityPercentage.Value)
            {
                hadPityDay = true;
            }

            return UnityEngine.Random.Range(minPercentage, maxPercentage);
        }

        // PATCHES

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.SetBuyingRateForDay))]
        [HarmonyPostfix]
        static void setBuyingRatePatch(ref TimeOfDay __instance)
        {
            StartOfRound.Instance.companyBuyingRate = generatePrice(__instance.daysUntilDeadline);
        }

        [HarmonyPatch("ResetShip")]
        [HarmonyPostfix]
        static void resetShipPatch(ref float ___companyBuyingRate, ref TimeOfDay __instance)
        {
            ___companyBuyingRate = generatePrice(__instance.daysUntilDeadline);
            hadPityDay = false;
        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.SyncNewProfitQuotaClientRpc))]
        [HarmonyPostfix]
        static void syncNewProfitQuotaClientPatch(ref StartOfRound __instance, ref float ___timeUntilDeadline)
        {
            __instance.companyBuyingRate = generatePrice((int)___timeUntilDeadline);
            hadPityDay = false;
        }
    }
}
