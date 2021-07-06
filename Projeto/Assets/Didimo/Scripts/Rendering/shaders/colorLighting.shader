
/// by Tom Sirdevan for Didimo

Shader "Didimo/colorLighting"
{
  Properties
  {
    baseColor ("Color", Color) = (1,1,1,1)
    directDiffInt ("Direct Diffuse", Range(0,2)) = 1
    indirectDiffInt ("Indirect Diffuse", Range(0,2)) = 1

    roughness ("Roughness", Range(0,1)) = 0.2
    fresnel ("Fresnel", Range(0,1)) = 0.5
    frenSpread ("Fresnel Spread", Range(0,1)) = 0.5
    directSpecInt ("Direct Specular", Range(0,2)) = 1
    indirectSpecInt ("Indirect Specular", Range(0,2)) = 1

    /// shadows
    selfShadowDiff ("Shadow", Range(0,1)) = 0.5
    // selfShadowSpec ("Shadow", Range(0,1)) = 0.5
    shadowSpread ("Shadow Spread", Range(0,1)) = 0.5
    shadowBias ("Shadow Bias", Range(0,0.05)) = 0.007

    zBias ("Z Bias", Range(-1,1)) = 0
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
        "LightMode"="DidimoDefault" // DidimoDefault ForwardBase 
      }

      Cull Back

      CGPROGRAM
      #define DIDIMO_SHADOWS
      #include "didimoCommon.cginc"
      #include "didimoLighting.cginc"
      #pragma vertex didimoVert
      #pragma fragment frag
      
      fixed3 baseColor;
      half roughness;
      half fresnel;
      half fresnelSpread;

			DidimoOut frag(v2f i)
			{
				DidimoOut OUT;

        /// constants
        const half3 LuminanceVector = {0.299f, 0.587f, 0.114f};

        half3 tN = half3(0, 0, 1);

        half3x3 tS = half3x3(i.wT, i.wB, i.wN);

        half3 tV = normalize(mul(tS, i.wV)); 

        half3 wN = normalize(mul(tN, tS));

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
				half shadow;
        evalShadowOptimized(i.wP, i.wN, 0, didimoLightSpace0, didimoShadowTex0, shadow);
        shadowSum += shadow;        

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
        spec += evalIndSpec(wN, i.wV, roughness) * indirectSpecInt;

        diffuse *= baseColor;

        half _frenSpread = lerp(10.0, 1.0, fresnelSpread);
        half fresnelTerm = lerp(1.0, min(1.0, pow(1.0 - abs(dot(tN, tV)), _frenSpread)), fresnel);
        spec *= fresnelTerm;

        /// shadows
        half directShadows = lerp(1, 1 - shadowSum, selfShadowDiff);
        diffuse *= directShadows;
        
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
