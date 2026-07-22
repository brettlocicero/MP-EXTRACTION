using UnityEngine;
using Unity.Cinemachine;

public class CinemachineShake : MonoBehaviour
{
	[SerializeField] CinemachineBasicMultiChannelPerlin cbmcp;

	float shakeTimer;
	float shakeTimerTotal;
	float startingIntensity;
	float priority;

    public void ShakeCamera(float intensity, float time, float shakeFreq, float priorityRequest)
	{
		if (priorityRequest >= priority)
		{
			cbmcp.AmplitudeGain = intensity;
			cbmcp.FrequencyGain = shakeFreq;
			startingIntensity = intensity;
			shakeTimerTotal = time;
			shakeTimer = time;
			priority = priorityRequest;
		}
	}

	void Update()
	{
		if (shakeTimer > 0f)
		{
			shakeTimer -= Time.deltaTime;
			cbmcp.AmplitudeGain = Mathf.Lerp(startingIntensity, 0f, 1 - (shakeTimer / shakeTimerTotal));
		}

		else
		{
			priority = 0f;
		}
	}
}