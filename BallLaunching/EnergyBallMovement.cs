using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.Throw;
using Unity.VisualScripting;
using UnityEngine;

public class EnergyBallMovement : MonoBehaviour
{
    #region Fields

    private Vector3 moveDirection;
    private bool interactable = false;

    [HideInInspector] public bool isCharged;
    
    [Header("Variables")]
    [SerializeField, Tooltip("Shot power equivalent to the number of bounces")] 
    private int startPower = 1;

    [SerializeField, Tooltip("Max allowed power for the ball")]
    private int maxPower = 2;

    [SerializeField, Tooltip("Speed lost by ball per bounce")]
    private float bounceDampingMultiplier = 0.5f;

    [SerializeField, Tooltip("Temp gradient to denote ball power")]
    private Gradient powerGradient;

    [SerializeField, Tooltip("Speed threshold to set to high power")]
    private float highPowerSpeedThreshold;

    [SerializeField, Tooltip("Speed threshold to set to low power")]
    private float lowPowerSpeedThreshold;

    [SerializeField, Tooltip("High power speed multiplier")]
    private float highPowerMulti;

    [SerializeField, Tooltip("Temp reference to the balls material to change color depending on power")]
    private Material ballMat;

    [Header("collision"), SerializeField, Tooltip("Distance the ball should move away from a wall after bouncing off it with no power")]
    private float bounceStopDistance;

    [SerializeField, Tooltip("Distance the ball is checking collisions")]
    private float bounceCollisionDistance;
    
    [SerializeField, Tooltip("Distance the ball is checking if it is stuck in a wall")]
    private float intersectionCollisionDistance;
    
    [SerializeField, Tooltip("Rate to slow the ball down at once it runs out of power")]
    private AnimationCurve noPowerSlowDownRate;

    [Header("References"), SerializeField, Tooltip("Reference to the temp charged ball particle system")]
    private GameObject chargedBallPS;

    [SerializeField, Tooltip("Reference to the green charged effect")]
    private GameObject greenChargedBallPS;

    [SerializeField, Tooltip("Reference to the ball Geo object")]
    private GameObject ballGeo;

    [SerializeField, Tooltip("Reference to the ball Mesh object")]
    private GameObject ballMesh;
    
    [SerializeField, Tooltip("Reference to the handles holder object")]
    private GameObject handlesHolderObj;
    
    [SerializeField, Tooltip("Reference to the overlap collider")]
    private SphereCollider ballOverlapCollider;
    
    [SerializeField, Tooltip("Reference to the collider that keeps the ball out of walls")]
    private SphereCollider ballIntersectionCollider;

    [Header("VFX"), SerializeField, Tooltip("Reference to the ball VFX")]
    private ParticleSystem ballVFX;
    
    [SerializeField, Tooltip("Reference to the ball VFX")]
    private ParticleSystem[] ballPunchVFX;
    
    [SerializeField, Tooltip("Reference to the ball VFX")]
    private GameObject ballDestructionVFX;
    
    [SerializeField, Tooltip("Reference to the ball Trail")]
    private TrailRenderer ballTrail;
    
    [SerializeField, Tooltip("Curve dampen value")]
    private float curveDampen;

    [Header("LayerMasks")]
    [SerializeField, Tooltip("Specify environment layers the ball will bounce off of")] 
    private LayerMask bounceLayer;
    
    [SerializeField, Tooltip("Specify layers the ball will be punched by")] 
    private LayerMask punchLayer;

    [Header("Sound System")]

    [SerializeField, Tooltip("The sound effect for the ball being launched")]
    private FMODUnity.StudioEventEmitter bounceSound;

    [SerializeField, Tooltip("The sound effect for the ball bouncing off things")]
    public FMODUnity.StudioEventEmitter bopSound;

    [SerializeField, Tooltip("The sound effect for the ball being destroyed")]
    public FMODUnity.StudioEventEmitter boomSound;

    [Header("Testing Zone")]
    [SerializeField, Tooltip("Debug bool to use starting direction")]
    private bool useStartingDirection = false;
   
