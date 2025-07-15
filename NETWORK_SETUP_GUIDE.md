# Network Setup Guide - Fixing Guard/Villager Glitching

## Problem Description
Guards and villagers appear to glitch or move differently for different players in your multiplayer game. This happens because:

1. **Missing Network Synchronization**: The AI scripts don't properly sync movement across the network
2. **Conflicting Movement Systems**: NavMeshAgent and network updates conflict with each other
3. **No Master Client Control**: All clients try to control AI behavior simultaneously
4. **Missing Lag Compensation**: Network latency causes position desyncs
5. **Animation Not Synchronized**: Animations are only visible to the master client

## Solution Overview

### 1. **Master Client Authority**
- Only the Master Client controls AI behavior
- Other clients receive and display synchronized movement
- Prevents conflicts between multiple clients

### 2. **Proper Network Synchronization**
- Added `IPunObservable` implementation
- Position and rotation synchronization with lag compensation
- Smooth interpolation for non-master clients

### 3. **NavMeshAgent Coordination**
- Master client controls NavMeshAgent
- Non-master clients disable NavMeshAgent to prevent conflicts
- Network position updates override local movement

### 4. **Animation Synchronization**
- Animation parameters are now synchronized across the network
- All players can see guard/villager animations
- Animations work even when master client changes

## Setup Instructions

### Step 1: Update Your Prefabs

For each Guard and Villager prefab:

1. **Add PhotonView Component** (if not already present)
   - Set `Synchronization` to `UnreliableOnChange`
   - Set `Ownership Transfer` to `Fixed`
   - Set `Group` to `0`

2. **Add NetworkSetupHelper Component**
   - This will automatically configure networking
   - Right-click the component → "Setup Networking"

3. **Verify Components**
   - Right-click NetworkSetupHelper → "Check Networking Setup"
   - Ensure all components are properly configured

### Step 2: Configure PhotonView Observed Components

Make sure your PhotonView observes these components:
- `GuardMovement` or `Villager` script
- `PhotonTransformView` (if using)

### Step 3: Test the Setup

1. **Build and Test Locally**
   - Run multiple instances of your game
   - Verify guards/villagers move smoothly for all clients
   - **Test animation visibility**: Have Player 1 die and verify Player 2 can still see guard animations

2. **Check Console Logs**
   - Look for networking setup messages
   - Verify no conflicts or errors

## Key Changes Made

### GuardMovement.cs
```csharp
// Added network synchronization
public class GuardMovement : MonoBehaviourPun, IPunObservable

// Master client control
if (PhotonNetwork.IsMasterClient) {
    // AI behavior here
} else {
    // Network interpolation here
}

// Animation synchronization
void UpdateAnimator() {
    if (PhotonNetwork.IsMasterClient) {
        // Calculate and store animation parameters
        networkHorizontal = horizontal;
        networkVertical = vertical;
        networkIsRunning = isRunning;
    } else {
        // Use network-synchronized animation parameters
        animator.SetFloat("Horizontal", networkHorizontal);
        animator.SetFloat("Vertical", networkVertical);
        animator.SetBool("IsRunning", networkIsRunning);
    }
}

// Network synchronization
public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
```

### Villager.cs
```csharp
// Similar changes as GuardMovement
// Master client controls AI and animations
// Non-master clients interpolate network position and animations
```

## Animation Synchronization

### What's Fixed
- **All players can see guard/villager animations**
- **Animations continue when master client dies**
- **Smooth animation transitions across network**
- **Boolean and float animation parameters synchronized**

### Animation Parameters Synchronized
- **Guards**: `Horizontal`, `Vertical`, `IsRunning`
- **Villagers**: `isWalking`, `isRunning`, `isCrouching`

### How It Works
1. **Master Client**: Calculates animation parameters and sends them over network
2. **Other Clients**: Receive animation parameters and apply them to local animators
3. **Network Updates**: Animation state is synchronized 20 times per second
4. **Lag Compensation**: Animation parameters are adjusted for network latency

## Network Settings

### Recommended Settings
- **Network Update Rate**: 20 updates/second
- **Position Threshold**: 0.1 units
- **Rotation Threshold**: 5 degrees
- **Synchronization**: UnreliableOnChange
- **Interpolation Speed**: 20 * Time.deltaTime
- **Animation Update Rate**: 10 updates/second

### Performance Optimization
- Increase update rate for smoother movement
- Decrease thresholds for more precise synchronization
- Use `ReliableDeltaCompressed` for critical AI state changes

## Troubleshooting

### Common Issues

1. **Still Glitching**
   - Check PhotonView observed components
   - Verify master client is controlling AI
   - Ensure NavMeshAgent is disabled on non-master clients

2. **AI Not Moving**
   - Check if PhotonNetwork.IsMasterClient is true
   - Verify NavMeshAgent is enabled on master client
   - Check NavMesh setup

3. **Network Lag**
   - Increase network update rate
   - Adjust interpolation speed
   - Check internet connection quality

4. **Animations Not Visible to Other Players**
   - Verify animation parameters are being sent in OnPhotonSerializeView
   - Check that non-master clients are receiving animation data
   - Ensure animator component is properly assigned

### Debug Commands
```csharp
// In NetworkSetupHelper component
Right-click → "Check Networking Setup"
Right-click → "Setup Networking"
Right-click → "Remove Networking Components"
```

## Advanced Configuration

### Custom Network Settings
You can adjust these values in the inspector:

```csharp
[Header("Network Settings")]
[SerializeField] private float networkUpdateRate = 20f;
[SerializeField] private float positionThreshold = 0.1f;
[SerializeField] private float rotationThreshold = 5f;
```

### Multiple AI Types
The system supports different AI behaviors:
- **Guards**: Patrol and chase players
- **Villagers**: Run away from players
- **Both**: Use master client authority

## Performance Considerations

1. **Update Rate**: Higher rates = smoother movement but more network traffic
2. **Thresholds**: Lower thresholds = more precise but more updates
3. **Interpolation**: Faster interpolation = less lag but more jitter
4. **Animation Sync**: Separate update rate for animations to balance performance

## Testing Checklist

- [ ] All guards/villagers have PhotonView components
- [ ] NetworkSetupHelper is configured
- [ ] Master client controls AI behavior
- [ ] Non-master clients show smooth movement
- [ ] **All players can see guard/villager animations**
- [ ] **Animations continue when master client dies**
- [ ] No console errors or warnings
- [ ] Movement is synchronized across all clients
- [ ] AI behavior works correctly (patrol, chase, run away)

## Support

If you continue to experience issues:
1. Check the console for error messages
2. Verify all components are properly attached
3. Test with different network conditions
4. Use the debug commands in NetworkSetupHelper
5. **Test animation visibility specifically when master client changes** 