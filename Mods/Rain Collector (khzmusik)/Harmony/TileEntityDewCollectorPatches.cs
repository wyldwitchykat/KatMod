using HarmonyLib;

namespace RainCollector.Harmony
{
    /// <summary>
    /// Harmony patches to <see cref="TileEntityDewCollector"/>.
    /// </summary>
    public static class TileEntityDewCollectorPatches
    {
        /// <summary>
        /// Fog Convert Multiplier property name.
        /// </summary>
        public const string PropFogConvertMultiplier = "FogConvertMultiplier";

        /// <summary>
        /// Minimum Convert Temperature property name.
        /// </summary>
        public const string PropMinConvertTemperature = "MinConvertTemperature";

        /// <summary>
        /// Rain Convert Multiplier property name.
        /// </summary>
        public const string PropRainConvertMultiplier = "RainConvertMultiplier";

        private static bool initialized = false;
        private static bool enabled = false;

        private static float fogConvertMultiplier = 0;
        private static float minConvertTemperature = float.MinValue;
        private static float rainConvertMultiplier = 0;

        /// <summary>
        /// Initializes the mod, if it hasn't already been initialized.
        /// </summary>
        /// <param name="dewCollector"></param>
        /// <returns></returns>
        public static void Initialize(TileEntityDewCollector dewCollector)
        {
            if (initialized)
            {
                return;
            }

            // In multiplayer games, this should only be done on the server, otherwise extra
            // fill time will be added for each player
            if (!(ConnectionManager.Instance.IsServer || GameManager.IsDedicatedServer))
            {
                RainCollector.DebugLog("Calculations must be done on the server in MP games");
                initialized = true;
                enabled = false;
                return;
            }

            InitalizeFromBlock(dewCollector.blockValue.Block);

            WeatherInfoManager.Initialize(enabled, minConvertTemperature);

            initialized = true;
        }

        private static void InitalizeFromBlock(Block block)
        {
            block.Properties.ParseFloat(PropFogConvertMultiplier, ref fogConvertMultiplier);
            block.Properties.ParseFloat(PropMinConvertTemperature, ref minConvertTemperature);
            block.Properties.ParseFloat(PropRainConvertMultiplier, ref rainConvertMultiplier);

            if (fogConvertMultiplier == 0 &&
                rainConvertMultiplier == 0 &&
                minConvertTemperature == float.MinValue)
            {
                enabled = false;
                return;
            }

            enabled = true;

            // We want to print this message even if we're not debugging
            Log.Out($@"RainCollector initialized: {
                PropFogConvertMultiplier}={fogConvertMultiplier} / {
                PropRainConvertMultiplier}={rainConvertMultiplier} / {
                PropMinConvertTemperature}={minConvertTemperature}");
        }

        /// <summary>
        /// Patches <see cref="TileEntityDewCollector.HandleUpdate(World)"/> to handle the
        /// additional features of this mod.
        /// </summary>
        [HarmonyPatch(typeof(TileEntityDewCollector), nameof(TileEntityDewCollector.HandleUpdate))]
        public class HandleUpdate
        {
            /// <summary>
            /// Harmony prefix for <see cref="TileEntityDewCollector.HandleUpdate(World)"/>
            /// that saves the weather info as state, and bypasses the method if the temperature
            /// check fails.
            /// </summary>
            /// <param name="world"></param>
            /// <param name="__instance"></param>
            /// <param name="__state"></param>
            /// <returns></returns>
            public static bool Prefix(
                World world,
                TileEntityDewCollector __instance,
                out WeatherInfo __state)
            {
                __state = null;

                if (!initialized)
                {
                    Initialize(__instance);
                }

                if (!enabled)
                {
                    return true;
                }

                float deltaTime = (__instance.lastWorldTime != 0UL &&
                                   __instance.lastWorldTime < world.worldTime)
                    ? GameUtils.WorldTimeToTotalSeconds(world.worldTime - __instance.lastWorldTime)
                    : 0f;

                if (deltaTime <= 0f)
                {
                    return true;
                }

                __state = GetWeatherInfo(__instance, deltaTime);


                // Run the original method only if it's above the minimum conversion temperature.
                return __state.Temperature >= minConvertTemperature;
            }

