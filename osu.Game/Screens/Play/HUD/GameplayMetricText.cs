// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Localisation;
using osu.Framework.Threading;
using osu.Framework.Timing;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation.HUD;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;
using osu.Game.Skinning;
using osu.Game.Skinning.Components;

namespace osu.Game.Screens.Play.HUD
{
    [UsedImplicitly]
    public partial class GameplayMetricText : FontAdjustableSkinComponent, ISerialisableDrawable
    {
        [Resolved]
        private ScoreProcessor scoreProcessor { get; set; } = null!;

        [Resolved]
        private GameplayState gameplayState { get; set; } = null!;

        [Resolved]
        private IBindable<WorkingBeatmap> beatmap { get; set; } = null!;

        [SettingSource(typeof(GameplayMetricTextStrings), nameof(GameplayMetricTextStrings.Metric))]
        public Bindable<GameplayMetric> Metric { get; } = new Bindable<GameplayMetric>(GameplayMetric.PerformancePoints);

        [SettingSource(typeof(GameplayMetricTextStrings), nameof(GameplayMetricTextStrings.DecimalPlaces))]
        public BindableNumber<int> DecimalPlacesBindable { get; } = new BindableNumber<int>
        {
            MinValue = 0,
            MaxValue = 5,
            Default = 0
        };

        public BindableBool Test { get; } = new BindableBool(false);

        private readonly CancellationTokenSource loadCancellationSource = new CancellationTokenSource();

        // Beatmap-related fields
        private Mod[] clonedMods = null!;
        private DifficultyAttributes difficultyAttributes = null!;

        // Performance-related fields
        private DifficultyAttributes? attrib;
        private List<TimedDifficultyAttributes>? timedAttributes;
        public virtual bool IsValid { get; set; }
        private JudgementResult lastJudgement = null!;
        private ScoreInfo scoreInfo = null!;
        private PerformanceCalculator? performanceCalculator;
        private HitEventExtensions.UnstableRateCalculationResult? unstableRateResult;

        // Display- and render-related fields
        private readonly OsuSpriteText text;
        private NumberRoller<double> roller;

        private Task<ScheduledDelegate> taskScheduler = null!;