    [SerializeField, Tooltip("Debug Vector to use starting direction")]
    private Vector3 startingDirectionDebug = Vector3.zero;
    
    [SerializeField, Tooltip("Debug bool to use starting Power")]
    private bool useStartingPower = false;
    
    [SerializeField, Tooltip("Debug Vector to use starting Power")]
    private int startingPowerDebug = 1;

    private float _power;
    private float _speed;
    private float _baseBallSpeed;
    private float _bounceCooldown = .25f;
    private float _bounceCooldownTimer;
    
    private Vector3 _lastBouncePosition = new Vector3();
    private Vector3 _lastMovingDirection = new Vector3();
    private Vector3 _lastIntersectionMovementDirection = new Vector3();
    private Vector3 _lastIntersectionClosestPoint = new Vector3();
    private Vector3 _lastIntersectionSurfaceNormal = new Vector3();
    
    private Vector3 _point0;
    private Vector3 _point1;
    private Vector3 _point2;
    private bool _isCurveShot;
    private float _t;
    
    private bool _playerInShootingRange;

    #endregion

    #region MonoBehavior

    private void OnEnable()
    {
        SetPower(useStartingPower ? startingPowerDebug : startPower);
        if (useStartingDirection) moveDirection = startingDirectionDebug;
        //SetHandlesDeactive();
    }

    private void Update()
    {
        if(_bounceCooldownTimer < 10)
            _bounceCooldownTimer += Time.deltaTime;
        
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, bounceCollisionDistance, punchLayer);
        
