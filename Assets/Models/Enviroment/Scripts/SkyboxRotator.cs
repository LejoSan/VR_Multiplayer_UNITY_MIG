using UnityEngine;

public class SkySphereRotator : MonoBehaviour
{
    [Tooltip("Velocidad de giro. Para una esfera física, valores entre 0.1 y 1.0 son suficientes.")]
    public float velocidadGiro = 0.5f;

    void Update()
    {
        // Rotamos el objeto físico (la esfera) sobre su eje vertical (Y)
        // Esto hará que las estrellas y planetas giren a tu alrededor
        transform.Rotate(Vector3.right * velocidadGiro * Time.deltaTime);
    }
}