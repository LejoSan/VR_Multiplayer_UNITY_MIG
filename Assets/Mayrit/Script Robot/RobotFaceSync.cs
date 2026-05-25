using UnityEngine;

public class RobotFaceSync : MonoBehaviour
{
    [Header("Referencias")]
    public AudioSource audioSource; // El que reproduce la voz
    public Animator faceAnimator;   // El animator de la cara
    public string paramName = "isTalking"; // El nombre exacto de tu Bool

    [Header("Ajustes de Sensibilidad")]
    [Range(0.001f, 0.1f)]
    public float umbralVolumen = 0.01f; // ¿Qué tan fuerte debe sonar para abrir la boca?
    public float suavizado = 5f; // Para que la boca no vibre como loca

    private float volumenActual = 0f;

    void Update()
    {
        // 1. Si no hay audio source o no está sonando, forzamos silencio
        if (audioSource == null || !audioSource.isPlaying)
        {
            faceAnimator.SetBool(paramName, false);
            return;
        }

        // 2. Analizar el espectro de audio (Tomamos una muestra de 256 datos)
        float[] muestras = new float[256];
        audioSource.GetOutputData(muestras, 0); // 0 = Canal izquierdo/Mono

        // 3. Calcular el volumen promedio (RMS simple)
        float suma = 0f;
        foreach (float muestra in muestras)
        {
            suma += Mathf.Abs(muestra);
        }
        float volumenPromedio = suma / 256;

        // 4. Suavizar el valor para evitar "jittering" (temblequeo)
        volumenActual = Mathf.Lerp(volumenActual, volumenPromedio, Time.deltaTime * suavizado);

        // 5. Decidir: ¿Hablo o me callo?
        // Si el volumen supera el umbral, activamos la animación
        bool estaHablando = volumenActual > umbralVolumen;

        faceAnimator.SetBool(paramName, estaHablando);
    }
}