        public GameplayMetricText()
        {
            Clock = new FramedClock();
            roller = new NumberRoller<double>(Clock);
            AutoSizeAxes = Axes.Both;
            InternalChildren = new Drawable[]
            {
                text = new OsuSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(BeatmapDifficultyCache difficultyCache)
        {
            if (gameplayState != null)
            {
                // Load the beatmap-related info
                clonedMods = gameplayState.Mods.Select(x => x.DeepClone()).ToArray();
                difficultyAttributes = new DifficultyAttributes(clonedMods, gameplayState.Beatmap.BeatmapInfo.StarRating);

                // Initiate the performance-related judgement
                scoreInfo = new ScoreInfo(gameplayState.Beatmap.BeatmapInfo, gameplayState.Ruleset.RulesetInfo) { Mods = clonedMods };
                performanceCalculator = gameplayState.Ruleset.CreatePerformanceCalculator();

                var gameplayWorkingBeatmap = new GameplayWorkingBeatmap(gameplayState.Beatmap);
                taskScheduler = difficultyCache.GetTimedDifficultyAttributesAsync(gameplayWorkingBeatmap, gameplayState.Ruleset, clonedMods, loadCancellationSource.Token)
                               .ContinueWith(task => Schedule(() =>
                               {
                                   timedAttributes = task.GetResultSafely();
                                   IsValid = true;

                                   if (lastJudgement != null)
                                       onJudgementChanged(lastJudgement);
                               }), TaskContinuationOptions.OnlyOnRanToCompletion);
            }
        }

        private DifficultyAttributes? getAttributeAtTime(JudgementResult judgement)
        {
            if (timedAttributes == null || timedAttributes.Count == 0)
                return null;

            int attribIndex = timedAttributes.BinarySearch(new TimedDifficultyAttributes(judgement.HitObject.GetEndTime(), null));
            if (attribIndex < 0)
                attribIndex = ~attribIndex - 1;

            return timedAttributes[Math.Clamp(attribIndex, 0, timedAttributes.Count - 1)].Attributes;
        }

        // Here goes every event-related whack

        private void onJudgementChanged(JudgementResult judgement)
        {
            lastJudgement = judgement;
            attrib = getAttributeAtTime(judgement);
            scoreProcessor.PopulateScore(scoreInfo);

            roller.Current.Value = getValue();
            IsValid = true;
        }

        private bool appendPercent
        {
            get
            {
                switch (Metric.Value)
                {
                    case GameplayMetric.Accuracy:
                    case GameplayMetric.MaxAchievableAccuracy:
                    case GameplayMetric.MinAchievableAccuracy:
                    case GameplayMetric.HP:
                        return true;
                    default:
                        return false;
                }
            }
        }

        private int getMaxPrecision()
        {
            switch (Metric.Value)
            {
                case GameplayMetric.Combo:
                case GameplayMetric.LongestCombo:
                    return 0;
                default:
                    return 5;
            }
        }

        private int getDefaultPrecision()
        {
            switch (Metric.Value)
            {
                case GameplayMetric.Accuracy:
                case GameplayMetric.MaxAchievableAccuracy:
                case GameplayMetric.MinAchievableAccuracy:
                case GameplayMetric.HP:
                    return 2;

                default:
                    return 0;
            }
        }

        private string formFloatFormat(int index, int numberOfDecimal)
        {
            return "{" + index.ToString() + ":F" + numberOfDecimal.ToString() + "}";
        }

        private double getValue()
        {
            if (gameplayState == null || attrib == null || scoreProcessor == null)
            {
                IsValid = false;
                return Metric.Value != GameplayMetric.Accuracy ? roller.Current.Value : 100.0;
            }

            double final_res;
            switch (Metric.Value)
            {
                case GameplayMetric.PerformancePoints:
                    final_res = performanceCalculator?.Calculate(scoreInfo, attrib).Total ?? 0;
                    break;
                case GameplayMetric.Score:
                    final_res = scoreInfo.TotalScore;
                    break;
                case GameplayMetric.Accuracy:
                    final_res = scoreInfo.Accuracy * 100.0;
                    break;
                case GameplayMetric.MaxAchievableAccuracy:
                    final_res = scoreProcessor.MaximumAccuracy.Value * 100.0;
                    break;
                case GameplayMetric.MinAchievableAccuracy:
                    final_res = scoreProcessor.MinimumAccuracy.Value * 100.0;
                    break;
                case GameplayMetric.Combo:
                    final_res = scoreInfo.Combo;
                    break;
                case GameplayMetric.HP:
                    final_res = gameplayState.HealthProcessor.Health.Value * 100.0;
                    break;
                case GameplayMetric.LongestCombo:
                    final_res = scoreInfo.MaxCombo;
                    break;
                case GameplayMetric.UnstableRate:
                    unstableRateResult = scoreInfo.HitEvents.CalculateUnstableRate(unstableRateResult);
                    final_res = unstableRateResult?.Result ?? 0.0;
                    break;
                case GameplayMetric.AverageHitError:
                    final_res = scoreInfo.HitEvents.CalculateAverageHitError() ?? 0.0;
                    break;
                case GameplayMetric.MedianHitError:
                    final_res = scoreInfo.HitEvents.CalculateMedianHitError() ?? 0.0;
                    break;
                case GameplayMetric.HitErrorStdDev:
                    unstableRateResult = scoreInfo.HitEvents.CalculateUnstableRate(unstableRateResult);
                    final_res = (unstableRateResult?.Result / 10.0) ?? 0.0;
                    break;
                default:
                    final_res = 0;
                    break;
            }

            final_res = Math.Round(final_res, DecimalPlacesBindable.Value, MidpointRounding.AwayFromZero);
            return final_res;
        }

        #region OVERRIDES
        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (scoreProcessor != null)
            {
                scoreProcessor.NewJudgement += onJudgementChanged;
                scoreProcessor.JudgementReverted += onJudgementChanged;

                Metric.BindValueChanged(_ =>
                {
                    int max_prec = getMaxPrecision();
                    DecimalPlacesBindable.Value = getDefaultPrecision();
                    DecimalPlacesBindable.MaxValue = max_prec;

                    updateText();
                });

                DecimalPlacesBindable.MaxValue = getMaxPrecision(); // To enforce an immediate correction upon load.
            }
        }

        protected override void Update()
        {
            base.Update();
            updateText();
        }

        private void updateText()
        {
            roller.Update();
            text.Text = LocalisableString.Format(formFloatFormat(0, DecimalPlacesBindable.Value) + (appendPercent ? "%" : string.Empty), roller.DisplayedCount);
        }

        protected override void SetFont(FontUsage font) => text.Font = font.With(size: 40);

        protected override void SetTextColour(Colour4 textColour) => text.Colour = textColour;

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            // TODO: Dispose!
            if (scoreProcessor != null)
            {
                scoreProcessor.NewJudgement -= onJudgementChanged;
                scoreProcessor.JudgementReverted -= onJudgementChanged;
            }

            taskScheduler?.GetResultSafely().Cancel();
            taskScheduler?.WaitSafely();
            loadCancellationSource?.Cancel();
            loadCancellationSource?.Dispose();
        }
        #endregion

