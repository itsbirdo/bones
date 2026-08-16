# AI Art Style Prompt

Reusable prompt blocks for generating BONES art with AI image tools (ChatGPT / gpt-image,
Gemini, Recraft). Derived from the art direction in [GAME_DESIGN_SPEC.md](../GAME_DESIGN_SPEC.md) §3.
Hex values match the README badge palette.

**How to use:** paste the core style block at the top of every prompt, then add one
asset-type block, then describe the specific asset. Never freestyle the style; if the look
needs to change, change it here first so every future asset inherits it.

## Core style block (prepend to every prompt)

```
STYLE: 1940s crime-comic noir, hand-inked graphic novel. Sin City contrast,
Mike Mignola shadow shapes: big, flat, decisive black shapes, no soft shading.

MEDIUM: brush-and-ink on paper. Confident, slightly rough linework with
varying line weight. Subtle halftone dot grain in mid-tone areas only.

PALETTE (strict, no other colors):
- Ink black #1f1d21
- Bone cream #ece5d3 (paper, never pure white)
- OPTIONAL single spot color, only if the prompt asks for it:
  amber gold #d4a838 (money, heat) OR blood red #be282c (danger).
  Spot color must cover less than 10% of the image.

RULES:
- Completely flat. No gradients, no gloss, no 3D bevels, no lens effects,
  no soft glow, no photorealism, no digital-painting rendering.
- Shadows are solid black shapes, not shading.
- Everything looks printed on cheap 1940s pulp paper.
- No text or lettering unless the prompt explicitly asks for lettering.

OUTPUT: transparent background, asset centered with a small margin,
crisp edges, high resolution.
```

## Asset-type add-ons (append one after the core block)

### UI chrome (buttons, panels, frames)

```
ASSET: a rectangular UI [button/panel frame] with a hand-inked border,
slightly irregular like it was drawn with a brush. Simple enough to stretch
as a 9-slice sprite: corners detailed, edges plain. Interior is flat bone cream.
```

### Icons (charms, Fence catalog items)

```
ASSET: a single icon of [X], bold silhouette-first design, readable at
64 pixels, 2 to 3 shapes maximum, thick outline.
```

### Comic SFX lettering (the one case text is allowed)

```
ASSET: hand-lettered comic onomatopoeia reading exactly "[HEADCRACK!]",
1940s crime-comic display lettering, jagged energy, black letters with a
bone cream outline, slight ink splatter.
```

### Scene art (street, wall, curb)

```
ASSET: a scene of [X]. Ground-level street view: cracked concrete, chalk
lines, curb as backstop. Composition looks down at the pavement. Mostly
black with cream picking out shapes.
```

## Workflow rules

1. **Kit sheet first.** Generate the first UI kit as one sheet (button states, a panel
   frame, six icons in a single image) so everything shares one palette and line weight.
   Slice it up, then attach that sheet as a reference image to every later prompt with
   "match this exact style". A reference image anchors consistency far better than words.
2. **Spot color in-engine, not in the asset.** Generate everything in black and cream
   only, then tint amber or red in Unity (sprite color or shader). The palette can never
   drift, and one asset serves both meanings (amber = money and heat, red = danger).
3. **No baked label text.** SFX lettering is display art and may be generated; button
   labels and numbers are TextMeshPro with a real 1940s comic hand-lettering font, so
   text stays crisp and localizable.
4. **9-slice everything rectangular.** Panels and buttons are authored once and stretched
   in Unity, not generated per size.
