using UnityEngine;
using UnityEngine.Events;

public enum Direction { UP, DOWN, LEFT, RIGHT, NONE }
public enum ActionType { FadeIn, FadeOut, InvokeEvent }
public enum ActionTiming { WithPrevious, AfterPrevious }

[System.Serializable]
public class UIActionConfig
{
    public ActionType actionType;
    public GameObject target;
    public float duration = 0.5f;

    public Direction direction;

    public UnityEvent unityEvent;

    public ActionTiming timing = ActionTiming.AfterPrevious;
}
