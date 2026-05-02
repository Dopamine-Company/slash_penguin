using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using DG.Tweening;

namespace Dopamine.SlashPenguin
{
    /// <summary>
    /// 흰박스 프로토타입 씬 진입점.
    /// 씬에 이 컴포넌트 하나만 있으면 Play 시 전체 오브젝트/컴포넌트를 자동 생성한다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private PenguinGameSettings _settings;

        void Awake()
        {
            if (_settings == null)
                _settings = ScriptableObject.CreateInstance<PenguinGameSettings>();

            DOTween.Init(recycleAllByDefault: true, useSafeMode: true);

            SetupCamera();
            BuildScene();
        }

        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.transform.position = new Vector3(0f, 0.5f, -5f);
            cam.transform.rotation = Quaternion.identity;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.18f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        private void BuildScene()
        {
            // ── 펭귄 계층 ──────────────────────────────
            var penguin = new GameObject("Penguin");
            penguin.transform.position = Vector3.zero;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(penguin.transform, false);

            var leftButtock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leftButtock.name = "LeftButtock";
            leftButtock.transform.SetParent(penguin.transform, false);
            leftButtock.transform.localPosition = new Vector3(-0.35f, -0.6f, -0.4f);
            leftButtock.transform.localScale = Vector3.one * 0.5f;

            var rightButtock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightButtock.name = "RightButtock";
            rightButtock.transform.SetParent(penguin.transform, false);
            rightButtock.transform.localPosition = new Vector3(0.35f, -0.6f, -0.4f);
            rightButtock.transform.localScale = Vector3.one * 0.5f;

            var panty = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panty.name = "Panty";
            panty.transform.SetParent(penguin.transform, false);
            panty.transform.localPosition = new Vector3(0f, -0.3f, -0.35f);
            panty.transform.localScale = new Vector3(0.85f, 0.15f, 0.4f);

            // 볼기짝에 emission 지원 머티리얼 적용
            var buttockMat = CreateButtockMaterial();
            leftButtock.GetComponent<Renderer>().material = buttockMat;
            rightButtock.GetComponent<Renderer>().sharedMaterial = buttockMat;

            // ── UI ────────────────────────────────────
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            var overlayGO = new GameObject("PoopOverlay");
            overlayGO.transform.SetParent(canvas.transform, false);
            var overlayImage = overlayGO.AddComponent<Image>();
            overlayImage.color = new Color(1f, 1f, 1f, 0f);
            var rt = overlayGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            overlayGO.SetActive(false);

            // ── 매니저 컴포넌트 ────────────────────────
            // 자신(GameManagers)에 추가한다. Awake에서 AddComponent하면 즉시 해당 Awake 호출.
            var managers = gameObject;

            var stateMachine = managers.AddComponent<GameStateMachine>();

            var swipeInput = managers.AddComponent<SwipeInputController>();
            Set(swipeInput, "_settings", _settings);

            var redness = managers.AddComponent<RednessController>();
            Set(redness, "_leftButtockRenderer", leftButtock.GetComponent<Renderer>());
            Set(redness, "_rightButtockRenderer", rightButtock.GetComponent<Renderer>());

            var fart = managers.AddComponent<FartController>();
            Set(fart, "_settings", _settings);
            Set(fart, "_rednessController", redness);

            var buttock = managers.AddComponent<ButtockController>();
            Set(buttock, "_settings", _settings);
            Set(buttock, "_leftButtock", leftButtock.transform);
            Set(buttock, "_rightButtock", rightButtock.transform);
            Set(buttock, "_rednessController", redness);
            Set(buttock, "_fartController", fart);

            var pantyCtrl = managers.AddComponent<PantyController>();
            Set(pantyCtrl, "_settings", _settings);
            Set(pantyCtrl, "_pantyTransform", panty.transform);

            var ending = managers.AddComponent<EndingController>();
            Set(ending, "_settings", _settings);
            Set(ending, "_poopOverlay", overlayImage);
            Set(ending, "_buttockController", buttock);
            Set(ending, "_pantyController", pantyCtrl);
            Set(ending, "_rednessController", redness);
            Set(ending, "_fartController", fart);
        }

        private static Material CreateButtockMaterial()
        {
            // 임시 Sphere에서 프로젝트 기본 셰이더를 그대로 가져온다.
            // Shader.Find()는 URP/HDRP 이름이 달라 핑크가 될 수 있어 사용하지 않음.
            var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var baseShader = temp.GetComponent<Renderer>().sharedMaterial.shader;
            Destroy(temp);

            var mat = new Material(baseShader) { name = "ButtockMat" };
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
            return mat;
        }

        // private SerializeField를 리플렉션으로 주입 (흰박스 bootstrap 전용)
        private static void Set(object target, string field, object value)
        {
            var f = target.GetType().GetField(field,
                BindingFlags.NonPublic | BindingFlags.Instance);
            f?.SetValue(target, value);
        }
    }
}
