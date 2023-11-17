using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Random_Sell_Prices.Patches;

namespace Random_Sell_Prices
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class RandomSellPrices : BaseUnityPlugin
    {
        private const string modGUID = "BigDaddy.RandomSellPrices";
        private const string modName = "Random Sell Prices";
        private const string modVersion = "1.0.0.0";

        public static ConfigEntry<float> minPercentage;
        public static ConfigEntry<float> maxPercentage;
        public static ConfigEntry<float> pityPercentage;

        private readonly Harmony harmony = new Harmony(modGUID);

        private static RandomSellPrices Instance;

        internal ManualLogSource mls;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            // Creating the config file
            minPercentage = Config.Bind("General", "Minimum Selling Percentage", 0.1f, "Minimum random selling price (NOTE: This value is the decimal form of a percentage i.e. 0.1f = 10%.)");
            maxPercentage = Config.Bind("General", "Maximum Selling Percentage", 1.2f, "Maximum random selling price (NOTE: This value is the decimal form of a percentage i.e. 1.2f = 120%.)");
            pityPercentage = Config.Bind("General", "Pity Selling Percentage", 0.8f, "At least one day per quota period is guaranteed to be at least this price (NOTE: This value is the decimal form of a percentage i.e. 0.8f = 80%.)");

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            mls.LogInfo("The Random Price Mod has awoken. Patching...");

            harmony.PatchAll(typeof(RandomSellPrices));
            harmony.PatchAll(typeof(TimeOfDayPatch));

            mls.LogInfo("Patching complete!");
        }
    }
}
