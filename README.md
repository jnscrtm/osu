## Forking and Disclaimer
This is currently a **personal fork** of [ppy/osu](https://github.com/ppy/osu) created primarily as a **development playground** for experimenting with whatever ideas---whether blessed, cursed, or blursed---that I have at moment's notice. Please note:
- **This is not a standalone release** or an official build of osu!lazer.
- Expect unfinished systems, questionable codes, and wild behaviors under execution.
- Expect messy ordering of functions, variables, and enums, especially those that I newly created.
- Anything can and will break at some point. Sometimes, just out of spite.

## My Current Working Area
I am currently active in developing a new HUD element that I want to have since the first time I touched osu!lazer client (after switching from osu!stable). It is, *at the moment*, named `GameplayMetricText` in namespace `osu.Game.Screens.Play.HUD` with its accompanying property source class `GameplayMetricTextStrings` in namespace `osu.Game.Localisation.HUD`. This is directly inspired by `osu.Game.Skinning.Components.BeatmapAttributeText`

## Status of Current Work
The name of the new HUD component is pretty self-explanatory; it can show a real-time gameplay metric with rolling number effect, currently able to handle either of (1) current combo, (2) longest combo, (3) HP percentage, (4) current accuracy percentage, (5) maximum achievable accuracy percentage, (6) minimum achievable accuracy percentage, and (7) PP.  Many expected properties are not present yet (including duration adjustment). Many expected properties may not be present yet.

The number of decimal places can be adjusted directly from Skin Editor as is plausible for each metric. I implemented a custom generic number roller that rolls only number named `NumberRoller<T>` in the same namespace (but it will be moved to a more appropriate namespace at some point) as `RollingCounter<T>` has not been cooperative with my current objectives. Many expected properties may not be present yet.

## Tentative Plans
I want to extend the component's capability to all gameplay metrics (including UR and judgement counters). Further, I want to eventually make this element to be capable of text templating, as has the existing `BeatmapAttributeText` been. If everything has been stable enough, I may check out a dedicated feature branch for it, then may or may not reset the playground branch. If I am confident enough, I may also draft a PR afterwards.

## Spectral Ideas
Ideas currently floating in the aether. They may manifest... or not---that is up to fate, free time, and caffeine level.
- Fadeable texts, or at least opening texts or closing texts.
- Color-interpolating texts. (Very unlikely to be possible, but the ideas are there)
- Input history overlay. Visualizing the keypress patterns, like ones you will see on osu! gameplay streams, clips, or contents. 

## About This Fork's Maintainer
I am going around in the online worlds with name "Jean-Valentin Auguste". A university student and an osu! player who, among others:
- is studying physics in university for science bachelor,
- likes physics, math, and coding (mostly C++), and
- is appreciating the sheer aesthetic potentials of osu!lazer,
- is somewhere deep in the lower end of 6-digit osu!std ranking.

osu! Profile: [jnscrtm](osu.ppy.sh/users/37827365)

(I still haven't made a proper README for myself even after all these years, smh)

## Copyright Notice (Verbatim Copy)
*osu!*'s code and framework are licensed under the [MIT licence](https://opensource.org/licenses/MIT). Please see [the licence file](LICENCE) for more information. [tl;dr](https://tldrlegal.com/license/mit-license) you can do whatever you want as long as you include the original copyright and license notice in any copy of the software/source.

Please note that this *does not cover* the usage of the "osu!" or "ppy" branding in any software, resources, advertising or promotion, as this is protected by trademark law.

Please also note that game resources are covered by a separate licence. Please see the [ppy/osu-resources](https://github.com/ppy/osu-resources) repository for clarifications.
