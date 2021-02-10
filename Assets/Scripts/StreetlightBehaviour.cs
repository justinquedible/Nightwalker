using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StreetlightBehaviour : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Coroutine to play ambient sounds
    private IEnumerator PlayAmbientSounds(float value, float time)
    {
        while (true)
        {
            yield return new WaitForSeconds(time);

            
        }
    }
}
