# Card visual refresh — 2026-09-17

## Reference and scope

Inspected the current REALM-BUILD scene, the other agent's RealmCard / RealmGameUI / DiscardPileHover, and the original web game's public/styles.css and original portrait art.

The actual web full-art card uses 63:88, full-bleed illustration, dark title overlay, a colored score badge, and a light victory-condition panel. Its depth reference is a 3px cream rim, a 7px #957e55 side wall and a darker offset shadow. The prior square-art/gold-panel interpretation was incorrect.

## Implemented

- New scene-authored RealmCardTemplate, directly created through Unity MCP. It has independent soft/contact shadows, side wall, paper edge, rim, rounded surface mask, back, art, title, score and rule objects. Inspector changes now persist; RealmCard does not construct UI at runtime.
- Gameplay clones the new template. The previous unconfigured fallback constructor was removed.
- 300 original art sprites are serialized in the template for build inclusion, with a separate corrected farmer-12 override. Other original artwork remains unchanged.
- Hand layout is five columns by two rows, with 182 x 254.222 cards and space for the side wall and selected lift.
- Pile positioning ignores the round badge, keeps cards inside the row and exposes the edges. Mini cards use 84 x 117.333.
- CardVisualReview provides six examples, including the new back and corrected farmer. Open from the lobby's 카드 보기; return using 로비로.

## Generated project assets

Built-in image generation was used; original files remain available.

- Assets/Textures/RealmCardBack-Mosaic-v2.png
- Assets/Textures/RoleCardArt/farmer-12-corrected.png

Back prompt: Create a full-bleed 63:88 medieval stained-glass card back using the farmer illustration only as a style reference. Bold black lead lines, luminous cobalt/royal blue and amber glass, restrained emerald/ruby details, a readable simple crown in a rose-window medallion, symmetrical geometric motifs and a narrow border. No text, isolated crest, empty black panel, photoreal metallic filigree, mockup or perspective.

Farmer edit prompt: Preserve the original sunset wheat field, house, blue oval border, straw hat, white rolled sleeves, brown overalls and wheat. Correct the twisted anatomy to a natural three-quarter front view with head and chest facing right and two naturally attached arms holding wheat in front. Final face-only edit removes eyes, eyebrows, mouth and expression, leaving a plain tan glass face and simple dark beard silhouette. Keep the corrected pose and all other composition unchanged.

## Verification scope

Visual checks use actual Unity Game View. A local presentation fixture exercises ten hand cards, two visible discards and three face-down discards without opening a network room. Review navigation and card selection are exercised via their existing Button events. This does not verify networking, a standalone build, or the full game loop.

Verified results: all 300 role/variant combinations resolve a sprite; the card hierarchy remains 23 transforms before and after all data changes. Two selected hand cards enable the discard button. Review close/reopen succeeds. Pile hover expands the two-card stack from 112 to 188 pixels. Final Unity console query returns zero errors. Saved in edit mode with the review visible; source template and gameplay panel inactive.

Evidence: qa/evidence/card-refresh-review-final.png and qa/evidence/card-refresh-gameplay-final.png.
