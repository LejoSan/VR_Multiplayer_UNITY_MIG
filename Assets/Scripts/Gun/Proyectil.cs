using UnityEngine;

public class Proyectil : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        AroObjetivo aro = collision.gameObject.GetComponent<AroObjetivo>();
        if (aro != null)
        {
            // Asegúrate de que llamas a la función que existe en AroObjetivo
            aro.DestruirAro();
        }
    }
}