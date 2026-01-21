# Hold the Line - Unity Integration Guide

## Project Setup (Unity 2021.3+ LTS recommended)

### 1. Initial Unity Setup

1. Create new Unity 3D project (not URP/HDRP - Standard RP is fine for hyper-casual)
2. Set build target to iOS: File → Build Settings → iOS → Switch Platform
3. Configure Player Settings:
   - Portrait orientation only
   - Disable "Auto Rotate"
   - Set Target Frame Rate to 60 in GameManager (already done in code)

### 2. Scene Hierarchy Setup

Create this hierarchy in your main scene:

```
Game (Scene)
├── Main Camera
│   └── (Position: 0, 0, -10)
│   └── (Orthographic Size: 6 for portrait)
│   └── (Clear Flags: Solid Color)
├── --- MANAGERS ---
├── GameManager (Empty GO)
│   └── [GameManager.cs]
├── ObjectPoolManager (Empty GO)
│   └── [ObjectPool.cs]
├── SpawnerManager (Empty GO)
│   └── [SpawnerManager.cs]
├── --- GAMEPLAY ---
├── Player
│   └── [PlayerController.cs]
│   └── [PlayerHealth.cs]
│   └── [WeaponSystem.cs]
│   └── [TargetingSystem.cs]
│   └── (Collider2D - Box, IsTrigger)
│   └── (Tag: "Player")
│   └── FirePoint (child, empty GO at gun position)
├── --- UI ---
└── Canvas
    └── [UIHUD.cs]
    └── (Canvas Scaler: Scale With Screen Size, 1080x1920, Match 0.5)
    └── HUD Elements...
```

### 3. Create Prefabs

#### 3.1 Player Prefab
```
Player (Empty GO)
├── PlayerModel (Cube or capsule, scaled)
│   └── MeshRenderer → PlayerMat
├── Components on root:
│   - PlayerController
│   - PlayerHealth (assign PlayerModel to Renderer field)
│   - WeaponSystem (assign FirePoint)
│   - TargetingSystem
│   - BoxCollider2D (IsTrigger = true, size ~1x1)
├── FirePoint (Empty GO, position at top of player)
└── Tag: "Player"
```

#### 3.2 Bullet Prefab
```
Bullet (Empty GO or Quad)
├── MeshRenderer or SpriteRenderer
├── Components:
│   - Bullet.cs
│   - BoxCollider2D or CircleCollider2D (IsTrigger = true, small ~0.2)
├── Layer: "Bullet" (create this layer)
└── Scale: Small (~0.1, 0.3, 0.1)
```

#### 3.3 Zombie Prefab
```
Zombie (Cube or Capsule)
├── MeshRenderer → ZombieMat
├── Components:
│   - ZombieUnit.cs (assign Renderer)
│   - BoxCollider2D (IsTrigger = true, ~0.8x0.8)
├── Layer: "Enemy" (create this layer)
└── Scale: ~0.8x0.8x0.8
```

#### 3.4 Pickup Prefab
```
Pickup (Sphere or custom shape)
├── MeshRenderer → PickupMat
├── (Optional) TextMesh for label
├── Components:
│   - PickupMultiplier.cs (assign Renderer)
│   - CircleCollider2D (IsTrigger = true, ~0.6)
├── Layer: "Pickup"
└── Scale: ~0.5x0.5x0.5
```

#### 3.5 UpgradeTarget Prefab
```
UpgradeTarget (Cube representing crate/drone)
├── MeshRenderer → UpgradeTargetMat
├── HealthBar (child)
│   └── Quad scaled for health bar
├── Components:
│   - UpgradeTarget.cs (assign Renderer and HealthBar)
│   - BoxCollider2D (IsTrigger = true, ~1.2x1.2)
├── Layer: "UpgradeTarget"
└── Scale: ~1x1x1
```

### 4. Layer Setup

Create these layers (Edit → Project Settings → Tags and Layers):
- Layer 8: Player
- Layer 9: Enemy
- Layer 10: Bullet
- Layer 11: Pickup
- Layer 12: UpgradeTarget

### 5. Physics 2D Settings (Edit → Project Settings → Physics 2D)

Configure Layer Collision Matrix:
- Bullet ↔ Enemy: ✓
- Bullet ↔ UpgradeTarget: ✓
- Player ↔ Enemy: ✓
- Player ↔ Pickup: ✓
- Everything else: ✗ (uncheck for performance)

### 6. Object Pool Setup

On ObjectPoolManager, assign:
- Bullet Prefab
- Zombie Prefab
- Pickup Prefab
- Upgrade Target Prefab

Adjust initial pool sizes if needed (defaults: 100 bullets, 30 zombies, 10 pickups, 5 upgrade targets)

### 7. UI Canvas Setup

Create Canvas with these elements:

```
Canvas (Screen Space - Overlay)
├── UIHUD.cs
├── HUD_Group (Active during gameplay)
│   ├── HealthBar (Slider, top-left)
│   ├── HealthText (Text)
│   ├── WaveText (Text, top-center)
│   ├── WaveProgressSlider (Slider)
│   ├── WaveProgressText (Text)
│   ├── WeaponTierText (Text, top-right)
│   ├── DamageMultiplierText (Text)
│   ├── KillCountText (Text)
│   ├── GameTimeText (Text)
│   ├── TargetingIndicator (GameObject with Text)
│   └── UpgradeProgressSlider (Slider, shows when targeting upgrade)
├── MenuPanel
│   ├── TitleText ("HOLD THE LINE")
│   └── StartButton
├── FailPanel
│   ├── FinalScoreText
│   ├── FinalWaveText
│   ├── RetryButton
│   └── MenuButton
├── WaveTransitionPanel
│   └── WaveTransitionText
└── UpgradeObtainedPanel
    └── UpgradeObtainedText
```

