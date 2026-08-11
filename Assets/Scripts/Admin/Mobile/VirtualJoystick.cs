using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("--- Referencias UI ---")]
    public RectTransform fondoJoystick;
    public RectTransform palancaJoystick;

    private Vector2 inputVector = Vector2.zero;

    public Vector2 InputVector => inputVector;

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            fondoJoystick,
            eventData.position,
            eventData.pressEventCamera,
            out pos))
        {
            // Convertir la posición a un rango de -1 a 1
            pos.x = (pos.x / fondoJoystick.sizeDelta.x);
            pos.y = (pos.y / fondoJoystick.sizeDelta.y);

            inputVector = new Vector2(pos.x * 2 - 1, pos.y * 2 - 1);
            inputVector = (inputVector.magnitude > 1.0f) ? inputVector.normalized : inputVector;

            // Mover la imagen gráfica de la palanca central
            if (palancaJoystick != null)
            {
                palancaJoystick.anchoredPosition = new Vector2(
                    inputVector.x * (fondoJoystick.sizeDelta.x / 2.5f),
                    inputVector.y * (fondoJoystick.sizeDelta.y / 2.5f)
                );
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        if (palancaJoystick != null)
        {
            palancaJoystick.anchoredPosition = Vector2.zero;
        }
    }
}