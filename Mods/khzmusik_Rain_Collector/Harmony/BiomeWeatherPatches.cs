using HarmonyLib;

namespace RainCollector.Harmony
{
    /// <summary>
    /// Harmony patches to <see cref="WeatherManager.BiomeWeather"/>.
    /// </summary>
    public static class BiomeWeatherPatches
    {
        /// <summary>
        /// Harmony patch to <see cref="WeatherManager.BiomeWeather.ServerFrameUpdate()"/>
        /// to add the new biome weather information to <see cref="WeatherInfoManager"/>.
        /// if needed.
        /// </summary>
        [HarmonyPatch(
            typeof(WeatherManager.BiomeWeather),
            nameof(WeatherManager.BiomeWeather.ServerFrameUpdate))]
        public static class ServerFrameUpdate
        {
            /// <summary>
            /// Harmony postfix to<see cref="WeatherManager.BiomeWeather.ServerFrameUpdate()"/>
            /// which adds the new biome weather information to <see cref="WeatherInfoManager"/>.
            /// </summary>
            /// <param name="__instance"></param>
            /// <param name="_te"></param>
            /// <returns></returns>
            public static void Postfix(WeatherManager.BiomeWeather __instance)
            {
                if (WeatherInfoManager.Enabled && !WeatherInfoManager.IsThrottled(__instance))
                {
                    WeatherInfoManager.UpdateAveragedWeather(__instance);
                }
            }
        }
    }
}
