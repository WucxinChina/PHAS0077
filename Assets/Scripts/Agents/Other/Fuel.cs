using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fuel : MonoBehaviour
{
	VehicleAgent vehicleAgent;
	CommunicationAgent communicationAgent;

	[ReadOnly] public float currentConsumption;
	[ReadOnly] public float airResistanceDropInFunctionOfDistanceBetweenVehiclesInPlatoon = 1; // 0-1 (or 0%-100%)
	[ReadOnly] public float fuelConsumptionInFunctionOfSpeed = 0;
	[ReadOnly] public float totalFuelUsed;

	void Start()
	{
		vehicleAgent = transform.GetComponent<VehicleAgent>();
		communicationAgent = transform.GetComponent<CommunicationAgent>();		
	}

	void Update()
	{
		// Air resistance, speed, fuel consumption
		// https://www.semanticscholar.org/paper/A-General-Simulation-Framework-for-Modeling-and-of-Deng/646204958f06527a480c9d3c3018b161e361fab7 Figure 1				
		if (communicationAgent.isPlatoonLeader) // Leader
		{
			airResistanceDropInFunctionOfDistanceBetweenVehiclesInPlatoon = 1;
		}
		else // Drafting (behind other vehicle)
		{
			if (communicationAgent.isStrictlyInPlatoon)
			{
				airResistanceDropInFunctionOfDistanceBetweenVehiclesInPlatoon = 1 - (-Mathf.Log10(communicationAgent.betweenVehicleDistances + 1.0f) * 25.0f + 68.0f) / 100.0f;
			}
			else
			{
				airResistanceDropInFunctionOfDistanceBetweenVehiclesInPlatoon = 1.0f;
			}
		}

		// Engine, speed, fuel consumption
		// https://www.researchgate.net/publication/311703927_Urban_Transportation_Solutions_for_the_CO2_Emissions_Reduction_Contributions Figure 4
		fuelConsumptionInFunctionOfSpeed = 0.0019f * Mathf.Pow(vehicleAgent.currentSpeed, 2) - 0.2506f * vehicleAgent.currentSpeed + 13.74f; // For 100% air resistance
		
		currentConsumption = fuelConsumptionInFunctionOfSpeed * airResistanceDropInFunctionOfDistanceBetweenVehiclesInPlatoon;

		totalFuelUsed += currentConsumption * (Time.deltaTime * 10.0f); // Fuel used corrected by time between frames (so it is not dependent on frame rate of the sumulation)
	}
}