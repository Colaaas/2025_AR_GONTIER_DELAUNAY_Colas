using UnityEngine;

public class PanierTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ballon"))
        {
            Debug.Log("Panier marqué !");
            ScoreManager.Instance?.AddScore(1);
        }
    }
}
