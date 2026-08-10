using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit.Experimental.UI;

public class showKeyboard : MonoBehaviour
{
    private TMP_InputField inputField;

    void Start()
    {
        inputField = GetComponent<TMP_InputField>();

        if (inputField != null)
        {
            // Escuchamos cuando el usuario hace clic o apunta al InputField
            inputField.onSelect.AddListener(x => OpenKeyboard());
        }
    }

    public void OpenKeyboard()
    {
        if (inputField == null) inputField = GetComponent<TMP_InputField>();

        // 1. Intentamos obtener la instancia del Singleton
        NonNativeKeyboard keyboard = NonNativeKeyboard.Instance;

        // 2. Si es null, lo buscamos en la jerarquía aunque esté apagado
        if (keyboard == null)
        {
            keyboard = FindFirstObjectByType<NonNativeKeyboard>(FindObjectsInactive.Include);
        }

        // 3. Si lo encontramos, nos aseguramos de encenderlo y presentar el teclado
        if (keyboard != null)
        {
            if (!keyboard.gameObject.activeInHierarchy)
            {
                keyboard.gameObject.SetActive(true);
            }

            keyboard.InputField = inputField;
            keyboard.PresentKeyboard(inputField.text);
        }
        else
        {
            Debug.LogError("[SHOW KEYBOARD] ¡Error Crítico! No se encontró el GameObject 'NonNativeKeyboard' en la escena.");
        }
    }
}