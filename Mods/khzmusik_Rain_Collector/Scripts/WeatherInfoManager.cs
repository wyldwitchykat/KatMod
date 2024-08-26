using System.Linq;
using UnityEngine;

namespace RainCollector
{
    /// <summary>
    /// Static class that manages weather information, current and historic, per biome.
    /// </summary>
    public static class WeatherInfoManager
    {
        // Incremental averages of weather info values, indexed by biome definition ID.
        private static WeatherInfo[] biomeWeatherInfo = null;

        /// <summary>
        /// True if the weather info manager is enabled.
        /// </summary>
        public static bool Enabled { get; private set; } = false;

        /// <summary>
        /// True if the weather info manager is initialized.
        /// </summary>
        public static bool Initialized { get; private set; } = false;

        /// <summary>
        /// The minimum temperature, in degrees Fahrenheit, at which the fog density and rainfall
        /// should be updated in the incremental averages of weather info values.
        /// For example, this could be set to the freezing point of water (32).
        /// </summary>
        public static float MinTemperature { get; private set; } = float.MinValue;

        private static float throttle = float.MinValue;

        /// <summary>
        /// Throttle time, in the world time equivalent of real-time seconds.
        /// Defaults to the equivalent of 2 seconds.
        /// Setting this property will convert real-time seconds to the world time equivalent.
        /// </summary>
        public static float Throttle
        {
            get => throttle > float.MinValue ? throttle : (throttle = 2.0f * TimeOfDayIncPerSec);
            set => throttle = value * TimeOfDayIncPerSec;
        }

        private static int incPerSec = int.MinValue;

        /// <summary>
        /// Helper property to lazily get the "24 Hour Cycle" setting from the game preferences.
        /// </summary>
        public static int TimeOfDayIncPerSec => incPerSec > int.MinValue
            ? incPerSec
            : (incPerSec = GameStats.GetInt(EnumGameStats.TimeOfDayIncPerSec));

        /// <summary>
        /// <para>
        /// Gets information representing the active (non-averaged) weather in a biome.
        /// </para>
        /// <para>
        /// If the biome ID represents <see cref="WeatherManager.currentWeather"/>, this method
        /// will call <see cref="GetCurrentWeatherInfo(float?)"/> and return the results.
        /// </para>
        /// </summary>
        /// <param name="biomeId">Biome ID.</param>
        /// <param name="deltaTime">Optional delta time to use.</param>
        /// <param name="deltaTime">Optional position to use to determine biome intensity.</param>
        /// <returns><c>null</c> if the ID is not a valid biome ID.</returns>
        public static WeatherInfo GetActiveWeatherInfo(
            byte biomeId,
            float? deltaTime = null,
            Vector3i? position = null)
        {
            if (IsCurrentWeather(biomeId))
                return GetCurrentWeatherInfo(deltaTime);

            var biomeWeather = GetBiomeWeather(biomeId);

            if (biomeWeather == null)
                return null;

            return GetWeatherInfo(biomeWeather, deltaTime, position);
        }

        /// <summary>
        /// Gets information representing the averaged historical weather for a biome.
        /// </summary>
        /// <param name="biomeId">Biome ID.</param>
        /// <param name="deltaTime">Optional delta time to use.</param>
        /// <returns><c>null</c> if the ID is not a valid biome ID.</returns>
        public static WeatherInfo GetAveragedWeatherInfo(byte biomeId, float? deltaTime = null)
        {
            if (biomeId < 0 || biomeId > biomeWeatherInfo.Length)
            {
                return null;
            }

            if (!biomeWeatherInfo[biomeId].Initialized)
            {
                UpdateAveragedWeather(GetActiveWeatherInfo(biomeId));
            }

            var avg = biomeWeatherInfo[biomeId];
            return new WeatherInfo
            {
                BiomeId = avg.BiomeId,
                IsAverage = avg.IsAverage,
                FogDensity = avg.FogDensity,
                Rainfall = avg.Rainfall,
                Temperature = avg.Temperature,
                DeltaTime = deltaTime ?? avg.DeltaTime,
                AvgDeltaTime = avg.AvgDeltaTime,
                Updated = avg.Updated,
                TotalTime = avg.TotalTime,
                AboveTemperatureTime = avg.AboveTemperatureTime
            };
        }

        /// <summary>
        /// <para>
        /// Gets information representing <see cref="WeatherManager.currentWeather"/>,
        /// which is the weather for the current biome.
        /// </para>
        /// <para>
        /// Because we're getting weather info for the current biome, we can use some methods
        /// which can't be used for other biomes. This makes the weather info more accurate.
        /// </para>
        /// </summary>
        /// <param name="deltaTime">Optional delta time to use.</param>
        /// <returns>
        /// Weather info for the current biome, or <c>null</c> if there is no current weather
        /// (e.g. the weather manager isn't initialized yet).
        /// </returns>
        public static WeatherInfo GetCurrentWeatherInfo(float? deltaTime = null)
        {
            var biomeId = GetCurrentWeatherBiomeId();
            if (biomeId == byte.MaxValue)
                return null;

            var now = GameManager.Instance.World.worldTime;

            var dt = deltaTime ??
                GameUtils.WorldTimeToTotalSeconds(now - biomeWeatherInfo[biomeId].Updated);

            return new WeatherInfo
            {
                BiomeId = biomeId,
                FogDensity = SkyManager.GetFogDensity(),
                Rainfall = WeatherManager.Instance.GetCurrentRainfallValue(),
                Temperature = WeatherManager.Instance.GetCurrentTemperatureValue(),
                DeltaTime = dt,
                Updated = now,
                TotalTime = dt
            };
        }

