using UnityEngine;

public class AroObjetivo : MonoBehaviour
{
    [Header("Configuración")]
    public int puntosQueDa = 10; // Cuántos puntos vale este aro
    public float tiempoDeVida = 4.0f; // Si no le das, desaparece solo (para el modo Time Attack)

    [Header("Efectos")]
    public GameObject efectoExplosion;

    void Start()
    {
        // En el modo Time Attack, los objetivos deben desaparecer si no los matas
        // para no llenar la escena de basura.
        Destroy(gameObject, tiempoDeVida);
    }

    // Esta función la llama la bala al chocar
    // (Asegúrate de que tu bala llame a ESTE nombre: RecibirDisparo o DestruirAro)
    public void DestruirAro()
    {
        // 1. Efecto Visual
        if (efectoExplosion != null)
        {
            Instantiate(efectoExplosion, transform.position, Quaternion.identity);
        }

        //// 2. AVISO AL MANAGER (Aquí estaba tu error)
        //if (GameplayManager.Instance != null)
        //{
        //    // CAMBIO CRÍTICO: Ya no usamos RegistrarAroDestruido()
        //    // Ahora usamos SumarPuntos()
        //    //GameplayManager.Instance.SumarPuntos(puntosQueDa);
        //}

        // 3. Adiós aro
        Destroy(gameObject);
    }

    // Si en tu script de la bala llamas a "RecibirDisparo", usa este alias:
    public void RecibirDisparo() => DestruirAro();
}