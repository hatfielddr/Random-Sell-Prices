using HarmonyLib;
using UnityEngine;
using LC_API.ServerAPI;

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

            if (daysUntilDeadline <= 1 && StartOfRound.Instance.companyBuyingRate >= RandomSellPrices.pityPercentage.Value)
            {
                hadPityDay = true;
            }

            return Random.Range(minPercentage, maxPercentage);
        }

        // PATCHES

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.SetBuyingRateForDay))]
        [HarmonyPostfix]
        static void setBuyingRatePatch(ref TimeOfDay __instance)
        {
            if (__instance.IsServer)
            {
                float companyBuyingRate = generatePrice(__instance.daysUntilDeadline);
                StartOfRound.Instance.companyBuyingRate = companyBuyingRate;
                Networking.Broadcast(companyBuyingRate, "companyBuyingRate");
            }
            else if (__instance.IsClient)
            {
                StartOfRound.Instance.companyBuyingRate = RandomSellPrices.receivedRate;
            }
        }

        [HarmonyPatch("ResetShip")]
        [HarmonyPostfix]
        static void resetShipPatch(ref float ___companyBuyingRate, ref TimeOfDay __instance)
        {
            hadPityDay = false;
            if (__instance.IsServer)
            {
                float companyBuyingRate = generatePrice(__instance.daysUntilDeadline);
                StartOfRound.Instance.companyBuyingRate = companyBuyingRate;
                Networking.Broadcast(companyBuyingRate, "companyBuyingRate");
            }
            else if (__instance.IsClient)
            {
                StartOfRound.Instance.companyBuyingRate = RandomSellPrices.receivedRate;
            }
        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.SetNewProfitQuota))]
        [HarmonyPrefix]
        static bool setNewProfitQuotaPrefix(ref TimeOfDay __instance)
        {
            hadPityDay = false;
            if (__instance.IsServer)
            {
                float companyBuyingRate = generatePrice((int)__instance.timeUntilDeadline);
                StartOfRound.Instance.companyBuyingRate = companyBuyingRate;
                Networking.Broadcast(companyBuyingRate, "companyBuyingRate");
            }
            return true;
        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.SyncNewProfitQuotaClientRpc))]
        [HarmonyPostfix]
        static void syncNewProfitQuotaClientPatch(ref TimeOfDay __instance)
        {
            if (__instance.IsClient)
            {
                StartOfRound.Instance.companyBuyingRate = RandomSellPrices.receivedRate;
            }
        }

        [HarmonyPatch(nameof(StartOfRound.SyncCompanyBuyingRateServerRpc))]
        [HarmonyPrefix]
        static bool syncCompanyBuyingRateServerPrefix(ref TimeOfDay __instance)
        {
            if (__instance.IsServer)
            {
                float companyBuyingRate = generatePrice(__instance.daysUntilDeadline);
                StartOfRound.Instance.companyBuyingRate = companyBuyingRate;
                Networking.Broadcast(companyBuyingRate, "companyBuyingRate");
            }
            return true;
        }

        // TODO: Check on saving/loading values with SaveGame and SetTimeAndPlanetToSavedSettings
        // TODO: Check OnPlayerConnectedClient
    }
}