        #region IMPL_CLASSES

        // TODO: This class shouldn't exist, but is required due to current IWorkingBeatmap/IBeatmap (hierarchial) design conflict.
        // Consider refactoring once this conflict along with the breaking changes has been resolved.
        private class GameplayWorkingBeatmap : WorkingBeatmap
        {
            private readonly IBeatmap gameplayBeatmap;
            public GameplayWorkingBeatmap(IBeatmap gameplayBeatmap)
                : base(gameplayBeatmap.BeatmapInfo, null)
            {
                this.gameplayBeatmap = gameplayBeatmap;
            }

            protected override IBeatmap GetBeatmap() => gameplayBeatmap;
            public override IBeatmap GetPlayableBeatmap(IRulesetInfo ruleset, IReadOnlyList<Mod> mods, CancellationToken cancellationToken) => gameplayBeatmap;

            public override Texture GetBackground() => throw new NotImplementedException();
            public override Stream GetStream(string storagePath) => throw new NotImplementedException();
            protected override Track GetBeatmapTrack() => throw new NotImplementedException();
            protected internal override ISkin GetSkin() => throw new NotImplementedException();
        }

        private partial class TextComponent : CompositeDrawable, IHasText
        {
            public LocalisableString Text
            {
                get => text.Text;
                set => text.Text = value;
            }

            private readonly OsuSpriteText text;

            public TextComponent()
            {
                AutoSizeAxes = Axes.Both;
                InternalChildren =
                [
                    text = new OsuSpriteText
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        Font = OsuFont.Numeric.With(size: 16, fixedWidth: true)
                    }
                ];
            }

            public bool UsesFixedAnchor { get; set; }
        }

        #endregion
    }


    public enum GameplayMetric
    {
        Score,
        HP,
        Combo, LongestCombo,
        Accuracy, MaxAchievableAccuracy, MinAchievableAccuracy,
        PerformancePoints,
        UnstableRate, AverageHitError, MedianHitError, HitErrorStdDev
    }

    public partial class NumberRoller<T> where T : struct, IComparable<T>
    {
        public T DisplayedCount { get; private set; }
        public Bindable<T> Current = new Bindable<T>();
        private T startValue;
        private double startTime;
        protected double RollingDuration = 375;

        public bool RollingEnabled = true;

        private IClock clock;

        public Action<T>? OnValueUpdated = null;
        public Easing Easing = Easing.OutQuint;
        public bool IsRolling => clock.CurrentTime < startTime + RollingDuration;

        public NumberRoller(IClock clock)
        {
            this.clock = clock;
            Current.ValueChanged += e =>
            {
                startValue = (e.OldValue.CompareTo(DisplayedCount) > 0) ? DisplayedCount : e.OldValue;
                startTime = clock.CurrentTime;
            };
        }

        public void Update()
        {
            if (!IsRolling) return;

            if (RollingDuration > 0 && RollingEnabled)
            {
                double delta_t = (clock.CurrentTime - startTime) / RollingDuration;
                DisplayedCount = Interpolation.ValueAt(delta_t, startValue, Current.Value, 0, 1, Easing);
            }
            else
                DisplayedCount = Current.Value;

            OnValueUpdated?.Invoke(DisplayedCount);
        }
    }
}