        /// <summary>
        /// Gets the weather info directly from the biome weather.
        /// </summary>
        /// <param name="biomeWeather">The biome weather.</param>
        /// <param name="deltaTime">Optional delta time to use.</param>
        /// <returns></returns>
        public static WeatherInfo GetWeatherInfo(
            WeatherManager.BiomeWeather biomeWeather,
            float? deltaTime = null,
            Vector3i? blockPosition = null)
        {
            var biomeId = GetBiomeId(biomeWeather);

            var now = GameManager.Instance.World.worldTime;

            var dt = deltaTime ??
                GameUtils.WorldTimeToTotalSeconds(now - biomeWeatherInfo[biomeId].Updated);

            return new WeatherInfo
            {
                BiomeId = biomeId,
                FogDensity = GetFogDensity(biomeWeather, blockPosition),
                Rainfall = GetCurrentRainfallValue(biomeWeather),
                Temperature = GetCurrentTemperature(biomeWeather),
                DeltaTime = dt,
                Updated = now,
                TotalTime = dt
            };
        }

        /// <summary>
        /// Initializes the static weather information, if it hasn't already been initialized.
        /// </summary>
        public static void Initialize(bool enabled, float minTemperature)
        {
            Harmony.RainCollector.DebugLog(
                $@"Initializing WeatherInfoManager: enabled={enabled} / minTemperature={minTemperature}");

            if (Initialized)
                return;

            if (!(Enabled = enabled))
                return;

            MinTemperature = minTemperature;

            InitializeBiomeWeatherInfo();

            Initialized = true;
        }

        /// <summary>
        /// Returns true if the biome weather with the given biome ID is the current (active) biome
        /// weather in <see cref="WeatherManager.currentWeather"/>.
        /// </summary>
        /// <param name="biomeWeather"></param>
        /// <returns></returns>
        public static bool IsCurrentWeather(byte biomeId)
        {
            return GetCurrentWeatherBiomeId() == biomeId;
        }

        /// <summary>
        /// Returns true if this biome is throttled for updating, meaning its averaged historical
        /// data has been updated too recently to be updated again right now. This is mainly done
        /// for performance reasons.
        /// </summary>
        /// <param name="biomeWeather"></param>
        /// <returns></returns>
        public static bool IsThrottled(WeatherManager.BiomeWeather biomeWeather)
        {
            var updated = biomeWeatherInfo[GetBiomeId(biomeWeather)].Updated;

            return (GameManager.Instance.World.worldTime - updated) < Throttle;
        }

        /// <summary>
        /// <para>
        /// Updates the averaged historical weather info from the given biome weather.
        /// </para>
        /// <para>
        /// If the biome weather represents <see cref="WeatherManager.currentWeather"/>,
        /// this method will call <see cref="GetCurrentWeatherInfo(float?)"/> and use the results.
        /// Otherwise, it will use the results from
        /// <see cref="GetWeatherInfo(WeatherManager.BiomeWeather, float?, Vector3i?)"/>.
        /// </para>
        /// </summary>
        /// <param name="biomeWeather"></param>
        public static void UpdateAveragedWeather(WeatherManager.BiomeWeather biomeWeather)
        {
            var currentInfo = IsCurrentWeather(GetBiomeId(biomeWeather))
                ? GetCurrentWeatherInfo()
                : GetWeatherInfo(biomeWeather);

            UpdateAveragedWeather(currentInfo);
        }

        /// <summary>
        /// Updates the averaged historical weather info using the current weather info.
        /// </summary>
        /// <param name="biomeWeather"></param>
        public static void UpdateAveragedWeather(WeatherInfo current)
        {
            if (current == null)
                return;

            var isWarmEnough = current.Temperature > MinTemperature;

            var averaged = biomeWeatherInfo[current.BiomeId];

            averaged.DeltaTime = current.DeltaTime;
            averaged.Updated = current.Updated;
            averaged.TotalTime += current.TotalTime;
            averaged.AboveTemperatureTime += isWarmEnough ? current.TotalTime : 0;

            if (!averaged.Initialized)
            {
                averaged.AvgDeltaTime = current.DeltaTime;
                averaged.Temperature = current.Temperature;
                averaged.FogDensity = current.FogDensity;
                averaged.Rainfall = current.Rainfall;

                return;
            }

            var avgDeltaTime = (averaged.AvgDeltaTime + current.DeltaTime) / 2;

            averaged.AvgDeltaTime = avgDeltaTime;

            averaged.Temperature += (current.Temperature - averaged.Temperature) / avgDeltaTime;

            // Fog density and rainfall should only be updated if it's currently above the
            // minimum temperature.
            if (isWarmEnough)
            {
                averaged.FogDensity += (current.FogDensity - averaged.FogDensity) / avgDeltaTime;
                averaged.Rainfall += (current.Rainfall - averaged.Rainfall) / avgDeltaTime;
            }
        }

