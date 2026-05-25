using UnityEngine;

public class AsteroidTravel : MonoBehaviour
{
    [Header("Movimiento")]
    public Vector3 direccionViaje = new Vector3(0, 0, -1); // Hacia dónde viajan
    public float velocidadMin = 1f;
    public float velocidadMax = 5f;

    [Header("Rotación (Tumbling)")]
    public float fuerzaGiro = 10f;

    private float velocidadFinal;
    private Vector3 ejeRotacion;

    void Start()
    {
        // Cada asteroide tendrá una velocidad y rotación única
        velocidadFinal = Random.Range(velocidadMin, velocidadMax);
        ejeRotacion = Random.onUnitSphere;
    }

    void Update()
    {
        // 1. Viaje lineal constante
        transform.Translate(direccionViaje * velocidadFinal * Time.deltaTime, Space.World);

        // 2. Rotación sobre su propio eje (Efecto de roca a la deriva)
        transform.Rotate(ejeRotacion * fuerzaGiro * Time.deltaTime);

        // OPTIMIZACIÓN: Si el asteroide se aleja demasiado (ej. 100 metros), podrías resetearlo
        if (transform.position.z < -100f)
        {
            // Aquí podrías teletransportarlo de nuevo al frente para reutilizarlo
            transform.position = new Vector3(Random.Range(-50, 50), Random.Range(-10, 20), 100f);
        }
    }
}