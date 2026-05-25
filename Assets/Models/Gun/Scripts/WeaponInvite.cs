using UnityEngine;

public class WeaponInvite : MonoBehaviour
{
    [Header("Levitación")]
    public float amplitud = 0.05f; // Qué tanto sube y baja (5cm)
    public float frecuencia = 2.0f; // Qué tan rápido lo hace

    [Header("Rotación")]
    public float velocidadGiro = 30f;

    private Vector3 posicionInicial;

    void Start()
    {
        posicionInicial = transform.position;
    }

    void Update()
    {
        // 1. Cálculo de la onda Seno para flotar suavemente
        // 
        float nuevaY = Mathf.Sin(Time.time * frecuencia) * amplitud;

        // Aplicamos la posición manteniendo X y Z intactos
        transform.position = posicionInicial + new Vector3(0, nuevaY, 0);

        // 2. Rotación constante en el eje Y (Giro de exhibición)
        transform.Rotate(Vector3.up * velocidadGiro * Time.deltaTime);
    }
}