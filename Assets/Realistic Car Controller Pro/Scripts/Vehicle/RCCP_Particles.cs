//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Central particle manager for a vehicle, handling contact/scratch particles during collisions 
/// and wheel slip particles (sparks/smoke) for each wheel.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Addons/RCCP Particles")]
public class RCCP_Particles : RCCP_Component {

    /// <summary>
    /// Prefab used for creating spark particles upon collisions (one-time contact).
    /// </summary>
    public GameObject contactSparklePrefab;

    /// <summary>
    /// Prefab used for creating scratch particles when collisions persist (OnCollisionStay).
    /// </summary>
    public GameObject scratchSparklePrefab;

    /// <summary>
    /// Prefab used for creating wheel sparkles if a wheel is severely deflated or spinning at high RPM.
    /// </summary>
    public GameObject wheelSparklePrefab;

    /// <summary>
    /// Collision layer filter. Only collisions against these layers will trigger scratch/contact effects.
    /// </summary>
    public LayerMask collisionFilter = -1;

    private List<ParticleSystem> contactSparkeList = new List<ParticleSystem>();
    private List<ParticleSystem> scratchSparkeList = new List<ParticleSystem>();
    private List<ParticleSystem> wheelSparkleList = new List<ParticleSystem>();

    /// <summary>
    /// Defines the slip particle systems used by each wheel, potentially referencing multiple ground friction variations.
    /// </summary>
    [System.Serializable]
    public class WheelParticles {

        public RCCP_WheelCollider wheelCollider;
        public List<RCCP_WheelSlipParticles> allWheelParticles = new List<RCCP_WheelSlipParticles>();

        /// <summary>
        /// Enables the slip particle at the specified ground index, disabling others. 
        /// The rate is set based on total slip and temperature.
        /// </summary>
        public void EnableParticleByIndex(int index, float totalSlip, float totalTemp) {

            // Disable all particles except the one at 'index'.
            for (int i = 0; i < allWheelParticles.Count; i++) {

                if (i != index) {

                    if (allWheelParticles[i] != null)
                        allWheelParticles[i].Emit(false, 0f);

                }

            }

            if (allWheelParticles[index] != null)
                allWheelParticles[index].Emit(true, totalSlip * Mathf.InverseLerp(20f, 125f, totalTemp));

        }

        /// <summary>
        /// Disables all slip particles for this wheel.
        /// </summary>
        public void DisableParticles() {

            for (int i = 0; i < allWheelParticles.Count; i++) {

                if (allWheelParticles[i] != null)
                    allWheelParticles[i].Emit(false, 0f);

            }

        }

    }

    /// <summary>
    /// Holds slip-particle references for each wheel in the vehicle.
    /// </summary>
    public WheelParticles[] wheelParticles;

    /// <summary>
    /// The maximum number of one-time contact spark effects to pool.
    /// </summary>
    private readonly int maximumContactSparkle = 5;

    public override void Start() {

        base.Start();

        // Create pooled contact spark particle objects.
        if (contactSparklePrefab && contactSparkeList.Count < 1) {

            for (int i = 0; i < maximumContactSparkle; i++) {

                GameObject sparks = Instantiate(contactSparklePrefab, transform.position, Quaternion.identity);
                sparks.transform.SetParent(transform, true);
                contactSparkeList.Add(sparks.GetComponent<ParticleSystem>());
                ParticleSystem.EmissionModule em = sparks.GetComponent<ParticleSystem>().emission;
                em.enabled = false;

            }

        }

        // Create pooled scratch spark particle objects.
        if (scratchSparklePrefab && scratchSparkeList.Count < 1) {

            for (int i = 0; i < maximumContactSparkle; i++) {

                GameObject sparks = Instantiate(scratchSparklePrefab, transform.position, Quaternion.identity);
                sparks.transform.SetParent(transform, true);
                scratchSparkeList.Add(sparks.GetComponent<ParticleSystem>());
                ParticleSystem.EmissionModule em = sparks.GetComponent<ParticleSystem>().emission;
                em.enabled = false;

            }

        }

        // Create wheel sparkle systems, one for each wheel, if deflated or spinning quickly.
        if (wheelSparklePrefab && wheelSparkleList.Count < 1) {

            for (int i = 0; i < CarController.AllWheelColliders.Length; i++) {

                GameObject sparks = Instantiate(wheelSparklePrefab, CarController.AllWheelColliders[i].transform.position, Quaternion.identity);
                sparks.transform.SetParent(CarController.AllWheelColliders[i].transform, true);
                wheelSparkleList.Add(sparks.GetComponent<ParticleSystem>());
                ParticleSystem.EmissionModule em = sparks.GetComponent<ParticleSystem>().emission;
                em.enabled = false;

            }

        }

        // Create a list of slip particle systems for each wheel friction type in RCCPGroundMaterials.
        wheelParticles = new WheelParticles[CarController.AllWheelColliders.Length];

        for (int i = 0; i < wheelParticles.Length; i++) {

            wheelParticles[i] = new WheelParticles();

            for (int k = 0; k < RCCPGroundMaterials.frictions.Length; k++) {

                GameObject ps = Instantiate(RCCPGroundMaterials.frictions[k].groundParticles, transform.position, transform.rotation);
                ParticleSystem.EmissionModule em = ps.GetComponent<ParticleSystem>().emission;
                em.enabled = false;
                ps.transform.SetParent(CarController.AllWheelColliders[i].transform, false);
                ps.transform.localPosition = Vector3.zero;
                ps.transform.localRotation = Quaternion.identity;

                wheelParticles[i].allWheelParticles.Add(ps.GetComponent<RCCP_WheelSlipParticles>());
                wheelParticles[i].wheelCollider = CarController.AllWheelColliders[i];

            }

        }

    }

