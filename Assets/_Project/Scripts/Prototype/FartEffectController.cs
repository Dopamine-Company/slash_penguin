using UnityEngine;
using UnityEngine.Rendering;

public class FartEffectController : MonoBehaviour
{
    [Header("Game Loop")]
    [SerializeField] private GameLoopController gameLoopController;

    [Header("Anchors")]
    [SerializeField] private Transform leftFartAnchor;
    [SerializeField] private Transform rightFartAnchor;

    [Header("Particles")]
    [SerializeField] private ParticleSystem leftFartParticle;
    [SerializeField] private ParticleSystem rightFartParticle;
    [SerializeField] private bool createParticlesIfMissing = true;

    [Header("Generated Particle Look")]
    [SerializeField] private Material particleMaterial;
    [SerializeField] private Color fartColor = new Color(0.72f, 0.82f, 0.45f, 0.65f);
    [SerializeField] private float particleLifetime = 0.55f;
    [SerializeField] private float particleStartSize = 0.08f;
    [SerializeField] private float particleEndSizeMultiplier = 3f;
    [SerializeField] private int burstCount = 6;

    private Material generatedParticleMaterial;

    private void Awake()
    {
        if (gameLoopController == null)
        {
            gameLoopController = GetComponent<GameLoopController>();
        }

        EnsureParticles();
    }

    private void OnEnable()
    {
        if (gameLoopController != null)
        {
            gameLoopController.OnPenalty.AddListener(PlayFart);
        }
    }

    private void OnDisable()
    {
        if (gameLoopController != null)
        {
            gameLoopController.OnPenalty.RemoveListener(PlayFart);
        }
    }

    public void PlayFart(ButtTarget target)
    {
        EnsureParticles();

        if (target == ButtTarget.Left)
        {
            PlayParticle(leftFartParticle);
            return;
        }

        if (target == ButtTarget.Right)
        {
            PlayParticle(rightFartParticle);
            return;
        }

        PlayParticle(leftFartParticle);
        PlayParticle(rightFartParticle);
    }

    private void EnsureParticles()
    {
        if (!createParticlesIfMissing)
        {
            return;
        }

        if (leftFartParticle == null && leftFartAnchor != null)
        {
            leftFartParticle = CreateParticle(leftFartAnchor, "Left Fart Particle");
        }

        if (rightFartParticle == null && rightFartAnchor != null)
        {
            rightFartParticle = CreateParticle(rightFartAnchor, "Right Fart Particle");
        }
    }

    private ParticleSystem CreateParticle(Transform parent, string objectName)
    {
        GameObject particleObject = new GameObject(objectName);
        particleObject.SetActive(false);
        particleObject.transform.SetParent(parent, false);
        particleObject.transform.localPosition = Vector3.zero;

        ParticleSystem particle = particleObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = particle.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = particleLifetime;
        main.startLifetime = particleLifetime;
        main.startSpeed = 0f;
        main.startSize = particleStartSize;
        main.startColor = fartColor;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        ParticleSystem.EmissionModule emission = particle.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)Mathf.Max(1, burstCount))
        });

        ParticleSystem.ShapeModule shape = particle.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.01f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particle.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(fartColor, 0f),
                new GradientColorKey(fartColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(fartColor.a, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particle.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            AnimationCurve.EaseInOut(0f, 1f, 1f, Mathf.Max(1f, particleEndSizeMultiplier)));

        ParticleSystemRenderer particleRenderer = particle.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.material = GetParticleMaterial();

        particleObject.SetActive(true);
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return particle;
    }

    private Material GetParticleMaterial()
    {
        if (particleMaterial != null)
        {
            return particleMaterial;
        }

        if (generatedParticleMaterial != null)
        {
            return generatedParticleMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Transparent");
        }

        if (shader == null)
        {
            return null;
        }

        generatedParticleMaterial = new Material(shader)
        {
            name = "Generated Fart Particle Material",
            renderQueue = (int)RenderQueue.Transparent
        };

        generatedParticleMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        generatedParticleMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        generatedParticleMaterial.SetInt("_ZWrite", 0);

        if (generatedParticleMaterial.HasProperty("_BaseColor"))
        {
            generatedParticleMaterial.SetColor("_BaseColor", Color.white);
        }

        if (generatedParticleMaterial.HasProperty("_Color"))
        {
            generatedParticleMaterial.SetColor("_Color", Color.white);
        }

        return generatedParticleMaterial;
    }

    private static void PlayParticle(ParticleSystem particle)
    {
        if (particle == null)
        {
            return;
        }

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play(true);
    }
}
