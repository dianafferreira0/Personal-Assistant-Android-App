
/// by Tom Sirdevan for Didimo

/// NOTE: currently defaults are set to accomodate mouthMat
/// obviously going forward we shouldn't be doing this, use material state json instead

Shader "Didimo/textureLighting"
{
  Properties
  {
    colorSampler ("Color Map", 2D) = "white" {} 
    directDiffInt ("Direct Diffuse", Range(0,2)) = 0.183//0.49
    indirectDiffInt ("Indirect Diffuse", Range(0,2)) = 0//0.24

    roughness ("Roughness", Range(0,1)) = 0.099
    fresnel ("Fresnel", Range(0,1)) = 0.858
    directSpecInt ("Direct Specular", Range(0,2)) = 1
    indirectSpecInt ("Indirect Specular", Range(0,2)) = 0.4

    /// geo
    normalSampler ("Normal Map", 2D) = "blue" {}
    height ("Height", Range(0,5)) = 1
    flipY  ("Flip Y", Int) = 0

    /// shadows
    selfShadowDiff ("Shadow Diffuse", Range(0,1)) = 0.5
    selfShadowSpec ("Shadow Specular ", Range(0,1)) = 0.5
    shadowSpread ("Shadow Spread", Range(0,1)) = 0.592
    shadowBias ("Shadow Bias", Range(0,0.05)) = 0.007

    aoSampler ("Occlusion Map", 2D) = "white" {}
    aoInt ("Occlusion", Range(0,1)) = 0.371

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
        "LightMode"="DidimoDefault"
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

      sampler2D normalSampler;
      half height;
      int flipY;

      sampler2D aoSampler;
      half aoInt;

      DidimoOut frag(v2f i)
      {
        DidimoOut OUT;
        
        half3 tN = getTsNormal(normalSampler, i.uv, height, 0, flipY);

        half3x3 tS = half3x3(i.wT, i.wB, i.wN);

        half3 wN = normalize(mul(tN, tS));

        half3 tV = normalize(mul(tS, i.wV));  

        /// direct lighting
        half3 diffuse = half3(0, 0, 0);
        half3 spec = half3(0, 0, 0);
        fixed shadowSum = 0;

        for(int di = 0; di < didimoNumDirLights; di++)
        {
          Light light = evalDirLight(di, tS);
          diffuse += evalLambert(light.tD, tN) * light.color;
          spec += evalPhong(light.tD, tN, tV, roughness) * light.color;
        }
        fixed lightShadow;
        evalShadow(i.wP, i.wN, 0, didimoLightSpace0, didimoShadowTex0, lightShadow);
        shadowSum += lightShadow;

        for(int si = 0; si < didimoNumSpotLights; si++)
        {
          Light light = evalSpotLight(si, tS, i.wP);
          diffuse += evalLambert(light.tD, tN) * light.color;
          spec += evalPhong(light.tD, tN, tV, roughness) * light.color;
        }

        for(int pi = 0; pi < didimoNumPointLights; pi++)
        {
          Light light = evalPointLight(pi, tS, i.wP);
          diffuse += evalLambert(light.tD, tN) * light.color;
          spec += evalPhong(light.tD, tN, tV, roughness) * light.color;
        }

        diffuse *= directDiffInt;
        spec *= directSpecInt;

        /// indirect lighting
        diffuse += evalIndDiffuse(wN, i.wP);
        spec += evalIndSpec(wN, i.wV, roughness);

        diffuse *= tex2D(colorSampler, i.uv);

        spec *= evalFresnel(fresnel, 5, tN, tV);

        /// shadows
        half shadow = 1.0 - shadowSum;
        diffuse *= lerp(1, shadow, selfShadowDiff);
        spec *= lerp(1, shadow, selfShadowSpec);
        
        fixed ao = lerp(1, tex2D(aoSampler, i.uv).r, aoInt);
        diffuse *= ao;
        spec *= ao;

        OUT.main = fixed4(diffuse + spec, 1);
        OUT.skin = fixed4(0, 0, 0, 0);
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
