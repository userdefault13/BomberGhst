using UnityEngine;
using System.Collections;

// Simple destruction effect that can be attached to walls
public class DestructionEffect : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.5f;
    [SerializeField] private ParticleSystem particles;
    
    void Start()
    {
        if (particles != null)
        {
            particles.Play();
        }
        
        Destroy(gameObject, lifetime);
    }
}