        private static byte GetBiomeId(WeatherManager.BiomeWeather biomeWeather)
        {
            return biomeWeather.biomeDefinition.m_Id;
        }

        private static WeatherManager.BiomeWeather GetBiomeWeather(byte biomeId)
        {
            return WeatherManager.Instance.biomeWeather
                .FirstOrDefault(w => w.biomeDefinition.m_Id == biomeId);
        }

        // This code is adapted from WorldEnvironment.SpectrumsFrameUpdate but modified to
        // optionally get the biome intensity at a world position (of, say, a tile entity)
        private static float GetFogDensity(
            WeatherManager.BiomeWeather biomeWeather,
            Vector3i? position)
        {
            var fogPercent = biomeWeather.FogPercent();

            var worldEnvironment = WeatherManager.Instance?.world?.m_WorldEnvironment;

            if (worldEnvironment == null)
            {
                return fogPercent;
            }

            if (worldEnvironment.fogDensityOverride >= 0f)
            {
                return worldEnvironment.fogDensityOverride;
            }

            var biomeAtmosphereEffects = worldEnvironment.world.BiomeAtmosphereEffects;

            BiomeIntensity biomeIntensity;
            if (position.HasValue)
            {
                worldEnvironment.world.GetBiomeIntensity(position.Value, out biomeIntensity);
            }
            else
            {
                biomeIntensity = new BiomeIntensity(GetBiomeId(biomeWeather));
            }

            var fogColorSpectrum = biomeAtmosphereEffects.getColorFromSpectrum(
                biomeIntensity,
                worldEnvironment.dayTimeScalar,
                AtmosphereEffect.ESpecIdx.Fog);

            var weatherSpectrum = WeatherManager.Instance.GetWeatherSpectrum(
                fogColorSpectrum,
                AtmosphereEffect.ESpecIdx.Fog,
                worldEnvironment.dayTimeScalar);

            var fogDensity =
                Mathf.Pow(
                    weatherSpectrum.a,
                    Utils.FastLerpUnclamped(
                        WorldEnvironment.dataFogPow.y,
                        WorldEnvironment.dataFogPow.x,
                        SkyManager.dayPercent))
                + fogPercent;

            return Mathf.Clamp01(fogDensity);
        }

        // Adopted from WeatherManager.GetCurrentRainfallValue
        private static float GetCurrentRainfallValue(WeatherManager.BiomeWeather biomeWeather)
        {
            return WeatherManager.forceRain >= 0f
                ? WeatherManager.forceRain
                : biomeWeather.rainParam.value;
        }

        // Adopted from WeatherManager.GetTemperature but added the grace period check
        private static float GetCurrentTemperature(WeatherManager.BiomeWeather biomeWeather)
        {
            if (WeatherManager.forceTemperature > -100f)
            {
                return WeatherManager.forceTemperature;
            }

            if (WeatherManager.inWeatherGracePeriod)
            {
                return GetGracePeriodTemperature(biomeWeather.biomeDefinition.m_Id);
            }

            return biomeWeather.parameters[0].value;
        }

        private static byte GetCurrentWeatherBiomeId()
        {
            // Use byte.MaxValue as a sentinel, because we can't return something like -1
            return WeatherManager.currentWeather?.biomeDefinition?.m_Id ?? byte.MaxValue;
        }

        // Adopted from WeatherManager.CurrentWeatherFromNearBiomesFrameUpdate
        private static float GetGracePeriodTemperature(byte biomeId)
        {
            // snow
            if (biomeId == 1)
            {
                return 45f;
            }
            // pine_forest and whatever would be in ID 2 if it existed
            if (biomeId - 2 <= 1)
            {
                return 60f;
            }
            // Everything else: desert, burnt_forest, wasteland, etc.
            return 70f;
        }

        private static byte GetMaxBiomeId()
        {
            return WeatherManager.Instance.biomeWeather.Max(w => w.biomeDefinition.m_Id);
        }

        private static void InitializeBiomeWeatherInfo()
        {
            var maxBiomeId = GetMaxBiomeId();

            biomeWeatherInfo = new WeatherInfo[maxBiomeId + 1];

            var now = GameManager.Instance.World.worldTime;

            // So all the biomes don't update at the same time, give some variance to the "updated"
            // time. The variance should have a total range that is roughly the same as the
            // throttle, so the updates are evenly spread out in time.
            var variancePerBiome = Throttle / maxBiomeId;

            for (byte id = 0; id <= maxBiomeId; id++)
            {
                biomeWeatherInfo[id] = new WeatherInfo
                {
                    BiomeId = id,
                    IsAverage = true,
                    // Subtract the variance so it's always prior to the current world time,
                    // and let the biomes with the lowest IDs update the earliest.
                    Updated = now - (ulong)((maxBiomeId - id) * variancePerBiome)
                };
            }
        }
    }
}
