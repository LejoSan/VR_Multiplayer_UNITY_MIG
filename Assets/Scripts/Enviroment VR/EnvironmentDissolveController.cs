using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentDissolveController : MonoBehaviour
{
    [Header("--- Duración ---")]
    public float duracionAparicion = 3.0f;

    [Header("--- Sonido de Materialización ---")]
    public AudioSource audioSource;
    public AudioClip sonidoMaterializacion;

    private static readonly int ID_Cutoff = Shader.PropertyToID("_Cutoff");
    private static readonly int ID_DissolveAmount = Shader.PropertyToID("_DissolveAmount");

    private List<Renderer> renderersEntorno = new List<Renderer>();
    private MaterialPropertyBlock propBlock;

    void Awake()
    {
        propBlock = new MaterialPropertyBlock();
        Renderer[] todos = GetComponentsInChildren<Renderer>(true);
        foreach (var r in todos)
        {
            if (r != null) renderersEntorno.Add(r);
        }

        // Si no se asignó un AudioSource en el Inspector, intentamos buscar uno local
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void ResetearAInvisibilidad()
    {
        AplicarValorShader(0.0f); // 0 = Disuelto / Invisible
    }

    public void IniciarAparicionGradual(System.Action alTerminar = null)
    {
        // 🌟 Reproduce el sonido una sola vez justo al iniciar la disolución
        ReproducirSonido();
        StartCoroutine(RutinaAparicion(alTerminar));
    }

    private void ReproducirSonido()
    {
        if (audioSource != null && sonidoMaterializacion != null)
        {
            audioSource.PlayOneShot(sonidoMaterializacion);
        }
    }

    private IEnumerator RutinaAparicion(System.Action alTerminar)
    {
        float tiempo = 0f;

        while (tiempo < duracionAparicion)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / Mathf.Max(duracionAparicion, 0.0001f));

            // Va desde 0.0f (Invisible) hasta 1.0f (Totalmente Visible)
            float valor = Mathf.Lerp(0.0f, 1.0f, Mathf.SmoothStep(0f, 1f, t));

            AplicarValorShader(valor);
            yield return null;
        }

        AplicarValorShader(1.0f); // 1 = Totalmente Visible
        alTerminar?.Invoke();
    }

    private void AplicarValorShader(float valor)
    {
        foreach (var rend in renderersEntorno)
        {
            if (rend != null)
            {
                rend.GetPropertyBlock(propBlock);
                propBlock.SetFloat(ID_Cutoff, valor);
                propBlock.SetFloat(ID_DissolveAmount, valor);
                rend.SetPropertyBlock(propBlock);
            }
        }
    }
}