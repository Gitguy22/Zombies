using UnityEngine;
using System;
using System.Collections.Generic;

namespace Tonic.PostProcessing
{

[System.Serializable]
public abstract class TonicEffectBase
{
    [Header("Effect State")]
    public bool enabled = false;
    public bool active = true;

    [HideInInspector] public Camera camera;

    public abstract string EffectName { get; }
    public abstract string ShaderName { get; }
    public abstract string Description { get; }
    public abstract int ExecutionOrder { get; }

    public abstract void RevertToDefaults();

    protected Shader _shader;
    protected Material _material;

    public Material GetMaterial()
    {
        if (_shader == null)
        {
            _shader = Shader.Find(ShaderName);
            if (_shader == null)
            {
                Debug.LogWarning($"[TonicPostProcessing] Shader not found: {ShaderName}");
                return null;
            }
        }

        if (_material == null || _material.shader != _shader)
        {
            if (_material != null) UnityEngine.Object.DestroyImmediate(_material);
            _material = new Material(_shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        return _material;
    }

    public abstract void ConfigureMaterial(Material material, RenderTexture source);

    public virtual bool ShouldRender()
    {
        return enabled && active && GetMaterial() != null;
    }

    public virtual void Render(RenderTexture source, RenderTexture destination)
    {
        Material material = GetMaterial();
        if (material != null)
        {
            ConfigureMaterial(material, source);
            Graphics.Blit(source, destination, material);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }

    public virtual void Cleanup()
    {
        if (_material != null)
        {
            UnityEngine.Object.DestroyImmediate(_material);
            _material = null;
        }
    }
}

[System.Serializable]
public class TonicVignetteEffect : TonicEffectBase
{
    public enum VignetteType
    {
        Circular,
        Square,
        TopBottom
    }

    [Header("Vignette Settings")]
    public Color color = Color.black;
    [Range(0f, 1f)] public float intensity = 0.8f;
    [Tooltip("Controls the softness of the vignette edge. Lower values result in softer edges.")]
    [Range(0.1f, 8f)] public float smoothness = 5f;
    [Range(0.01f, 1.0f)] public float size = 0.5f;
    public VignetteType type = VignetteType.TopBottom;

    private static readonly Color defaultColor = Color.black;
    private static readonly float defaultIntensity = 0.8f;
    private static readonly float defaultSmoothness = 5f;
    private static readonly float defaultSize = 0.5f;
    private static readonly VignetteType defaultType = VignetteType.TopBottom;

    public override string EffectName => "Tonic Vignette";
    public override string ShaderName => "Hidden/TonicPostProcessing/TonicVignetteShader";
    public override string Description => "Screen-space vignette effect";
    public override int ExecutionOrder => 9;

    public override void RevertToDefaults()
    {
        color = defaultColor;
        intensity = defaultIntensity;
        smoothness = defaultSmoothness;
        size = defaultSize;
        type = defaultType;
    }

    public override void ConfigureMaterial(Material material, RenderTexture source)
    {
        if (material == null || source == null) return;

        float minSliderSmoothness = 0.1f;
        float maxSliderSmoothness = 8.0f;
        float shaderMinExponent = 0.1f;
        float shaderMaxExponent = 8.0f;

        float normalizedUserSmoothness = (smoothness - minSliderSmoothness) / (maxSliderSmoothness - minSliderSmoothness);
        float remappedShaderSmoothness = shaderMaxExponent - (normalizedUserSmoothness * (shaderMaxExponent - shaderMinExponent));
        remappedShaderSmoothness = Mathf.Clamp(remappedShaderSmoothness, shaderMinExponent, shaderMaxExponent);

        material.SetColor("_VignetteColor", color);
        material.SetFloat("_VignetteIntensity", intensity);
        material.SetFloat("_VignetteSmoothness", remappedShaderSmoothness);
        material.SetFloat("_VignetteSize", Mathf.Max(0.01f, size));
        material.SetFloat("_VignetteType", (float)type);
        material.SetFloat("_ScreenAspect", (float)source.width / source.height);
    }
}

[System.Serializable]
public class TonicSharpnessEffect : TonicEffectBase
{
    [Header("Sharpness Settings")]
    [Range(0f, 3f)]
    [Tooltip("How much to enhance texture details. Higher = more crisp textures")]
    public float strength = 1.5f;

    [Range(0.5f, 2f)]
    [Tooltip("Detail size to enhance. Lower = fine details, Higher = broader features")]
    public float fineness = 1.0f;

    private static readonly float defaultStrength = 1.5f;
    private static readonly float defaultFineness = 1.0f;

    public override string EffectName => "Tonic Sharpness";
    public override string ShaderName => "Hidden/TonicPostProcessing/TonicSharpness";
    public override string Description => "LumaBased Sharpness";
    public override int ExecutionOrder => 1;

    public override void RevertToDefaults()
    {
        strength = defaultStrength;
        fineness = defaultFineness;
    }

    public override void ConfigureMaterial(Material material, RenderTexture source)
    {
        if (material == null) return;

        material.SetFloat("_Strength", strength);
        material.SetFloat("_Fineness", fineness);
    }
}



[System.Serializable]
public class TonicMotionBlurEffect : TonicEffectBase 
{
    [Header("Motion-blur")]
    [Range(0f, 2f)] public float blurScale = 1.0f;
    [Range(2, 16)] public int sampleCount = 8;
    [Range(1f, 100f)] public float maxBlurPixels = 30f;
    [Range(0f, 5f)] public float minBlurPixels = 0.5f;


    static readonly Vector3[] k_ViewDirs =
    {
        new Vector3( 0f,  0f,  1f),  
        new Vector3( 0.35f,  0f,  1f),
        new Vector3(-0.35f,  0f,  1f),
        new Vector3( 0f,  0.35f,  1f),
        new Vector3( 0f, -0.35f,  1f),
    };

    Matrix4x4 _prevVP;
    bool _first = true;

    public override string EffectName => "Tonic Camera Motion Blur";
    public override string Description => "Camera Motion (Experimental)";
    public override string ShaderName => "Hidden/TonicPostProcessing/TonicMotionBlur";
    public override int ExecutionOrder => 2;

    public override void ConfigureMaterial(Material mat, RenderTexture src)
    {
        if (mat == null || camera == null || src == null) 
        {
            if (mat != null) mat.SetVector("_ScreenSpaceVelocity", Vector2.zero);
            return;
        }

        var currP = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        var currV = camera.worldToCameraMatrix;
        var currVP = currP * currV;

        Vector2 uvVelocity = Vector2.zero;


        if (!_first)
        {
            float depthForTestPoints = Mathf.Lerp(camera.nearClipPlane, camera.farClipPlane, 0.25f);
            depthForTestPoints = Mathf.Max(depthForTestPoints, camera.nearClipPlane + 0.01f);

            Vector2 sumNdcVelocity = Vector2.zero;
            int validSamples = 0;

            Matrix4x4 currentCameraInvViewMatrix = camera.cameraToWorldMatrix;

            foreach (var dirOffset in k_ViewDirs)
            {
                var pointInCurrentViewSpace = new Vector4(dirOffset.x * depthForTestPoints, dirOffset.y * depthForTestPoints, depthForTestPoints, 1f);
            
                var v4_currentView = new Vector4(dirOffset.x, dirOffset.y, depthForTestPoints, 1f);


                Vector4 pointInWorldSpace = currentCameraInvViewMatrix * v4_currentView;
                var currentClipH = currP * v4_currentView; 
                var prevClipH = _prevVP * pointInWorldSpace;    


                if (Mathf.Abs(currentClipH.w) < 1e-4f || Mathf.Abs(prevClipH.w) < 1e-4f ||
                    Mathf.Sign(currentClipH.w) * Mathf.Sign(prevClipH.w) < 0) 
                {
                    continue;
                }

                var currentNdc = new Vector2(currentClipH.x / currentClipH.w, currentClipH.y / currentClipH.w);
                var prevNdc = new Vector2(prevClipH.x / prevClipH.w, prevClipH.y / prevClipH.w);

                sumNdcVelocity += (currentNdc - prevNdc);
                validSamples++;
            }

            if (validSamples > 0)
            {
                Vector2 avgNdcVelocity = sumNdcVelocity / validSamples;

                float maxNdcVelocityMagnitude = 3.0f; // Tune this: 2.0 (full screen) to 4.0 (allows some overshoot)
                if (avgNdcVelocity.magnitude > maxNdcVelocityMagnitude)
                {
                    avgNdcVelocity = avgNdcVelocity.normalized * maxNdcVelocityMagnitude;
                }

                Vector2 pixVel = new Vector2(avgNdcVelocity.x * src.width * 0.5f,
                                             avgNdcVelocity.y * src.height * 0.5f);

                pixVel *= blurScale;

                float mag = pixVel.magnitude;
                if (mag < minBlurPixels)
                {
                    pixVel = Vector2.zero;
                }
                else if (mag > maxBlurPixels)
                {
                    pixVel = pixVel.normalized * maxBlurPixels;
                }

                if (src.width > 0 && src.height > 0)
                {
                    uvVelocity = new Vector2(pixVel.x / src.width, pixVel.y / src.height);
                }
            }
        }

        mat.SetVector("_ScreenSpaceVelocity", uvVelocity);
        mat.SetInt("_SampleCount", sampleCount);

        _prevVP = currVP;
        _first = false;
    }

    public override void RevertToDefaults()
    {
        blurScale = 1.0f;
        sampleCount = 8;
        maxBlurPixels = 30f;
        minBlurPixels = 0.5f;
    }

    public abstract class TonicEffectBase
    {
        public abstract string EffectName { get; }
        public abstract string Description { get; }
        public abstract string ShaderName { get; }
        public abstract int ExecutionOrder { get; }
        public abstract void ConfigureMaterial(Material mat, RenderTexture src);
        public abstract void RevertToDefaults();
    }
}




[System.Serializable]
public class TonicTonemapper : TonicEffectBase
{
    [Header("Core Tonemapping")]
    [Tooltip("Overall brightness adjustment. Applied in linear space before tonemapping.")]
    [Range(0.1f, 10f)] public float exposure = 1.5f;

    [Tooltip("Strength of the filmic S-curve contrast. Enhances visual punch after tonemapping.")]
    [Range(1f, 2.5f)] public float filmicContrast = 1.2f;

    [Header("Color Grading")]
    [Tooltip("Overall color saturation.")]
    [Range(0f, 2f)] public float saturation = 1.3f;

    [Tooltip("Artistic tint for shadows. RGB for color, Alpha for intensity.")]
    [ColorUsage(true, false)] public Color shadowTint = new Color(0.5f, 0.6f, 0.8f, 1f);

    [Tooltip("Artistic tint for highlights. RGB for color, Alpha for intensity.")]
    [ColorUsage(true, false)] public Color highlightTint = new Color(1f, 1f, 1f, 1f);

    [Tooltip("Color temperature adjustment. Negative values are cooler (blueish), positive are warmer (orangeish).")]
    [Range(-1f, 1f)] public float temperature = 1f;

    [Tooltip("Color tint adjustment. Negative values shift towards green, positive towards magenta.")]
    [Range(-1f, 1f)] public float tint = 0f;

    [Header("LUT (Look-Up Texture) Grading")]
    public Texture2D lutTexture1 = null;
    public Texture2D lutTexture2 = null;
    [Tooltip("Blend factor between LUT 1 and LUT 2. 0 uses LUT 1, 1 uses LUT 2, 0.5 blends them equally.")]
    [Range(0f, 1f)] public float lutBlend = 0.0f;


    [Header("Output Quality")]
    [Tooltip("Strength of the dithering effect to reduce color banding. Uses high-quality TPDF dithering.")]
    [Range(0f, 2.5f)] public float ditherStrength = 2.5f;

    private static readonly float defaultExposure = 1.5f;
    private static readonly float defaultFilmicContrast = 1.2f;
    private static readonly float defaultSaturation = 1.3f;
    private static readonly Color defaultShadowTint = new Color(0.5f, 0.6f, 0.8f, 1f);
    private static readonly Color defaultHighlightTint = new Color(1f, 1f, 1f, 0f);
    private static readonly float defaultTemperature = 1f;
    private static readonly float defaultTint = 0f;
    private static readonly float defaultDitherStrength = 2.5f;

    private static readonly Texture2D defaultLutTexture1 = null;
    private static readonly Texture2D defaultLutTexture2 = null;
    private static readonly float defaultLutBlend = 0.0f;


    public override string EffectName => "Tonic Filmic Tonemap";
    public override string ShaderName => "Hidden/TonicPostProcessing/TonicTonemapper";
    public override string Description => "High-quality filmic tonemapper.";
    public override int ExecutionOrder => 5;

    public override void RevertToDefaults()
    {
        exposure = defaultExposure;
        filmicContrast = defaultFilmicContrast;
        saturation = defaultSaturation;
        shadowTint = defaultShadowTint;
        highlightTint = defaultHighlightTint;
        temperature = defaultTemperature;
        tint = defaultTint;
        ditherStrength = defaultDitherStrength;

        lutTexture1 = defaultLutTexture1;
        lutTexture2 = defaultLutTexture2;
        lutBlend = defaultLutBlend;
    }

    public override void ConfigureMaterial(Material material, RenderTexture source)
    {
        if (material == null) return;

        material.SetFloat("_Exposure", exposure);
        material.SetFloat("_FilmicContrast", filmicContrast);
        material.SetFloat("_Saturation", saturation);
        material.SetColor("_ShadowTint", shadowTint);
        material.SetColor("_HighlightTint", highlightTint);
        material.SetFloat("_Temperature", temperature);
        material.SetFloat("_Tint", tint);
        material.SetFloat("_DitherStrength", ditherStrength);

        bool hasLut1 = lutTexture1 != null;
        bool hasLut2 = lutTexture2 != null;

        if (hasLut1 || hasLut2)
        {
            if (hasLut1 && !hasLut2)
            {
                material.SetTexture("_LutTex1", lutTexture1);
                material.SetTexture("_LutTex2", lutTexture1);
                material.SetFloat("_LutBlend", 0.0f);
            }
            else if (!hasLut1 && hasLut2)
            {
                material.SetTexture("_LutTex1", lutTexture2);
                material.SetTexture("_LutTex2", lutTexture2);
                material.SetFloat("_LutBlend", 1.0f);
            }
            else
            {
                material.SetTexture("_LutTex1", lutTexture1);
                material.SetTexture("_LutTex2", lutTexture2);
                material.SetFloat("_LutBlend", lutBlend);
            }
            material.SetFloat("_LutDim", 32.0f);
        }
        else
        {
            material.SetFloat("_LutDim", 0.0f);
            material.SetTexture("_LutTex1", Texture2D.blackTexture);
            material.SetTexture("_LutTex2", Texture2D.blackTexture);
            material.SetFloat("_LutBlend", 0.0f);
        }
    }
}





[System.Serializable]
public class TonicNoiseEffect : TonicEffectBase
{
    [Header("Noise Settings")]
    [Range(0f, 0.5f)]
    public float Intensity = 0.1f;

    public bool NoiseAnimation = true;

    public bool Monochrome = false;


    private static readonly float defaultStrength = 0.1f;
    private static readonly bool defaultNoiseAnimation = true;
    private static readonly bool defaultMonochrome = false;

    public override string EffectName => "Tonic Noise";
    public override string ShaderName => "Hidden/TonicPostProcessing/TonicNoise";
    public override string Description => "Noise Effect";
    public override int ExecutionOrder => 8;

    public override void RevertToDefaults()
    {
        Intensity = defaultStrength;
        NoiseAnimation = defaultNoiseAnimation;
        Monochrome = defaultMonochrome;
    }

    public override void ConfigureMaterial(Material material, RenderTexture source)
    {
        if (material == null) return;

        material.SetFloat("_NoiseIntensity", Intensity);
        material.SetFloat("_NoiseSpeed", NoiseAnimation ? 1f : 0f);
        material.SetFloat("_Monochrome", Monochrome ? 1f : 0f);
    }
}


[System.Serializable]
public class TonicSSAOEffect : TonicEffectBase
{
    public enum VisualizationMode { None = 0, ViewAO = 1, ViewBleeding = 2 }
    public enum Quality { Low, Medium, High, Ultrahigh }

    [Header("AO Settings")]
    public Quality quality = Quality.High;
    [Range(0f, 20f)] public float strength = 2f;
    [Range(0.0f, 10f)] public float radius = 0.2f;
    [Range(1, 4)] public int downsampling = 1;
    public bool blur = true;
    public Color color = Color.black;
    public VisualizationMode visualization = VisualizationMode.None;

    private readonly Vector2 radiusRange = new Vector2(0.02f, 0.3f);
    private readonly float occlusionBias = 0.05f;
    private readonly float occlusionOffset = 0f;
    private readonly float occlusionExponent = 2f;
    private readonly float luminanceModulation = 0f;
    private readonly float bleedingIntensity = 0f;
    private readonly float blurDepthThreshold = 1.0f;
    private readonly float blurNormalThreshold = 0.1f;

    private Texture2D _axisPattern;
    private string[] _keywords = new string[2];

    public override string EffectName => "Tonic SSAO";
    public override string ShaderName => "Hidden/TonicPostProcessing/TonicSSAO";
    public override string Description => "High Quality SSAO";
    public override int ExecutionOrder => 4; 

    public override void RevertToDefaults()
    {
        quality = Quality.High;
        strength = 2f;
        radius = 0.2f;
        downsampling = 1;
        blur = true;
        color = Color.black;
        visualization = VisualizationMode.None;
    }

    public override void ConfigureMaterial(Material material, RenderTexture source) { }

    public override void Render(RenderTexture source, RenderTexture destination)
    {
        Material material = GetMaterial();
        if (material == null || camera == null)
        {
            Graphics.Blit(source, destination);
            return;
        }


        if (_axisPattern == null)
        {
            _axisPattern = Resources.Load<Texture2D>("SSAO_Pattern");
            if (_axisPattern == null)
            {
                Debug.LogError("SSAO Pattern texture not found at Assets/Resources/SSAO_Pattern. Please ensure it exists.");
                Graphics.Blit(source, destination);
                return;
            }
        }

        _keywords[1] = (bleedingIntensity > 0) ? "COLORBLEEDING_ON" : null;
        _keywords[0] = (quality == Quality.Low) ? "SAMPLES_2" : (quality == Quality.Medium) ? "SAMPLES_4" : (quality == Quality.High) ? "SAMPLES_6" : "SAMPLES_8";
        material.shaderKeywords = _keywords;

        float far = camera.farClipPlane;
        float y = 2 * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * far;
        float x = y * camera.aspect;
        material.SetVector("_FarCorner", new Vector3(x, y, far));

        material.SetVector("_Params", new Vector4(radius, occlusionBias, occlusionOffset, 1.0f / (radius * radius * 10)));
        material.SetVector("_Params2", new Vector4(strength, occlusionExponent, 0, radiusRange.x));
        material.SetVector("_Params3", new Vector4(bleedingIntensity, 0, blurDepthThreshold, blurNormalThreshold));
        material.SetVector("_Params4", new Vector4(radiusRange.y, luminanceModulation, 0, 0));
        material.SetColor("_OcclusionColor", color);
        material.SetTexture("_AxisTexture", _axisPattern);


        RenderTexture ssao = RenderTexture.GetTemporary(source.width / downsampling, source.height / downsampling, 0, RenderTextureFormat.ARGBHalf);
        material.SetVector("_InterleavePatternScale", new Vector2((float)ssao.width / _axisPattern.width, (float)ssao.height / _axisPattern.height));
        Graphics.Blit(source, ssao, material, 0);

        if (blur)
        {
            RenderTexture rtBlurX = RenderTexture.GetTemporary(source.width, source.height, 0);
            material.SetVector("_TexelOffsetScale", new Vector4(1f / ssao.width, 0, 0, 0));
            material.SetTexture("_SSAO", ssao);
            Graphics.Blit(null, rtBlurX, material, 1); 
            RenderTexture.ReleaseTemporary(ssao);

            RenderTexture rtBlurY = RenderTexture.GetTemporary(source.width, source.height, 0);
            material.SetVector("_TexelOffsetScale", new Vector4(0, 1f / ssao.height, 0, 0));
            material.SetTexture("_SSAO", rtBlurX);
            Graphics.Blit(null, rtBlurY, material, 1);
            RenderTexture.ReleaseTemporary(rtBlurX);

            ssao = rtBlurY;
        }

        material.SetTexture("_SSAO", ssao);
        Graphics.Blit(source, destination, material, 2 + (int)visualization);

        RenderTexture.ReleaseTemporary(ssao);
    }
}

[System.Serializable] 
public class TonicBloomEffect : TonicEffectBase
{
    private const int PASS_EXTRACT_BRIGHT_DOWNSAMPLE_INIT = 0;
    private const int PASS_DOWNSAMPLE = 1;
    private const int PASS_BLUR_HORIZONTAL = 2;
    private const int PASS_BLUR_VERTICAL = 3;
    private const int PASS_UPSAMPLE_ADDITIVE = 4;
    private const int PASS_COMPOSITE_FINAL = 5;

    [Header("Bloom Core Settings")]
    [Range(0f, 20f)] public float intensity = 1.0f;
    [Tooltip("Luminance threshold for pixels to start blooming.")]
    [Range(0f, 5f)] public float threshold = 1.0f;
    [Tooltip("Controls the softness of the threshold transition (0=hard, 1=very soft).")]
    [Range(0.01f, 1f)] public float softKnee = 1f;
    [Tooltip("Scales the blur offsets, affecting the bloom's spread.")]
    [Range(0.25f, 2f)] public float scatter = 1f;
    [Tooltip("Exposure adjustment for the bloom component before tonemapping.")]
    [Range(0.1f, 5f)] public float bloomExposure = 0.25f;

    [Header("Quality & Performance")]
    [Tooltip("Number of downsample iterations (mip levels for blur). More levels = wider, softer bloom, more cost.")]
    [Range(1, 6)] public int downsampleLevels = 5;
    [Tooltip("Number of blur iterations (Horizontal + Vertical) per mip level. More = softer, costlier.")]
    [Range(1, 3)] public int blurPassesPerLevel = 1;


    private RenderTexture[] _bloomPyramid; 

    public override string EffectName => "Tonic Bloom";
    public override string ShaderName => "Hidden/TonicBloomShader";
    public override string Description => "High-quality Bloom";
    public override int ExecutionOrder => 0;

    public override void RevertToDefaults()
    {
        intensity = 1.0f;
        threshold = 1.0f;
        softKnee = 1f;
        scatter = 1;
        bloomExposure = 0.25f;
        downsampleLevels = 5;
        blurPassesPerLevel = 1;
    }

    public override void ConfigureMaterial(Material material, RenderTexture source)
    {
    }

    public override bool ShouldRender()
    {
        bool baseShouldRender = base.ShouldRender();
        return baseShouldRender && intensity > 0.0001f;
    }

    void ReleasePyramid()
    {
        if (_bloomPyramid != null)
        {
            for (int i = 0; i < _bloomPyramid.Length; i++)
            {
                if (_bloomPyramid[i] != null)
                {
                    RenderTexture.ReleaseTemporary(_bloomPyramid[i]);
                    _bloomPyramid[i] = null;
                }
            }
        }
    }

    public override void Cleanup()
    {
        ReleasePyramid();
        base.Cleanup();
    }

    public override void Render(RenderTexture source, RenderTexture destination)
    {
        Material material = GetMaterial(); 

        if (material == null || camera == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        material.SetFloat("_Intensity", intensity);
        material.SetFloat("_Threshold", threshold);
        material.SetFloat("_SoftKneeWidth", threshold * softKnee);
        material.SetFloat("_Scatter", scatter);
        material.SetFloat("_BloomExposure", bloomExposure);

        RenderTextureFormat format = source.format;
        if (camera.allowHDR && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.DefaultHDR))
        {
            format = RenderTextureFormat.DefaultHDR;
        }

        int numEffectivePyramidLevels = Mathf.Max(1, downsampleLevels);
        if (_bloomPyramid == null || _bloomPyramid.Length != numEffectivePyramidLevels)
        {
            ReleasePyramid(); 
            _bloomPyramid = new RenderTexture[numEffectivePyramidLevels];
        }

        int currentWidth = source.width / 2;
        int currentHeight = source.height / 2;
        if (currentWidth < 1 || currentHeight < 1) { currentWidth = 1; currentHeight = 1; } 

        _bloomPyramid[0] = RenderTexture.GetTemporary(currentWidth, currentHeight, 0, format);
        Graphics.Blit(source, _bloomPyramid[0], material, PASS_EXTRACT_BRIGHT_DOWNSAMPLE_INIT);

        RenderTexture lastLevelRT = _bloomPyramid[0];
        for (int i = 1; i < numEffectivePyramidLevels; i++)
        {
            currentWidth /= 2;
            currentHeight /= 2;
            if (currentWidth < 1 || currentHeight < 1)
            {
                numEffectivePyramidLevels = i; break; 
            }
            _bloomPyramid[i] = RenderTexture.GetTemporary(currentWidth, currentHeight, 0, format);
            material.SetVector("_TexelSize", new Vector4(1.0f / lastLevelRT.width, 1.0f / lastLevelRT.height, lastLevelRT.width, lastLevelRT.height));
            Graphics.Blit(lastLevelRT, _bloomPyramid[i], material, PASS_DOWNSAMPLE);
            lastLevelRT = _bloomPyramid[i];
        }

        // --- 3. Blur Each Level of the Pyramid ---
        for (int i = 0; i < numEffectivePyramidLevels; i++)
        {
            RenderTexture levelToBlur = _bloomPyramid[i];
            RenderTexture tempBlurredForThisLevel = levelToBlur; 

            for (int j = 0; j < blurPassesPerLevel; j++)
            {
                RenderTexture rtBlurH = RenderTexture.GetTemporary(tempBlurredForThisLevel.width, tempBlurredForThisLevel.height, 0, format);
                material.SetVector("_TexelSize", new Vector4(1.0f / tempBlurredForThisLevel.width, 1.0f / tempBlurredForThisLevel.height, tempBlurredForThisLevel.width, tempBlurredForThisLevel.height));
                Graphics.Blit(tempBlurredForThisLevel, rtBlurH, material, PASS_BLUR_HORIZONTAL);

                RenderTexture rtBlurV = RenderTexture.GetTemporary(tempBlurredForThisLevel.width, tempBlurredForThisLevel.height, 0, format);
                material.SetVector("_TexelSize", new Vector4(1.0f / rtBlurH.width, 1.0f / rtBlurH.height, rtBlurH.width, rtBlurH.height));
                Graphics.Blit(rtBlurH, rtBlurV, material, PASS_BLUR_VERTICAL);

                RenderTexture.ReleaseTemporary(rtBlurH);
                if (j > 0)
                { 
                    RenderTexture.ReleaseTemporary(tempBlurredForThisLevel);
                }
                tempBlurredForThisLevel = rtBlurV; 
            }
            if (_bloomPyramid[i] != tempBlurredForThisLevel)
            {
                RenderTexture.ReleaseTemporary(_bloomPyramid[i]);
            }
            _bloomPyramid[i] = tempBlurredForThisLevel;
        }

        RenderTexture currentUpsampleResult = _bloomPyramid[numEffectivePyramidLevels - 1];

        for (int i = numEffectivePyramidLevels - 2; i >= 0; i--)
        {
            RenderTexture higherResAccumulationTarget = _bloomPyramid[i]; 
            material.SetTexture("_LowResTex", currentUpsampleResult);
            material.SetVector("_TexelSize", new Vector4(1.0f / higherResAccumulationTarget.width, 1.0f / higherResAccumulationTarget.height, higherResAccumulationTarget.width, higherResAccumulationTarget.height));

            RenderTexture tempCombined = RenderTexture.GetTemporary(higherResAccumulationTarget.width, higherResAccumulationTarget.height, 0, format);
            Graphics.Blit(higherResAccumulationTarget, tempCombined, material, PASS_UPSAMPLE_ADDITIVE);

            if (currentUpsampleResult != _bloomPyramid[numEffectivePyramidLevels - 1]) 
            {
                RenderTexture.ReleaseTemporary(currentUpsampleResult);
            }
            currentUpsampleResult = tempCombined;
        }
        material.SetTexture("_BloomTex", currentUpsampleResult);
        material.SetTexture("_SourceTex", source); // Original screen texture
        Graphics.Blit(source, destination, material, PASS_COMPOSITE_FINAL);

      
        RenderTexture.ReleaseTemporary(currentUpsampleResult);
   
        for (int i = 0; i < numEffectivePyramidLevels; ++i)
        {
            if (_bloomPyramid[i] != null)
            {
                RenderTexture.ReleaseTemporary(_bloomPyramid[i]);
                _bloomPyramid[i] = null; 
            }
        }
    }
}

[System.Serializable]
public class TonicLensEffects : TonicEffectBase
{
    [Header("Barrel/Pincushion Distortion")]
    [Tooltip("Strength of the distortion. Positive for barrel, negative for pincushion.")]
    [Range(-0.1f, 0.1f)] public float distortionAmount = 0.0f;
    [Tooltip("Center of the distortion effect (0.5, 0.5 is screen center).")]
    private Vector2 distortionCenter = new Vector2(0.5f, 0.5f);

    [Header("Chromatic Aberration")]
    [Tooltip("Intensity of the chromatic aberration effect.")]
    [Range(0f, 0.01f)] public float chromaticAberrationAmount = 0.0f;

    public override string EffectName => "Tonic Lens Effects";
    public override string ShaderName => "Hidden/TonicLensEffectsShader";
    public override string Description => "Applies Barrel/Pincushion Distortion and Chromatic Aberration.";
    public override int ExecutionOrder => 40; 

    public override void RevertToDefaults()
    {
        distortionAmount = 0.0f;
        distortionCenter = new Vector2(0.5f, 0.5f);
        chromaticAberrationAmount = 0.0f;
    }

    public override bool ShouldRender()
    {
        bool baseShould = base.ShouldRender();
        bool hasDistortion = Mathf.Abs(distortionAmount) > 0.001f;
        bool hasChromaticAberration = chromaticAberrationAmount > 0.0001f;
        return baseShould && (hasDistortion || hasChromaticAberration);
    }

    public override void ConfigureMaterial(Material material, RenderTexture source)
    {
    }

    public override void Render(RenderTexture source, RenderTexture destination)
    {
        Material material = GetMaterial();
        if (material == null || camera == null) 
        {
            Graphics.Blit(source, destination);
            return;
        }

        material.SetFloat("_DistortionAmount", distortionAmount);
        material.SetVector("_DistortionCenter", distortionCenter);
        material.SetFloat("_ChromaticAberrationAmount", chromaticAberrationAmount);


        Graphics.Blit(source, destination, material, 0);
    }

}

public enum AntiAliasingMode
{
    None = 0,
    FXAA = 1,
    NFAA = 2,
    DLAA = 3,
    SSAA = 4
}

    // Main Post Processing component
    [ExecuteInEditMode]
    [AddComponentMenu("Rendering/Tonic Post Processing")]
    [RequireComponent(typeof(Camera))]
    [ImageEffectAllowedInSceneView]
    public class TonicPostProcessing : MonoBehaviour
    {
        [Header("Default Settings")]
        [Range(0.5f, 1.0f)]
        [Tooltip("Controls the render resolution scale. Lower values improve performance, higher values improve quality.")]
        public float renderScale = 1.0f;

        [Tooltip("Anti-aliasing method to use for smoothing jagged edges.")]
        public AntiAliasingMode antiAliasingMode = AntiAliasingMode.None;

        [Header("Enhancement Effects")]
        public TonicSharpnessEffect tonicSharpness = new TonicSharpnessEffect();

        [Header("Lighting Effects")]
        public TonicSSAOEffect tonicSSAO = new TonicSSAOEffect();

        [Header("Motion Effect")]
        public TonicMotionBlurEffect tonicMotionBlur = new TonicMotionBlurEffect();

        [Header("Screen Effects")]
        public TonicVignetteEffect tonicVignette = new TonicVignetteEffect();

        [Header("Noise Effect")]
        public TonicNoiseEffect tonicNoise = new TonicNoiseEffect();

        [Header("Dithering Effect")]
        public TonicTonemapper tonicTonemapper = new TonicTonemapper();

        [Header("Bloom Effect")]
        public TonicBloomEffect tonicBloom = new TonicBloomEffect();

        [Header("DoF Effect")]
        public TonicLensEffects tonicLensEffects = new TonicLensEffects();


        private Camera _camera;
        private List<TonicEffectBase> _allEffects;
        private List<TonicEffectBase> _activeEffects;


        // AA materials
        private Material dlaaMaterial;
        private Material fxaaMaterial;
        private Material nfaaMaterial;
        private Material ssaaMaterial;
        private Shader dlaaShader;
        private Shader fxaaShader;
        private Shader nfaaShader;
        private Shader ssaaShader;



        void Awake()
        {
            _camera = GetComponent<Camera>();
            InitializeEffects();
        }

        void OnEnable()
        {
            _camera = GetComponent<Camera>();


            InitializeEffects();
            // This ensures depth+normals are available for effects that need them
            _camera.depthTextureMode |= DepthTextureMode.DepthNormals;

            // Warm up shader variants (if in build)
            var shaderVariants = Resources.Load<ShaderVariantCollection>("TonicShaderVariants");
            if (shaderVariants != null && !shaderVariants.isWarmedUp)
            {
                shaderVariants.WarmUp();
                Debug.Log("TonicPostProcessing: Shader variants warmed up.");
            }

            LoadAAShaders();


        }

        void LoadAAShaders()
        {
            // DLAA
            dlaaShader = Shader.Find("Hidden/TonicDLAA");
            if (dlaaShader != null)
                dlaaMaterial = new Material(dlaaShader);
            else
                Debug.LogWarning("DLAA shader not found in AA/DLAA");

            // FXAA
            fxaaShader = Shader.Find("Hidden/TonicFXAA");
            if (fxaaShader != null)
                fxaaMaterial = new Material(fxaaShader);
            else
                Debug.LogWarning("FXAA shader not found in AA/FXAA");

            // NFAA
            nfaaShader = Shader.Find("Hidden/TonicNFAA");
            if (nfaaShader != null)
                nfaaMaterial = new Material(nfaaShader);
            else
                Debug.LogWarning("NFAA shader not found in AA/NFAA");

            // SSAA
            ssaaShader = Shader.Find("Hidden/TonicSSAA");
            if (ssaaShader != null)
                ssaaMaterial = new Material(ssaaShader);
            else
                Debug.LogWarning("NFAA shader not found in AA/SSAA");
        }


        void InitializeEffects()
        {
            // Collect all effects
            _allEffects = new List<TonicEffectBase>
        {
            tonicSharpness,
            tonicSSAO,
            tonicVignette,
            tonicMotionBlur,
            tonicTonemapper,
            tonicNoise,
            tonicBloom,
            tonicLensEffects
        };

            // Pass the camera reference to each effect.
            foreach (var effect in _allEffects)
            {
                if (effect != null)
                    effect.camera = _camera;
            }

            _activeEffects = new List<TonicEffectBase>();
        }

        void OnDisable()
        {
            CleanupEffects();
        }

        void CleanupEffects()
        {
            if (_allEffects != null)
            {
                foreach (var effect in _allEffects)
                {
                    effect?.Cleanup();
                }
            }
        }



        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (_camera == null) _camera = GetComponent<Camera>();

            bool wantAA = antiAliasingMode != AntiAliasingMode.None;
            int fullW = source.width;
            int fullH = source.height;

            int scaledW = Mathf.RoundToInt(fullW * renderScale);
            int scaledH = Mathf.RoundToInt(fullH * renderScale);

            RenderTexture scaledRT = RenderTexture.GetTemporary(scaledW, scaledH, 0, source.format);
            scaledRT.filterMode = FilterMode.Point;
            Graphics.Blit(source, scaledRT);

            _activeEffects.Clear();
            foreach (var e in _allEffects)
                if (e != null && e.ShouldRender()) _activeEffects.Add(e);

            _activeEffects.Sort((a, b) => a.ExecutionOrder.CompareTo(b.ExecutionOrder));

            RenderTexture postFXTarget = wantAA
                ? RenderTexture.GetTemporary(fullW, fullH, 0, source.format)
                : destination;

            if (_activeEffects.Count == 0)
            {
                Graphics.Blit(scaledRT, postFXTarget);
            }
            else
            {
                RenderTexture current = scaledRT;

                for (int i = 0; i < _activeEffects.Count; i++)
                {
                    bool last = (i == _activeEffects.Count - 1);

                    RenderTexture next = last
                        ? postFXTarget                                   // final effect
                        : RenderTexture.GetTemporary(scaledW, scaledH, 0, source.format);

                    _activeEffects[i].Render(current, next);

                    if (current != scaledRT)
                        RenderTexture.ReleaseTemporary(current);

                    current = next;
                }
            }

            if (wantAA)
                ApplyAntiAliasing(postFXTarget, destination);

            if (postFXTarget != destination)
                RenderTexture.ReleaseTemporary(postFXTarget);
            RenderTexture.ReleaseTemporary(scaledRT);
        }

        void ApplyAntiAliasing(RenderTexture source, RenderTexture destination)
        {
            switch (antiAliasingMode)
            {
                case AntiAliasingMode.FXAA:
                    {
                        if (fxaaMaterial == null)
                        {
                            Graphics.Blit(source, destination);
                            break;
                        }

                        Graphics.Blit(source, destination, fxaaMaterial);
                        break;
                    }


                case AntiAliasingMode.SSAA:
                    if (ssaaMaterial != null)
                        Graphics.Blit(source, destination, ssaaMaterial);
                    else
                        Graphics.Blit(source, destination);
                    break;


                case AntiAliasingMode.NFAA:
                    if (nfaaMaterial != null)
                        Graphics.Blit(source, destination, nfaaMaterial);
                    else
                        Graphics.Blit(source, destination);
                    break;

                case AntiAliasingMode.DLAA:
                    {
                        if (dlaaMaterial == null)
                        {
                            Graphics.Blit(source, destination);
                            break;
                        }

                        int edgePass = 0;

                        int wEdge = true ? source.width : source.width / 2;
                        int hEdge = true ? source.height : source.height / 2;

                        dlaaMaterial.SetFloat("_Intensity", 0.5f);

                        RenderTexture rt1 = RenderTexture.GetTemporary(wEdge, hEdge, 0, source.format);
                        RenderTexture rt2 = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);

                        Graphics.Blit(source, rt1, dlaaMaterial, edgePass);
                        Graphics.Blit(rt1, rt2, dlaaMaterial, 1);
                        Graphics.Blit(rt2, destination, dlaaMaterial, 3);

                        RenderTexture.ReleaseTemporary(rt1);
                        RenderTexture.ReleaseTemporary(rt2);
                        break;
                    }

            }
        }


        public List<TonicEffectBase> GetAllEffects()
        {
            if (_allEffects == null) InitializeEffects();
            return _allEffects;
        }

        public List<TonicEffectBase> GetActiveEffects()
        {
            var activeEffects = new List<TonicEffectBase>();
            foreach (var effect in GetAllEffects())
            {
                if (effect != null && effect.enabled)
                {
                    activeEffects.Add(effect);
                }
            }
            return activeEffects;
        }

        public T GetEffect<T>() where T : TonicEffectBase
        {
            foreach (var effect in GetAllEffects())
            {
                if (effect is T)
                {
                    return effect as T;
                }
            }
            return null;
        }

        public void EnableEffect<T>() where T : TonicEffectBase
        {
            var effect = GetEffect<T>();
            if (effect != null) effect.enabled = true;
        }

        public void DisableEffect<T>() where T : TonicEffectBase
        {
            var effect = GetEffect<T>();
            if (effect != null) effect.enabled = false;
        }

        public void ToggleEffect<T>() where T : TonicEffectBase
        {
            var effect = GetEffect<T>();
            if (effect != null) effect.enabled = !effect.enabled;
        }
    }
}