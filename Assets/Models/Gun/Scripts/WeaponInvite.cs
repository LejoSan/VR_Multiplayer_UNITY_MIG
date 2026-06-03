using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class WeaponInvite : MonoBehaviour
{
    [Header("Levitación")]
    public float amplitud = 0.05f;
    public float frecuencia = 2.0f;

    [Header("Rotación")]
    public float velocidadGiro = 30f;

    private Vector3 posicionInicial;
    private XRGrabInteractable grabInteractable;
    private bool estaAgarrada = false;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(AlAgarrarArma);
        }
    }

    void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(AlAgarrarArma);
        }
    }

    void Start()
    {
        posicionInicial = transform.position;
    }

    void Update()
    {
        // 🌟 SI EL JUGADOR LA AGARRA, APAGAMOS EL EFECTO PARA NO ROMPER EL TRACKING VR
        if (estaAgarrada) return;

        // 1. Cálculo de la onda Seno para flotar suavemente
        float nuevaY = Mathf.Sin(Time.time * frecuencia) * amplitud;

        // Aplicamos la posición manteniendo X y Z intactos
        transform.position = posicionInicial + new Vector3(0, nuevaY, 0);

        // 2. Rotación constante en el eje Y (Giro de exhibición)
        transform.Rotate(Vector3.up * velocidadGiro * Time.deltaTime);
    }

    private void AlAgarrarArma(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs arg)
    {
        estaAgarrada = true;
        // Apagamos este script para liberar los transforms de la física de manos
        enabled = false;
    }
}