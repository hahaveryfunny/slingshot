using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MyButton : MonoBehaviour
{

    public bool isPressed;
    public float dampenPress = 0;
    public float sensitivity = 2f;
    void Start()
    {
        SetUpButton();
    }

    void Update()
    {
        if (isPressed)
        {
            // if (ParkParkController.Instance.gameState != GameState.Playing)
            // {
            //     ParkParkController.Instance.gameState = GameState.Playing;
            // }
            dampenPress += sensitivity * Time.deltaTime;
            transform.localScale = Vector3.one * .9f;
        }
        else
        {
            dampenPress -= sensitivity * Time.deltaTime;
            transform.localScale = Vector3.one;
        }
        dampenPress = Mathf.Clamp01(dampenPress);
    }

    void SetUpButton()
    {
        EventTrigger trigger = gameObject.AddComponent<EventTrigger>();
        var pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((e) => OnClickDown());

        var pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((e) => OnClickUp());

        trigger.triggers.Add(pointerDown);
        trigger.triggers.Add(pointerUp);




    }

    public void OnClickDown()
    {
        isPressed = true;
    }
    public void OnClickUp()
    {
        isPressed = false;
    }
}
