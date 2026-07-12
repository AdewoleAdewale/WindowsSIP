# Fonts

## Icon font — resolved

`MaterialIcons-Regular.ttf` in this folder is the **real Font Awesome 4.7.0 webfont**
(fetched via the `font-awesome` npm package, OFL-1.1 licensed — see
`FONT-AWESOME-LICENSE.txt`), not a placeholder. It's registered under the alias
`"MaterialIcons"` in `MauiProgram.cs`.

Why Font Awesome under a "MaterialIcons" alias: every icon glyph reference across the app
(`FontFamily="MaterialIcons"` with codepoints like `&#xF060;`, `&#xF095;`, `&#xF0AE;`) turned
out to be Font Awesome 4's Private Use Area codepoints, not Google's Material Icons mapping —
confirmed against Font Awesome's own CSS (`fa-arrow-left` = `\f060`, `fa-phone` = `\f095`,
`fa-tasks` = `\f0ae`, etc., all matching usage throughout the app one-for-one). Rather than
touch every one of the ~30+ files that reference `"MaterialIcons"` as a font alias, the actual
Font Awesome font is registered under that same alias name — so every existing glyph reference
now resolves to the correct icon with zero XAML changes anywhere. If you'd rather standardize
on Google's actual Material Icons font going forward, that's still a one-way rename across
those files whenever you want to make that call; nothing here blocks it later.

## Body/display typeface

`Typography.xaml` (Batch 16 design pass) intentionally stays on Segoe UI (Windows' native
system font) rather than importing a webfont for body/display text. `OpenSans-Regular.ttf`/
`OpenSans-Semibold.ttf` are referenced in `MauiProgram.cs` as a placeholder alternate face in
case you want to differentiate from Segoe — add the actual files here if so (Open Sans is
Apache-2.0 licensed and available via Google Fonts or the `@fontsource/open-sans` npm
package), or remove those two `fonts.AddFont(...)` lines if you're happy with the Segoe UI
look already in place everywhere.
