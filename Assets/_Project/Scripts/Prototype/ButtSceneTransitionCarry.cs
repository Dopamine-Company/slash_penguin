using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtSceneTransitionCarry : MonoBehaviour
{
    [SerializeField] private GameObject leftVisual;
    [SerializeField] private GameObject rightVisual;
    [SerializeField] private string gameLoopSceneName = "GameLoop";
    [SerializeField] private float holdBeforeConvergeDuration = 0.15f;
    [SerializeField] private float convergeDuration = 0.75f;
    [SerializeField] private float autoStartDelay = 3f;
    [SerializeField] private Ease convergeEase = Ease.InOutSine;

    private bool isRunning;

    public static ButtSceneTransitionCarry Create(
        Transform sourceLeft,
        Transform sourceRight,
        string sceneName,
        float holdDuration,
        float duration,
        float startDelay,
        Ease ease)
    {
        GameObject root = new GameObject("ButtSceneTransitionCarry");
        ButtSceneTransitionCarry carry = root.AddComponent<ButtSceneTransitionCarry>();
        carry.gameLoopSceneName = sceneName;
        carry.holdBeforeConvergeDuration = holdDuration;
        carry.convergeDuration = duration;
        carry.autoStartDelay = startDelay;
        carry.convergeEase = ease;
        carry.leftVisual = CloneVisual(sourceLeft, "TransitionLeftButt");
        carry.rightVisual = CloneVisual(sourceRight, "TransitionRightButt");

        if (carry.leftVisual != null)
        {
            carry.leftVisual.transform.SetParent(root.transform, true);
        }

        if (carry.rightVisual != null)
        {
            carry.rightVisual.transform.SetParent(root.transform, true);
        }

        DontDestroyOnLoad(root);
        return carry;
    }

    public void StartTransition()
    {
        if (isRunning)
        {
            return;
        }

        isRunning = true;
        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        SceneManager.LoadScene(gameLoopSceneName);
        yield return null;

        GameLoopController gameLoopController = FindObjectOfType<GameLoopController>();

        if (gameLoopController == null)
        {
            Debug.LogWarning("ButtSceneTransitionCarry could not find GameLoopController after loading GameLoop scene.", this);
            Destroy(gameObject);
            yield break;
        }

        if (holdBeforeConvergeDuration > 0f)
        {
            yield return new WaitForSeconds(holdBeforeConvergeDuration);
        }

        yield return ConvergeToGameLoopButts(gameLoopController);

        HideVisuals();

        if (autoStartDelay > 0f)
        {
            yield return new WaitForSeconds(autoStartDelay);
        }

        gameLoopController.StartGameLoop();
        Destroy(gameObject);
    }

    private IEnumerator ConvergeToGameLoopButts(GameLoopController gameLoopController)
    {
        Sequence sequence = DOTween.Sequence();
        JoinConverge(sequence, leftVisual, gameLoopController.LeftButtTransform);
        JoinConverge(sequence, rightVisual, gameLoopController.RightButtTransform);

        if (!sequence.IsActive())
        {
            yield break;
        }

        yield return sequence.WaitForCompletion();
    }

    private void JoinConverge(Sequence sequence, GameObject visual, Transform target)
    {
        if (sequence == null || visual == null || target == null)
        {
            return;
        }

        Transform visualTransform = visual.transform;
        sequence.Join(visualTransform.DOMove(target.position, convergeDuration).SetEase(convergeEase));
        sequence.Join(visualTransform.DORotateQuaternion(target.rotation, convergeDuration).SetEase(convergeEase));
        sequence.Join(visualTransform.DOScale(target.lossyScale, convergeDuration).SetEase(convergeEase));
    }

    private static GameObject CloneVisual(Transform source, string cloneName)
    {
        if (source == null)
        {
            return null;
        }

        GameObject clone = Instantiate(source.gameObject, source.position, source.rotation);
        clone.name = cloneName;
        clone.transform.SetParent(null, true);
        clone.transform.localScale = source.lossyScale;
        RemoveRuntimeBehaviours(clone);
        return clone;
    }

    private static void RemoveRuntimeBehaviours(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            Destroy(behaviours[i]);
        }
    }

    private void HideVisuals()
    {
        if (leftVisual != null)
        {
            leftVisual.SetActive(false);
        }

        if (rightVisual != null)
        {
            rightVisual.SetActive(false);
        }
    }
}
