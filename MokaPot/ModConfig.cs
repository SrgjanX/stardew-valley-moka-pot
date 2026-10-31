namespace MokaPot
{
    /// <summary>The player-editable settings, read from config.json.</summary>
    internal class ModConfig
    {
        /// <summary>How many coffee beans one brew uses.</summary>
        public int BeansPerBrew { get; set; } = 2;

        /// <summary>How many in-game minutes a brew takes.</summary>
        public int BrewMinutes { get; set; } = 30;

        /// <summary>Whether to drink coffee right away instead of asking "Drink Coffee?".</summary>
        public bool SkipCoffeePrompt { get; set; } = true;
    }
}
