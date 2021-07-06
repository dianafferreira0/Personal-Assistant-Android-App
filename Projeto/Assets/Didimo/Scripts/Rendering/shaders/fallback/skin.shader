
/// by Tom Sirdevan for Didimo

Shader "Didimo/fallback/skin"
{
  Properties
  {
    /// diffuse
    colorSampler ("Color Map", 2D) = "white" {}
    colorTint ("Tint", Color) = (1, 0.95, 0.95)
    directDiffInt ("Direct Diffuse Intensity", Range(0,2)) = 1
    indirectDiffInt ("Indirect Diffuse Intensity", Range(0,2)) = 0.26

    /// sss
    desatInt ("Desaturation", Range(0,1)) = 0.35
    desatSpread ("Desaturation Spread", Range(0,1)) = 0.093
    transColor ("Transmission", Color) = (1,0,0,1)
    transSampler ("Transmission", 2D) = "black" {}
    // curveMap ("Curvature", 2D) = "white" {}

    /// spec
    SpecColor ("Spec Tint", Color) = (0.329, 0.626, 0.988, 1)
    specSampler ("Spec Map", 2D) = "white" {}
    roughSampler ("Roughness Map", 2D) = "black" {}
    // specMapAsRough ("Inv SpecMap as Roughness", Range(0,1)) = 0
    dirSpecRoughBias ("Direct Roughness Bias", Range(0,1)) = 0
    indSpecRoughBias ("Indirect Roughness Bias", Range(0,1)) = 0
    // specRoughness ("Roughness", Range(0,1)) = 1
    dirFresnel ("Direct Fresnel", Range(0,1)) = 0.69
    indFresnel ("Indirect Fresnel", Range(0,1)) = 0.69
    // frenSpread ("Fresnel Spread", Range(0,1)) = 0.966
    directSpecInt ("Direct Specular", Range(0,2)) = 0.281
    indirectSpecInt ("Indirect Specular", Range(0,2)) = 0.1

    /// geo
    normalSampler ("Normal Map", 2D) = "blue" {}
    height ("Height", Range(0,5)) = 1.1
    flipY  ("Flip Y", Int) = 1

    /// shadows
    selfShadowDiff ("Shadow Diffuse", Range(0,1)) = 0.279
    selfShadowSpec ("Shadow Spec", Range(0,1)) = 0.24
    shadowSpread ("Shadow Spread", Range(0,1)) = 0.67
    shadowBias ("Shadow Bias", Range(0,0.05)) = 0.006
    shadowNBias ("Shadow Normal Bias", Range(0,0.05)) = 0.01
    
    aoSampler ("Occlusion Map", 2D) = "white" {}
    aoInt ("Occlusion", Range(0,1)) = 0.371

    cavitySampler ("Cavity Map", 2D) = "white" {}
    cavityInt ("Cavity", Range(0,1)) = 0.371

    /// extra
    hairColor ("Hair Color", Color) = (0.064, 0.052, 0.042, 1)
    hairCapCutoff ("Hair Cap Cutoff",  Range(0,1)) = 0
    hairCapSampler ("Hair Cap Map", 2D) = "black" {}

    zBias ("Z Bias", Range(-1,1)) = 0

    zBiasSampler ("Z Bias Map", 2D) = "gray" {}
  }
  SubShader
  {
    Tags { 
      "RenderType" = "Opaque" 
    }
    LOD 100

    Pass
    {
      Tags { 
        "LightMode"="ForwardBase"
      }

      Cull Back

      CGPROGRAM
      #include "didimoCommon.cginc"
      #include "didimoLighting.cginc"
      #pragma vertex didimoVert
      #pragma fragment frag
      
      sampler2D colorSampler;
      fixed3 colorTint;
      half desatInt;
      half desatSpread;
      fixed3 transColor;
      sampler2D transSampler;
      // sampler2D curveMap;
      int didimoSssOn;

      fixed3 SpecColor;
      sampler2D specSampler;
      sampler2D roughSampler;
      // fixed specMapAsRough;
      // half dirSpecRoughBias;
      half indSpecRoughBias;
      half dirFresnel;
      half indFresnel;
      // half frenSpread;

      sampler2D normalSampler;
      half height;
      int flipY;

      sampler2D aoSampler;
      half aoInt;
      sampler2D cavitySampler;
      half cavityInt;

      float shadowNBias;

      fixed3 hairColor;
      fixed hairCapCutoff;
      sampler2D hairCapSampler;

      fixed4 _LightColor0;

      half evalSkinSpec(half3 tlD, half3 tN, half3 tV, fixed roughness)
      {
        half3 halfVec = normalize(tlD + tV);
        float dotNH = max(0, dot(tN, halfVec));
        half specTerm = 0;
        specTerm += pow(dotNH, (1 - roughness) * 70); // * 0.5;
        specTerm += pow(dotNH, (1 - roughness) * 40); // * 0.5;
        return specTerm;
      }

      fixed4 frag(v2f i) : SV_Target
      {
        /// constants
        const half3 LuminanceVector = {0.299f, 0.587f, 0.114f};

        /// single channel maps
        fixed transMap = tex2D(transSampler, i.uv).r;
        fixed aoMap = tex2D(aoSampler, i.uv).r;
        // fixed cavityMap = tex2D(cavitySampler, i.uv).r;
        fixed specMap = tex2D(specSampler, i.uv).r;
        fixed roughMap = tex2D(roughSampler, i.uv).r;

        fixed roughness = roughMap;
        
        half3 tN = getTsNormal(normalSampler, i.uv, height, 0, flipY);

        float3x3 tS = float3x3(i.wT, i.wB, i.wN);

        half3 tV = normalize(mul(tS, i.wV));

        float3 tD = normalize(mul(tS, _WorldSpaceLightPos0.xyz));

        /// direct lighting
        fixed3 diffuse = fixed3(0, 0, 0);
        half3 spec = half3(0, 0, 0);
        // fixed shadowSum = 0;

        diffuse = evalLambert(tD, tN) * _LightColor0;
        spec = evalSkinSpec(tD, tN, tV, roughness) * _LightColor0;

        diffuse *= directDiffInt;
        spec *= directSpecInt * evalFresnel(dirFresnel, 5, tN, tV);

        /// indirect
        half3 wN = normalize(mul(tN, tS));
        diffuse += evalIndDiffuse(wN, i.wP);

        half iRoughness = min(1, (roughness) + indSpecRoughBias);
        half3 indSpec = evalIndSpec(wN, i.wV, iRoughness);
        spec += indSpec * evalFresnel(indFresnel, 5, tN, tV);

        diffuse *= tex2D(colorSampler, i.uv);
        diffuse *= colorTint;

        spec *= specMap * SpecColor;

        spec *= lerp(1, aoMap, aoInt);

        /// sss desaturate
        // if(didimoSssOn)
        // {
        half _desatSpread = lerp(1.0, 0.01, desatSpread);
        half lum = dot(diffuse, LuminanceVector);
        half _desatInt = min(1.0, (lum / _desatSpread));
        diffuse = lerp(diffuse, half3(lum, lum, lum), _desatInt * desatInt);

        diffuse *= lerp(1, 1 - evalFresnel(1, 5, tN, normalize(mul(tS, i.wV))), lum * 0.3);

        half4 hairCapMask = tex2D(hairCapSampler, i.uv);

        half3 hairCapColor = hairColor * hairCapMask.rgb;
        half hairCapMaskCutoff = hairCapMask.a > hairCapCutoff ? hairCapMask.a : 0;
        diffuse = lerp(diffuse, hairColor, hairCapMaskCutoff);

        spec *= hairCapMaskCutoff;

        half ao = lerp(1, aoMap, aoInt);
        diffuse *= ao;
        spec *= ao;

        return fixed4(diffuse + spec, 1.0f);
      }
      ENDCG
    }
  }

  Fallback "Standard"
}
