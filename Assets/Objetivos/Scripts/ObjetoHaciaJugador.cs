using UnityEngine;

public class ObjetoHaciaJugador : MonoBehaviour
{
    [Header("Configuración de Velocidad")]
    public float velocidadMin = 2f;
    public float velocidadMax = 8f;
    private float velocidadFinal;

    private Transform objetivoJugador;

    void Start()
    {
        // 1. Buscamos al jugador (usualmente la cámara principal en VR)
        if (Camera.main != null)
            objetivoJugador = Camera.main.transform;

        // 2. Elegimos una velocidad aleatoria para este objeto específico
        velocidadFinal = Random.Range(velocidadMin, velocidadMax);

        // 3. Hacemos que el objeto mire hacia el jugador al aparecer
        if (objetivoJugador != null)
            transform.LookAt(objetivoJugador);
    }

    void Update()
    {
        if (objetivoJugador == null) return;

        // 4. Movimiento constante hacia adelante (hacia el jugador)
        // La fórmula es: posición = posición + (dirección * velocidad * tiempo)
        transform.Translate(Vector3.forward * velocidadFinal * Time.deltaTime);

        // 5. Autodestrucción por cercanía (Si te "toca" y no le disparaste)
        float distancia = Vector3.Distance(transform.position, objetivoJugador.position);
        if (distancia < 1.0f)
        {
            // Aquí podrías restar vida al jugador en el futuro
            Destroy(gameObject);
        }
    }
}