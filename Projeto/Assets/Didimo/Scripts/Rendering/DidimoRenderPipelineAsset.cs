
/// by Tom Sirdevan for Didimo

using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using System.Collections.Generic;
// using UnityEngine.Experimental.Rendering; /// NOTE: for 2018 support


namespace Didimo.Rendering
{
    ///
    /// DidimoRenderPipelineAsset
    /// 
    [CreateAssetMenu(menuName = "Rendering/Didimo Render Pipeline")]
    public class DidimoRenderPipelineAsset : RenderPipelineAsset
    {
        [SerializeField]
        bool SubsurfaceScatter = false;

        [SerializeField] // [Range(0.0f, 1.0f)]
        float SubsurfaceScatterRadius = 0.7f;

        [SerializeField]
        float SubsurfaceDepthBias = 0.01f;

        [SerializeField]
        float SubsurfaceDesaturate = 0.5f;

        [SerializeField]
        float SubsurfaceDesaturateSpread = 0.5f;

        [SerializeField]
        bool ToneMapping = true;

        [SerializeField]
        float Exposure = 0f;

        [SerializeField]
        Color BgColor1 = new Color(0.08f, 0.091f, 0.091f);

        [SerializeField]
        Color BgColor2 = new Color(0.008f, 0.008f, 0.008f);

        [SerializeField]
        bool UseCubeMaps = true;

        [SerializeField]
        Cubemap Irradiance;

        [SerializeField]
        Cubemap Radiance;

        static DidimoRender rpInstance = null;

        public void setSssProfile(ref Vector4[] sssProfile)
        {
            rpInstance.setSssProfile(ref sssProfile);
        }

        // public void setSssRadius(float v)
        // {
        //   rpInstance.setSssRadius(v);
        // }

        // public void setSssBias(float v)
        // {
        //   rpInstance.setSssBias(v);
        // }

        // public void setSssDesat(float v)
        // {
        //   rpInstance.setSssDesat(v);
        // }

        // public void setSssDesatSpread(float v)
        // {
        //   rpInstance.setSssDesat(v);
        // }

        protected override RenderPipeline CreatePipeline()
        {
            QualitySettings.shadows = ShadowQuality.All;
            // GraphicsSettings.useScriptableRenderPipelineBatching = true;

            // Application.targetFrameRate = -1; /// -1 by default

            rpInstance = new DidimoRender();
            rpInstance.updateParams(SubsurfaceScatter, SubsurfaceScatterRadius, SubsurfaceDepthBias, SubsurfaceDesaturate, SubsurfaceDesaturateSpread, ToneMapping, Exposure, BgColor1, BgColor2, UseCubeMaps, ref Irradiance, ref Radiance);
            return rpInstance;
        }

        /// TODO: figure out which parameter changed and only have that one set
        protected override void OnValidate()
        {
            //Log.info("OnValidate");
            updateParams();
        }

        private void updateParams()
        {
            if (rpInstance != null) rpInstance.updateParams(SubsurfaceScatter, SubsurfaceScatterRadius, SubsurfaceDepthBias, SubsurfaceDesaturate, SubsurfaceDesaturateSpread, ToneMapping, Exposure, BgColor1, BgColor2, UseCubeMaps, ref Irradiance, ref Radiance);
        }

        /// NOTE: for 2018 support
        // protected override IRenderPipeline InternalCreatePipeline () {
        // 	return new DidimoRender();
        // }
    }


    ///
    /// DidimoRender
    /// 
    public partial class DidimoRender : RenderPipeline
    {
        const string bufferName = "Didimo Render Pipeline";

        /// direct lighting
        const int maxLights = 8; /// NOTE: should be same as MAX_NUM_LIGHTS in didimoLighting.cginc

        const int maxDirLights = 4;
        static Vector4[] dirLightColors = new Vector4[maxDirLights];
        static Vector4[] dirLightDirVecs = new Vector4[maxDirLights];

        const int maxSpotLights = 4;
        static Vector4[] spotLightColors = new Vector4[maxSpotLights];
        static Vector4[] spotLightDirVecs = new Vector4[maxSpotLights];
        static Vector4[] spotLightPosVecs = new Vector4[maxSpotLights];
        static Vector4[] spotLightAttens = new Vector4[maxSpotLights];

