using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class WeaponDisplay : NetworkBehaviour
{
    [Header("Pantalla del Arma")]
    public TextMeshProUGUI textoTiempo;
    public TextMeshProUGUI textoPuntuacion;

    private PlayerNetworkState estadoDueñoJugador;

    void Start()
    {
        if (GameplayManager.Instance != null)
        {
            GameplayManager.Instance.RegistrarPantallaArma(this);
        }
    }

    public override void OnNetworkSpawn()
    {
        StartCoroutine(RutinaAutoVincularConDueño());
    }

    private IEnumerator RutinaAutoVincularConDueño()
    {
        yield return new WaitForSeconds(0.4f);
        ulong idMiDueño = OwnerClientId;
        int intentos = 0;

        while (estadoDueñoJugador == null && intentos < 15)
        {
            PlayerNetworkState[] todosLosEstados = FindObjectsByType<PlayerNetworkState>(FindObjectsSortMode.None);

            foreach (var estado in todosLosEstados)
            {
                if (estado.OwnerClientId == idMiDueño)
                {
                    estadoDueñoJugador = estado;
                    break;
                }
            }

            if (estadoDueñoJugador == null)
            {
                intentos++;
                yield return new WaitForSeconds(0.2f);
            }
        }

        if (estadoDueñoJugador != null)
        {
            estadoDueñoJugador.puntuacion.OnValueChanged -= AlCambiarPuntosRed;
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