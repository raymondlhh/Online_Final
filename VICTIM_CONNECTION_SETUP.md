# Victim Connection System Setup

## Overview
The connect skill has been modified to support connecting victim objects to the player. When a player uses the connect skill on a victim, the victim will be attached to the player's connection slot and follow the player around.

## Changes Made

### 1. PlayerConnector.cs
- Added support for detecting Victim objects in the `TryConnect()` method
- Added `connectedVictim` field to track victim connections
- Updated `CancelConnection()` to handle victim disconnections
- Victims are connected to the player's existing `connectionSlot`

### 2. Victim.cs
- Added connection functionality with `isConnected` state
- Added RPC methods `GetConnectedToPlayer()` and `ForceDetachFromPlayer()`
- Added `VictimConnectionLifetime()` coroutine to move victim to player's slot
- Victim continuously moves to player's connection slot position during connection

### 3. PlayerSkillDetails.cs
- No changes needed - the existing connect skill logic works with both players and victims

## Setup Instructions

### For Victim Prefabs:
1. Ensure your victim prefabs have a `PhotonView` component
2. Add the `Victim` script component to each victim prefab
3. The victim will automatically move to the player's connection slot when connected

### For GameManager:
1. Make sure your `victimPrefabs` array in GameManager is populated with the victim prefabs
2. Ensure victim spawners are assigned in the `victimSpawners` array

### For Player Prefabs:
1. Ensure the player prefab has the `PlayerConnector` component
2. Set up the connect skill in `PlayerSkillDetails` by checking `isConnectMovementSkill`
3. Assign the appropriate UI elements for connection feedback

## How It Works

1. **Detection**: When a player uses the connect skill, the system first checks for Victim objects, then Player objects
2. **Connection**: If a victim is found, the victim is connected to the player's connection slot
3. **Movement**: The victim continuously moves to the player's connection slot position during the connection duration
4. **Duration**: The connection lasts for the skill duration, during which the victim follows the player
5. **Cancellation**: Players can cancel the connection early by pressing the skill key again
6. **Cleanup**: When the duration expires or connection is cancelled, the victim is detached and stops following

## Usage
- Aim at a victim or player and press the connect skill key
- The victim will be connected to the player and follow them around
- Press the skill key again to cancel the connection early
- The connection skill will go on cooldown after use

## Notes
- Victims must have both `PhotonView` and `Victim` components
- The victim will follow the player's connection slot position (typically above the player)
- The system works with the existing UI and cooldown systems
- Player-to-player connections work as before, victim connections are new functionality
- Victims update their position every 0.1 seconds to smoothly follow the player 