        const int maxPointLights = 4;
        static Vector4[] pointLightColors = new Vector4[maxPointLights];
        static Vector4[] pointLightPosVecs = new Vector4[maxPointLights];
        static float[] pointLightAttens = new float[maxPointLights];

        // const int maxShadowLights = 1;
        int shadowRes = 2048;
        int shadowDepthRes = 16;
        const int shadowKernalSize = 4; // 4 8 16 should be the same as SHADOW_KERNAL_SIZE in shaders/didimoLighting.cginc
        const float maxShadowDistance = 2.0f;

        bool sssOn = false;

        static Mesh fullScreenTriangle;

        static Material backgroundMat;
        static Material sssMat;
        static Material postProcessMat;

        /// NOTE: for 2018 support use ShaderPassName instead of ShaderTagId
        static ShaderTagId utilityAlphaId = new ShaderTagId("DidimoUtilityAlpha");
        static ShaderTagId defaultId = new ShaderTagId("DidimoDefault");
        static ShaderTagId hairAlphaId = new ShaderTagId("DidimoAlpha");
        static ShaderTagId hairId = new ShaderTagId("DidimoTrans");

        static ShaderTagId unityDefaultUnlitId = new ShaderTagId("SRPDefaultUnlit");
        // static ShaderTagId unityPrePasstId = new ShaderTagId("PrepassBase"); 
        // static ShaderTagId unityDefaultId = new ShaderTagId("ForwardBase"); // SRPDefault Always ForwardBase

        /// material shader params
        // static int numLightsId = Shader.PropertyToID("didimoNumLights");

        static int dirLightColorsId = Shader.PropertyToID("didimoDirLightColors");
        static int dirLightDirVecsId = Shader.PropertyToID("didimoDirLightDirVecs");
        static int numDirLightsId = Shader.PropertyToID("didimoNumDirLights");

        static int spotLightColorsId = Shader.PropertyToID("didimoSpotLightColors");
        static int spotLightDirVecsId = Shader.PropertyToID("didimoSpotLightDirVecs");
        static int spotLightPosVecsId = Shader.PropertyToID("didimoSpotLightPosVecs");
        static int spotLightAttensId = Shader.PropertyToID("didimoSpotLightAttens");
        static int numSpotLightsId = Shader.PropertyToID("didimoNumSpotLights");

        static int pointLightColorsId = Shader.PropertyToID("didimoPointLightColors");
        static int pointLightPosVecsId = Shader.PropertyToID("didimoPointLightPosVecs");
        static int pointLightAttensId = Shader.PropertyToID("didimoPointLightAttens");
        static int numPointLightsId = Shader.PropertyToID("didimoNumPointLights");

        static int lightSpace0Id = Shader.PropertyToID("didimoLightSpace0");
        static int shadow0TexId = Shader.PropertyToID("didimoShadowTex0");
        // static int shadowLightSpacesId = Shader.PropertyToID("didimoLightSpaces");
        // static int shadowTexsId = Shader.PropertyToID("didimoShadowTexs");

        static int didimoShadowKernalId = Shader.PropertyToID("didimoShadowKernal");
        static int didimoLinearId = Shader.PropertyToID("didimoLinear");

        /// backroung params
        static int bgColor1Id = Shader.PropertyToID("didimoBgColor1");
        static int bgColor2Id = Shader.PropertyToID("didimoBgColor2");

        /// sss params
        // static int sssOnId = Shader.PropertyToID("didimoSssOn");
        static int sssRadiusId = Shader.PropertyToID("didimoSssRadius");
        static int sssBiasId = Shader.PropertyToID("didimoSssDepthBias");
        static int sssDesatId = Shader.PropertyToID("didimoSssDesat");
        static int sssDesatSpreadId = Shader.PropertyToID("didimoSssDesatSpread");
        static int sssKernalId = Shader.PropertyToID("didimoSssKernal");
        static int sssHorizontalId = Shader.PropertyToID("didimoSssHorizontal");

