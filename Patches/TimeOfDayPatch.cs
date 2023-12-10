using HarmonyLib;
using System;

namespace Random_Sell_Prices.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    public class TimeOfDayPatch
    {
        // VARS

        static bool hadPityDay = false;

        // PATCHES

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.SetBuyingRateForDay))]
        [HarmonyPostfix]
        static void setBuyingRateForDayPatch()
        {
            float minPercentage = RandomSellPrices.minPercentage.Value;
            float maxPercentage = RandomSellPrices.maxPercentage.Value;
            bool pityEnabled = RandomSellPrices.pityEnabled.Value;
            float daysUntilDeadline = TimeOfDay.Instance != null ? TimeOfDay.Instance.daysUntilDeadline : 3;

            if (pityEnabled && daysUntilDeadline <= 1 && !hadPityDay)
            {
                minPercentage = RandomSellPrices.pityPercentage.Value;
            }

            if (daysUntilDeadline <= 1 && StartOfRound.Instance.companyBuyingRate >= RandomSellPrices.pityPercentage.Value)
            {
                hadPityDay = true;
            }
            var random = new Random(StartOfRound.Instance.randomMapSeed);
            float price = (float)random.NextDouble() * (maxPercentage - minPercentage) + minPercentage;
            RandomSellPrices.mls.LogInfo("Set daily price to: " + price);
            StartOfRound.Instance.companyBuyingRate = price;
        }
    }
}
