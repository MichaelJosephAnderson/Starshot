using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class LightmapSwapper : MonoBehaviour
{
	#region Feilds

	[Header("Settings")] 
	[SerializeField, Tooltip("Set if this scene should use the swapper on scene load")]
	private bool setMapsOnLoad = true;
	
	[SerializeField, Tooltip("Set if this swapper should use the list or not")]
	private bool useList;
	
	[SerializeField, Tooltip("Set if this scene should start bright or dark")]
	private bool startDark;
	
	[Header("Dark Light Assets")]
	[SerializeField, Tooltip("Drag in the light data object for the dark lightmaps")]
	public AlternativeLightingData darkLightAsset;

	[SerializeField, Tooltip("Dark light setup")]
	private GameObject darkLightSetup;
	
	[Header("Bright Light Assets")]
	[SerializeField, Tooltip("Drag in the light data object for the bright lightmaps")]
	public AlternativeLightingData brightLightAsset;

	[SerializeField, Tooltip("Bright light setup")]
	private GameObject brightLightSetup;

	[Header("List Light Assets")]
	[SerializeField, Tooltip("List of lightmap data assets in order of how they should be set")]
	private List<AlternativeLightingData> lightmapList;

	[SerializeField, Tooltip("List of all the different light setups for the scene to be set active in order")]
	private List<GameObject> lightSetupsList;
	
	[Header("Temp")]
	[SerializeField, Tooltip("Drag in the light data object for the bright lightmaps")]
	public AlternativeLightingData lerpedLightAsset;
	
	private SphericalHarmonicsL2[] _probesCurrent;
	private LightmapData[] _lightMapsData;
	private int _lightmapIndex;
	private int _size;

	#endregion

	#region MonoBehavior

	#region Initilization
	private void OnEnable()
    {
	    LevelManager.LevelLoaded += LevelLoadedResponse;
	    DoorOpener.BallHitOpener += DoorOpenerResponse;
    }

    private void OnDisable()
    {
	    LevelManager.LevelLoaded -= LevelLoadedResponse;
	    DoorOpener.BallHitOpener -= DoorOpenerResponse;
    }

    #endregion

	#endregion

	#region Public Functions
	public void SetRoomDark()
	{
		SwapLightmaps(darkLightAsset);
		AssignLightProbesSegment(darkLightAsset);
		darkLightSetup.SetActive(true);
		brightLightSetup.SetActive(false);
	}

	public void SetRoomBright()
	{
		SwapLightmaps(brightLightAsset);
		AssignLightProbesSegment(brightLightAsset);
		darkLightSetup.SetActive(false);
		brightLightSetup.SetActive(true);
	}

	public void SetStartMaps()
	{
		if (startDark)
		{
			SetRoomDark();
		}
		else
		{
			SetRoomBright();
		}
	}

	public void SetCustomLightmap(AlternativeLightingData lightData)
	{
		SwapLightmaps(lightData);
		AssignLightProbesSegment(lightData);
	}

	#endregion

	#region Private Functions
    
	
	private void SwapLightmaps(AlternativeLightingData lightData)
	{
		//Make new lightmapData that we can edit at the start of the game
		_lightMapsData = new LightmapData[lightData.l_Light.Length];

		//Add all our lightmaps textures to the temps LightmapData
		for (int i = 0; i < lightData.l_Light.Length; i++)
		{
			_lightMapsData[i] = new LightmapData();
			_lightMapsData[i].lightmapColor = lightData.l_Light[i];

			if (lightData.l_Dir.Length >= i)
			{
				_lightMapsData[i].lightmapDir = lightData.l_Dir[i];
			}

			if (lightData.s_mask.Length != 0)
			{
				_lightMapsData[i].shadowMask = lightData.s_mask[i];
			}
		}
		ReflectionProbe[] probes = FindObjectsOfType<ReflectionProbe>();

		foreach (var probe in probes)
		{
			probe.RenderProbe();
		}
		
		//Replace the starting lightmap data with our new edited one
		LightmapSettings.lightmaps = _lightMapsData;
	}
	
	private void AssignLightProbesSegment(AlternativeLightingData TargetState)
	{
		_probesCurrent = new SphericalHarmonicsL2[TargetState.lightProbesData.Length];
		//Go through the list of light probes index and change the probes_current values
		for (int i = 0; i < TargetState.lightProbesData.Length; i++)
		{
			_probesCurrent[i] = TargetState.lightProbesData[i];
		}

		//Apply the changed probe data to the scenes lightmap settings
		LightmapSettings.lightProbes.bakedProbes = _probesCurrent;
	}

	private void SwapLightSetups()
	{
		if (lightSetupsList.Count > 0)
		{
			foreach (var setup in lightSetupsList)
			{
				setup.SetActive(false);
			}
		
			lightSetupsList[_lightmapIndex].SetActive(true);
		}
	}

	private void DoorOpenerResponse()
	{
		if (useList)
		{
			_lightmapIndex++;
			SwapLightmaps(lightmapList[_lightmapIndex]);
			AssignLightProbesSegment(lightmapList[_lightmapIndex]);
			SwapLightSetups();
		}
		else
		{
			SetRoomBright();
		}
	}

	private void LevelLoadedResponse()
	{
		if (setMapsOnLoad)
		{
			Debug.Log("Starting Lighting Setup");
			SetStartMaps();	
		}
	}

	#endregion
}
