namespace RainCollector
{
    /// <summary>
    /// Holds information about the weather.
    /// </summary>
    public class WeatherInfo
    {
        /// <summary>
        /// The ID of the biome in which this weather occurs.
        /// </summary>
        public byte BiomeId { get; set; }

        /// <summary>
        /// The name of the biome in which this weather occurs.
        /// </summary>
        public string BiomeName { get; set; }

        /// <summary>
        /// True if these values represent incremental averages, false if they represent the
        /// current weather values.
        /// </summary>
        public bool IsAverage { get; set; }

        /// <summary>
        /// Fog density, 0..1.
        /// </summary>
        public float FogDensity { get; set; }

        /// <summary>
        /// Rainfall, 0..1.
        /// </summary>
        public float Rainfall { get; set; }

        /// <summary>
        /// Temperature.
        /// </summary>
        public float Temperature { get; set; }

        /// <summary>
        /// <para>
        /// Number of <em>in-game</em> seconds since the last time this weather information was
        /// used to update some type of weather calculations. This is always current, it is never
        /// an incremental average.
        /// </para>
        /// <para>
        /// The delta time may be different depending upon what is doing the calculations.
        /// For this reason, methods that create this object should allow callers to specify their
        /// own delta time.
        /// </para>
        /// </summary>
        public float DeltaTime { get; set; }

        /// <summary>
        /// The average <see cref="DeltaTime"/>.
        /// </summary>
        public float AvgDeltaTime { get; set; }

        /// <summary>
        /// <para>
        /// The world time when this weather info was last updated.
        /// </para>
        /// <para>
        /// The weather info is also "updated" when it is first created, so this does not indicate
        /// whether the information has been initialized or not. Use the <see cref="Initialized"/>
        /// property for that.
        /// </para>
        /// </summary>
        public ulong Updated { get; set; }

        /// <summary>
        /// Total time, in <em>in-game</em> seconds, this weather was active.
        /// For non-averaged weather, this is the same as <see cref="DeltaTime"/>.
        /// </summary>
        public float TotalTime { get; set; }

        /// <summary>
        /// Total time, in <em>in-game</em> seconds, this weather was above the minimum
        /// temperature needed for rain and fog to be considered in the averaged info.
        /// </summary>
        public float AboveTemperatureTime { get; set; }

        /// <summary>
        /// The ratio of time spent above the minimum conversion temperature.
        /// </summary>
        public float AboveTemperatureRatio => AboveTemperatureTime / TotalTime;

        /// <summary>
        /// Whether this object has been initialized with weather information.
        /// Only objects representing averaged historical data need initialization.
        /// </summary>
        public bool Initialized => !IsAverage ||
            !(FogDensity == default && Rainfall == default && Temperature == default);

        /// <summary>
        /// Deconstruct into fog density, rainfall, temperature, and delta time.
        /// </summary>
        /// <param name="fogDensity"></param>
        /// <param name="rainfall"></param>
        /// <param name="temperature"></param>
        /// <param name="deltaTime"></param>
        public void Deconstruct(
            out float fogDensity,
            out float rainfall,
            out float temperature,
            out float deltaTime)
        {
            fogDensity = FogDensity;
            rainfall = Rainfall;
            temperature = Temperature;
            deltaTime = DeltaTime;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $@"({BiomeName
                }) Rainfall = {Rainfall
                } / Fog Density = {FogDensity
                } / Temperature = {Temperature
                }{(IsAverage ? " (inc avg) " : string.Empty)}";
        }
    }
}
