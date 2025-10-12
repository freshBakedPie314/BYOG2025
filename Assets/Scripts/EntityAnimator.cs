using System.Collections;
using UnityEngine;

public class EntityAnimator : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public IEnumerator DoSlashAttack(Vector3 TargetPosition, float t, EntityAnimator targetAnimator)
    {
        float timer = 0;
        Vector3 origPos = transform.position;
        while (timer <= t / 2)
        {
            timer += Time.deltaTime;
            transform.localPosition = (TargetPosition - transform.position) * Mathf.Sin(Mathf.PI * (2 * timer / t) / 2);
            yield return null;
        }

        
    }
}
