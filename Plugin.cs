using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LC_API.ServerAPI;
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
        public static ConfigEntry<bool> pityEnabled;

        private readonly Harmony harmony = new Harmony(modGUID);

        private static RandomSellPrices Instance;

        public static ManualLogSource mls;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            // CREATING/BINDING THE CONFIG FILE
            minPercentage = Config.Bind("General", "Minimum Selling Percentage", 0.1f, "Minimum random selling price (NOTE: This value is the decimal form of a percentage i.e. 0.1f = 10%.)");
            maxPercentage = Config.Bind("General", "Maximum Selling Percentage", 1.2f, "Maximum random selling price (NOTE: This value is the decimal form of a percentage i.e. 1.2f = 120%.)");
            pityEnabled = Config.Bind("General", "Pity Day Enabled", true, "Whether or not to allow for one pity day, where the sell percentage is at leasr the amount shown below.");
            pityPercentage = Config.Bind("General", "Pity Selling Percentage", 0.8f, "At least one day per quota period is guaranteed to be at least this price (NOTE: This value is the decimal form of a percentage i.e. 0.8f = 80%.)");

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);


            // PATCHING
            mls.LogInfo("The Random Price Mod has awoken. Patching...");

            harmony.PatchAll(typeof(RandomSellPrices));
            harmony.PatchAll(typeof(TimeOfDayPatch));

            mls.LogInfo("Patching complete!");
        }
    }
}
