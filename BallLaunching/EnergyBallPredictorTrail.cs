using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnergyBallPredictorTrail : MonoBehaviour
{
	#region Feilds

	[Header("General Settings")] 
	[SerializeField, Tooltip("Reference to the line renderer component")]
	private LineRenderer predictorTrail;
	
	[SerializeField, Tooltip("Layermask for what you can shoot at")]
	private LayerMask shootableSurfaces;

	[SerializeField, Tooltip("Distance the other hand has to be to show curves")]
	private float showCurveDistance;

	[SerializeField, Tooltip("Reference to the predictor trail start position")]
	private Transform predictorTrailStartTransform;
	
	[Header("Curve Trail Settings")] 
	[SerializeField, Tooltip("Total length of the curved trail")]
	private float curveTotalDist;
	
	[SerializeField, Tooltip("Set to the same as the straight hit threshold on the launcher")]
	private float curveTrailThreshold;

	[SerializeField, Tooltip("Set the curve trail resolution, used to set how many points should be added to the line renderer for smoothness")]
	private float curveTrailResolution;

	[Header("Straight Trail Settings")]
	[SerializeField, Tooltip("Total length of the straight trail")]
	private float straightTotalDist;

	private Transform _predictorTrailShootFromTransform;
	private Transform _punchingHandPos;
	private int _numPoints = 1;

	private EnergyBallMovement _ball;
	private bool _curveShots = true;
	
	#endregion

	#region MonoBehavior

	#region Initilization

	private void OnEnable()
	{
		BallSpawnList.ToggleCurveShots += ToggleCurveShots;
	}

	private void OnDisable()
	{
		BallSpawnList.ToggleCurveShots -= ToggleCurveShots;
	}

	private void Start()
	{
		_predictorTrailShootFromTransform = predictorTrailStartTransform;
	}

	#endregion
	

	#endregion

	#region Public Functions

	public void SetNewBallRef(EnergyBallMovement ball)
	{
		_ball = ball;
	}

	public void ShowPredictorTrail()
	{
		if(!predictorTrail.gameObject.activeSelf)
			predictorTrail.gameObject.SetActive(true);

		CalculatePredictorTrail();
	}

	public void HidePredictorTrail()
	{
		if(predictorTrail.gameObject.activeSelf)
			predictorTrail.gameObject.SetActive(false);
	}

	public void SetPredictorTrailShootFromTransform(Vector3 position, Vector3 forwardDirection)
	{
		_predictorTrailShootFromTransform.position = position;
		_predictorTrailShootFromTransform.forward = forwardDirection;
	}

	public void SetPunchingHandPosition(Transform pos)
	{
		_punchingHandPos = pos;
	}

	#endregion

	#region Private Functions

	private void CalculatePredictorTrail()
	{
		var dir = (_predictorTrailShootFromTransform.position - _punchingHandPos.position).normalized;
		var dot = Vector3.Dot(_predictorTrailShootFromTransform.forward, dir);

		if (dot <= curveTrailThreshold && Vector3.Distance(_punchingHandPos.position, _predictorTrailShootFromTransform.position) <= showCurveDistance && _curveShots) //Curve Trail
		{
			var distBetweenPoints = 1 / curveTrailResolution;
			var point0 = dir;
			var point1 = _predictorTrailShootFromTransform.forward;
			var point2 = Vector3.Reflect(-point0, point1);
			var t = 0f;

			List<Vector3> curvePoints = new List<Vector3>();
			for (int i = 0; i <= curveTrailResolution; i++)
			{
				var point = QuadraticBezierCurve.CalculatePointOnCurve(point0, point1, point2, t);
				curvePoints.Add(point);
				t += distBetweenPoints;
			}

			_numPoints = 1;
			predictorTrail.positionCount = _numPoints;
			var curveStartPoint = _predictorTrailShootFromTransform.position;
			predictorTrail.SetPosition(0, curveStartPoint);
			var currentPoint = curveStartPoint;

			var distTraveled = 0f;
			for (int i = 1; i < curvePoints.Count - 1; i++)
			{
				var nextPoint = currentPoint + curvePoints[i];
				var castDir = (nextPoint - currentPoint).normalized;
				var castDist = Vector3.Distance(currentPoint, nextPoint);
				if (Physics.Raycast(currentPoint, castDir, out RaycastHit curveOutHit, castDist, shootableSurfaces, QueryTriggerInteraction.Ignore))
				{
					if (CheckEnergyGate(curveOutHit))
					{
						_numPoints++;
						predictorTrail.positionCount = _numPoints;
						predictorTrail.SetPosition(i, curveOutHit.point);

						var origin = curveOutHit.point;
						var direction = Vector3.Reflect(castDir, curveOutHit.normal);
						float newDist = curveTotalDist - (distTraveled + Vector3.Distance(currentPoint, curveOutHit.point));

						Vector3 newPoint;
						if (Physics.Raycast(origin, direction, out RaycastHit outHit2, newDist,shootableSurfaces, QueryTriggerInteraction.Ignore))
						{
							newPoint = outHit2.point;
						}
						else
						{
							newPoint = origin + direction * newDist;
						}
						
						_numPoints++;
						predictorTrail.positionCount = _numPoints;
						predictorTrail.SetPosition(i + 1, newPoint);
						break;
					}
				}

				if (curveTotalDist > distTraveled)
				{
					var newDist = Vector3.Distance(currentPoint, currentPoint + curvePoints[i]);
					distTraveled += newDist;
					
					currentPoint += curvePoints[i];
					_numPoints++;
					predictorTrail.positionCount = _numPoints;
					predictorTrail.SetPosition(i, currentPoint);
				}
			}
		}
		else //Straight Trail
		{
			List<Vector3> points = new List<Vector3>();
			if (Physics.Raycast(_predictorTrailShootFromTransform.position, _predictorTrailShootFromTransform.forward, out RaycastHit outHit, straightTotalDist, shootableSurfaces, QueryTriggerInteraction.Ignore))
			{
				if (CheckEnergyGate(outHit))
				{
					points.Add(outHit.point);
				
					Vector3 origin = outHit.point;
					Vector3 direction = Vector3.Reflect(_predictorTrailShootFromTransform.forward, outHit.normal);
					float newDist = straightTotalDist - Vector3.Distance(_predictorTrailShootFromTransform.position, outHit.point);

					Vector3 newPoint;
					if (Physics.Raycast(origin, direction, out RaycastHit outHit2, newDist,shootableSurfaces, QueryTriggerInteraction.Ignore))
					{
						newPoint = outHit2.point;
					}
					else
					{
						newPoint = origin + direction * newDist;
					}
					points.Add(newPoint);
				}
				else
				{
					var newDist2 = straightTotalDist - Vector3.Distance(_predictorTrailShootFromTransform.position, outHit.point);
					var newStartPoint = outHit.point + _predictorTrailShootFromTransform.forward * 0.1f;
					if (Physics.Raycast(newStartPoint, _predictorTrailShootFromTransform.forward, out RaycastHit outHit2, newDist2, shootableSurfaces, QueryTriggerInteraction.Ignore))
					{
						if (CheckEnergyGate(outHit2))
						{
							points.Add(outHit2.point);

							Vector3 origin = outHit2.point;
							Vector3 direction = Vector3.Reflect(_predictorTrailShootFromTransform.forward, outHit2.normal);
							float newDist3 = newDist2 - Vector3.Distance(newStartPoint, outHit2.point);

							Vector3 newPoint;
							if (Physics.Raycast(origin, direction, out RaycastHit outHit3, newDist3, shootableSurfaces, QueryTriggerInteraction.Ignore))
							{
								newPoint = outHit3.point;
							}
							else
							{
								newPoint = origin + direction * newDist3;
							}
							points.Add(newPoint);
						}
					}
				}
			}

			var startPoint = _predictorTrailShootFromTransform.position;
			
			if(points.Count > 0)
			{
				predictorTrail.positionCount = points.Count + 1;
				predictorTrail.SetPosition(0, startPoint);
				
				for (int i = 0; i < points.Count; i++)
				{
					predictorTrail.SetPosition(i + 1, points[i]);
				}
			}
			else
			{
				predictorTrail.positionCount = 2;
				predictorTrail.SetPosition(0, startPoint);
				predictorTrail.SetPosition(1, startPoint + _predictorTrailShootFromTransform.forward * straightTotalDist);
			}
		}
	}

	private bool CheckEnergyGate(RaycastHit outHit)
	{
		if (outHit.transform.gameObject.layer != 11)
			return true;
		
		var gate = outHit.transform.root.GetComponent<EnergyGate>();
		return gate.GetEnergyGateState() != _ball.isCharged;
	}

	private void ToggleCurveShots()
	{
		_curveShots = !_curveShots;
	}

	#endregion
}
