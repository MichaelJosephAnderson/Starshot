using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lever : MonoBehaviour
{
	#region Events

	public static event Action<string> LeverPulled; 

	#endregion
	
	#region Fields

	[SerializeField, Tooltip("Call sign for this lever")]
	private string leverCallSign;

	[SerializeField, Tooltip("Is this button once use?")]
	private bool onceUse;

	[SerializeField, Tooltip("Reference to the left collider")]
	private BoxCollider leftCollider;

	[SerializeField, Tooltip("Reference to the right collider")]
	private BoxCollider rightCollider;

	[SerializeField, Tooltip("FMOD emitter for the lever pull sound")]
	private FMODUnity.StudioEventEmitter pullSound;

	private bool _hasTriggered;
	private bool _leftState;
	private bool _rightState;

	#endregion

	#region MonoBehavior

	#region Initilization

    // Start is called before the first frame update
    void Awake()
    {
	    _leftState = false;
	    _rightState = true;
	    leftCollider.enabled = _leftState;
    }

	#endregion

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("LeverArm"))
		{
			if(_hasTriggered) return;
			
			LeverPulled?.Invoke(leverCallSign);
			
			_leftState = !_leftState;
			leftCollider.enabled = _leftState;

			_rightState = !_rightState;
			rightCollider.enabled = _rightState;

			pullSound.Play();

			if (onceUse)
			{
				_hasTriggered = true;
			}
		}
	}

	#endregion

	#region Public Functions

	

	#endregion

	#region Private Functions

	

	#endregion
}