    private void Update() {

        // Enable wheel sparkle if the wheel is deflated and rotating quickly; otherwise disable.
        if (wheelSparkleList.Count >= 1) {

            for (int i = 0; i < CarController.AllWheelColliders.Length; i++) {

                if (CarController.AllWheelColliders[i].WheelCollider.enabled) {

                    if (CarController.AllWheelColliders[i].deflated && Mathf.Abs(CarController.AllWheelColliders[i].WheelCollider.rpm) >= 250f) {

                        ParticleSystem.EmissionModule em = wheelSparkleList[i].emission;

                        if (!em.enabled)
                            em.enabled = true;

                    } else {

                        ParticleSystem.EmissionModule em = wheelSparkleList[i].emission;

                        if (em.enabled)
                            em.enabled = false;

                    }

                } else {

                    ParticleSystem.EmissionModule em = wheelSparkleList[i].emission;

                    if (em.enabled)
                        em.enabled = false;

                }

            }

        }

        // Manage wheel slip particle systems based on isSkidding and ground friction index.
        for (int i = 0; i < wheelParticles.Length; i++) {

            if (wheelParticles[i].wheelCollider.WheelCollider.enabled && wheelParticles[i].wheelCollider.isSkidding) {

                wheelParticles[i].EnableParticleByIndex(
                    wheelParticles[i].wheelCollider.groundIndex,
                    wheelParticles[i].wheelCollider.TotalSlip,
                    wheelParticles[i].wheelCollider.totalWheelTemp);

            } else {

                wheelParticles[i].DisableParticles();

            }

        }

    }

    /// <summary>
    /// Called on collisions. Spawns a quick contact particle system (sparks) if collision velocity is sufficient.
    /// </summary>
    public void OnCollision(Collision collision) {

        if (!enabled)
            return;

        // If no collision points or velocity is too low, ignore.
        if (collision.contactCount < 1)
            return;

        if (collision.relativeVelocity.magnitude < 5)
            return;

        // Find an unused contact spark in the pool and play it at the first contact point.
        for (int i = 0; i < contactSparkeList.Count; i++) {

            if (!contactSparkeList[i].isPlaying) {

                contactSparkeList[i].transform.position = collision.GetContact(0).point;
                ParticleSystem.EmissionModule em = contactSparkeList[i].emission;
                em.rateOverTimeMultiplier = collision.impulse.magnitude / 500f;
                em.enabled = true;
                contactSparkeList[i].Play();
                break;

            }

        }

    }

    /// <summary>
    /// Called each frame while colliding. Spawns scratch particles if velocity is high enough and the collision is in the specified layerMask.
    /// </summary>
    public void OnCollisionStay(Collision collision) {

        if (!enabled)
            return;

        // If the collision�s speed is low, disable all scratch particles and return.
        if (collision.contactCount < 1 || collision.relativeVelocity.magnitude < 2f) {

            if (scratchSparkeList != null) {

                for (int i = 0; i < scratchSparkeList.Count; i++) {

                    ParticleSystem.EmissionModule em = scratchSparkeList[i].emission;
                    em.enabled = false;

                }

            }

            return;

        }

        // If collision is with a layer in collisionFilter, spawn scratch effects.
        if (((1 << collision.gameObject.layer) & collisionFilter) != 0) {

            ContactPoint[] contacts = new ContactPoint[collision.contactCount];
            collision.GetContacts(contacts);

            int ind = -1;

            foreach (ContactPoint cp in contacts) {

                ind++;

                if (ind < scratchSparkeList.Count && !scratchSparkeList[ind].isPlaying) {

                    scratchSparkeList[ind].transform.position = cp.point;
                    ParticleSystem.EmissionModule em = scratchSparkeList[ind].emission;
                    em.enabled = true;
                    em.rateOverTimeMultiplier = collision.relativeVelocity.magnitude / 1f;
                    scratchSparkeList[ind].Play();

                }

            }

        }

    }

    /// <summary>
    /// Called when a collision ends (no longer in contact). Stops any active scratch particles.
    /// </summary>
    public void OnCollisionExit(Collision collision) {

        if (!enabled)
            return;

        for (int i = 0; i < scratchSparkeList.Count; i++) {

            ParticleSystem.EmissionModule em = scratchSparkeList[i].emission;
            em.enabled = true;
            scratchSparkeList[i].Stop();

        }

    }

    private void Reset() {

        // Automatically assign default prefabs from RCCP_Settings if none are set.
        contactSparklePrefab = RCCP_Settings.Instance.contactParticles;
        scratchSparklePrefab = RCCP_Settings.Instance.scratchParticles;
        wheelSparklePrefab = RCCP_Settings.Instance.wheelSparkleParticles;

    }

}
