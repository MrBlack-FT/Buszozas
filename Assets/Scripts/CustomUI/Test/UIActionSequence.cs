using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class UIActionSequence : MonoBehaviour
{
    public List<CustomAction> osActions;
    public List<CustomAction> csActions;

    private int callCount = 0;

    public void OpenSettings()
    {
        // Colored boxes:🟩, 🟦, 🟨, 🟧, 🟫, 🟪, 🟥
        callCount++;
        Debug.Log("! callCount: " + callCount);

        Debug.Log("OpeningSettings() called.");
        HandleActions(osActions);
    }

    public void CloseSettings()
    {
        Debug.Log("ClosingSettings() called.");
        HandleActions(csActions);
    }

    private void HandleActions(List<CustomAction> actions)
    {
        Sequence sequence = DOTween.Sequence();

        for (int i = 0; i < actions.Count; i++)
        {
            var action = actions[i];
            Debug.Log("Action: " + action.actionType + " \n\tTarget: " + action.target + " \n\tDuration: " + action.duration + " \n\tDirection: " + action.direction + " \n\tTiming: " + action.timing);

            // NULL TARGET
            if (!action.target)
            {
                Debug.LogWarning("Missing target on action: " + action.actionType);
                continue;
            }

            // UNITY EVENT
            if (action.actionType == ActionType.InvokeEvent)
            {
                if (i == 0 || action.timing == ActionTiming.AfterPrevious)
                {
                    sequence.AppendCallback(() => action.unityEvent?.Invoke());
                }
                else if (action.timing == ActionTiming.WithPrevious)
                {
                    sequence.JoinCallback(() => action.unityEvent?.Invoke());
                }
                continue;
            }

            var canvasGroup = action.target.GetComponent<CanvasGroup>();
            if (!canvasGroup)
            {
                Debug.LogWarning("Missing CanvasGroup on " + action.target.name);
                continue;
            }

            Vector3 originalPosition = action.target.transform.localPosition; // Eredeti pozíció mentése
            Vector3 offset = GetDirectionOffset(action.direction); // Irány szerinti eltolás

            if (action.actionType == ActionType.FadeIn)
            {
                action.target.transform.localPosition = originalPosition + offset;
                canvasGroup.alpha = 0;

                if (i == 0 || action.timing == ActionTiming.AfterPrevious)
                {
                    sequence.Append(action.target.transform.DOLocalMove(originalPosition, action.duration).SetEase(Ease.OutCubic));
                    sequence.Join(canvasGroup.DOFade(1f, action.duration));
                }
                else if (action.timing == ActionTiming.WithPrevious)
                {
                    sequence.Join(action.target.transform.DOLocalMove(originalPosition, action.duration).SetEase(Ease.OutCubic));
                    sequence.Join(canvasGroup.DOFade(1f, action.duration));
                }
            }
            else if (action.actionType == ActionType.FadeOut)
            {
                canvasGroup.alpha = 1;

                if (i == 0 || action.timing == ActionTiming.AfterPrevious)
                {
                    sequence.Append(canvasGroup.DOFade(0f, action.duration));
                    sequence.Join(action.target.transform.DOLocalMove(originalPosition + offset, action.duration).SetEase(Ease.InCubic));
                }
                else if (action.timing == ActionTiming.WithPrevious)
                {
                    sequence.Join(canvasGroup.DOFade(0f, action.duration));
                    sequence.Join(action.target.transform.DOLocalMove(originalPosition + offset, action.duration).SetEase(Ease.InCubic));
                }
            }
        }

        sequence.OnComplete(() => Debug.Log("Sequence completed!"));
    }

    private Vector3 GetDirectionOffset(Direction direction)
    {
        switch (direction)
        {
            case Direction.UP:
                return new Vector3(0, 100, 0);
            case Direction.DOWN:
                return new Vector3(0, -100, 0);
            case Direction.LEFT:
                return new Vector3(-100, 0, 0);
            case Direction.RIGHT:
                return new Vector3(100, 0, 0);
            default:
                Debug.LogWarning("Unknown direction: " + direction);
                return Vector3.zero;
        }
    }

    public void MyOnClickHandler()
    {
        Debug.Log("EZ ITT AZ onClick ESEMÉNY!"); // EZ FOG FUTNI
    }
}