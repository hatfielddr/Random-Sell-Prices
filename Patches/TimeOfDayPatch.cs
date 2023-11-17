using HarmonyLib;

namespace Random_Sell_Prices.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class TimeOfDayPatch
    {
        static bool hadGoodDay = false;

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.SetBuyingRateForDay))]
        [HarmonyPostfix]
        static void setBuyingRatePatch(ref TimeOfDay __instance)
        {
            float minPercentage = RandomSellPrices.minPercentage.Value;
            float maxPercentage = RandomSellPrices.maxPercentage.Value;

            if (__instance.daysUntilDeadline <= 1 && !hadGoodDay)
            {
                minPercentage = 0.8f;
            }

            StartOfRound.Instance.companyBuyingRate = UnityEngine.Random.Range(minPercentage, maxPercentage);

            if (StartOfRound.Instance.companyBuyingRate >= 0.8f)
            {
                hadGoodDay = true;
            }
        }

        [HarmonyPatch("ResetShip")]
        [HarmonyPostfix]
        static void resetShipPatch()
        {
            hadGoodDay = false;
        }
    }
}
