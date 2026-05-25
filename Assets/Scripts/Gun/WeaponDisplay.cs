using UnityEngine;
using TMPro;
using Unity.Netcode;

public class WeaponDisplay : MonoBehaviour
{
    [Header("Pantalla del Arma")]
    public TextMeshProUGUI textoTiempo;
    public TextMeshProUGUI textoPuntuacion;

    private PlayerNetworkState estadoDueñoJugador;

    void Start()
    {
        // El arma se registra en el GameplayManager para poder actualizar el reloj global de la partida
        if (GameplayManager.Instance != null)
        {
            GameplayManager.Instance.RegistrarPantallaArma(this);
        }

        // Buscamos el componente de red del jugador que tiene el arma (está en los padres del objeto al agarrarla)
        estadoDueñoJugador = GetComponentInParent<PlayerNetworkState>();

        if (estadoDueñoJugador != null)
        {
            // Nos suscribimos al cambio de puntos de NUESTRO DNI de red
            estadoDueñoJugador.puntuacion.OnValueChanged += AlCambiarPuntosRed;
            ActualizarPuntos(estadoDueñoJugador.puntuacion.Value);
        }
    }

    private void OnDestroy()
    {
        // Limpieza para evitar errores de memoria al destruir el arma
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
        if (textoTiempo) textoTiempo.text = segundos.ToString("00");
    }

    public void ActualizarPuntos(int puntos)
    {
        if (textoPuntuacion) textoPuntuacion.text = puntos.ToString("0000");
    }
}