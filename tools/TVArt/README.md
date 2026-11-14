# BILGEBOX painted TV casing

Original hand-painted fantasy MMO UI artwork, matching the warm brass/copper palette of Forever-Fone.
The master contains sculpted gold bevels, screws, gears, copper pipes, orange antenna bulbs and a green glass jewel.
`controls-painted.png` adds complete round TUNE knobs in two pointer states, a power switch and a resize crank.
The controls have no overlapping labels. BILGEBOX is an addon FontString on the painted nameplate.

`tv-painted.png` is the unchanged 1254 x 1254 RGBA image-generation master. `prompts.json` preserves the exact
built-in image tool prompt and reference provenance. Generation is nondeterministic; the saved master makes the
texture build reproducible. `controls-prompts.json` records the new built-in image_gen prompt, which used
`tv-painted.png` as its style reference. Both masters are saved as generated and remain unchanged by the build.

## Rebuild

With Python 3 and Pillow installed, run from the repository root (or use the absolute script path):

```powershell
python tools/TVArt/make-art.py
```

The script reads the saved masters, recreates the five delivery PNGs here and the matching `.tga` files in
`addon/ForeverScreen/art/`, then writes `preview.png`. The script uses no network, game directory or desktop capture.
All TGA files are power-of-two, uncompressed true-colour (type 2), 32-bit BGRA, bottom-up, with eight alpha bits and
no colour map or image ID. PNG intermediates retain RGBA. Pillow is the only external build dependency.

## Slice contract

All rectangles below are **half-open**, with a top-left origin: `(left, top, right, bottom)`.
Lua divides x by texture width and y by texture height for `SetTexCoord`; do not invert v for the bottom-up TGA.
One addon UI unit remains one physical pixel through the original inherited frame scale.

| Texture | Canvas | Rectangle | Display / stretch contract |
| --- | --- | --- | --- |
| `rails.tga` | 256 x 128 | `(0,0,22,28)` | Fixed upper-left corner, 22 x 28. |
| | | `(22,0,196,28)` | Top edge, height 28, pixel-identical columns; stretch horizontally. |
| | | `(196,0,256,28)` | Fixed upper-right corner, 60 x 28. |
| | | `(0,32,22,54)` | Fixed lower-left corner, 22 x 22. |
| | | `(22,32,196,54)` | Bottom edge, height 22, pixel-identical columns; stretch horizontally. |
| | | `(196,32,256,54)` | Fixed lower-right corner, 60 x 22. |
| | | `(0,64,22,128)` | Left edge, width 22, pixel-identical rows; stretch vertically. |
| `controls.tga` | 64 x 256 | `(0,0,60,124)` | Fixed upper column backing, 60 x 124; same painted profile as the middle. |
| | | `(0,128,60,132)` | Middle column, width 60, pixel-identical rows; stretch vertically. |
| | | `(0,160,60,224)` | Fixed speaker and jewel, 60 x 64. |
| `antenna.tga` | 128 x 128 | `(0,0,128,128)` | Transparent rabbit ears, displayed 104 x 104 above the top rail. |
| `badge.tga` | 256 x 32 | `(0,0,146,24)` | Fixed gold nameplate, 146 x 24, centered on top rail. |
| `switches.tga` | 128 x 128 | `(0,0,64,64)` | Playing TUNE, displayed 48 x 48 at right-column `(6,8)`. |
| | | `(64,0,128,64)` | Paused TUNE, same anchor and display size. |
| | | `(0,64,64,128)` | Power switch, displayed 48 x 48 at right-column `(6,68)`. |
| | | `(64,64,128,128)` | Resize crank, displayed 32 x 32, 6 px left of the bottom rail's right edge, bottom aligned. |

The top rail's uniform `(22,0,196,28)` profile also covers the original title strip at height 26. The bottom
rail's uniform `(22,32,196,54)` profile covers the original footer strip at height 22. These two strips span the
original frame width. Their bounds are respectively `[0,26)` and `[height-22,height)` within that frame.

Unused atlas pixels are transparent. Horizontal cap transitions and control-column end transitions are blended
to their exact uniform profile. No rivet, dial, speaker rib, jewel or antenna is stretched during frame resizing.
The minimum original frame height is 195: 124 fixed upper pixels + 7 middle pixels + 64 fixed lower pixels.

The casing retains rail thicknesses **top 28, bottom 22, left 22, right 60**. Top and bottom corners extend left
and right of the original frame. Side panels start at its outer edge. The antenna is wholly above the top rail;
the badge stays within that rail. Only the title/footer bezel strips enter the original frame. The top marker
row occupies `[26,32)` and the bottom row `[height-28,height-22)`, so both remain clear, together with the state
cell and video. The green jewel is centered about 30 pixels outboard of the frame's right edge.
Saturated marker-like colours in generated cutout fringes are constrained to brass tones; the jewel's interior
is the only permitted green region. The saved master and original marker colours are never modified.

## Master measurements and processing

The generated painting does not follow the requested coordinate grid exactly. The build uses measured master
bounds: outer shell `(24,322,1232,1218)`, opening `(114,425,958,1126)`. Fixed corners are cropped from their
corresponding intersections. Top/bottom profiles use source column 270, left profile source row 800, right
profile source row 832. The upper control backing repeats that same profile without a seam;
the speaker uses `(958,923,1232,1126)`, antenna `(82,18,372,316)`, badge `(362,330,765,416)`.
The full crop and transition definitions live in `make-art.py`.

The new 1254 x 1254 control master uses equal square crops `(60,60,560,560)` and `(688,60,1188,560)` for the dial
states. Each is resized to 60 x 60 and centered in a 64 x 64 cell, retaining circular proportions and a transparent
margin. The power crop `(168,672,452,1162)` is resized to 30 x 52 and placed at `(17,70)` in the atlas. The crank
crop `(672,700,1196,1120)` is resized to 58 x 46 at `(67,73)`. All four controls have fixed display sizes.

TUNE calls `SetPaused`; its atlas UV changes whenever saved state or a playback command refreshes the state cell.
Two painted states use the existing `SetTexCoord` API without a texture-rotation dependency. Power calls the
original frame's `Hide`; `/fscreen` shows it again. The crank shares the original grip's resize/save handlers.
GameTooltip explains all three controls. Rail dragging continues to move the frame. Switching skins or hiding
the frame releases active resizing and dragging. TV mode hides title counters, plain buttons, footer help, the
grey grip and waiting text. Switching it off restores those elements.

## Verification

The build asserts power-of-two output, exact TGA payload sizes, pixel-identical stretch regions and restricted
edge colours. `AddonSourceTests` checks all texture references and TGA headers, Lua 5.0 syntax restrictions,
unchanged marker/frame geometry at minimum/default/large sizes, saved toggle state, simulated combat, dragging,
play/pause, both pointer states, plain-control visibility, power, tooltips and resize interruption. Run:

```powershell
dotnet test ForeverScreen.slnx
```

`preview.png` is an offline composition showing default/playing and minimum/paused sizes, enlarged pointer states
and all five atlases. It uses the same source rectangles and geometry as Lua, with approximate branding text.
Actual Interface 16001 texture filtering, in-game appearance and real combat interactions require client verification.
