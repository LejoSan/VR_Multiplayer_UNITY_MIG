using System.Collections;
using UnityEngine;
using TMPro;

public class CuentaRegresivaVR : MonoBehaviour
{
    [Header("--- UI & Texto ---")]
    public TextMeshProUGUI textoCuenta;
    public GameObject contenedorUI; // Canvas o Panel de la cuenta atrás

    [Header("--- Sonidos ---")]
    public AudioSource audioSource;
    public AudioClip sonidoConteo; // Beep para 3, 2, 1

    [Header("--- Tiempos y Escala ---")]
    public int tiempoInicial = 3;
    public float duracionPorNumero = 1.0f;

    void Awake()
    {
        if (contenedorUI != null) contenedorUI.SetActive(false);
    }

    public IEnumerator RutinaCuentaRegresiva()
    {
        if (contenedorUI != null) contenedorUI.SetActive(true);

        int contador = tiempoInicial;

        while (contador > 0)
        {
            if (textoCuenta != null) textoCuenta.text = contador.ToString();

            // Reproduce el beep en 3, 2 y 1
            if (audioSource != null && sonidoConteo != null)
            {
                audioSource.PlayOneShot(sonidoConteo);
            }

            // Animación de escala Pop-In
            yield return StartCoroutine(AnimarEfectoPopText(contador.ToString(), Color.yellow));

            contador--;
        }

        if (contenedorUI != null) contenedorUI.SetActive(false);
    }

    private IEnumerator AnimarEfectoPopText(string texto, Color colorTexto)
    {
        if (textoCuenta == null) yield break;

        textoCuenta.color = colorTexto;
        float tiempo = 0f;

        while (tiempo < duracionPorNumero)
        {
            tiempo += Time.deltaTime;
            float t = tiempo / duracionPorNumero;

            // Escala de 2.0x a 1.0x
            float escala = Mathf.Lerp(2.0f, 1.0f, t);
            textoCuenta.transform.localScale = Vector3.one * escala;

            // Transparencia al final del segundo
            float alpha = (t > 0.7f) ? Mathf.Lerp(1.0f, 0.0f, (t - 0.7f) / 0.3f) : 1.0f;
            Color c = colorTexto;
            c.a = alpha;
            textoCuenta.color = c;

            yield return null;
        }
    }
}