using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour {

    private float shakeAmount;
    
    public void Shake(float amount, float length)
    {
        // Cancel current shake

        if (shakeAmount != 0)
        {
            CancelInvoke("EndShake");

            CancelInvoke("DoShake");
        }

        // Start new shake

        shakeAmount = amount;

        InvokeRepeating("DoShake", 0, 0.01f);
        Invoke("EndShake", length);
    }

    void DoShake()
    {
        Vector3 pos = transform.position;

        transform.localPosition = new Vector3(Random.Range(-shakeAmount, +shakeAmount), Random.Range(-shakeAmount, +shakeAmount), 0);
    }

    void EndShake()
    {
        CancelInvoke("DoShake");

        shakeAmount = 0;

        transform.localPosition = Vector3.zero;
    }
}
