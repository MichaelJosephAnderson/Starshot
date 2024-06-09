using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectRotator : MonoBehaviour
{
	#region CustomClasses

	[System.Serializable]
	public class ButtonReceiver
	{
		public enum ButtonActions
		{
			ToggleRotate
		}

		public string receiverCallSign;
		public ButtonActions buttonAction;
	}
	
	#endregion
	
	#region Feilds

	[Header("Settings")]
	public ButtonReceiver[] buttonReceivers;

	[SerializeField, Tooltip("Should this start spinning or not")]
	private bool startOn = true;
	
	[SerializeField, Tooltip("Input desired rotation axis")]
	private Vector3 rotationAxis = Vector3.up;

	[SerializeField, Tooltip("Input a desired speed")]
	private float rotateSpeed;

	[SerializeField, Tooltip("Set how fast the object should slow down")]
	private float slowDownSpeed;

	[SerializeField, Tooltip("Reference to the player detector if this object has one")]
	private GameObject playerDetector;

	[SerializeField, Tooltip("Set if this object has sound that should toggle on and off as it spins")]
	private bool hasSound;
	
	private bool _onOff;
	private float _timeElapsed;
	private float _lerpValue;
	private float _currentRotateSpeed;
	private float _startRotateSpeed;

	#endregion

	#region MonoBehavior

	#region Initilization

	private void OnEnable()
	{
		EnergyBallButton.ButtonPressed += ButtonResponse;
		Lever.LeverPulled += ButtonResponse;
		Generator.GeneratorOn += ButtonResponse;
	}

	private void OnDisable()
	{
		EnergyBallButton.ButtonPressed -= ButtonResponse;
		Lever.LeverPulled -= ButtonResponse;
		Generator.GeneratorOn -= ButtonResponse;
	}

	private void Start()
	{
		_onOff = startOn;
		_startRotateSpeed = rotateSpeed;
		if (!_onOff && playerDetector != null)
		{
			playerDetector.SetActive(false);
		}
	}

	#endregion

    // Update is called once per frame
    void Update()
    {
	    transform.Rotate(rotationAxis, rotateSpeed * Time.deltaTime);
	    if (!_onOff)
	    {
		    if (_timeElapsed < slowDownSpeed)
		    {
			    _lerpValue = Mathf.Lerp(_currentRotateSpeed, 0, _timeElapsed / slowDownSpeed);
			    _timeElapsed += Time.deltaTime;
		    }
		    else
		    {
			    _lerpValue = 0;
			    if (hasSound)
			    {
				    SendMessage("Stop");
			    }
		    }
		    
		    rotateSpeed = _lerpValue;
	    }
	    else
	    {
		    if (rotateSpeed != _startRotateSpeed)
		    {
			    if (_timeElapsed < slowDownSpeed)
			    {
				    _lerpValue = Mathf.Lerp(0, _startRotateSpeed, _timeElapsed / slowDownSpeed);
				    _timeElapsed += Time.deltaTime;
			    }
			    else
			    {
				    _lerpValue = _startRotateSpeed;
				    if (hasSound)
				    {
					    SendMessage("Play");
				    }
			    }
		    
			    rotateSpeed = _lerpValue;
		    }
	    }
    }

    #endregion

	#region Public Functions

	

	#endregion

	#region Private Functions

	private void ButtonResponse(string callSign)
	{
		if (buttonReceivers.Length == 0) return;
		foreach (var receiver in buttonReceivers)
		{
			if (!receiver.receiverCallSign.Equals(callSign)) continue;
			switch (receiver.buttonAction)
			{
				case ButtonReceiver.ButtonActions.ToggleRotate:
					ToggleRotate();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

	private void ToggleRotate()
	{
		_onOff = !_onOff;
		_currentRotateSpeed = rotateSpeed;
		_timeElapsed = 0;
		
		if (playerDetector != null)
		{
			playerDetector.SetActive(false);
		}
	}

	#endregion
}
