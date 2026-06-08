namespace NormalizeJapaneseAddresses
{
    public static class Configs
    {
        /// <summary>
        /// TypeScriptのconstとは機能が異なるが、readonlyとした。ただし、InterfaceVersionなどのPropertyはreadonlyではない。
        /// </summary>
        public readonly static Config CurrentConfig = new()
        {
            TownCacheSize = 1000,
        };
    }
}