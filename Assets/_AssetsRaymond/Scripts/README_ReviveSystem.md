# Player Revive System

## Overview
The player revive system has been updated to use raycasting instead of trigger-based detection. This provides a more precise and reliable way to detect dead players for revival.

## How It Works

### PlayerReviver Script
- **Raycast Detection**: Uses a raycast from the player's camera to detect dead players within a specified range
- **Player Type Display**: Shows the character type (Jaden, Alice, Jack) in the revive prompt UI
- **F Key Activation**: Press F to revive the detected dead player

### Key Features
1. **Raycast Distance**: Configurable distance for detecting dead players (default: 5 units)
2. **Player Layer Mask**: Can be configured to only detect specific layers
3. **Character Type Detection**: Automatically detects and displays the character type of the dead player
4. **Visual Debugging**: Optional gizmo drawing in scene view for debugging

### Setup Requirements
1. **PlayerReviver Component**: Must be attached to the player GameObject
2. **Camera Reference**: Automatically finds the player's camera for raycasting
3. **UI Elements**: 
   - `revivePromptUI`: GameObject containing the revive prompt UI
   - `revivePromptText`: TextMeshProUGUI component for displaying the revive message

### Configuration
- **Revive Key**: Default is F, can be changed in the inspector
- **Raycast Distance**: Default is 5 units, adjust based on your game's scale
- **Player Layer Mask**: Set to detect only player layers (default: -1 for all layers)

### UI Message Format
The revive prompt displays: `"Press F to Revive [PlayerName] ([CharacterType])"`

Examples:
- "Press F to Revive Player1 (Jaden)"
- "Press F to Revive Player2 (Alice)"
- "Press F to Revive Player3 (Jack)"

### Character Type Detection
The system automatically detects character types by checking the Photon room properties:
- Jaden: `"JadenChosen"` property matches player's ActorNumber
- Alice: `"AliceChosen"` property matches player's ActorNumber  
- Jack: `"JackChosen"` property matches player's ActorNumber

### Migration from Old System
The old trigger-based system using DeadMarkTrigger has been removed. The new system:
- No longer requires DeadMark prefabs
- No longer uses trigger colliders
- Provides more precise targeting
- Shows character information in the UI

### Debugging
- Enable gizmos in scene view to see the raycast line
- Check console for error messages if camera is not found
- Verify player layer mask settings if raycast is not detecting players 