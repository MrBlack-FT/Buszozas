using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Linq;

public class UIActionSequence : MonoBehaviour
{
    private UIVars uiVars;
    [SerializeField] private Debugger debugger;

    private Sequence sequence;

    // Colored boxes:🟩, 🟦, 🟨, 🟧, 🟫, 🟪, 🟥

    //Az Awake-ben keresi meg a szükséges komponenseket. Ha nem találja, warning-ot ír ki a konzolra.
    private void Awake()
    {
        uiVars = GameObject.Find("UIVars").GetComponent<UIVars>();
        if (uiVars == null)
        {
            Debug.LogWarning("UIVars not found in the scene.");
        }

        if (debugger == null)
        {
            debugger = Resources.FindObjectsOfTypeAll<Debugger>().FirstOrDefault();
        }
    }

    // Update-ben frissíti a Debugger-t az IsMenuTransitioning státuszával.
    private void Update()
    {
        if (debugger != null && debugger.gameObject.activeSelf)
        {
            string isMenuTransitioningStatus = debugger.ColoredString(uiVars.IsMenuTransitioning ? "TRUE" : "FALSE", uiVars.IsMenuTransitioning ? Color.green : Color.red);
            debugger.UpdatePersistentLog("IsMenuTransitioning", isMenuTransitioningStatus);
        }
    }
    
    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, float> originalAlphas = new Dictionary<GameObject, float>();

    public void BuildAndRunSequence(List<UIActionConfig> actions)
    {
        /*
        string debugLogstring = "";
        foreach (var action in actions)
        {
            debugLogstring += $"\n🟩 Action: {action.actionType} \n\t🟦 Target: {action.target} \n\t🟨 Duration: {action.duration} \n\t🟧 Direction: {action.direction} \n\t🟫 Timing: {action.timing}";
        }
        debugLogstring += $"\n🟪 Total Actions: {actions.Count}";
        Debug.Log(debugLogstring);
        */

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

        //Debug.Log($"{stopwatch.ElapsedMilliseconds} ms telt el miután mentésre került {targets.Count} target eredeti pozíciója és alpha értéke.");
        sequence = DOTween.Sequence();

        // Szekvencia felépítése
        for (int i = 0; i < actions.Count; i++)
        {
            var action = actions[i];
            
            switch (action.actionType)
            {
                case ActionType.InvokeEvent:
                    // UNITY EVENT - nem kell target
                    if (i == 0 || action.timing == ActionTiming.AfterPrevious)
                    {
                        sequence.AppendCallback(() => action.unityEvent?.Invoke());
                    }
                    else if (action.timing == ActionTiming.WithPrevious)
                    {
                        sequence.JoinCallback(() => action.unityEvent?.Invoke());
                    }
                    break;

                case ActionType.FadeIn:
                case ActionType.FadeOut:
                    // NULL TARGET ellenőrzés FadeIn/FadeOut esetén
                    if (!action.target)
                    {
                        Debug.LogWarning("Missing target on action: " + action.actionType);
                        continue;
                    }

                    var canvasGroup = action.target.GetComponent<CanvasGroup>();
                    if (!canvasGroup)
                    {
                        Debug.LogWarning("Missing CanvasGroup on " + action.target.name + "    | Parent: " + action.target?.transform.parent?.name);
                        continue;
                    }

                    Vector3 originalPosition = originalPositions[action.target];
                    Vector3 offset = GetDirectionOffset(action.direction);

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
                    else // ActionType.FadeOut
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
                    continue;

                default:
                    Debug.LogWarning("Unknown ActionType: " + action.actionType);
                    break;
            }
        }

        //Debug.Log($"{stopwatch.ElapsedMilliseconds} ms has elapsed, sequence built with {actions.Count} actions.");

        sequence.OnComplete(() =>
        {
            //Debug.Log($"{stopwatch.ElapsedMilliseconds} ms has elapsed, sequence completed!");
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
            case Direction.NONE:
                return Vector3.zero;
            default:
                Debug.LogWarning("Unknown direction: " + direction);
                return Vector3.zero;
        }
    }
}