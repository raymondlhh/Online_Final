// Designed by KINEMATION, 2025.

using System;
using System.Collections.Generic;
using KINEMATION.KAnimationCore.Runtime.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace KINEMATION.ProceduralRecoilAnimationSystem.Runtime
{
    public enum FireMode
    {
        Semi,
        Burst,
        Auto
    }
    
    public struct StartRest
    {
        public StartRest(bool x, bool y, bool z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
	
        public bool x;
        public bool y;
        public bool z;
    }

    public delegate bool ConditionDelegate();
    public delegate void PlayDelegate();
    public delegate void StopDelegate();
    
    public struct AnimState
    {
        public ConditionDelegate checkCondition;
        public PlayDelegate onPlay;
        public StopDelegate onStop;
    }
    
    [Serializable]
    public struct RecoilCurves
    {
        public VectorCurve semiRotCurve;
        public VectorCurve semiLocCurve;
        public VectorCurve autoRotCurve;
        public VectorCurve autoLocCurve;
        
        public static float GetMaxTime(AnimationCurve curve)
        {
            return curve[curve.length - 1].time;
        }

        public RecoilCurves(Keyframe[] keyFrame)
        {
            semiRotCurve = new VectorCurve(keyFrame);
            semiLocCurve = new VectorCurve(keyFrame);
            autoRotCurve = new VectorCurve(keyFrame);
            autoLocCurve = new VectorCurve(keyFrame);
        }
    }

    [HelpURL("https://kinemation.gitbook.io/scriptable-animation-system/recoil-system/recoil-animation")]
    public class RecoilAnimation : MonoBehaviour
    {
        public Quaternion OutRot { get; private set; }
        public Vector3 OutLoc { get; private set; }

        public bool isAiming;

        public RecoilAnimData RecoilData { get; private set; }
        private float _fireRate;
        public FireMode fireMode;
        private List<AnimState> _stateMachine;
        private int _stateIndex;

        private Vector3 _targetRot;
        private Vector3 _targetLoc;

        private VectorCurve _tempRotCurve;
        private VectorCurve _tempLocCurve;

        private Vector3 _startValRot;
        private Vector3 _startValLoc;

        private StartRest _canRestRot;
        private StartRest _canRestLoc;

        private Vector3 _rawRotOut;
        private Vector3 _rawLocOut;

        private Vector3 _smoothRotOut;
        private Vector3 _smoothLocOut;

        private Vector2 _noiseTarget;
        private Vector2 _noiseOut;

        private float _pushTarget;
        private float _pushOut;
    
        private float _lastFrameTime;
        private float _playBack;
        private float _lastTimeShot;
    
        private bool _isPlaying;
        private bool _isLooping;
        private bool _enableSmoothing;

        private Vector2 _pitchSway;
        private Vector2 _yawSway;
        
        private Vector2 _pitchProgress;
        private Vector2 _upProgress;
    
        public void Init(RecoilAnimData data, float fireRate, FireMode newFireMode)
        {
            fireMode = newFireMode;
            
            RecoilData = data;

            OutRot = Quaternion.identity;
            OutLoc = Vector3.zero;

            if (Mathf.Approximately(fireRate, 0f))
            {
                _fireRate = 0.001f;
                Debug.LogWarning("RecoilAnimation: FireRate is zero!");
            }

            _fireRate = fireRate;
            _targetRot = Vector3.zero;
            _targetLoc = Vector3.zero;

            _pushTarget = 0f;
            _noiseTarget = Vector2.zero;

            SetupStateMachine();
        }

        public void Play()
        {
            if (RecoilData == null) return;
            
            //Iterate through each transition, if true execute
            for (int i = 0; i < _stateMachine.Count; i++)
            {
                if (_stateMachine[i].checkCondition.Invoke())
                {
                    _stateIndex = i;
                    break;
                }
            }
        
            _stateMachine[_stateIndex].onPlay.Invoke();
            _lastTimeShot = Time.unscaledTime;
        }

        public void Stop()
        {
            if (RecoilData == null) return;

            _stateMachine[_stateIndex].onStop.Invoke();
            _isLooping = false;
        }

        private void Update()
        {
            if (RecoilData == null) return;
            
            if (_isPlaying)
            {
                UpdateSolver();
                UpdateTimeline();
            }
        
            Vector3 finalLoc = _smoothLocOut;
            Vector3 finalEulerRot = _smoothRotOut;
            
            ApplyNoise(ref finalLoc);
            ApplyPushback(ref finalLoc);
            ApplyProgression(ref finalLoc, ref finalEulerRot);
            
            Quaternion finalRot = Quaternion.Euler(finalEulerRot);

            Vector3 pivotOffset = isAiming ? RecoilData.aimPivotOffset : RecoilData.hipPivotOffset;
            finalLoc += finalRot * pivotOffset - pivotOffset;
            
            ApplySway(ref finalLoc, ref finalRot);

            OutRot = finalRot;
            OutLoc = finalLoc;
        }

        private void CalculateTargetData()
        {
            float pitch = Random.Range(RecoilData.pitch.x, RecoilData.pitch.y);
            float yawMin = Random.Range(RecoilData.yaw.x, RecoilData.yaw.y);
            float yawMax = Random.Range(RecoilData.yaw.z, RecoilData.yaw.w);

            float yaw = Random.value >= 0.5f ? yawMax : yawMin;

            float rollMin = Random.Range(RecoilData.roll.x, RecoilData.roll.y);
            float rollMax = Random.Range(RecoilData.roll.z, RecoilData.roll.w);

            float roll = Random.value >= 0.5f ? rollMax : rollMin;

            roll = _targetRot.z * roll > 0f && RecoilData.smoothRoll ? -roll : roll;

            float kick = Random.Range(RecoilData.kickback.x, RecoilData.kickback.y);
            float kickRight = Random.Range(RecoilData.kickRight.x, RecoilData.kickRight.y);
            float kickUp = Random.Range(RecoilData.kickUp.x, RecoilData.kickUp.y);

            _noiseTarget.x += Random.Range(RecoilData.noiseX.x, RecoilData.noiseX.y);
            _noiseTarget.y += Random.Range(RecoilData.noiseY.x, RecoilData.noiseY.y);

            _noiseTarget.x *= isAiming ? RecoilData.noiseScalar : 1f;
            _noiseTarget.y *= isAiming ? RecoilData.noiseScalar : 1f;

            pitch *= isAiming ? RecoilData.aimRot.x : 1f;
            yaw *= isAiming ? RecoilData.aimRot.y : 1f;
            roll *= isAiming ? RecoilData.aimRot.z : 1f;

            kick *= isAiming ? RecoilData.aimLoc.z : 1f;
            kickRight *= isAiming ? RecoilData.aimLoc.x : 1f;
            kickUp *= isAiming ? RecoilData.aimLoc.y : 1f;

            _targetRot = new Vector3(pitch, yaw, roll);
            _targetLoc = new Vector3(kickRight, kickUp, kick);
            
            // Compute target progression.
            float adsScalar = isAiming ? RecoilData.adsProgressAlpha : 1f;
            _pitchProgress.y += RecoilData.pitchProgress.amount * adsScalar;
            _upProgress.y += RecoilData.upProgress.amount * adsScalar;
            
            // Compute target recoil sway.
            var recoilSway = RecoilData.recoilSway;

            float value = Random.Range(recoilSway.pitchSway.x, recoilSway.pitchSway.y);
            if (isAiming) value *= recoilSway.adsScale;
            _pitchSway.y += value;

            value = Random.Range(recoilSway.yawSway.x, recoilSway.yawSway.y);
            if (isAiming) value *= recoilSway.adsScale;
            _yawSway.y += value;
        }

        private void UpdateTimeline()
        {
            _playBack += Time.deltaTime * RecoilData.playRate;
            _playBack = Mathf.Clamp(_playBack, 0f, _lastFrameTime);

            // Stop updating if the end is reached
            if (Mathf.Approximately(_playBack, _lastFrameTime))
            {
                if (_isLooping)
                {
                    _playBack = 0f;
                    _isPlaying = true;
                }
                else
                {
                    _isPlaying = false;
                    _playBack = 0f;
                }
            }
        }

        private void UpdateSolver()
        {
            if (Mathf.Approximately(_playBack, 0f))
            {
                CalculateTargetData();
            }
        
            // Current playback position
            float lastPlayback = _playBack - Time.deltaTime * RecoilData.playRate;
            lastPlayback = Mathf.Max(lastPlayback, 0f);

            Vector3 alpha = _tempRotCurve.GetValue(_playBack);
            Vector3 lastAlpha = _tempRotCurve.GetValue(lastPlayback);
            
            Vector3 output = Vector3.zero;

            output.x = Mathf.LerpUnclamped(
                CorrectStart(ref lastAlpha.x, alpha.x, ref _canRestRot.x, ref _startValRot.x),
                _targetRot.x, alpha.x);

            output.y = Mathf.LerpUnclamped(
                CorrectStart(ref lastAlpha.y, alpha.y, ref _canRestRot.y, ref _startValRot.y),
                _targetRot.y, alpha.y);

            output.z = Mathf.LerpUnclamped(
                CorrectStart(ref lastAlpha.z, alpha.z, ref _canRestRot.z, ref _startValRot.z),
                _targetRot.z, alpha.z);

            _rawRotOut = output;

            alpha = _tempLocCurve.GetValue(_playBack);
            lastAlpha = _tempLocCurve.GetValue(lastPlayback);

            output.x = Mathf.LerpUnclamped(
                CorrectStart(ref lastAlpha.x, alpha.x, ref _canRestLoc.x, ref _startValLoc.x),
                _targetLoc.x, alpha.x);

            output.y = Mathf.LerpUnclamped(
                CorrectStart(ref lastAlpha.y, alpha.y, ref _canRestLoc.y, ref _startValLoc.y),
                _targetLoc.y, alpha.y);

            output.z = Mathf.LerpUnclamped(
                CorrectStart(ref lastAlpha.z, alpha.z, ref _canRestLoc.z, ref _startValLoc.z),
                _targetLoc.z, alpha.z);

            _rawLocOut = output;
            ApplySmoothing();
        }

        private void ApplySmoothing()
        {
            if(_enableSmoothing)
            {
                Vector3 lerped = _smoothRotOut;

                Vector3 smooth = RecoilData.smoothRot;

                Func<float, float, float, float, float> Interp = (a, b, speed, scale) =>
                {
                    scale = Mathf.Approximately(scale, 0f) ? 1f : scale;
                    return Mathf.Approximately(speed, 0f)
                        ? b * scale
                        : Mathf.Lerp(a, b * scale, KMath.ExpDecayAlpha(speed, Time.deltaTime));
                };

                lerped.x = Interp(_smoothRotOut.x, _rawRotOut.x, smooth.x, RecoilData.extraRot.x);
                lerped.y = Interp(_smoothRotOut.y, _rawRotOut.y, smooth.y, RecoilData.extraRot.y);
                lerped.z = Interp(_smoothRotOut.z, _rawRotOut.z, smooth.z, RecoilData.extraRot.z);
                _smoothRotOut = lerped;

                lerped = _smoothLocOut;
                smooth = RecoilData.smoothLoc;
                
                lerped.x = Interp(_smoothLocOut.x, _rawLocOut.x, smooth.x, RecoilData.extraLoc.x);
                lerped.y = Interp(_smoothLocOut.y, _rawLocOut.y, smooth.y, RecoilData.extraLoc.y);
                lerped.z = Interp(_smoothLocOut.z, _rawLocOut.z, smooth.z, RecoilData.extraLoc.z);

                _smoothLocOut = lerped;
            }
            else
            {
                _smoothRotOut = _rawRotOut;
                _smoothLocOut = _rawLocOut;
            }
        }

        private void ApplyNoise(ref Vector3 finalized)
        {
            _noiseTarget.x = Mathf.Lerp(_noiseTarget.x, 0f, KMath.ExpDecayAlpha(RecoilData.noiseDamp.x, Time.deltaTime));
            _noiseTarget.y = Mathf.Lerp(_noiseTarget.y, 0f, KMath.ExpDecayAlpha(RecoilData.noiseDamp.y, Time.deltaTime));
	
            _noiseOut.x = Mathf.Lerp(_noiseOut.x, _noiseTarget.x, 
                KMath.ExpDecayAlpha(RecoilData.noiseAccel.x, Time.deltaTime));

            _noiseOut.y = Mathf.Lerp(_noiseOut.y, _noiseTarget.y, 
                KMath.ExpDecayAlpha(RecoilData.noiseAccel.y, Time.deltaTime));
            
            finalized += new Vector3(_noiseOut.x, _noiseOut.y, 0f);
        }

        private void ApplyPushback(ref Vector3 finalized)
        {
            _pushTarget = Mathf.Lerp(_pushTarget, 0f, 
                KMath.ExpDecayAlpha(RecoilData.pushDamp, Time.deltaTime));
            
            _pushOut = Mathf.Lerp(_pushOut, _pushTarget, 
                KMath.ExpDecayAlpha(RecoilData.pushAccel, Time.deltaTime));

            finalized += new Vector3(0f, 0f, _pushOut);
        }

        private void ApplyProgression(ref Vector3 translation, ref Vector3 rotation)
        {
            float alpha = KMath.ExpDecayAlpha(RecoilData.pitchProgress.acceleration, Time.deltaTime);
            _pitchProgress.x = Mathf.Lerp(_pitchProgress.x, _pitchProgress.y, alpha);
            
            alpha = KMath.ExpDecayAlpha(RecoilData.upProgress.acceleration, Time.deltaTime);
            _upProgress.x = Mathf.Lerp(_upProgress.x, _upProgress.y, alpha);
            
            alpha = KMath.ExpDecayAlpha(RecoilData.pitchProgress.damping, Time.deltaTime);
            _pitchProgress.y = Mathf.Lerp(_pitchProgress.y, 0f, alpha);
            
            alpha = KMath.ExpDecayAlpha(RecoilData.upProgress.damping, Time.deltaTime);
            _upProgress.y = Mathf.Lerp(_upProgress.y, 0f, alpha);

            translation.y += _upProgress.x;
            rotation.x += _pitchProgress.x;
        }

        private void ApplySway(ref Vector3 translation, ref Quaternion rotation)
        {
            float alpha = KMath.ExpDecayAlpha(RecoilData.recoilSway.acceleration, Time.deltaTime);
            _pitchSway.x = Mathf.Lerp(_pitchSway.x, _pitchSway.y, alpha);
            _yawSway.x = Mathf.Lerp(_yawSway.x, _yawSway.y, alpha);
            
            alpha = KMath.ExpDecayAlpha(RecoilData.recoilSway.damping, Time.deltaTime);
            _pitchSway.y = Mathf.Lerp(_pitchSway.y, 0f, alpha);
            _yawSway.y = Mathf.Lerp(_yawSway.y, 0f, alpha);

            Quaternion swayRotation = Quaternion.Euler(new Vector3(_pitchSway.x, _yawSway.x,
                _yawSway.x * RecoilData.recoilSway.rollMultiplier));
            Vector3 swayPosition = swayRotation * RecoilData.recoilSway.pivotOffset - RecoilData.recoilSway.pivotOffset;

            rotation *= swayRotation;
            translation += swayPosition;
        }
    
        private float CorrectStart(ref float last, float current, ref bool bStartRest, ref float startVal)
        {
            if (Mathf.Abs(last) > Mathf.Abs(current) && bStartRest && !_isLooping)
            {
                startVal = 0f;
                bStartRest = false;
            }
	
            last = current;
	
            return startVal;
        }
        
        private void SetupStateMachine()
        {
            _stateMachine ??= new List<AnimState>();

            AnimState semiState;
            AnimState autoState;

            semiState.checkCondition = () =>
            {
                float timerError = (60f / _fireRate) / Time.deltaTime + 1;
                timerError *= Time.deltaTime;
            
                if(_enableSmoothing && !_isLooping)
                {
                    _enableSmoothing = false;
                }
            
                return GetDelta() > timerError + 0.01f && !_isLooping || fireMode == FireMode.Semi;
            };

            semiState.onPlay = () =>
            {
                SetupTransition(_smoothRotOut, _smoothLocOut, RecoilData.recoilCurves.semiRotCurve, 
                    RecoilData.recoilCurves.semiLocCurve);
            };

            semiState.onStop = () =>
            {
                //Intended to be empty
            };

            autoState.checkCondition = () => true;

            autoState.onPlay = () =>
            {
                if (_isLooping)
                {
                    return;
                }
            
                var curves = RecoilData.recoilCurves;
                bool bCurvesValid = curves.autoRotCurve.IsValid() && curves.autoLocCurve.IsValid();

                _enableSmoothing = bCurvesValid;
                float correction = 60f / _fireRate;

                if (bCurvesValid)
                {
                    CorrectAlpha(curves.autoRotCurve, curves.autoLocCurve, correction);
                    SetupTransition(_startValRot, _startValLoc, curves.autoRotCurve, curves.autoLocCurve);
                }
                else if(curves.autoRotCurve.IsValid() && curves.autoLocCurve.IsValid())
                {
                    CorrectAlpha(curves.semiRotCurve, curves.semiLocCurve, correction);
                    SetupTransition(_startValRot, _startValLoc, curves.semiRotCurve, curves.semiLocCurve);
                }

                _pushTarget = RecoilData.pushAmount;
            
                _lastFrameTime = correction;
                _isLooping = true;
            };

            autoState.onStop = () =>
            {
                if (!_isLooping)
                {
                    return;
                }
                
                float tempRot = _tempRotCurve.GetCurveLength();
                float tempLoc = _tempLocCurve.GetCurveLength();
                _lastFrameTime = tempRot > tempLoc ? tempRot : tempLoc;
                _isPlaying = true;
            };

            _stateMachine.Add(semiState);
            _stateMachine.Add(autoState);
        }

        private void SetupTransition(Vector3 startRot, Vector3 startLoc, VectorCurve rot, VectorCurve loc)
        {
            if(!rot.IsValid() || !loc.IsValid())
            {
                Debug.Log("RecoilAnimation: Rot or Loc curve is nullptr");
                return;
            }
        
            _startValRot = startRot;
            _startValLoc = startLoc;
	
            _canRestRot = _canRestLoc = new StartRest(true, true, true);

            _tempRotCurve = rot;
            _tempLocCurve = loc;

            _lastFrameTime = rot.GetCurveLength() > loc.GetCurveLength() ? rot.GetCurveLength() : loc.GetCurveLength();
        
            PlayFromStart();
        }
    
        private void CorrectAlpha(VectorCurve rot, VectorCurve loc, float time)
        {
            Vector3 curveAlpha = rot.GetValue(time);
        
            _startValRot.x = Mathf.LerpUnclamped(_startValRot.x, _targetRot.x, curveAlpha.x);
            _startValRot.y = Mathf.LerpUnclamped(_startValRot.y, _targetRot.y, curveAlpha.y);
            _startValRot.z = Mathf.LerpUnclamped(_startValRot.z, _targetRot.z, curveAlpha.z);

            curveAlpha = loc.GetValue(time);
	
            _startValLoc.x = Mathf.LerpUnclamped(_startValLoc.x, _targetLoc.x, curveAlpha.x);
            _startValLoc.y = Mathf.LerpUnclamped(_startValLoc.y, _targetLoc.y, curveAlpha.y);
            _startValLoc.z = Mathf.LerpUnclamped(_startValLoc.z, _targetLoc.z, curveAlpha.z);
        }
    
        private void PlayFromStart()
        { 
            _playBack = 0f;
            _isPlaying = true;
        }
    
        private float GetDelta()
        {
            return Time.unscaledTime - _lastTimeShot;
        }
    }
}