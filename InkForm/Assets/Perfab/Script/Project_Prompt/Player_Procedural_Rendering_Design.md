# Player Procedural Rendering - Design Document

## 1. Overview

The player body can render as a procedural slime instead of relying only on the fallback `SpriteRenderer`. The first production-ready pass is split across two components on the player body:

- `S_PlayerProceduralRenderer`: generates runtime meshes for the slime body, outline, highlight, eyes, and eye glow.
- `S_PlayerDynamicCollider`: adjusts the existing `CircleCollider2D` conservatively so the physics shape follows the visual squash/slick behavior.

The system is intentionally independent from movement. It reads player state, velocity, and fluid climb surface state, then updates visuals and collider shape without owning gameplay decisions.

---

## 2. Runtime Hierarchy

```
Pre_MainChar
`-- body
    |-- Rigidbody2D
    |-- CircleCollider2D
    |-- SpriteRenderer fallback
    |-- S_coleve
    |-- S_PlayerProceduralRenderer
    `-- S_PlayerDynamicCollider

Runtime children created by S_PlayerProceduralRenderer:
    ProceduralSlime_Outline
    ProceduralSlime_Body
    ProceduralSlime_Highlight
    ProceduralSlime_EyeGlow
    ProceduralSlime_Eyes
```

The runtime mesh children inherit the body layer and the fallback sprite sorting layer/order.

---

## 3. Visual Slime Rendering

`S_PlayerProceduralRenderer` builds fan meshes each frame:

- Body mesh: black slime mass with velocity, impact, contact, and wobble deformation.
- Outline mesh: slightly larger version constrained against contact planes.
- Highlight mesh: optional subtle body highlight, currently transparent by default for a pure black body.
- Eye glow mesh: translucent white halo around the eyes.
- Eye mesh: solid white eye shapes.

### Main Deformation Inputs

| Input | Effect |
|-------|--------|
| Rigidbody2D velocity | Stretches along movement direction and adds a small rear lag |
| Impact pulse | Squashes on floor/wall impact then recovers |
| Fluid climb surface | Pulls/flatten body toward walls or ceiling |
| Contact points | Prevents visual boundary from crossing ground/walls |
| Move direction | Offsets eyes slightly toward movement |

---

## 4. Environment Fit

The renderer samples `targetCollider.GetContacts()` and builds contact planes from the real collision points and normals.

Each generated body vertex is processed in two stages:

1. Contact plane deformation:
   - Compresses vertices pointing into the contact surface.
   - Spreads vertices along the tangent.
   - Adds rounded-triangle weighting with shoulder bulge and apex taper.

2. Contact plane fitting:
   - Pushes any vertex that crosses the contact plane back outside by `contactPlaneSkin`.
   - Applies to both body and outline meshes.

This keeps the slime from visually sinking into floors and helps it read as having weight and volume.

---

## 5. Dynamic Circle Collider

`S_PlayerDynamicCollider` is the current physical middle ground before the capsule phase.

It adjusts the existing `CircleCollider2D`:

- Crouch/slick input shrinks the radius and shifts the offset down.
- Wall/ceiling attach shrinks the radius and offsets toward the contact surface.
- High speed applies small extra shrink.
- Impacts briefly shrink then recover.
- `keepSurfaceContact` nudges the Rigidbody2D as radius changes so the collider does not visibly detach from contact surfaces.

This is conservative and stable, but still limited because a circle cannot represent wide slick crawling or tall wall adhesion.

---

## 6. Planned Capsule Phase

The next phase upgrades the physical representation from a dynamic circle to a dynamic `CapsuleCollider2D`.

Target states:

| State | Collider Shape |
|-------|----------------|
| Normal | Near-round capsule/circle-like proportions |
| Crouch / slick | Horizontal capsule |
| Wall climb | Vertical capsule offset toward wall |
| Ceiling climb | Horizontal flattened capsule offset upward |

Capsule switching must be smoothed and guarded against wall penetration. The visual mesh should continue using contact-plane fitting while the capsule provides a better physical silhouette.

---

## 7. Prefab Source Of Truth

`Assets/Perfab/Pre_MainChar.prefab` is the source of truth for:

- `S_Player.useProceduralRenderer`
- `S_Player.proceduralRenderer`
- `S_Player.useDynamicCollider`
- `S_Player.dynamicCollider`
- Body `S_PlayerProceduralRenderer` parameters
- Body `S_PlayerDynamicCollider` parameters
- Fallback `sprites[]`

Scene overrides should be avoided for these rendering/collider settings unless a scene deliberately needs a different player variant.