        /// render targets
        static RenderTargetIdentifier[] rts = new RenderTargetIdentifier[2];
        static int defaultTexId = Shader.PropertyToID("didimoDefaultTex");
        static int skinTexId = Shader.PropertyToID("didimoSkinTex"); 
        static int defaultDepthTexId = Shader.PropertyToID("didimoDepthTex");
        static int sssTexId = Shader.PropertyToID("didimoSssTex");

        /// post process params
        static int tonemappingId = Shader.PropertyToID("didimoTonemapping");
        static int exposureId = Shader.PropertyToID("didimoExposure");

        /// cube maps
        static int useCubeMapsId = Shader.PropertyToID("didimoCubeMaps");
        static int irradianceId = Shader.PropertyToID("didimoIrradiance");
        static int radianceId = Shader.PropertyToID("didimoRadiance");

        Cubemap irradiance;
        Cubemap radiance;

        
        public void setSssProfile(ref Vector4[] sssProfile)
        {
            Log.info("setting sss kernal");
            cb.SetGlobalVectorArray(sssKernalId, sssProfile);
        }

        CommandBuffer cb = new CommandBuffer
        {
            name = bufferName
        };

        Camera camera;
        CullingResults cullingResults;
        ScriptableRenderContext context;

        public void updateParams(bool sssOn, float sssRadius, float sssBias, float sssDesat, float sssDesatSpread, bool tonemapping, float exposure, Color bgColor1, Color bgColor2, bool useCubeMaps, ref Cubemap irradiance, ref Cubemap radiance)
        {
            // Log.info(string.Format("updateParams: sssOn: {0},  sssRadius: {1}, sssBias: {2}, sssDesat: {3}, sssDesatSpread {4}, exposure: {5}", sssOn, sssRadius, sssBias, sssDesat, sssDesatSpread, exposure));

            if (QualitySettings.activeColorSpace == ColorSpace.Gamma)
                GraphicsSettings.lightsUseLinearIntensity = false;
            else
                GraphicsSettings.lightsUseLinearIntensity = true;


            this.sssOn = sssOn;
            // cb.SetGlobalInt(sssOnId, sssOn ? 1 : 0);
            cb.SetGlobalFloat(sssRadiusId, sssRadius);
            cb.SetGlobalFloat(sssBiasId, sssBias);
            cb.SetGlobalFloat(sssDesatId, sssDesat);
            cb.SetGlobalFloat(sssDesatSpreadId, sssDesatSpread);
            cb.SetGlobalInt(tonemappingId, tonemapping ? 1 : 0);
            cb.SetGlobalFloat(exposureId, exposure);

            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                cb.SetGlobalInt(didimoLinearId, 1);
                cb.SetGlobalColor(bgColor1Id, bgColor1.linear);
                cb.SetGlobalColor(bgColor2Id, bgColor2.linear);
            }
            else
            {
                cb.SetGlobalColor(bgColor1Id, bgColor1);
                cb.SetGlobalColor(bgColor2Id, bgColor2);
                cb.SetGlobalInt(didimoLinearId, 0);
            }

