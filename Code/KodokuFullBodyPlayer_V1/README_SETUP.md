# Kodoku Full Body Player — V1

## Scope

This is the first implementation milestone for the Kodoku full-body FPS player.

It intentionally does **not** use:

- `Sandbox.PlayerController`
- `Sandbox.CharacterController`
- `Sandbox.MoveMode`
- `Sandbox.BaseCombatWeapon`
- `Sandbox.ClothingContainer`

It uses the low-level engine primitives that remain necessary:

- `Rigidbody`
- `CapsuleCollider`
- `BoxCollider`
- `CameraComponent`
- `SkinnedModelRenderer`
- traces, animation and networking

## Important cleanup

Delete every previous prototype before copying this folder:

```text
Code/KodokuPlayerController.cs
Code/KodokuPlayerController_fixed.cs
Code/KodokuPlayerControllerV2/
KodokuPlayerControllerV2_Combined.cs
```

Do not keep two generations of the player code in the project.

## Current asset facts

`kodokan(25).vanmgrph` currently declares:

```text
move_x   float  [-100, 100]
move_y   float  [-100, 100]
duck     float  [0, 1]
aim_head Vector3
b_attack bool   auto-reset
b_reload bool   auto-reset
b_aim    bool
holdtype enum   none=0, rifle=1, pistol=2
Sanity   float  [0, 100]
Sadness  float  [0, 100]
```

`sebastian(7).vmdl` currently contains `head`, `hand_r`, `hand_l`,
`foot_r` and `foot_l`.

The submitted VMDL does not currently contain `aim_eye.r`.
The camera code falls back to the head bone and logs one warning until that
technical bone is added and the model is rebuilt.

## Recommended prefab

```text
KodokanPlayer
├── Components
│   ├── Rigidbody
│   ├── CapsuleCollider
│   ├── BoxCollider
│   ├── KodokuPlayerState
│   ├── KodokuCharacterMotor
│   ├── KodokuPlayerInput
│   ├── KodokuAnimatorDriver
│   ├── KodokuPlayerCamera
│   ├── KodokuFirstPersonVisibility
│   └── KodokuSocketController
│
├── Body
│   └── SkinnedModelRenderer
│
├── HeadGroup
│   ├── Head
│   │   └── SkinnedModelRenderer
│   ├── Hair
│   ├── Helmet
│   └── Mask
│
└── Sockets
    ├── HeadSocket
    │   └── KodokuBoneSocket (BoneName = head)
    ├── FaceSocket
    │   └── KodokuBoneSocket (BoneName = head + offset)
    ├── BackSocket
    │   └── KodokuBoneSocket (BoneName = upperchest + offset)
    ├── RightHandSocket
    │   └── KodokuBoneSocket (BoneName = hand_r)
    └── LeftHandSocket
        └── KodokuBoneSocket (BoneName = hand_l)
```

Assign all references explicitly in the Inspector after creating the prefab.

## Rigidbody

The motor configures:

```text
Gravity = enabled
Motion = enabled
Mass = 500
Pitch/Roll/Yaw = locked
```

The root object stays physically upright. Visual yaw is applied to the body
renderer by `KodokuAnimatorDriver`.

## Camera

Normal first-person position:

```text
CameraBoneName = head
```

Aiming position:

```text
AimEyeBoneName = aim_eye.r
AimEyeFallbackBoneName = aim_eye_r
```

The camera interpolates from the animated `head` transform to the animated
`aim_eye.r` transform when `b_aim` becomes active.

`HeadRotationInfluence` controls how much animated head movement is added to
mouse-controlled `EyeAngles`.

## FPS visibility

Place these roots in `KodokuFirstPersonVisibility.HiddenRoots`:

```text
HeadGroup
any independent hair root
any independent helmet root
any independent mask root
```

Every object receives the tag:

```text
fps_hide_head
```

Only the local Kodoku camera excludes that tag. Other players and spectator
cameras continue to render the head equipment.

## AnimGraph variables

The single source of truth is:

```text
Code/Kodoku/Player/Animation/KodokuAnimatorDriver.Parameters.cs
```

No other component should write raw AnimGraph parameter strings.

## Holdtype test

The temporary mouse-wheel order is:

```text
None -> Pistol -> Rifle -> None
```

The underlying enum values remain aligned with the AnimGraph:

```text
None   = 0
Rifle  = 1
Pistol = 2
```

Later, the wheel will select the networked hotbar and the active item will
provide the holdtype.

## Equipment

Use `KodokuEquipmentAttachment`:

- `BoneSocket` for weapons, rigid helmets, masks, backpacks, hair roots and props.
- `BoneMerge` for skinned armor and clothing that use the exact Kodokan skeleton.

Several objects may be children of the same socket.

## Known first-milestone limitations

Not implemented yet:

- final hotbar integration
- hair state graph (`Free`, `Compressed`, `Hidden`)
- ragdoll camera
- final fall damage
- swimming and ladders
- final foot IK
- two-player validation
- production equipment spawning/despawning

The movement motor is a first custom Rigidbody implementation and must be
tested in S&box before tuning step, slope, friction and capsule values.
