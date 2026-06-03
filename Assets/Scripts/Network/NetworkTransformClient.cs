using Unity.Netcode.Components;
using UnityEngine;

[DisallowMultipleComponent]
public class NetworkTransformClient : NetworkTransform
{
    //  LA MAGIA: Este método le dice a Unity Netcode que la autoridad del servidor es FALSA.
    // Al devolver 'false', el componente se transforma automáticamente en Autoridad del Cliente.
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}