            cb.SetGlobalInt(useCubeMapsId, useCubeMaps ? 1 : 0);
            cb.SetGlobalTexture(irradianceId, irradiance);
            cb.SetGlobalTexture(radianceId, radiance);
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            /// static initialization
            if (!fullScreenTriangle)
            {
                string colorSpace = "linear";
                if (QualitySettings.activeColorSpace == ColorSpace.Gamma)
                {
                    colorSpace = "gamma";
                }
                Log.info("DidimoRender: initializing, color space: " + colorSpace);

                fullScreenTriangle = new Mesh
                {
                    name = "FullScreenTriangle",
                    vertices = new Vector3[] {
                      new Vector3(-1f, -1f, 0f),
                      new Vector3(-1f,  3f, 0f),
                      new Vector3( 3f, -1f, 0f)
                    },
                    triangles = new int[] { 0, 1, 2 },
                };
                fullScreenTriangle.UploadMeshData(true);

                backgroundMat =
                  new Material(Shader.Find("Didimo/background"))
                  {
                      name = "Background",
                      hideFlags = HideFlags.HideAndDontSave
                  };

                sssMat =
                  new Material(Shader.Find("Didimo/sss"))
                  {
                      name = "SSS",
                      hideFlags = HideFlags.HideAndDontSave
                  };

                postProcessMat =
                  new Material(Shader.Find("Didimo/postProcess"))
                  {
                      name = "Post-Processing",
                      hideFlags = HideFlags.HideAndDontSave
                  };

                /// default
                Vector4[] sssKernal = new Vector4[] {
                    new Vector4(0.560479f, 0.669086f, 0.784728f, 0f),
                    new Vector4(0.00471691f, 0.000184771f, 5.07566e-005f, -2f),
                    new Vector4(0.0192831f, 0.00282018f, 0.00084214f, -1.28f),
                    new Vector4(0.03639f, 0.0130999f, 0.00643685f, -0.72f),
                    new Vector4(0.0821904f, 0.0358608f, 0.0209261f, -0.32f),
                    new Vector4(0.0771802f, 0.113491f, 0.0793803f, -0.08f),
                    new Vector4(0.0771802f, 0.113491f, 0.0793803f, 0.08f),
                    new Vector4(0.0821904f, 0.0358608f, 0.0209261f, 0.32f),
                    new Vector4(0.03639f, 0.0130999f, 0.00643685f, 0.72f),
                    new Vector4(0.0192831f, 0.00282018f, 0.00084214f, 1.28f),
                    new Vector4(0.00471691f, 0.000184771f, 5.07565e-005f, 2f)
                };

                cb.SetGlobalVectorArray(sssKernalId, sssKernal);

                /// set shadow samples
                Vector4[] shadowKernal = new Vector4[shadowKernalSize];
                for (int i = 0; i < shadowKernalSize; i++)
                {
                    shadowKernal[i] = new Vector4(Random.value * 2.0f - 1.0f, Random.value * 2.0f - 1.0f);
                }

                cb.SetGlobalVectorArray(didimoShadowKernalId, shadowKernal);

                
            }


            this.context = context;

            foreach (Camera camera in cameras)
            {
                this.camera = camera;

                /// setup
#if UNITY_EDITOR
                if (camera.cameraType == CameraType.SceneView)
                {
                    ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
                }
#endif

                if (!camera.TryGetCullingParameters(out ScriptableCullingParameters p)) return;
                p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane); // maxShadowDistance camera.farClipPlane * 0.4f
                cullingResults = context.Cull(ref p);

                context.SetupCameraProperties(camera);

                // cb.BeginSample(bufferName);

