using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class WeaponDisplay : NetworkBehaviour
{
    [Header("Pantalla del Arma")]
    public TextMeshProUGUI textoTiempo;
    public TextMeshProUGUI textoPuntuacion;

    // 🌟 CONFIGURADO: Ahora apuntamos al script real que está ganando los puntos en tu juego
    private PlayerAvatarSync estadoDueñoJugador;

    void Start()
    {
        if (GameplayManager.Instance != null)
        {
            GameplayManager.Instance.RegistrarPantallaArma(this);
        }
    }

    public override void OnNetworkSpawn()
    {
        // El arma inicia su auto-vinculación en cuanto aparece en la red
        StartCoroutine(RutinaAutoVincularConDueño());
    }

    private IEnumerator RutinaAutoVincularConDueño()
    {
        // Esperamos un momento a que Netcode asiente el OwnerClientId
        yield return new WaitForSeconds(0.3f);

        ulong idMiDueño = OwnerClientId;
        int intentos = 0;

        // 🌟 BÚSQUEDA REAL: Escaneamos la escena buscando el PlayerAvatarSync de tu avatar
        while (estadoDueñoJugador == null && intentos < 15)
        {
            PlayerAvatarSync[] todosLosAvatares = FindObjectsByType<PlayerAvatarSync>(FindObjectsSortMode.None);

            foreach (var avatar in todosLosAvatares)
            {
                if (avatar.OwnerClientId == idMiDueño)
                {
                    estadoDueñoJugador = avatar;
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
            // Vinculación de eventos limpia y directa
            estadoDueñoJugador.puntuacion.OnValueChanged -= AlCambiarPuntosRed;
            estadoDueñoJugador.puntuacion.OnValueChanged += AlCambiarPuntosRed;

            // Pintamos el valor que tenga el jugador en este instante
            ActualizarPuntos(estadoDueñoJugador.puntuacion.Value);

            Debug.Log($"<color=green><b>[WEAPON UI] ¡CONECTADO CON ÉXITO!</b></color> Escuchando los puntos de PlayerAvatarSync del Jugador: {idMiDueño}");
        }
        else
        {
            Debug.LogError($"<color=red><b>[WEAPON UI] ERROR:</b></color> No se encontró ningún script PlayerAvatarSync para el ID de red: {idMiDueño}");
        }
    }

    public override void OnNetworkDespawn()
    {
        if (estadoDueñoJugador != null)
        {
            estadoDueñoJugador.puntuacion.OnValueChanged -= AlCambiarPuntosRed;
        }
    }

    // Este evento de red se activa al milisegundo en tu mano cuando el asteroide suma puntos
    private void AlCambiarPuntosRed(int antiguoValor, int nuevoValor)
    {
        Debug.Log($"<color=orange><b>[WEAPON UI] ¡EVENTO NETCODE DETECTADO!</b></color> Los puntos en red cambiaron a {nuevoValor}. Refrescando UI...");
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
            string textoFormateado = puntos.ToString("0000");
            textoPuntuacion.text = textoFormateado;
            Debug.Log($"<color=white><b>[WEAPON UI] Éxito visual.</b></color> TextMeshPro actualizado físicamente a: {textoFormateado}");
        }
    }
}