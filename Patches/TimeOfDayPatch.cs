using HarmonyLib;
using UnityEngine;
using LC_API.ServerAPI;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;

namespace Random_Sell_Prices.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    public class TimeOfDayPatch
    {
        // CODE MATCH

        private static readonly CodeMatch[] SetFromSaveIlMatch = new CodeMatch[] {
            new CodeMatch(i => i.opcode == OpCodes.Stfld),
            new CodeMatch(i => i.opcode == OpCodes.Call),
            new CodeMatch(i => i.Calls(typeof(TimeOfDay).GetMethod("SetBuyingRateForDay", BindingFlags.Instance | BindingFlags.Public)))
        };

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
            float price = Random.Range(minPercentage, maxPercentage);
            RandomSellPrices.mls.LogInfo("Server price set to: " + price);
            return price;
        }

        // PATCHES

        [HarmonyTranspiler]
        [HarmonyPatch("SetTimeAndPlanetToSavedSettings")]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions);

            codeMatcher.Start();
            codeMatcher.MatchStartForward(SetFromSaveIlMatch);
            codeMatcher.Advance(2);
            codeMatcher.RemoveInstructionsWithOffsets(0, 2);
            codeMatcher.Insert(new CodeInstruction(OpCodes.Call, typeof(TimeOfDayPatch).GetMethod(nameof(TimeOfDayPatch.altBuyingRateForDayPatch), BindingFlags.Static | BindingFlags.NonPublic)));

            return codeMatcher.Instructions();
        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.SetBuyingRateForDay))]
        [HarmonyPostfix]
        static void setBuyingRateForDayPatch()
        {
            RandomSellPrices.mls.LogInfo("isServer: " + TimeOfDay.Instance.IsServer + "\nisHost: " + TimeOfDay.Instance.IsHost + "\nisClient: " + TimeOfDay.Instance.IsClient);

            if (TimeOfDay.Instance.IsServer || TimeOfDay.Instance.IsHost)
            {
                float companyBuyingRate = generatePrice(TimeOfDay.Instance.daysUntilDeadline);
                StartOfRound.Instance.companyBuyingRate = companyBuyingRate;
                RandomSellPrices.mls.LogInfo("Broadcasting: " + companyBuyingRate);
                Networking.Broadcast(companyBuyingRate.ToString(), "companyBuyingRate");
            }
            else if (!TimeOfDay.Instance.IsServer && TimeOfDay.Instance.IsClient)
            {
                StartOfRound.Instance.companyBuyingRate = RandomSellPrices.receivedRate;
                RandomSellPrices.mls.LogInfo("Received: " + RandomSellPrices.receivedRate);
            }
        }

        private static void altBuyingRateForDayPatch()
        {
            RandomSellPrices.mls.LogInfo("-----Your thought worked!-----");
            float companyBuyingRate = generatePrice(3);
            StartOfRound.Instance.companyBuyingRate = companyBuyingRate;
        }

        [HarmonyPatch("ResetShip")]
        [HarmonyPostfix]
        static void resetShipPatch(ref TimeOfDay __instance)
        {
            hadPityDay = false;
            if (__instance.IsServer || __instance.IsHost)
            {
                float companyBuyingRate = generatePrice(__instance.daysUntilDeadline);
                StartOfRound.Instance.companyBuyingRate = companyBuyingRate;
                Networking.Broadcast(companyBuyingRate, "companyBuyingRate");
            }
            else if (!__instance.IsServer && __instance.IsClient)
            {
                StartOfRound.Instance.companyBuyingRate = RandomSellPrices.receivedRate;
                RandomSellPrices.mls.LogInfo("Received: " + RandomSellPrices.receivedRate);
            }
        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.SetNewProfitQuota))]
        [HarmonyPrefix]
        static bool setNewProfitQuotaPrefix(ref TimeOfDay __instance)
        {
            hadPityDay = false;
            if (__instance.IsServer || __instance.IsHost)
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
            if (!__instance.IsServer && __instance.IsClient)
            {
                StartOfRound.Instance.companyBuyingRate = RandomSellPrices.receivedRate;
                RandomSellPrices.mls.LogInfo("Received: " + RandomSellPrices.receivedRate);
            }
        }

        [HarmonyPatch(nameof(StartOfRound.SyncCompanyBuyingRateServerRpc))]
        [HarmonyPrefix]
        static bool syncCompanyBuyingRateServerPrefix(ref TimeOfDay __instance)
        {
            if (__instance.IsServer || __instance.IsHost)
            {
                float companyBuyingRate = generatePrice(__instance.daysUntilDeadline);
                StartOfRound.Instance.companyBuyingRate = companyBuyingRate;
                Networking.Broadcast(companyBuyingRate, "companyBuyingRate");
            }
            return true;
        }

        [HarmonyPatch(nameof(StartOfRound.OnClientConnect))]
        [HarmonyPrefix]
        static bool onClientConnectPrefix(ref TimeOfDay __instance)
        {
            if (__instance.IsServer || __instance.IsHost)
            {
                float companyBuyingRate = generatePrice(__instance.daysUntilDeadline);
                StartOfRound.Instance.companyBuyingRate = companyBuyingRate;
                Networking.Broadcast(companyBuyingRate, "companyBuyingRate");
            }
            return true;
        }

        [HarmonyPatch("OnPlayerConnectedClientRpc")]
        [HarmonyPostfix]
        static void onPlayerConnectedClientRpc(ref TimeOfDay __instance)
        {
            if (!__instance.IsServer && __instance.IsClient)
            {
                StartOfRound.Instance.companyBuyingRate = RandomSellPrices.receivedRate;
                RandomSellPrices.mls.LogInfo("Received: " + RandomSellPrices.receivedRate);
            }
        }

        // TODO: Check on saving/loading values with SaveGame and SetTimeAndPlanetToSavedSettings
        // TODO: Check OnPlayerConnectedClient
    }
}
