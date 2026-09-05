# Single player experiment

Open `Assets/Scenes/Game.unity`, enter Play Mode, and click **Play**. This creates one local player in the hub. Use the existing shrine to start the region, as before. No Steam client, lobby, host, server, or network connection is needed.

The existing gameplay components, movement settings, attacks, inventory, item/shard data, enemy spawning, room combat, region generation, and effects remain in place. Components now use `MonoBehaviour`, local state, direct method calls, and `Instantiate`/`Destroy`. GameManager creates the player using the former network player prefab. Scene and prefab script GUIDs were preserved for gameplay components.

Netcode components, transports, lobby scripts, and multiplayer packages were removed. The former network overlay retains its FPS/frame timing display. The unused Join button is inactive and has no callback.

Souls save to `playerdata.json` in `Application.persistentDataPath`, including in the editor. Currency changes persist locally. Existing Steam Cloud saves are not automatically imported or modified; this experiment starts with the local save, or zero souls if none exists. Health and inventory persistence have not been added.

## Validation

- C# compilation: `dotnet build Assembly-CSharp.csproj` passed (Unity-generated project files refreshed to exclude removed dependencies).
- Static scene/prefab validation: no newly dangling local references, no references to deleted script GUIDs, and the Play button/player prefab/camera references are wired.
- Gameplay scripts and package manifests checked for remaining multiplayer dependencies.
- An in-editor gameplay parity playtest has not been completed.

## Playtest

1. Enter through Play; confirm exactly one player, active first-person camera, hidden menu camera, movement, jumping, sprinting, sounds, and inventory camera.
2. Use weapon racks; equip both weapon slots and attach shards. Confirm pickups, melee, projectiles, hit reactions, debuffs, damage numbers, and death effects. A full inventory should leave a loot drop available.
3. Kill an enemy; confirm souls and their flight effect. Spend souls, then exit/re-enter Play Mode to check local persistence.
4. Use the shrine; check the transition, atmosphere, landmarks, and roaming spawns.
5. Enter a combat room; check its timer, kill counter, enemy spawns, and barrier opening after spawning finishes and all enemies die.

Unity regenerates ignored `.csproj` files. If your IDE still lists removed networking assemblies, regenerate project files from Unity.
