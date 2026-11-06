# Episode Two local portal jungle scenery

These two generated textures fill the exposed left and right jungle terrain beside the Episode Two portal approach. They are decorative scenery on the native track layer, with no colliders or gameplay changes. The original road, stone ring, water, bridge, tilemaps and atlases remain original assets.

## Runtime resources

| Resource | PNG dimensions | World X | World Z |
| --- | --- | --- | --- |
| [PortalJungleLeft.png](../../EpisodeTwo/Assets/Resources/PortalJungleLeft.png) | 256 × 512 RGBA | [-256, -128] | [-51840, -51584] |
| [PortalJungleRight.png](../../EpisodeTwo/Assets/Resources/PortalJungleRight.png) | 320 × 512 RGBA | [128, 288] | [-51840, -51584] |

The images use two pixels per world unit. Image top is positive world Z. Each quad uses its whole texture once, without mirroring or repetition: `u = (X - xmin) / (xmax - xmin)`, `v = (Z + 51840) / 256`.

The measured native road surface is **Y = 80**. The component derives the actual height from the native road batch and parents both quads to that batch, retaining its `scrollValue = 1` transform. The earlier Y100 analysis and smaller 224 × 448 / 288 × 448 crops were superseded after native rendering established the real plane and visible footprint. They are not the final runtime assets.

SHA-256 of the final PNG bytes:

```text
PortalJungleLeft.png   13d9e1d7bff03b54b5089f73ff0736769e1c216924799cf8ff9a626acf70ced7
PortalJungleRight.png  a055da00702f4e6e9f8a2221f99ce5d407654f707c27914a9121b9348a74c0eb
```

The PNGs total 626,231 bytes and 294,912 pixels. Left contains 83,464 opaque pixels (63.68%) and 47,608 transparent pixels; right contains 113,184 opaque pixels (69.08%) and 50,656 transparent pixels. Transparent pixels retain the original water, bridge and native terrain context.

## Mask and rendering constraints

The runtime alpha mask is binary. It permits new ground in the original missing terrain and a small irregular replacement band along the non-road cliff edge. That replacement removes the native black rectangular cutoff; placing new art behind the black cutoff would leave it visible.

The central corridor `abs(X) <= 160`, for `Z <= -51584`, stays transparent so the original water and bridge render normally. Nothing above `Z = -51584` is painted. The native cliff replacement band is limited to the approved irregular boundary; the final coverage padding adds only missing terrain at `Z <= -51648`, without widening the replacement into the upper core. No blur, feathering or faded seam band is used.

The two masked quads draw immediately after the native road scenery (queue 2991 when native scenery is 2990), before the portal grid at queue 3000. They use `ZTest Always`, `ZWrite Off`, `Cull Off`, `Blend Off` and `clip(alpha - 0.5)`. The component follows the native material's queue plus one as the portal stage changes. Original native renderers remain in place. There are no added native-tile cliff quads, mirrored strips, full-map underlay texture or screen-space backdrop in this resource set.

Import retains source RGBA and original non-power-of-two dimensions, with maximum size 512, clamp wrapping and uncompressed RGBA32 for the reviewed production import. The runtime uses only the two small resources above. The local generation source and raw image are not runtime textures.

## Generation and local provenance

Generated on 2026-11-06 through the user-selected OpenAI-compatible provider `https://internal-api.pvzflare.com/v1`, using its advertised **`gpt-image-2`** model through the unmodified bundled imagegen CLI in masked edit mode. The successful local request returned **1536 × 1024 RGB PNG**, exactly the requested dimensions. Raw SHA-256:

```text
b38305127e6065a8d5008ef2b46ec100beda1329573e6aa740942075ef8d8a0e
```

The edit target was a local 1152 × 768 native-pixel band covering X[-288,288], Z[-51872,-51488], with protected original wall, floor, water and bridge context. The masked sides contained the earlier scenery proposal and were repainted with the narrow non-road cliff repair. The returned image was downsampled to that original local two-pixel-per-world scale; locked context was restored exactly. Only the two final runtime rectangles were exported. The coverage correction for the measured Y80 plane reused this same generated image and did not make another API request.

Local production materials remain under `output/imagegen/portal-sides-local-v3-20261106/`: the untouched raw, input target and mask, exact prompt, sanitized invocation result, local mapping, crop scripts, `runtime-y80/manifest.json`, output hashes and compact review images. These large development sources and API credentials are not included with the game or required by a source build. The two final PNGs and this resource note are sufficient for the checked-in runtime asset provenance.

The earlier whole-map attempt under `portal-sides-v2-20261106` was rejected for visible native-edge discontinuities and excessive scope. It is retained locally as a production record, not imported as a runtime resource. Generated scenery is distinct from the recovered publisher artwork; this note does not change the rights or notices for the original reference materials.

## Exact prompt used for the successful local edit

```text
Use case: precise-object-edit
Asset type: a small local two-sided terrain repair for an existing top-down Unity jungle scene, not a full new map.
Primary request: Repaint ONLY the two masked side regions into natural continuous jungle cliff edges and ground. Repair the abrupt black L-shaped native map cutoff and the mismatched generated terrain directly below it. Both left and right local regions must be newly and coherently painted, with distinct nonmirrored details.
Input: Image 1 is the local edit target. Its upper cliff, central stone bridge, water, floor and surrounding unmasked pixels come from the original game. The masked portions currently contain a previous generated proposal with a visibly incorrect straight black edge. The binary mask allows the black cutoff and a narrow irregular part of the original non-road outer cliff to be replaced, as well as the missing ground below. Everything outside the mask is fixed original context and must remain unchanged.
Required repair: Continue the visible original rock strata, roots, moss, small foliage and earthy ground across the allowed region as one natural terrain surface. Remove every straight black horizontal or L-shaped cutoff in the editable regions. Let the new irregular rocky/leafy terrain edges connect directly to the unchanged original cliff above; match their positions, detail size, local color and lighting. Keep crisp native game-art detail at the joins. Make the lower ground a continuous dark green jungle floor at the same orthographic scale.
Invariants: Preserve the original central road, stone bridge, water and floor, all unmasked native terrain, the image framing, the exact top-down projection and scale. Do not alter anything outside the binary mask. The small replaceable outer cliff edge is scenery only; it is not a road or central structure.
Avoid: black lines or rectangular tile cutoffs, visible seams, blur, feathering, fog, faded transition bands, repeated or mirrored tile strips, pasted collage, oversized rocks or leaves, new roads or bridges, new structures, characters, vehicles, pickups, HUD, portal grid, lettering or watermark. Do not repaint the entire scene. Do not use soft blending to hide a seam.
Output: one opaque 3:2 PNG, keeping the local input framing. Only the masked left and right side repairs change.
```

Prompt UTF-8 SHA-256 (including its final newline): `56b8f5ff6a43e25258adfc70a649707f2ce63160c04d5953300ce118505850cb`.