        for (int i = 0; i < hitColliders.Length; i++)
        {
            CheckBall(hitColliders[i]);
        }
    }

    private void FixedUpdate()
    {
        Move();
        CheckForIntersections();
    }

    // private void OnTriggerEnter(Collider other)
    // {
    //     if (other.gameObject.layer == 6) //Hand Layer
    //     {
    //         Debug.Log("Here1");
    //         if (EnergyBallShooterPunchOnly.instance.GetIsGrabbingHandle())
    //         {
    //             Debug.Log("Here2");
    //             if (EnergyBallShooterPunchOnly.instance.GetIsRight() != other.GetComponent<EnergyBallShader>().isRightHand)
    //             {
    //                 Debug.Log("Here3");
    //                 EnergyBallShooterPunchOnly.instance.LaunchBallWithVel(other.gameObject);
    //             }
    //         }
    //     }
    // }

    #endregion

    #region PublicMethods

    //Handel Movement
    
    public void SetMoveDirection(Vector3 newMoveDirection)
    {
        moveDirection = newMoveDirection.normalized;
    }
    public Vector3 GetMoveDirection()
    {
        return moveDirection;
    }
    
    //Handle power settings

    public float GetBallPower()
    {
        return _power;
    }
    public void SetBallPower(int power)
    {
        SetPower(power);
    }
    
    public void SetBallToStartPower()
    {
        SetPower(startPower);
    }

    public void SetBallSpeed(float speed, bool changePower = false)
    {
        _speed = speed;
        
        if (!changePower) return;
        
        if (speed >= highPowerSpeedThreshold)
        {
            _speed *= highPowerMulti;
            SetPower(maxPower);
        }
        else if (speed <= lowPowerSpeedThreshold)
        {
            SetPower(1);
        }
        else
        {
            SetPower(2);
        }
        
        _baseBallSpeed = _speed;
    }

    public float GetBallSpeed()
    {
        return _speed;
    }
    public float GetBaseBallSpeed()
    {
        return _baseBallSpeed;
    }
    
    //Handle ball charge state
    public void ChargeBall()
    {
        isCharged = true;
        chargedBallPS.SetActive(true);
    }
    public void UnchargeBall()
    {
        isCharged = false;
        chargedBallPS.SetActive(false);
    }
    
    //Handle Curved movement
    public void SetCurveMovementParams(Vector3 point0)
    {
        _point0 = point0;
        
        //_point0 = moveDirection;
        _point1 = ballGeo.transform.forward;
        _point2 = Vector3.Reflect(-_point0, ballGeo.transform.forward);
        // Debug.Log(_point0);
        // Debug.DrawRay(transform.position, _point0 * .5f, Color.blue, 10);
        // Debug.DrawRay(transform.position, _point1, Color.red, 10);
        // Debug.DrawRay(transform.position, _point2 * 1.5f, Color.green, 10);
        
        _isCurveShot = true;
    }
    public void StopCurveMovement()
    {
        _isCurveShot = false;
        _t = 0;
    }
    
    //Handle Misc

    public void AnimateDestruction()
    {
        GetComponent<Collider>().enabled = false;
        moveDirection = Vector3.zero;
        ballTrail.time = .1f;
        ballMesh.SetActive(false);
        chargedBallPS.SetActive(false);
        ballDestructionVFX.SetActive(true);
        boomSound.Play();
    }
    
    public void LaunchBall()
    {
        foreach (var vfx in ballPunchVFX)
        {
            vfx.Play();
        }
    }
    
    public void SetBallInteractable(bool state)
    {
        interactable = state;
    }

    public void SetBallGeoRotation(Vector3 rotation)
    {
        ballGeo.transform.forward = rotation;
    }

    public GameObject GetBallGeo()
    {
        return ballGeo;
    }

    public float GetCollisionRadius()
    {
        return bounceCollisionDistance;
    }
    
    public void SetHandlesActive()
    {
        // handlesHolderObj.SetActive(true);
        _playerInShootingRange = true;
    }

    public void SetHandlesDeactive()
    {
        // handlesHolderObj.SetActive(false);
        _playerInShootingRange = false;
    }

    public bool GetPlayerInShootingRange()
    {
        return _playerInShootingRange;
    }

    public void SetGreenPSActive()
    {
        greenChargedBallPS.SetActive(true);
    }

    #endregion

    #region PrivateMethods

    private void CheckBall(Collider other)
    {
        //Debug.Log("Here1");
        if (EnergyBallShooterPunchOnly.instance.GetIsGrabbingHandle())
        {
            //Debug.Log("Here2");
            if (EnergyBallShooterPunchOnly.instance.GetIsRight() != other.GetComponent<EnergyBallShader>().isRightHand && EnergyBallShooterPunchOnly.instance.GetCanPunch())
            {
                //Debug.Log("Here3");
                Debug.LogWarning(other.gameObject.name);
                EnergyBallShooterPunchOnly.instance.LaunchBallWithVel(other.gameObject);
            }
        }
    }
    
    bool canMoveInDirection(Vector3 moveToPoint, out Vector3 updatedMoveToPoint)
    {
        Vector3 surfaceNormal = Vector3.zero;
        Vector3 movementDelta = moveToPoint - transform.position;
        //Vector3 directionalMovementDelta = movementDelta;
        
        movementDelta = Vector3.ClampMagnitude(movementDelta, 3);
        updatedMoveToPoint = transform.position + movementDelta;

        if (Physics.Raycast(transform.position, movementDelta.normalized, out RaycastHit hitInfo,
                Vector3.Distance(transform.position, moveToPoint), bounceLayer))
        {
            if(CheckEnergyGateBounce(hitInfo.collider.gameObject))
            {
                moveToPoint = hitInfo.point;
                movementDelta = moveToPoint - transform.position;
                Bounce(hitInfo.normal,hitInfo.point,hitInfo.collider.gameObject);
                return false;
            }
        }
        
        bool slowDown = false;
        
        Collider[] intersectingColliders = Physics.OverlapSphere(transform.position, ballOverlapCollider.radius, bounceLayer);
        foreach (var targetCollider in intersectingColliders)
        {
            if (CheckRayOnHeading(movementDelta, targetCollider, out Vector3 closestPoint, out surfaceNormal))
            {
                if (Vector3.Distance(moveToPoint, closestPoint) <= bounceCollisionDistance ||
                    Vector3.Distance(transform.position, closestPoint) <= bounceCollisionDistance)
                {
                    Vector3 closestPointHeading = moveToPoint - closestPoint;

                    Debug.DrawRay(transform.position, movementDelta.normalized, Color.green, 10);
                    Debug.DrawRay(closestPoint, closestPointHeading.normalized, Color.red, 10);
                
                    Debug.Log("Intersection Dot: " + Vector3.Dot(movementDelta, closestPointHeading));
                    if (Vector3.Dot(movementDelta, closestPointHeading) > 0 && CheckEnergyGateBounce(targetCollider.gameObject))
                    {
                        //transform.position = closestPoint - surfaceNormal * (bounceCollisionDistance / 2);
                        Debug.Log("Normal Bounce");
                        Bounce(surfaceNormal,closestPoint,targetCollider.gameObject);
                        return false;
                    }
                }
            }
        }

        
        
        updatedMoveToPoint = transform.position + movementDelta;
        
        return true;
    }
    
    //Returns true if a wall is in the way of the ball
    bool CheckRayOnHeading(Vector3 moveToPoint, Collider targetCollider, out Vector3 closestPoint, out Vector3 surfaceNormal)
    {
        closestPoint = Vector3.zero;
        surfaceNormal = Vector3.zero;

        CheckOverlapSphere(targetCollider, ballOverlapCollider, moveToPoint, out closestPoint, out surfaceNormal);
        //Debug.Log("Surface Dot " + Vector3.Dot(surfaceNormal, moveToPoint));
        if (Vector3.Dot(surfaceNormal, moveToPoint) < 0) return true;
        

        return false;
    }

    bool CheckOverlapSphere(Collider targetCollider, SphereCollider inputCollider, Vector3 spherePos, out Vector3 closestPoint, out Vector3 surfaceNormal)
    {
        closestPoint = Vector3.zero;
        surfaceNormal = Vector3.zero;
        float surfacePenetrationDepth = 0;
        
        //spherePos = inputCollider.transform.position;
        if (Physics.ComputePenetration(targetCollider, targetCollider.transform.position,targetCollider.transform.rotation,
                inputCollider, spherePos, Quaternion.identity,
                out surfaceNormal, out surfacePenetrationDepth))
        {
            closestPoint = inputCollider.transform.position + (surfaceNormal * (inputCollider.radius - surfacePenetrationDepth));
            surfaceNormal = -surfaceNormal;
            return true;
        }

        return false;
    }
    
    public void CheckForIntersections()
    {
        List<Vector3> wallNormalDirections = new List<Vector3>();
        Vector3 resultantVector = Vector3.zero;
        Vector3 closestPoint = Vector3.zero;
        Vector3 surfaceNormal = Vector3.zero;
        Collider[] intersectingColliders = Physics.OverlapSphere(transform.position, ballIntersectionCollider.radius, bounceLayer);
        foreach (var targetCollider in intersectingColliders)
        {
            CheckOverlapSphere(targetCollider, ballIntersectionCollider, ballIntersectionCollider.transform.position, out closestPoint,
                out surfaceNormal);
            
            wallNormalDirections.Add(surfaceNormal);
        }
        foreach (var wallNormal in wallNormalDirections)
        {
            resultantVector += wallNormal;
        }
        
        resultantVector /= wallNormalDirections.Count;
        
        if (resultantVector.magnitude >= 2)
        {
            resultantVector = _lastIntersectionMovementDirection;
            closestPoint = _lastIntersectionClosestPoint;
            surfaceNormal = _lastIntersectionSurfaceNormal;
        }
        if(intersectingColliders.Length > 0)
        {
            GameObject otherObject = intersectingColliders[0].gameObject;
            if (!CheckEnergyGateBounce(intersectingColliders[0].gameObject))
            {
                foreach (var collider in intersectingColliders)
                {
                    if (collider.gameObject.layer != 11)
                    {
                        otherObject = collider.gameObject;
                    }
                }
                if(otherObject == intersectingColliders[0].gameObject) return;
            }
            
            if (wallNormalDirections.Count == 0 || resultantVector == Vector3.zero)
            {
                resultantVector = (PullMovement.instance.transform.position - transform.position).normalized;
                closestPoint = transform.position;
                surfaceNormal = resultantVector;
                Collider[] intersectingCollidersLarge = Physics.OverlapSphere(transform.position, ballOverlapCollider.radius, bounceLayer);
                foreach (var targetCollider in intersectingCollidersLarge)
                {
                    CheckOverlapSphere(targetCollider, ballIntersectionCollider, ballIntersectionCollider.transform.position, out closestPoint,
                        out surfaceNormal);
            
                    wallNormalDirections.Add(surfaceNormal);
                }
                foreach (var wallNormal in wallNormalDirections)
                {
                    resultantVector += wallNormal;
                }
        
                resultantVector /= wallNormalDirections.Count;
        
                if (resultantVector.magnitude >= 2)
                {
                    resultantVector = _lastIntersectionMovementDirection;
                    closestPoint = _lastIntersectionClosestPoint;
                    surfaceNormal = _lastIntersectionSurfaceNormal;
                }

                if (intersectingCollidersLarge.Length > 0)
                {
                    otherObject = intersectingCollidersLarge[0].gameObject;
                    if (!CheckEnergyGateBounce(intersectingCollidersLarge[0].gameObject))
                    {
                        foreach (var collider in intersectingCollidersLarge)
                        {
                            if (collider.gameObject.layer != 11)
                            {
                                otherObject = collider.gameObject;
                            }
                        }

                        if (otherObject == intersectingCollidersLarge[0].gameObject) return;
                    }
                }
            }
            
            Debug.Log("Intersection: " + resultantVector);
            Debug.Log("Intersection Point: " + closestPoint);
            Debug.Log("Intersection Normal: " + surfaceNormal);
            Debug.DrawRay(transform.position, resultantVector, Color.magenta, 10);

            //closestPoint = closestPoint == Vector3.zero ? closestPoint : _lastIntersectionClosestPoint;
            if(closestPoint != Vector3.zero)
                transform.position = closestPoint + (resultantVector.normalized * bounceCollisionDistance);
            //if (_power > 0)
            {
                Debug.Log("Intersection Bounce");
                Bounce(surfaceNormal,closestPoint,otherObject);
            }
            _lastIntersectionMovementDirection = resultantVector.normalized;
            _lastIntersectionSurfaceNormal = surfaceNormal;
            _lastIntersectionClosestPoint = closestPoint;
        }
    }
    
    //Handle Movement
    private void Bounce(Vector3 normal, Vector3 contactPoint, GameObject otherObject)
    {
        //if(!CheckEnergyGateBounce(otherObject)) return;
        if(CheckChargedWall(otherObject)) ChargeBall();
        
        StopCurveMovement();
        PlayBounceEffect(contactPoint, normal, (int)GetBallPower());

        Debug.Log("Bounce: " + _power + "\nPoint: " + contactPoint);
        
        var newMoveDirection = moveDirection;
        newMoveDirection = Vector3.Reflect(newMoveDirection, normal).normalized;
        
         _lastBouncePosition = contactPoint;
         
        moveDirection = newMoveDirection;
        if(_bounceCooldownTimer >= _bounceCooldown)
        {
            SetPower(GetBallPower() - 1);
            SlowOnBounce();
            _bounceCooldownTimer = 0;
        }
        
    }

    private void PlayBounceEffect(Vector3 point, Vector3 direction, int rings)
    {
        ballVFX.transform.position = point;
        ballVFX.transform.forward = direction;
        var burst = new ParticleSystem.Burst(0,1,rings,.15f);
        ballVFX.emission.SetBurst(0,burst);
        ballVFX.Play();
        bopSound.Play();
    }
    
    private void Move()
    {
        if (moveDirection != Vector3.zero) _lastMovingDirection = moveDirection;
        Vector3 nextMovePoint = Vector3.zero;
        if (_power <= 0)
        {
            NoPowerMovement();
        }

        if (_isCurveShot)
        {
            CurveMovement();
        }
        // Debug.LogWarning("Ball Speed: " + _speed);
        // Debug.LogWarning("BallBase Speed: " + _baseBallSpeed);
        nextMovePoint = transform.position + (moveDirection * (_speed * Time.deltaTime));

        if (canMoveInDirection(nextMovePoint, out Vector3 updatedMoveToPoint))
        {
            if (moveDirection != Vector3.zero)
            {
                transform.position = updatedMoveToPoint;
            }
        }
    }

    private void NoPowerMovement()
    {
        List<Vector3> wallNormalDirections = new List<Vector3>();
        Vector3 resultantVector = Vector3.zero;
        Vector3 closestPoint = Vector3.zero;
        Vector3 surfaceNormal = Vector3.zero;
        Collider[] intersectingColliders = Physics.OverlapSphere(transform.position, ballOverlapCollider.radius, bounceLayer);
        foreach (var targetCollider in intersectingColliders)
        {
            CheckOverlapSphere(targetCollider, ballOverlapCollider, ballIntersectionCollider.transform.position, out closestPoint, out surfaceNormal);
            wallNormalDirections.Add(surfaceNormal);
        }
        foreach (var wallNormal in wallNormalDirections)
        {
            resultantVector += wallNormal;
        }
        
        resultantVector /= wallNormalDirections.Count;
        
        if (wallNormalDirections.Count == 0 || resultantVector == Vector3.zero) 
            resultantVector = (PullMovement.instance.transform.position - transform.position).normalized;
        if (resultantVector.magnitude >= 2)
        {
            resultantVector = _lastIntersectionMovementDirection;
            closestPoint = _lastIntersectionClosestPoint;
            surfaceNormal = _lastIntersectionSurfaceNormal;
        }
        if(intersectingColliders.Length > 0)
        {
            //Debug.Log("Intersection: " + resultantVector);
            Debug.DrawRay(transform.position, resultantVector, Color.magenta, 10);
            float scaledDelta = Vector3.Scale(resultantVector, moveDirection).magnitude;
            Vector3 newDirection = moveDirection + (resultantVector * scaledDelta);
            moveDirection = newDirection.normalized;
            
            _lastIntersectionMovementDirection = resultantVector.normalized;
            _lastIntersectionSurfaceNormal = surfaceNormal;
            _lastIntersectionClosestPoint = closestPoint;
        }

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hitForward, bounceStopDistance,bounceLayer)) 
        {
            closestPoint = hitForward.point;
            Vector3 newDirection = transform.position - closestPoint;
            moveDirection += newDirection;
            moveDirection = moveDirection.normalized;
        }
        if (Physics.Raycast(transform.position, -transform.forward, out RaycastHit hitBackwards, bounceStopDistance,bounceLayer)) 
        {
            closestPoint = hitBackwards.point;
            Vector3 newDirection = transform.position - closestPoint;
            moveDirection += newDirection;
            moveDirection = moveDirection.normalized;
        }
        if (Physics.Raycast(transform.position, transform.right, out RaycastHit hitRight, bounceStopDistance,bounceLayer)) 
        {
            closestPoint = hitRight.point;
            Vector3 newDirection = transform.position - closestPoint;
            moveDirection += newDirection;
            moveDirection = moveDirection.normalized;
        }
        if (Physics.Raycast(transform.position, -transform.right, out RaycastHit hitLeft, bounceStopDistance,bounceLayer)) 
        {
            closestPoint = hitLeft.point;
            Vector3 newDirection = transform.position - closestPoint;
            moveDirection += newDirection;
            moveDirection = moveDirection.normalized;
        }
        if (Physics.Raycast(transform.position, transform.up, out RaycastHit hitUp, bounceStopDistance,bounceLayer)) 
        {
            closestPoint = hitUp.point;
            Vector3 newDirection = transform.position - closestPoint;
            moveDirection += newDirection;
            moveDirection = moveDirection.normalized;
        }
        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hitDown, bounceStopDistance,bounceLayer))
        {
            closestPoint = hitDown.point;
            Vector3 newDirection = transform.position - closestPoint;
            moveDirection += newDirection;
            moveDirection = moveDirection.normalized;
        }
            
        var distance = Vector3.Distance(transform.position, closestPoint);
        if (distance >= bounceStopDistance)
        {
            moveDirection = Vector3.zero;
        }
        else
        {
            float eval = bounceStopDistance / distance;
            
            //_speed -= Time.deltaTime * ((bounceStopDistance - distance) * noPowerSlowDownRate);
            //_speed *= noPowerSlowDownRate.Evaluate(eval - 1);
            if (_speed < .01f) _speed = 0;
            _speed = Mathf.Clamp(_speed, 0, 8);
        }
    }

    void CurveMovement()
    {
        //var dist = Vector3.Distance(_point0, _point2);
        _t += (_speed * Time.deltaTime) / curveDampen;//dist;
        
        var P = QuadraticBezierCurve.CalculatePointOnCurve(_point0, _point1, _point2, _t);

        //transform.position = P;
        if (_t == 1)
        {
            StopCurveMovement();
            //CheckForCollisions();
        }

        Vector3 nextMoveDirection = P - moveDirection; //- transform.position;

        moveDirection += nextMoveDirection;
    }

    //Handle Power settings
    private void SetPower(float ballPower)
    {
        _power = ballPower;
        //ballMat.color = powerGradient.Evaluate(_power / maxPower);
        bounceSound.SetParameter("Ball Bounce Energy", ballPower);
    }

    private void SlowOnBounce()
    {
        _speed *= bounceDampingMultiplier;
        _baseBallSpeed = _speed;
    }
    
    //Handle Misc
    private bool CheckEnergyGateBounce(GameObject otherGameObject)
    {
        if (otherGameObject.layer != 11) return true;
        var energyGate = otherGameObject.transform.root.GetComponent<EnergyGate>();

        if (energyGate.GetEnergyGateState() == isCharged)
        {
            return false;
        }
           
        return true;
    }

    private bool CheckChargedWall(GameObject otherGameObject)
    {
        if (otherGameObject.layer != 13) return false; //Charged Wall Layer
        var chargedWall = otherGameObject.GetComponent<ChargedWall>();

        return chargedWall.GetIsOn();
    }

    private float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    #endregion

    #region Debug

    void DebugBounces(Vector3 origin, RaycastHit hit)
    {
        Debug.DrawLine(origin, hit.point, Color.red, 100);
        GameObject debugObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(debugObj.GetComponent<Collider>());
        debugObj.transform.localScale = Vector3.one * .1f;
        debugObj.transform.position = transform.position;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, bounceCollisionDistance);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, intersectionCollisionDistance);
        
        Gizmos.color = Color.blue;
        Collider[] intersectingColliders = Physics.OverlapSphere(transform.position, ballOverlapCollider.radius, bounceLayer);
        foreach (var collider in intersectingColliders)
        {
            CheckOverlapSphere(collider, ballOverlapCollider, ballIntersectionCollider.transform.position, out Vector3 closestPoint, out Vector3 surfaceNormal);
            Gizmos.DrawLine(closestPoint, closestPoint + surfaceNormal);
            Gizmos.DrawWireSphere(closestPoint, 0.1f);
        }

        Gizmos.color = Color.cyan;
        Collider[] intersectionColliders = Physics.OverlapSphere(transform.position, bounceCollisionDistance, bounceLayer);
        foreach (var targetCollider in intersectionColliders)
        {
            CheckOverlapSphere(targetCollider, ballIntersectionCollider, ballIntersectionCollider.transform.position, out Vector3 closestPoint,
                out Vector3 surfaceNormal);
            Gizmos.DrawLine(closestPoint, closestPoint + surfaceNormal);
            Gizmos.DrawWireSphere(closestPoint, 0.1f);
        }
    }

    #endregion
}
