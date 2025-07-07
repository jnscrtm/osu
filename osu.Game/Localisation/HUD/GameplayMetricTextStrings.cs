// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation.HUD
{
    public static class GameplayMetricTextStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.HUD.GameplayMetricTextStrings";

        /// <summary>
        /// "Gameplay Metric"
        /// </summary>
        public static LocalisableString Metric => new TranslatableString(getKey(@"gameplay_metric"), @"Gameplay Metric");

        /// <summary>
        /// "Number of Decimal Places"
        /// </summary>
        public static LocalisableString DecimalPlaces => new TranslatableString(getKey(@"decimal_places"), @"Number of Decimal Places");

        /// <summary>
        /// "Template"
        /// </summary>
        public static LocalisableString Template => new TranslatableString(getKey(@"template"), @"Template");

        /// <summary>
        /// "Supports {{Label}} and {{Value}}, but also including arbitrary attributes like {{TheHeck}} (see metric list for supported values)."
        /// </summary>
        public static LocalisableString TemplateDescription => new TranslatableString(getKey(@"template_description"), @"Supports {{Label}} and {{Value}}, but also including arbitrary attributes like {{TheHeck}} (see metric list for supported values).");

        /// <summary>
        /// "PP"
        /// </summary>
        public static LocalisableString CurrentPP => new TranslatableString(getKey(@"current_pp"), @"PP");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
