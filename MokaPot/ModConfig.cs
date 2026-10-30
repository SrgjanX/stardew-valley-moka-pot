namespace MokaPot
{
    /// <summary>The player-editable settings, read from config.json.</summary>
    internal class ModConfig
    {
        /// <summary>How many coffee beans one brew uses.</summary>
        public int BeansPerBrew { get; set; } = 2;

        /// <summary>How many in-game minutes a brew takes.</summary>
        public int BrewMinutes { get; set; } = 30;
    }
}