### 8. Materials (Low-Poly Style)

Create simple unlit or standard materials:
- PlayerMat: Blue-ish
- ZombieMat: Green/Gray
- BulletMat: Yellow/White
- PickupMat: Bright colors
- UpgradeTargetMat: Orange/Gold

---

## Playable Prototype Checklist

Use this checklist to verify your prototype works correctly:

### Core Setup
- [ ] Project builds without errors
- [ ] Scene loads with all managers present
- [ ] Camera is orthographic, portrait aspect ratio (~9:16)
- [ ] ObjectPool has all 4 prefabs assigned

### Menu State
- [ ] Menu panel shows on game start
- [ ] Start button is clickable
- [ ] Clicking Start transitions to Playing state

### Player Movement
- [ ] Player is visible near bottom of screen
- [ ] Touch/mouse drag moves player horizontally
- [ ] Player is clamped to playfield bounds
- [ ] Movement feels smooth (no jitter)

### Weapon System
- [ ] Bullets fire automatically when playing
- [ ] Bullets move upward (or toward targets)
- [ ] Bullets despawn when off-screen
- [ ] Multiple weapon tiers have different characteristics

### Zombie Spawning
- [ ] Zombies spawn above the visible area
- [ ] Zombies move downward toward player
- [ ] Zombies take damage from bullets
- [ ] Zombies flash when hit
- [ ] Zombies die and return to pool when health depleted
- [ ] Kill count increases

### Wave System
- [ ] Wave number displays correctly
- [ ] Zombies spawn faster/stronger in later waves
- [ ] Wave completes when all zombies killed
- [ ] Wave transition message shows
- [ ] Next wave starts after transition

### Pickup System
- [ ] Pickups spawn periodically
- [ ] Pickups move downward with oscillation
- [ ] Pickups have visible type labels (x2, +1, etc.)
- [ ] Collecting pickup grants bonus
- [ ] Damage multiplier shows in UI when active

### Upgrade Target System
- [ ] Upgrade targets spawn periodically
- [ ] Targets float and drift slowly
- [ ] Tapping a target switches fire to it
- [ ] Target shows health/selection visual
- [ ] Destroying target upgrades weapon
- [ ] "UPGRADED" message displays
- [ ] Fire returns to zombies after target destroyed
- [ ] Tap elsewhere to deselect target

### Damage & Health
- [ ] Player health bar displays correctly
- [ ] Zombie contact damages player
- [ ] Player flashes red when damaged
- [ ] Health reaching 0 triggers Fail state

### Fail State
- [ ] Fail panel shows with score/wave
- [ ] Retry button restarts game
- [ ] Menu button returns to menu
- [ ] All entities cleared on restart

### Performance (Test on Device)
- [ ] Consistent 60 FPS
- [ ] No stuttering during heavy spawning
- [ ] No memory warnings
- [ ] Touch input is responsive

---

## Quick Test Checklist (5-minute smoke test)

1. [ ] Press Play in Editor
2. [ ] Click Start
3. [ ] Drag mouse left/right - player moves
4. [ ] Bullets fire automatically
5. [ ] Zombies spawn and approach
6. [ ] Kill a few zombies - count updates
7. [ ] Wait for pickup - collect it
8. [ ] Wait for upgrade target - tap it - weapon upgrades
9. [ ] Let zombies hit you - health decreases
10. [ ] Die - fail screen appears
11. [ ] Click Retry - game restarts cleanly

---

## Mobile Build Notes

### iOS Build Settings
- Minimum iOS Version: 12.0
- Target SDK: Device SDK
- Architecture: ARM64
- Strip Engine Code: Yes
- Scripting Backend: IL2CPP
- Managed Stripping Level: Medium

### Performance Optimization Already Implemented
- Object pooling for all spawned entities
- Cached Transform references
- No per-frame allocations (using stacks, not lists for pools)
- Simple 2D colliders
- No complex shaders

### Additional Recommendations
- Use Texture compression (ASTC)
- Disable unused physics layers
- Profile with Unity Profiler before release
- Consider using Unity's Addressables for larger asset management

---

## Interaction Model Reference

**Model B: Tap-to-Toggle (Implemented)**

| Action | Result |
|--------|--------|
| Drag anywhere | Move player horizontally |
| Tap upgrade target | Focus fire on it |
| Tap elsewhere (while targeting) | Return to zombie targeting |
| Target destroyed | Auto-return to zombies |
| Timeout (10s) | Auto-return to zombies |

This model allows one-thumb play: drag to move, tap to toggle targeting, seamless switching.

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Bullets don't hit zombies | Check layer collision matrix, ensure triggers enabled |
| Player doesn't move | Check PlayerController is attached and enabled |
| Nothing spawns | Ensure ObjectPool has prefabs assigned, SpawnerManager is in scene |
| UI not updating | Check UIHUD event subscriptions, ensure singletons exist |
| Pool exhausted warnings | Increase initial pool sizes |
| Low framerate | Profile, reduce particle effects, check draw calls |
