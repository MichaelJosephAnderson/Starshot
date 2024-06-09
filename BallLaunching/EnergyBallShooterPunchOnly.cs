using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnergyBallShooterPunchOnly : MonoBehaviour
{
    #region Singleton

    public static EnergyBallShooterPunchOnly instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }
    }

    #endregion

    #region Events

    public static event Action PlayerGrabbingHandle; 
    public static event Action PlayerShootBall; 

    #endregion
    
    #region Fields

    [Header("General")]
    [SerializeField, Tooltip("Reference to the Predictor Trail Component")]
    private EnergyBallPredictorTrail predictorTrail;

    [SerializeField, Tooltip("Reference to the Pull Movement script")]
    private PullMovement pullMovement;

    [Header("Values")]
    [SerializeField, Tooltip("Player position offset from middle to cast the shoot from ray from a better location")]
    private Vector3 playerPositionOffset = new Vector3(0, .25f, 0);
    
    [SerializeField, Tooltip("What to multiply the one frame velocity by")]
    private float launchMultiplier = 40;
    [SerializeField, Tooltip("The max speed you can launch the ball at")]
    public float maxLaunchSpeed = 5;
    [SerializeField, Tooltip("The min speed you can launch the ball at")]
    private float minLaunchSpeed = .5f;
    [SerializeField, Tooltip("The min speed you have to punch to launch the ball")]
    private float minPunchVelocity = .5f;
    
    [SerializeField, Tooltip("")]
    private float punchDistance = 2;
    
    [SerializeField, Tooltip("At what distance we will lock in where the hand is headed")]
    private float lockAngleDistance = .25f;
    
    [SerializeField, Tooltip("At what Velocity we will lock in where the hand is headed")]
    private float lockAngleVelocity = .25f;

    [SerializeField, Tooltip("This value controls how strong the curve of the ball should be when hit to the side")] 
    private float curveMultiplier = 2;

    [SerializeField, Tooltip("This value controls how accurate to 'straight on' the hit must be to hit it straight 0-1")]
    private float straightHitAccuracy = .85f;

    [SerializeField, Tooltip("How long after hitting the ball will it take to reset the hands to active so you dont start moving right away")]
    private float postLaunchWaitTime = 1;

    [SerializeField, Tooltip("How smooth should the aim rotation be")]
    private float aimRotateSpeed = 5f;

    [SerializeField, Tooltip("Aim dead zone size determines how far the player has to move their wrist to move the shoot direction")]
    private float aimDeadZoneSize = .99995f;

    [Header("Sound")]

    [SerializeField, Tooltip("Ball launch sound")]
    private FMODUnity.StudioEventEmitter launchSound;

    private EnergyBallMovement _currentSpawnedBall;
    private Transform _predictorTrailStart;
    private Vector3 _playerPosition;
    private Vector3 _shootDirection;

    private Vector3 _handStartPoint = Vector3.zero;
    private Vector3 _lastHandPoint = Vector3.zero;
    private Vector3 _handMovementVector;
    private Transform _leftHand, _rightHand;
    private float _distanceToBall = float.MaxValue;
    
    private bool _isGrabbingHandle;
    private bool _lockAngle = false;
    private bool _isRight;
    private bool _canPunch = false;
    private float _postGrabPunchCooldown = .05f;
    private bool _postGrabPunch = false;
    private bool _postGrabReset = false;

    private Transform _grabbingHandTransform;
    private ObjectVelocity _grabbingHandObjectVelocity;
    private bool _grabbingWithRight;

    private bool _curveShots = true;
    private bool _isEndGame;
    private bool _canInteract = true;

    #endregion

    #region Initialize

    private void OnEnable()
    {
        BallSpawnList.ToggleCurveShots += ToggleCurveShots;
        EndGameBallHolder.BallInPosition += DisableInteractions;
        HubManager.HandlesSet += SetEndGame;
    }

    private void OnDisable()
    {
        BallSpawnList.ToggleCurveShots -= ToggleCurveShots;
        EndGameBallHolder.BallInPosition -= DisableInteractions;
        HubManager.HandlesSet -= SetEndGame;
    }
    
    private void Start()
    {
        PhysicsHand[] hands = FindObjectsOfType<PhysicsHand>();
        foreach (var hand in hands)
        {
            if (hand.GetComponent<HandChecker>().isRightHand) _rightHand = hand.transform;
            if (!hand.GetComponent<HandChecker>().isRightHand) _leftHand = hand.transform;
        }
    }


    #endregion
    
    #region Monobehavior

    private void Update()
    {
        _playerPosition = PlayerManager.instance.GetPlayerHeadPosition().position;
        _playerPosition -= playerPositionOffset;

        if (_grabbingHandTransform != null)
        {
            float aimDot = _grabbingWithRight
                ? Vector3.Dot(_shootDirection.normalized, -_grabbingHandTransform.right.normalized)
                : Vector3.Dot(_shootDirection.normalized, _grabbingHandTransform.right.normalized);
            //Debug.Log(aimDot);
            if (aimDot < aimDeadZoneSize)
            {
                float singleStep = aimRotateSpeed * Time.deltaTime;
                _shootDirection = _grabbingWithRight 
                    ? Vector3.RotateTowards(_shootDirection.normalized, -_grabbingHandTransform.right.normalized, singleStep, 100.0f) 
                    : Vector3.RotateTowards(_shootDirection.normalized, _grabbingHandTransform.right.normalized, singleStep, 100.0f);
            }
        }
        else
        {
            if (_currentSpawnedBall != null)
            {
                _shootDirection = _currentSpawnedBall.transform.position - _playerPosition;
            }
        }

        Debug.DrawRay(_playerPosition, _shootDirection);

        if (_currentSpawnedBall != null)
        {
            if (Vector3.Distance(_playerPosition, _currentSpawnedBall.transform.position) <= punchDistance)
            {
                if (_isGrabbingHandle)
                {
                    predictorTrail.SetPredictorTrailShootFromTransform(_currentSpawnedBall.transform.position, _shootDirection);
                    predictorTrail.SetPunchingHandPosition(!_grabbingWithRight? pullMovement.GetRightHandPosition() : pullMovement.GetLeftHandPosition());
                    predictorTrail.ShowPredictorTrail();
                
                    CheckHandPosition();
                    PlayerGrabbingHandle?.Invoke();
                }
                else
                {
                    predictorTrail.HidePredictorTrail();
                }

                _currentSpawnedBall.SetBallGeoRotation(_shootDirection);
                _currentSpawnedBall.SetHandlesActive();
            }
            else
            {
                _lockAngle = false;
                _canPunch = false;
                _postGrabPunch = false;
                _postGrabReset = true;
                predictorTrail.HidePredictorTrail();
                _currentSpawnedBall.SetHandlesDeactive();
            }
        }
    }

    private void OnDrawGizmos()
    {
        if(_currentSpawnedBall)
            Gizmos.DrawWireSphere(_currentSpawnedBall.transform.position, lockAngleDistance);
    }

    #endregion

    #region PublicMethods
    
    public void LaunchBallWithVel(GameObject col)
    {
        var vel = col.GetComponent<ObjectVelocity>().oneFrameVelocity.magnitude * launchMultiplier;
        var localVel = col.GetComponent<ObjectVelocity>().oneFrameLocalVelocity.magnitude * launchMultiplier;
        //Debug.LogWarning(vel);
        if(vel <= minPunchVelocity || localVel <= minPunchVelocity) return;

        vel = Mathf.Clamp(vel, minLaunchSpeed, maxLaunchSpeed);
        
        //Debug.Log(vel);
        _currentSpawnedBall.SetBallSpeed(vel, true);

        var colPos = _handStartPoint;
        var ballPos = _currentSpawnedBall.transform.position;
        var colDir = (ballPos - colPos).normalized;
        var point1 = ballPos + (colDir * curveMultiplier);
        var dot = Vector3.Dot(_shootDirection.normalized, colDir);
        ShootBall(_shootDirection);
        if (dot <= straightHitAccuracy && _curveShots && !_isEndGame)
        {
            _currentSpawnedBall.SetCurveMovementParams(colDir);
        }

        _isGrabbingHandle = false;
        _canPunch = false;
        _postGrabPunch = false;
        _postGrabReset = true;
        PlayerShootBall?.Invoke();
        PullMovement.instance.BreakGrab();
    }

    public void SetNewBallRef(GameObject newRef)
    {
        _currentSpawnedBall = newRef.GetComponent<EnergyBallMovement>();
        predictorTrail.SetNewBallRef(_currentSpawnedBall);
    }

    public void AnimateBallDestruction()
    {
        _currentSpawnedBall.AnimateDestruction();
    }
    
    public void DestroyCurrentBall()
    {
        if (_currentSpawnedBall != null)
        {
            Destroy(_currentSpawnedBall.gameObject);
        }
    }

    public EnergyBallMovement GetCurrentBall()
    {
        return _currentSpawnedBall;
    }
    
    public bool GetIsGrabbingHandle()
    {
        return _isGrabbingHandle;
    }
    
    public void SetIsGrabbingHandle(bool value, bool isRight, Transform handTransform = null)
    {
        if (_canInteract)
        {
            _isGrabbingHandle = value;
            _grabbingWithRight = isRight;
            if (handTransform != null)
            {
                _grabbingHandTransform = handTransform;
                _grabbingHandObjectVelocity = handTransform.GetComponent<ObjectVelocity>();
                if (_isGrabbingHandle && _postGrabReset)
                {
                    StartCoroutine(WaitSetCanPunch(_postGrabPunchCooldown));
                }
            }
            else
            {
                _grabbingHandTransform = null;
                _grabbingHandObjectVelocity = null;
            }
        }
    }
    
    public bool GetIsRight()
    {
        return _grabbingWithRight;
    }
    
    public void SetIsRight(bool value)
    {
        _grabbingWithRight = value;
    }
    
    public bool GetCanPunch()
    {
        return _canPunch;
    }
    
    public void SetCanPunch(bool value)
    {
        _canPunch = value;
    }

    public Vector3 GetShootDirection()
    {
        return _shootDirection;
    }

    #endregion

    #region PrivateMethods

    void CheckHandPosition()
    {
        Transform punchingHand = !_grabbingWithRight ? _rightHand : _leftHand;
        Vector3 handPos = punchingHand.position;

        if(_postGrabPunch)
        {
            if (!_canPunch)
            {
                _canPunch = Vector3.Distance(handPos, _currentSpawnedBall.transform.position) >=
                            _currentSpawnedBall.GetCollisionRadius() + .15f;
            }
        }
        
        if(Vector3.Distance(handPos,_currentSpawnedBall.transform.position) <= lockAngleDistance && _canPunch)
        {
            if (!_lockAngle)
            {
                // Debug.LogWarning("Lock Hand");
                _handStartPoint = handPos;
                _lockAngle = true;
            }
        }
        else if(punchingHand.GetComponent<ObjectVelocity>().oneFrameVelocity.magnitude > lockAngleVelocity && _canPunch)
        {
            if (!_lockAngle)
            {
                // Debug.LogWarning("Lock Hand Velocity");
                _handStartPoint = _lastHandPoint;
                _lockAngle = true;
            }
        }
        else
        {
            _lockAngle = false;
        }

        _lastHandPoint = handPos;
        // if(Vector3.Distance())
    }
    
    private void ShootBall(Vector3 shootDir)
    {
        
        if (_isEndGame)
        {
            _currentSpawnedBall.SetMoveDirection(Vector3.forward);
            _currentSpawnedBall.SetBallSpeed(5, true);
        }
        else
        {
            _currentSpawnedBall.SetMoveDirection(shootDir);
            _currentSpawnedBall.SetBallInteractable(true);
        }
        
        _currentSpawnedBall.LaunchBall();
        launchSound.Play();
        predictorTrail.HidePredictorTrail();

        StartCoroutine(WaitSetHandsActive(postLaunchWaitTime));
    }

    private void ToggleCurveShots()
    {
        _curveShots = !_curveShots;
    }

    private void DisableInteractions()
    {
        _canInteract = false;
    }

    private void SetEndGame()
    {
        _canInteract = true;
        _isEndGame = true;
    }

    #endregion
    
    #region Coroutines

    private IEnumerator WaitSetHandsActive(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        PlayerManager.SetHandsAsNotActing();
    }
    
    private IEnumerator WaitSetCanPunch(float waitTime)
    {
        _postGrabReset = false;
        yield return new WaitForSeconds(waitTime);
        _postGrabPunch = true;
    }

    #endregion
}