                /// lights
                // int totalLights = 0;
                int numDirLights = 0;
                int numSpotLights = 0;
                int numPointLights = 0;
                bool drewShadows = false;
                NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
                for (int i = 0; i < visibleLights.Length; i++)
                {
                    VisibleLight vLight = visibleLights[i];

                    if (vLight.lightType == LightType.Directional)
                    {
                        dirLightColors[numDirLights] = vLight.finalColor;//.linear;
                        dirLightDirVecs[numDirLights] = -vLight.localToWorldMatrix.GetColumn(2);

                        numDirLights++;
                        // totalLights++;
                        if (numDirLights >= maxDirLights) break;
                        // if(totalLights >= maxLights) break;

                        /// only do shadows for the first light
                        if (i == 0 && cullingResults.GetShadowCasterBounds(i, out Bounds b) && cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(i, 0, 1, Vector3.zero, shadowRes, vLight.light.shadowNearPlane, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData))
                        {
                            cb.GetTemporaryRT(
                              shadow0TexId,
                              shadowRes, shadowRes,
                              shadowDepthRes, 
                              FilterMode.Bilinear, RenderTextureFormat.Shadowmap
                            );

                            cb.SetRenderTarget(
                              shadow0TexId,
                              RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                            );
                            cb.ClearRenderTarget(true, false, Color.clear, 1.0f); /// just clear the depth buffer
                            ExecuteBuffer(context);

                            var shadowSettings = new ShadowDrawingSettings(cullingResults, i);

                            shadowSettings.splitData = splitData;
                            cb.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                            cb.SetGlobalDepthBias(0f, vLight.light.shadowBias);

                            if (SystemInfo.usesReversedZBuffer)
                            {
                                projectionMatrix.m20 = -projectionMatrix.m20;
                                projectionMatrix.m21 = -projectionMatrix.m21;
                                projectionMatrix.m22 = -projectionMatrix.m22;
                                projectionMatrix.m23 = -projectionMatrix.m23;
                            }

                            /// -1 - 1 to 0 - 1 scale to match texture
                            var scaleOffset = Matrix4x4.identity;
                            scaleOffset.m00 = scaleOffset.m11 = scaleOffset.m22 = 0.5f;
                            scaleOffset.m03 = scaleOffset.m13 = scaleOffset.m23 = 0.5f;

                            cb.SetGlobalMatrix(lightSpace0Id, scaleOffset * (projectionMatrix * viewMatrix));
                            ExecuteBuffer(context);

                            context.DrawShadows(ref shadowSettings);

                            cb.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

                            drewShadows = true;
                        }
                    }
                    else if (vLight.lightType == LightType.Spot)
                    {
                        // Log.info("spot light found");

                        spotLightColors[numSpotLights] = vLight.finalColor;
                        spotLightDirVecs[numSpotLights] = -vLight.localToWorldMatrix.GetColumn(2);
                        spotLightPosVecs[numSpotLights] = vLight.localToWorldMatrix.GetColumn(3);

                        Vector4 attenuation = Vector4.zero;
                        attenuation.x = 1f / Mathf.Max(vLight.range * vLight.range, 0.00001f);

                        float outerRad = Mathf.Deg2Rad * 0.5f * vLight.spotAngle;
                        float outerCos = Mathf.Cos(outerRad);
                        float outerTan = Mathf.Tan(outerRad);
                        float innerCos =
                            Mathf.Cos(Mathf.Atan((46f / 64f) * outerTan));
                        float angleRange = Mathf.Max(innerCos - outerCos, 0.001f);
                        attenuation.z = 1f / angleRange;
                        attenuation.w = -outerCos * attenuation.z;

                        spotLightAttens[numSpotLights] = attenuation;

                        numSpotLights++;
                        // totalLights++;
                        if (numSpotLights >= maxSpotLights) break;
                        // if(totalLights >= maxLights) break;
                    }

                    else if (vLight.lightType == LightType.Point)
                    {
                        pointLightColors[numPointLights] = vLight.finalColor;
                        pointLightPosVecs[numPointLights] = vLight.localToWorldMatrix.GetColumn(3);
                        pointLightAttens[numPointLights] = 1f / Mathf.Max(vLight.range * vLight.range, 0.00001f);
                        numPointLights++;
                        // totalLights++;
                        if (numPointLights >= maxPointLights) break;
                        // if(totalLights >= maxLights) break;          
                    }
                }

                // cb.SetGlobalInt(numLightsId, totalLights);

                cb.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
                cb.SetGlobalVectorArray(dirLightDirVecsId, dirLightDirVecs);
                cb.SetGlobalInt(numDirLightsId, numDirLights);

                cb.SetGlobalVectorArray(spotLightColorsId, spotLightColors);
                cb.SetGlobalVectorArray(spotLightDirVecsId, spotLightDirVecs);
                cb.SetGlobalVectorArray(spotLightPosVecsId, spotLightPosVecs);
                cb.SetGlobalVectorArray(spotLightAttensId, spotLightAttens);
                cb.SetGlobalInt(numSpotLightsId, numSpotLights);

                cb.SetGlobalVectorArray(pointLightColorsId, pointLightColors);
                cb.SetGlobalVectorArray(pointLightPosVecsId, pointLightPosVecs);
                cb.SetGlobalFloatArray(pointLightAttensId, pointLightAttens);
                cb.SetGlobalInt(numPointLightsId, numPointLights);

                // Log.info("numDirLights {0}, numSpotLights: {1}, numPointLights: {2}", numDirLights, numSpotLights, numPointLights);

                var sortingSettings = new SortingSettings(camera);
                sortingSettings.criteria = SortingCriteria.CommonOpaque;
                var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
                var drawingSettings = new DrawingSettings(
                  defaultId
                  , sortingSettings
                );
                // {
                //     enableDynamicBatching = true, /// NOTE: doesn't seem to be working, for hair anyways (tried CBUFFER_START in the shader)
                //     enableInstancing = false
                // };

