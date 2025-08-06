using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class UIActionSequence : MonoBehaviour
{
    private UIVars uiVars;
    private Debugger debugger;

    private Sequence sequence;

    // Colored boxes:🟩, 🟦, 🟨, 🟧, 🟫, 🟪, 🟥

    private void Awake()
    {
        uiVars = GameObject.Find("UIVars").GetComponent<UIVars>();
        if (uiVars == null)
        {
            Debug.LogWarning("UIVars not found in the scene.");
        }
        GameObject debugPanel = GameObject.Find("!DEBUGGER!");
        if (debugPanel != null)
        {
            debugger = debugPanel.GetComponent<Debugger>();
        }
        else
        {
            Debug.LogWarning("!DEBUGGER! GameObject not found in the scene.");
        }
    }

    private void Update()
    {
        if (debugger != null)
        {
            string isMenuTransitioningStatus = debugger.ColoredString(uiVars.IsMenuTransitioning ? "TRUE" : "FALSE", uiVars.IsMenuTransitioning ? Color.green : Color.red);
            debugger.UpdatePersistentLog("IsMenuTransitioning", isMenuTransitioningStatus);
        }
    }
    
    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, float> originalAlphas = new Dictionary<GameObject, float>();

    public void BuildAndRunSequence(List<UIActionConfig> actions)
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

        List<GameObject> targets = new List<GameObject>();

        // Eredeti pozíciók és alpha értékek mentése
        foreach(var action in actions)
        {
            if (action.target != null && !targets.Contains(action.target))
            {
                targets.Add(action.target);

                // Eredeti pozíció és alpha mentése
                if (!originalPositions.ContainsKey(action.target))
                {
                    originalPositions[action.target] = action.target.transform.localPosition;
                }

                var canvasGroup = action.target.GetComponent<CanvasGroup>();
                if (canvasGroup != null && !originalAlphas.ContainsKey(action.target))
                {
                    originalAlphas[action.target] = canvasGroup.alpha;
                }
            }
        }

        uiVars.IsMenuTransitioning = true;

        Debug.Log($"{stopwatch.ElapsedMilliseconds} ms has elapsed after saving original positions and alphas for {targets.Count} targets.");
        sequence = DOTween.Sequence();

        // Szekvencia felépítése
        for (int i = 0; i < actions.Count; i++)
        {
            var action = actions[i];
            //Debug.Log("Action: " + action.actionType + " \n\tTarget: " + action.target + " \n\tDuration: " + action.duration + " \n\tDirection: " + action.direction + " \n\tTiming: " + action.timing);

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

            Vector3 originalPosition = originalPositions[action.target];
            Vector3 offset = GetDirectionOffset(action.direction);

            // Action típusok kezelése
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

        Debug.Log($"{stopwatch.ElapsedMilliseconds} ms has elapsed, sequence built with {actions.Count} actions.");

        sequence.OnComplete(() =>
        {
            Debug.Log($"{stopwatch.ElapsedMilliseconds} ms has elapsed, sequence completed!");
            stopwatch.Stop();

            // Eredeti pozíciók és alpha értékek visszaállítása
            foreach (var target in targets)
            {
                // Pozíció visszaállítása
                if (originalPositions.ContainsKey(target))
                {
                    target.transform.localPosition = originalPositions[target];
                }

                // Alpha visszaállítása
                var canvasGroup = target.GetComponent<CanvasGroup>();
                if (canvasGroup != null && originalAlphas.ContainsKey(target))
                {
                    canvasGroup.alpha = originalAlphas[target];
                }
            }

            uiVars.IsMenuTransitioning = false;        
        });
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
}