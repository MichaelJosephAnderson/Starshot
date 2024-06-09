using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class EnergyGate : MonoBehaviour
{
    
    #region CustomClasses
    
    [System.Serializable]
    public class ButtonReceiver
    {
        public enum ButtonActions
        {
            ChangeGateState,
            ToggleGateActiveState
        }
        
        public string receiverCallSign;
        public ButtonActions buttonAction;
    }

    #endregion
    
    #region Fields

    [Header("Energy Gate Settings")] 
    public ButtonReceiver[] buttonReceivers;

    
    [Header("Materials")] 
    [SerializeField, Tooltip("Temp gate off material")]
    private Color gateOffColor;

    [SerializeField, Tooltip("Temp gate on material")]
    private Color gateOnColor;

    [SerializeField, Tooltip("Temp reference to the object to set its material")]
    private GameObject gateObj;
    
    [SerializeField, HideInInspector]
    private bool _isOn = true;

    #endregion

    #region Initilization

    private void OnEnable()
    {
        EnergyBallButton.ButtonPressed += ButtonResponse;
        Lever.LeverPulled += ButtonResponse;
    }

    private void OnDisable()
    {
        EnergyBallButton.ButtonPressed -= ButtonResponse;
        Lever.LeverPulled -= ButtonResponse;
    }

    #endregion
    
    #region PublicMethods

    public bool GetEnergyGateState()
    {
        return _isOn;
    }

    public void CallChangeGateState()
    {
        ChangeGateState();
    }

    #endregion

    #region PrivateMethods
    
    private void ChangeGateState()
    {
        if(!gateObj.activeSelf) return;
        
        _isOn = !_isOn;
        Color color = !_isOn ? gateOnColor : gateOffColor;
        gateObj.GetComponent<Renderer>().material.SetColor("_MainColor", color);
        gateObj.GetComponent<Renderer>().material.SetColor("_EmissionColor", color);
    }

    private void ToggleGateActiveState()
    {
        gateObj.SetActive(!gateObj.activeSelf);
    }

    private void ButtonResponse(string callSign)
    {
        if(buttonReceivers.Length == 0) return;
        foreach (var receiver in buttonReceivers)
        {
            if (!receiver.receiverCallSign.Equals(callSign)) continue;
            switch (receiver.buttonAction)
            {
                case ButtonReceiver.ButtonActions.ChangeGateState:
                    ChangeGateState();
                    break;
                case ButtonReceiver.ButtonActions.ToggleGateActiveState:
                    ToggleGateActiveState();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    #endregion
}
