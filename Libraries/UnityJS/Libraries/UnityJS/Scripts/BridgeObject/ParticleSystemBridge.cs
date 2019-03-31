////////////////////////////////////////////////////////////////////////
// ParticleSystemBridge.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace UnityJS {


public class ParticleSystemHelper : Tracker {


    ////////////////////////////////////////////////////////////////////////
    // Component references


    public ParticleSystem ps;
    public ParticleSystemRenderer psr;
    public GameObject collisionObject;
    public Collider collisionCollider;
    public int collisionCount;
    public List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();


    ////////////////////////////////////////////////////////////////////////
    // ParticleSystemHelper properties


    ////////////////////////////////////////////////////////////////////////
    // Instance Methods


    public override void HandleEvent(JObject ev)
    {
        base.HandleEvent(ev);

        Debug.Log("ParticleSystemHelper: HandleEvent: this: " + this + " ev: " + ev, this);

        string eventName = (string)ev["event"];
        //Debug.Log("ParticleSystemHelper: HandleEvent: eventName: " + eventName, this);
        if (string.IsNullOrEmpty(eventName)) {
            Debug.LogError("ParticleSystemHelper: HandleEvent: missing event name in ev: " + ev);
            return;
        }

        JObject data = (JObject)ev["data"];
        //Debug.Log("ParticleSystemHelper: HandleEvent: eventName: " + eventName, this);

        switch (eventName) {

            case "Foo": {
                break;
            }

        }
    }


    public override void HandleMouseUpAsButton()
    {
        //Debug.Log("ParticleSystemHelper: HandleMouseUpAsButton", this);

        base.HandleMouseUpAsButton();
    }


    void Update()
    {
        if (ps == null) {
            return;
        }

        UpdateState();
    }


    public void UpdateState()
    {
        if (mouseEnteredChanged || mouseDownChanged) {
            mouseEnteredChanged = false;
            mouseDownChanged = false;
        }
    }


    void OnParticleCollision(GameObject obj)
    {
        if (ps == null) {
            Debug.LogError("ParticleSystemHelper: OnParticleCollision: null ps");
            return;
        }

        collisionObject = obj;
        collisionCollider = obj.GetComponent<Collider>();
        if (collisionCollider == null) {
            return;
        }

        collisionEvents.Clear();
        collisionCount = ps.GetCollisionEvents(obj, collisionEvents);

        //Debug.Log("ParticleSystemHelper: OnParticleCollision: obj: " + obj + " ps: " + ps + " collisionCount: " + collisionCount + " collisionObject: " + collisionObject + " collisionCollider: " + collisionCollider);

        for (int i = 0; i < collisionCount; i++) {
            ParticleCollisionEvent collision = collisionEvents[i];
            //Debug.Log("ParticleSystemHelper: OnParticleCollision: i: " + i + " collision: " + collision + " colliderComponent: " + collision.colliderComponent + " intersection: " + collision.intersection.x + " " + collision.intersection.y + " " + collision.intersection.z + " " + " normal: " + collision.normal.x + " " + collision.normal.y + " " + collision.normal.x + " velocity: " + collision.velocity.x + " " + collision.velocity.y + " " + collision.velocity.z);
            float r = 0.1f;
            Debug.DrawLine(collision.intersection - (Vector3.up * r), collision.intersection + (Vector3.up * r), Color.red, 1.0f, false);
            Debug.DrawLine(collision.intersection - (Vector3.forward * r), collision.intersection + (Vector3.forward * r), Color.red, 1.0f, false);
            Debug.DrawLine(collision.intersection - (Vector3.right * r), collision.intersection + (Vector3.right * r), Color.red, 1.0f, false);
        }

        SendEventName("ParticleCollision");
    }


    public ParticleSystem.Particle[] particles {

        get {
            ParticleSystem.Particle[] particlesArray = new ParticleSystem.Particle[ps.particleCount];
            int count = ps.GetParticles(particlesArray);
            Debug.Log("ParticleSystemHelper: particles: get: count: " + count);
            return particlesArray;
        }

        set {
            int count = value.Length;
            Debug.Log("ParticleSystemHelper: particles: set: count: " + count);
            if (count == 0) {
                ParticleSystem.Particle[] particles = new ParticleSystem.Particle[1];
                particles [0].startSize = 0.0f;
                ps.SetParticles(particles, 1);
            } else {
                ps.SetParticles(value, value.Length);
            }
        }

    }

    public List<ParticleSystemVertexStream> activeVertexStreams {

        get {
            List<ParticleSystemVertexStream> streams = new List<ParticleSystemVertexStream>();
            psr.GetActiveVertexStreams(streams);
            Debug.Log("ParticleSystemHelper: activeVertexStreams: get: streams: " + streams.Count + " " + streams);
            return streams;
        }

        set {
            Debug.Log("ParticleSystemHelper: activeVertexStreams: set: value: " + value.Count + " " + value);
            psr.SetActiveVertexStreams(value);
        }

    }

}


}