            /// <summary>
            /// Harmony postfix for <see cref="TileEntityDewCollector.HandleUpdate(World)"/>
            /// that handles adding additional fill time due to fog density and rainfall.
            /// </summary>
            /// <param name="world"></param>
            /// <param name="__instance"></param>
            /// <param name="__state"></param>
            public static void Postfix(
                World world,
                TileEntityDewCollector __instance,
                WeatherInfo __state)
            {
                if (!enabled)
                {
                    return;
                }

                if (__state == null)
                {
                    return;
                }

                if (__instance.IsBlocked)
                {
                    return;
                }

                if (__instance.blockValue.Block.IsUnderwater(
                    GameManager.Instance.World,
                    __instance.ToWorldPos(),
                    __instance.blockValue))
                {
                    return;
                }

                // Don't add any time if the dew collector is already full.
                if (DewCollectorHelpers.IsFull(__instance))
                {
                    RainCollector.DebugLog(__instance, "Full; not adding time");
                    return;
                }

                // Update the world times now, in case the original method was skipped.
                __instance.lastWorldTime = __instance.worldTimeTouched = world.worldTime;

                var (fogDensity, rainfall, temperature, deltaTime) = __state;

                if (fogDensity <= 0 && rainfall <= 0)
                {
                    return;
                }

                var additionalTime = deltaTime * fogDensity * fogConvertMultiplier;
                additionalTime += deltaTime * rainfall * rainConvertMultiplier;

                if (temperature < minConvertTemperature)
                {
                    // If this is the current weather, don't add anything.
                    if (!__state.IsAverage)
                    {
                        RainCollector.DebugLog(
                            __instance,
                            $"Temperature is {temperature}, minimum converstion temperature is {minConvertTemperature}");

                        return;
                    }

                    // Approximate how much would have been added, taking temperature into account.
                    additionalTime *= __state.AboveTemperatureRatio;

                    // Set delta time to zero (for logging)
                    deltaTime = 0;
                }

                // Take the modded conversion speed into account.
                additionalTime *= __instance.CurrentConvertSpeed;

                RainCollector.DebugLog(
                    __instance,
                    $"Adding {additionalTime} to {deltaTime}. {__state}");

                AddAdditionalTime(__instance, additionalTime);

                // This sends another message over the wire but I don't think it can be avoided.
                __instance.setModified();
            }

            private static void AddAdditionalTime(
                TileEntityDewCollector dewCollector,
                float additionalTime)
            {
                var currentIndex = dewCollector.CurrentIndex;
                if (currentIndex == -1)
                {
                    // Add any extra time to the leftover time, it will be handled next update.
                    dewCollector.leftoverTime += additionalTime;
                    return;
                }

                // Copied nearly verbatim from the vanilla code.
                // This adds the additional time to the fill value of the current slot.
                // If that is more than the current convert time, it creates a new item stack,
                // sets the leftover time, and resets everything to -1 so the game handles the
                // leftover time on the next update.
                dewCollector.fillValues[currentIndex] += additionalTime;
                if (dewCollector.fillValues[currentIndex] >= dewCollector.CurrentConvertTime)
                {
                    dewCollector.leftoverTime =
                        dewCollector.fillValues[currentIndex] - dewCollector.CurrentConvertTime;
                    
                    dewCollector.items[currentIndex] = new ItemStack(
                        new ItemValue(
                            dewCollector.IsModdedConvertItem
                                ? dewCollector.ModdedConvertToItem.Id
                                : dewCollector.ConvertToItem.Id),
                        dewCollector.CurrentConvertCount);
                    
                    dewCollector.fillValues[currentIndex] = -1f;
                    dewCollector.CurrentConvertTime = -1f;
                    dewCollector.CurrentIndex = -1;
                }
            }

            private static float CalculateLiveSeconds(float deltaTime)
            {
                // Delta time has been converted to game seconds from world time using this code:
                //   gameSeconds = _worldTime * 3.6f
                // Convert it back, and use TimeOfDayIncPerSec to turn that into live seconds.
                return deltaTime / (3.6f * WeatherInfoManager.TimeOfDayIncPerSec);
            }

            private static byte GetBiomeId(TileEntity tileEntity)
            {
                var worldPosition = tileEntity.ToWorldPos();

                return tileEntity.GetChunk().GetBiomeId(
                    World.toBlockXZ(worldPosition.x),
                    World.toBlockXZ(worldPosition.z));
            }

            private static WeatherInfo GetWeatherInfo(TileEntity tileEntity, float deltaTime)
            {
                var biomeId = GetBiomeId(tileEntity);

                // Dew collector updates happen roughly every two real-time seconds.
                // If it's been more than three seconds since the last check, assume we're
                // returning to the chunk, and use the incremental average.
                var liveSeconds = CalculateLiveSeconds(deltaTime);
                if (liveSeconds > 3)
                {
                    RainCollector.DebugLog(
                        tileEntity,
                        $"Using biome average weather: live seconds = {liveSeconds}");

                    return WeatherInfoManager.GetAveragedWeatherInfo(biomeId, deltaTime);
                }

                return WeatherInfoManager.GetActiveWeatherInfo(
                    biomeId,
                    deltaTime,
                    tileEntity.ToWorldPos());
            }
        }
    }
}
