# OneMoreSip — Unity 2D Prototype

A fully playable vertical-slice prototype built entirely from primitive shapes. Everything
(camera, floor, player, NPCs, bottles, UI, both "scenes") is generated **at runtime** — there
is nothing to wire up in the editor. Press **Play** and it works.

## Setup (one minute)

1. Create a new **Unity 2D** project (Unity **2021.3 LTS or newer** recommended).
2. Copy the `Assets/OneMoreSip` folder into your project's `Assets` folder.
3. Press **Play** on *any* scene — even an empty one.

> How it works: `GameBootstrap` uses `[RuntimeInitializeOnLoadMethod]` to spawn the
> `GameManager` the instant Play is pressed, which then builds the whole game. No prefabs,
> no Build Settings, no manual setup.

## Controls

Walking mode:
- `A` / `D` — move left / right
- `E` — drink (takes 3 seconds)
- `Shift` — hide the bottle (5s, 1s cooldown)
- Reach the **blue dorm** on the far right

Pee minigame:
- `W` / `S` — aim up / down (angle −30° to +60°)
- `Hold Space` — continuous pee stream

## Game flow

**Scene 1 — Walking:** Pick up green bottles (one at a time — finish the current one first),
drink to raise Drunk Level (+5 per sip, bottle −5), and avoid the gray NPCs while a bottle is
visible or you're drinking. The map is ~48 units long with three widely-spaced NPC patrol
zones. At Drunk Level ≥ 30, random drunk effects kick in every 6s for 4s each
(screen shake, double vision, reversed controls, input delay, slow actions).

**Reaching the dorm:**
- Drunk Level < 40 → "not that drunk, still anxious" ending.
- Drunk Level ≥ 40 → **Scene 2 — Pee minigame**.

**Scene 2 — Pee:** Pee Amount = Drunk Level × 10 particles. Aim and fire at the
painting (+50), urinal (+20), trash can (+10). Hitting anything else — the floor or the back
wall (i.e. any miss) — is −10. Each zone scores at most once per 0.5s. When Pee Amount hits 0,
the run ends with the final scoreboard and a Restart button.

## Placeholder color key

| Element | Shape / Color |
|---|---|
| Player | White square |
| NPC | Gray square |
| Alcohol bottle | Green square |
| Dorm entrance | Blue rectangle |
| Painting target | Red square |
| Urinal target | Cyan square |
| Trash can target | Yellow square |
| Floor | Dark gray rectangle |
| Pee particle | Small yellow circle |

## Scripts

| Script | Role |
|---|---|
| `GameBootstrap` | Runtime entry point (spawns GameManager on Play) |
| `GameManager` | Global state + builds both scenes + transitions |
| `PrimitiveFactory` | Generates all placeholder sprites/objects |
| `PlayerController` | Walking movement + action input |
| `BottleSystem` | Pickup, drinking, hiding, anti-waste rule |
| `BottlePickup` | World bottle → hands itself to BottleSystem |
| `NPCDetection` | Patrol + 3-unit catch detection |
| `DrunkEffectManager` | Picks/runs the 5 drunk effects |
| `DoubleVisionGhost` | Fading duplicate for the double-vision effect |
| `CameraShake` | Cinemachine-free camera shake |
| `DormTrigger` | Ends the walk / enters pee mode |
| `PeeController` | Aiming + firing the pee stream |
| `PeeParticle` | Single droplet (physics + lifetime) |
| `PeeTarget` | Scoring zone with 0.5s cooldown |
| `ScoreManager` | Applies point values, tracks floor hits |
| `UIManager` | All runtime UI + ending screen |

## Notes

- No `Time.timeScale` is used; the "slow actions" effect only scales player actions.
- Uses legacy uGUI `Text` (no TextMeshPro import required).
- `Rigidbody2D.velocity` is used for particles; on Unity 6 this shows a deprecation
  warning (still compiles) — rename to `linearVelocity` if you target Unity 6 only.