                drawingSettings.perObjectData =
                PerObjectData.ReflectionProbes |
                PerObjectData.Lightmaps |
                PerObjectData.LightProbe;
                // | PerObjectData.ShadowMask |
                //  | PerObjectData.OcclusionProbe |
                // PerObjectData.LightProbeProxyVolume |
                // PerObjectData.OcclusionProbeProxyVolume;


                /// render targets

                /// default
                cb.GetTemporaryRT(
                    defaultTexId, camera.pixelWidth, camera.pixelHeight, 0
                );
                cb.GetTemporaryRT(
                    defaultDepthTexId, camera.pixelWidth, camera.pixelHeight, 24, // 16 24
                    FilterMode.Point, RenderTextureFormat.Depth
                );
                cb.GetTemporaryRT(
                    skinTexId, camera.pixelWidth, camera.pixelHeight, 0
                );
                cb.GetTemporaryRT(
                    sssTexId, camera.pixelWidth, camera.pixelHeight, 0
                );

                /// set render targets for the main color buffer
                rts[0] = defaultTexId;
                rts[1] = skinTexId;
                
                cb.SetRenderTarget(rts, defaultDepthTexId);
                cb.ClearRenderTarget(false, true, Color.clear); /// only clear color
                ExecuteBuffer(context);

                /// draw the background
                cb.DrawMesh(fullScreenTriangle, Matrix4x4.identity, backgroundMat);

                cb.ClearRenderTarget(true, false, Color.clear, 1.0f); /// only clear depth
                ExecuteBuffer(context);

                /// draw default
                drawingSettings.SetShaderPassName(0, defaultId);

                context.DrawRenderers(
                cullingResults, ref drawingSettings, ref filteringSettings);

                /// draw transparent
                drawingSettings.SetShaderPassName(0, hairAlphaId);
                drawingSettings.SetShaderPassName(1, hairId);
                drawingSettings.SetShaderPassName(2, unityDefaultUnlitId);

                sortingSettings.criteria = SortingCriteria.CommonTransparent;
                drawingSettings.sortingSettings = sortingSettings;
                filteringSettings.renderQueueRange = RenderQueueRange.transparent;
                context.DrawRenderers(
                cullingResults, ref drawingSettings, ref filteringSettings);

                /// switch back to opaque
                sortingSettings.criteria = SortingCriteria.CommonOpaque;
                drawingSettings.sortingSettings = sortingSettings;
                filteringSettings.renderQueueRange = RenderQueueRange.opaque;

                drawingSettings.SetShaderPassName(1, ShaderTagId.none);
                drawingSettings.SetShaderPassName(2, ShaderTagId.none);

                /// SSS
                if(this.sssOn)
                {
                    cb.SetRenderTarget(
                        sssTexId,
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                    );

                    cb.SetGlobalInt(sssHorizontalId, 1);
                    cb.DrawMesh(fullScreenTriangle, Matrix4x4.identity, sssMat);
                    cb.CopyTexture(sssTexId, skinTexId);
                    cb.SetGlobalInt(sssHorizontalId, 0);
                    cb.DrawMesh(fullScreenTriangle, Matrix4x4.identity, sssMat);
                }
                else
                {
                    cb.CopyTexture(skinTexId, sssTexId);
                }

                /// draw to screen
                cb.SetRenderTarget(
                BuiltinRenderTextureType.CameraTarget,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                );
                cb.DrawMesh(fullScreenTriangle, Matrix4x4.identity, postProcessMat);

                cb.ReleaseTemporaryRT(defaultTexId);
                // cb.ReleaseTemporaryRT(defaultDepthTexId);
                cb.ReleaseTemporaryRT(skinTexId);
                cb.ReleaseTemporaryRT(sssTexId);

                if (drewShadows) cb.ReleaseTemporaryRT(shadow0TexId);

                /// submit
                // cb.EndSample(bufferName);
                ExecuteBuffer(context);
                context.Submit();
            }
        }

        private void ExecuteBuffer(ScriptableRenderContext context)
        {
            context.ExecuteCommandBuffer(cb);
            cb.Clear();
        }
    }
}
