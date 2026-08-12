using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class WeaponDisplay : NetworkBehaviour
{
    [Header("Pantalla del Arma")]
    public TextMeshProUGUI textoTiempo;
    public TextMeshProUGUI textoPuntuacion;

    private PlayerNetworkState estadoDueñoJugador;
    private XRGrabInteractable grabInteractable;

    void Awake()
    {
        grabInteractable = GetComponentInParent<XRGrabInteractable>();
    }

    void Start()
    {
        if (GameplayManager.Instance != null)
        {
            GameplayManager.Instance.RegistrarPantallaArma(this);
        }
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(AlAgarrarArma);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(AlAgarrarArma);
        }
    }

    public override void OnNetworkSpawn()
    {
        VincularConDueñoActual(OwnerClientId);
    }

    private void AlAgarrarArma(SelectEnterEventArgs args)
    {
        ulong idJugador = NetworkManager.Singleton.LocalClientId;
        VincularConDueñoActual(idJugador);
    }

    public void VincularConDueñoActual(ulong idDueño)
    {
        if (estadoDueñoJugador != null)
        {
            estadoDueñoJugador.puntuacion.OnValueChanged -= AlCambiarPuntosRed;
            estadoDueñoJugador = null;
        }

        PlayerNetworkState[] todosLosEstados = FindObjectsByType<PlayerNetworkState>(FindObjectsSortMode.None);
        foreach (var estado in todosLosEstados)
        {
            if (estado.OwnerClientId == idDueño)
            {
                estadoDueñoJugador = estado;
                break;
            }
        }

        if (estadoDueñoJugador != null)
        {
            estadoDueñoJugador.puntuacion.OnValueChanged += AlCambiarPuntosRed;
            ActualizarPuntos(estadoDueñoJugador.puntuacion.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (estadoDueñoJugador != null)
        {
            estadoDueñoJugador.puntuacion.OnValueChanged -= AlCambiarPuntosRed;
        }
    }

    private void AlCambiarPuntosRed(int antiguoValor, int nuevoValor)
    {
        ActualizarPuntos(nuevoValor);
    }

    public void ActualizarTiempo(int segundos)
    {
        if (textoTiempo != null) textoTiempo.text = segundos.ToString("00");
    }

    public void ActualizarPuntos(int puntos)
    {
        if (textoPuntuacion != null)
        {
            textoPuntuacion.text = puntos.ToString("0000");
        }
    }
}