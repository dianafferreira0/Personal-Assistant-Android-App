
/// by Tom Sirdevan for Didimo

Shader "Didimo/skin"
{
  Properties
  {
    /// diffuse
    colorSampler ("Color Map", 2D) = "white" {}
    // colorTint ("Tint", Color) = (1, 0.95, 0.95)
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
    // dirSpecRoughBias ("Direct Roughness Bias", Range(0,1)) = 0
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
    normalBias ("Shadow Normal Bias", Range(0,0.05)) = 0.01
    
    aoSampler ("Occlusion Map", 2D) = "white" {}
    aoInt ("Occlusion", Range(0,1)) = 0.371

    cavitySampler ("Cavity Map", 2D) = "white" {}
    cavityDiffuse ("Diffuse Cavity", Range(0,1)) = 0
    cavitySpecular ("Specular Cavity", Range(0,1)) = 0

    /// extra
    hairColor ("Hair Color", Color) = (0.064, 0.052, 0.042, 1)
    // hairCapCutoff ("Hair Cap Cutoff",  Range(0,1)) = 0
    hairCapSampler ("Hair Cap Map", 2D) = "black" {}

    zBias ("Z Bias", Range(-2,2)) = 0

    zBiasSampler ("Z Bias Mask", 2D) = "white" {}
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
        "LightMode"="DidimoDefault"
      }

      Cull Back

      CGPROGRAM
      #include "didimoCommon.cginc"
      #include "didimoLighting.cginc"
      #pragma vertex didimoSkinVert
      #pragma fragment frag
      
      sampler2D colorSampler;
      // fixed3 colorTint;
      half desatInt;
      half desatSpread;
      fixed3 transColor;
      sampler2D transSampler;
      // sampler2D curveMap;
      int didimoSssOn;

      sampler2D normalSampler;
      half height;
      int flipY;

      sampler2D aoSampler;
      half aoInt;
      sampler2D cavitySampler;
      half cavityDiffuse;

      float normalBias;

      fixed3 hairColor;
      sampler2D hairCapSampler;

      /// spec in case optimizing
      fixed3 SpecColor;
      sampler2D specSampler;
      sampler2D roughSampler;
      half indSpecRoughBias;
      half dirFresnel;
      half indFresnel;
      half cavitySpecular;

      half evalSkinSpec(half3 tlD, half3 tN, half3 tV, fixed roughness)
      {
        half3 halfVec = normalize(tlD + tV);
        float dotNH = max(0, dot(tN, halfVec));
        half specTerm = 0;
        specTerm += pow(dotNH, (1 - roughness) * 70);
        specTerm += pow(dotNH, (1 - roughness) * 40);
        return specTerm;
      }

      DidimoOut frag(v2f i)
      {
        DidimoOut OUT;

        /// constants
        const half3 LuminanceVector = {0.299f, 0.587f, 0.114f};

        half3 tN = getTsNormal(normalSampler, i.uv, height, 0, flipY);

        half3x3 tS = half3x3(i.wT, i.wB, i.wN);

        half3 wN = normalize(mul(tN, tS));

        half3 tV = normalize(mul(tS, i.wV));

        fixed roughness = tex2D(roughSampler, i.uv).r;

        /// direct lighting
        fixed3 diffuse = fixed3(0, 0, 0);
        half3 spec = half3(0, 0, 0);

        for(int di = 0; di < didimoNumDirLights; di++)
        {
          Light light = evalDirLight(di, tS);
          diffuse += evalLambert(light.tD, tN) * light.color;
          spec += evalSkinSpec(light.tD, tN, tV, roughness) * light.color;
        }

        for(int si = 0; si < didimoNumSpotLights; si++)
        {
          Light light = evalSpotLight(si, tS, i.wP);
          diffuse += evalLambert(light.tD, tN) * light.color;
          spec += evalSkinSpec(light.tD, tN, tV, roughness) * light.color;
        }

        for(int pi = 0; pi < didimoNumPointLights; pi++)
        {
          Light light = evalPointLight(pi, tS, i.wP);
          diffuse += evalLambert(light.tD, tN) * light.color;
          spec += evalSkinSpec(light.tD, tN, tV, roughness) * light.color;
        }

        /// shadow
        fixed shadow;//, transmission;
        // evalShadowAndTrans(i.wP, i.wN, normalBias, didimoLightSpace0, didimoShadowTex0, shadow, transmission);
        evalShadow(i.wP, i.wN, normalBias, didimoLightSpace0, didimoShadowTex0, shadow);
        // evalShadowOptimized(i.wP, i.wN, normalBias, didimoLightSpace0, didimoShadowTex0, shadow);

        diffuse *= lerp(1, 1.0 - shadow, 0.4); /// hardcoded
        spec *= lerp(1.0, 1.0 - shadow, 0.6);

        // diffuse += transmission * transColor * tex2D(transSampler, i.uv).r; // * colorMap;

        diffuse *= directDiffInt;

        diffuse += evalIndDiffuse(wN, i.wP);

        diffuse *= tex2D(colorSampler, i.uv);
        
        diffuse = lerp(diffuse, diffuse * fixed3(1, 0.68, 0.3), 0.8); /// tint to mimic SSS

        /// sss desaturate
        half _desatSpread = lerp(1.0, 0.01, desatSpread);
        half lum = dot(diffuse, LuminanceVector);
        half _desatInt = min(1.0, (lum / _desatSpread));
        diffuse = lerp(diffuse, half3(lum, lum, lum), _desatInt * 0.6); /// overriding desatInt

        spec *= directSpecInt * evalFresnel(dirFresnel, 5, tN, tV);

        half iRoughness = min(1, (roughness + indSpecRoughBias));

        half3 indSpec = evalIndSpec(wN, i.wV, iRoughness);

        spec += indSpec * evalFresnel(indFresnel, 5, tN, tV);

        half specMap = tex2D(specSampler, i.uv).r;
        spec *= specMap * SpecColor;

        fixed aoMap = tex2D(aoSampler, i.uv).r;
        diffuse *= lerp(1, aoMap, aoInt);
        spec *= lerp(1, aoMap, aoInt);

        fixed cavityMap = tex2D(cavitySampler, i.uv).r;
      // diffuse *= lerp(1, remapTo01(cavityMap, 0, 0.08), cavityDiffuse);
        spec *= lerp(1, remapTo01(cavityMap, 0, 0.5), cavitySpecular);

        fixed hairCap = tex2D(hairCapSampler, i.uv).r;
        diffuse = lerp(diffuse, hairColor, hairCap);
        spec = lerp(spec, 0, hairCap);

        OUT.skin = fixed4(diffuse, 1 - pow(hairCap, 2.2));
        OUT.main = fixed4(spec, 1);
        
        return OUT;
      }
      ENDCG
    }

    Pass
    {
      Tags { 
        "LightMode"="ShadowCaster"
      }

      Cull Off

      CGPROGRAM
      #include "didimoCommon.cginc"
      #pragma vertex didimoShaderCasterVert
      #pragma fragment frag

      void frag(v2f i)
      {
      }
      ENDCG
    }
  }

  Fallback "Standard"
}
