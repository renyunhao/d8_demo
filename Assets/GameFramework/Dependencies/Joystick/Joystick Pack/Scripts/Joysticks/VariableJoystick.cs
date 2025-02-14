using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class VariableJoystick : Joystick
{
    public float MoveThreshold { get { return moveThreshold; } set { moveThreshold = Mathf.Abs(value); } }

    [SerializeField] private float moveThreshold = 1;
    [SerializeField] private float backgroundFollowCoeff = 0.03f;
    [SerializeField] private JoystickType joystickType = JoystickType.Fixed;

    private Vector2 fixedPosition = Vector2.zero;

    public void SetMode(JoystickType joystickType)
    {
        this.joystickType = joystickType;
        if(joystickType == JoystickType.Fixed)
        {
            background.anchoredPosition = fixedPosition;
            background.gameObject.SetActive(true);
        }
        else
            background.gameObject.SetActive(false);
    }

    protected override void Start()
    {
        base.Start();
        fixedPosition = background.anchoredPosition;
        SetMode(joystickType);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if(joystickType != JoystickType.Fixed)
        {
            var pos = ScreenPointToAnchoredPosition(eventData.position);
            background.anchoredPosition = pos;
            anchorTarget = pos;
            background.gameObject.SetActive(true);
        }
        base.OnPointerDown(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if(joystickType != JoystickType.Fixed)
            background.gameObject.SetActive(false);

        base.OnPointerUp(eventData);
    }

    Vector2 anchorTarget;

    protected override void HandleInput(float magnitude, Vector2 normalised, Vector2 radius, Camera cam)
    {
        if (joystickType == JoystickType.Dynamic && magnitude > moveThreshold)
        {
            Vector2 difference = normalised * (magnitude - moveThreshold) * radius;
            anchorTarget = background.anchoredPosition + difference;
        }
        base.HandleInput(magnitude, normalised, radius, cam);
    }

    protected override void Update()
    {
        var diff = anchorTarget - background.anchoredPosition;
        var norm = diff.normalized;
        var radius = background.sizeDelta.x / 2;

        var mag = diff.magnitude;
        if(mag > radius)
        {
            mag = radius;
        }

        var normalized = norm * mag;
        Vector2 backgroundTargetPosition = background.anchoredPosition + normalized * backgroundFollowCoeff * normalizedCharSpeed;
        float screenDivisionX = 4;
        float screenDivisionY = 4.5f;
        float width = 800;
        float height = 1426;
        backgroundTargetPosition.x = Mathf.Clamp(backgroundTargetPosition.x, width / screenDivisionX, width - width / screenDivisionX);
        backgroundTargetPosition.y = Mathf.Clamp(backgroundTargetPosition.y, height / screenDivisionY, height - height / screenDivisionY);
        background.anchoredPosition = backgroundTargetPosition;


        base.Update();
    }
}

public enum JoystickType { Fixed, Floating, Dynamic }
