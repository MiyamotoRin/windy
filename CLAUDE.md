# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Windy** is a minimalist Unity physics playground. Clicking anywhere on screen triggers an explosion force applied to nearby physics objects, with an animated orange highlight effect at the click point. The project targets Unity **2021.3.45f2 LTS** with the **Universal Render Pipeline (URP) 12.1.15**.

## Development Commands

Unity projects are built and run through the Unity Editor — there is no CLI build command configured. Open the project in Unity Hub by pointing it at this directory.

- **Play/Test:** Open `Assets/Scenes/SampleScene.unity` and press Play in the Unity Editor.
- **Build:** File → Build Settings → Build (Windows Standalone is the configured target).
- **Tests:** Window → General → Test Runner (uses `com.unity.test-framework` 1.1.33).

## Architecture

### Single Scene

Everything runs in `Assets/Scenes/SampleScene.unity`. There is one scene and no scene management system.

### Scripts

**`Assets/Scripts/Explosion.cs`** — the core mechanic, attached to Main Camera.
- On left-click, raycasts from camera into the scene.
- Calls `Physics.OverlapSphere()` and `Rigidbody.AddExplosionForce()` on all objects within `explosionRadius`.
- Dynamically adds `Rigidbody` to any collider that lacks one.
- Uses a `HashSet<Rigidbody>` to deduplicate force application.
- Spawns a temporary canvas overlay with an animated circular highlight (coroutine-based, scale + alpha tween).

**`Assets/Scripts/MoveCube.cs`** — attached to the Cube object; auto-adds `Rigidbody` if missing and rotates it on the Y-axis each frame.

### Key Parameters (Explosion.cs)

| Field | Default | Purpose |
|---|---|---|
| `explosionForce` | 12 | Force magnitude |
| `explosionRadius` | 3.5 | Sphere radius for affected objects |
| `upwardsModifier` | 0.3 | Upward bias on explosion |
| `maxRayDistance` | 300 | Max click raycast length |
| `highlightDuration` | 0.35s | Animated circle lifetime |

### Packages

- URP (`com.unity.render-pipelines.universal` 12.1.15)
- TextMesh Pro (`com.unity.textmeshpro` 3.0.6)
- Visual Scripting, Timeline (included but unused by current scripts)

## IDE Setup

VSCode is configured (`.vscode/settings.json`) with Unity file exclusions and an "Attach to Unity" debug launch config. Install the **Unity (VSTUC)** extension for debugging support.
