using UnityEngine;

public class ComboSystem : MonoBehaviour
{
    [Header("Configuración de Feedback")]
    public AudioSource audioSource;
    public AudioClip[] vocesElogio;

    [Header("Dificultad")]
    public float tiempoMaximoEntreHits = 1.5f; // Tiempo máx para mantener la racha
    public int hitsParaSonar = 3; // ¡NUEVO! Tienes que dar a 3 seguidos para que suene

    private float tiempoUltimoAcierto = -999f;
    private int contadorRacha = 0; // Cuenta cuántos llevas seguidos

    public void ProcesarDisparo()
    {
        float tiempoActual = Time.time;
        float diferencia = tiempoActual - tiempoUltimoAcierto;

        // LÓGICA DE RACHA
        if (diferencia < tiempoMaximoEntreHits)
        {
            // Si disparaste rápido, sumamos a la racha
            contadorRacha++;
        }
        else
        {
            // Si tardaste mucho, reiniciamos la racha a 1 (el actual)
            contadorRacha = 1;
        }

        // Guardamos el tiempo para la próxima comparación
        tiempoUltimoAcierto = tiempoActual;

        // LÓGICA DE AUDIO
        // Solo suena si alcanzas o superas el número objetivo (ej. 3, 6, 9...)
        if (contadorRacha > 0 && contadorRacha % hitsParaSonar == 0)
        {
            ReproducirElogio();
        }
    }

    void ReproducirElogio()
    {
        if (audioSource == null || vocesElogio.Length == 0) return;

        // Reproducir sonido
        audioSource.PlayOneShot(vocesElogio[Random.Range(0, vocesElogio.Length)]);
        Debug.Log($"¡COMBO DE {contadorRacha}! Reproduciendo audio.");
    }
}