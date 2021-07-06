
/// by Tom Sirdevan for Didimo

/// NOTE: currently defaults are set to accomodate mouthMat
/// obviously going forward we shouldn't be doing this, use material state json instead

Shader "Didimo/fallback/textureLighting"
{
  Properties
  {
    colorSampler ("Color Map", 2D) = "white" {} 
    directDiffInt ("Direct Diffuse", Range(0,2)) = 0.183//0.49
    indirectDiffInt ("Indirect Diffuse", Range(0,2)) = 0//0.24

    roughness ("Roughness", Range(0,1)) = 0.099
    fresnel ("Fresnel", Range(0,1)) = 0.858
    frenSpread ("Fresnel Spread", Range(0,1)) = 0.992
    directSpecInt ("Direct Specular", Range(0,2)) = 1
    indirectSpecInt ("Indirect Specular", Range(0,2)) = 0.4

    /// geo
    normalSampler ("Normal Map", 2D) = "blue" {}
    height ("Height", Range(0,5)) = 1
    flipY  ("Flip Y", Int) = 0

    /// shadows
    selfShadowDiff ("Shadow", Range(0,1)) = 0.508
    shadowSpread ("Shadow Spread", Range(0,1)) = 0.592
    shadowBias ("Shadow Bias", Range(0,0.05)) = 0.007

    zBias ("Z Bias", Range(-1,1)) = -0.6 //0
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
      half roughness;
      half fresnel;
      half fresnelSpread;

      sampler2D normalSampler;
      half height;
      int flipY;

      fixed4 _LightColor0;

      fixed4 frag(v2f i) : SV_Target
      {
        /// constants
        const half3 LuminanceVector = {0.299f, 0.587f, 0.114f};

        half3 tN = getTsNormal(normalSampler, i.uv, height, 0, flipY);

        half3x3 tS = half3x3(i.wT, i.wB, i.wN);

        half3 wN = normalize(mul(tN, tS));

        half3 tV = normalize(mul(tS, i.wV));  

        /// direct lighting
        half3 diffuse = half3(0, 0, 0);
        half3 spec = half3(0, 0, 0);
        fixed shadowSum = 0;

        float3 tD = normalize(mul(tS, _WorldSpaceLightPos0.xyz));

        diffuse = evalLambert(tD, tN) * _LightColor0;
        spec = evalPhong(tD, tN, tV, roughness) * _LightColor0;

        // for(int di = 0; di < didimoNumDirLights; di++)
        // {
        //   Light light = evalDirLight(di, tS);
        //   diffuse += evalLambert(light.tD, tN) * light.color;
        //   spec += evalPhong(light.tD, tN, tV, roughness) * light.color;
        // }
        // half shadow, trans;
        // evalShadow(i.wP, i.wN, 0, didimoLightSpace0, didimoShadowTex0, shadow, trans);
        // shadowSum += shadow;

        // for(int si = 0; si < didimoNumSpotLights; si++)
        // {
        //   Light light = evalSpotLight(si, tS, i.wP);
        //   diffuse += evalLambert(light.tD, tN) * light.color;
        //   spec += evalPhong(light.tD, tN, tV, roughness) * light.color;
        // }

        // for(int pi = 0; pi < didimoNumPointLights; pi++)
        // {
        //   Light light = evalPointLight(pi, tS, i.wP);
        //   diffuse += evalLambert(light.tD, tN) * light.color;
        //   spec += evalPhong(light.tD, tN, tV, roughness) * light.color;
        // }

        diffuse *= directDiffInt;
        spec *= directSpecInt;

        /// indirect lighting
        diffuse += evalIndDiffuse(wN, i.wP);
        spec += evalIndSpec(wN, i.wV, roughness) * indirectSpecInt;

        diffuse *= tex2D(colorSampler, i.uv);

        half _frenSpread = lerp(10.0, 1.0, fresnelSpread);
        half fresnelTerm = lerp(1.0, min(1.0, pow(1.0 - abs(dot(tN, tV)), _frenSpread)), fresnel);
        spec *= fresnelTerm;

        /// shadows
        half directShadows = lerp(1, 1 - shadowSum, selfShadowDiff);
        diffuse *= directShadows;
        
        return fixed4(diffuse + spec, 1);
      }
      ENDCG
    }
  }

  Fallback "Standard"
}
