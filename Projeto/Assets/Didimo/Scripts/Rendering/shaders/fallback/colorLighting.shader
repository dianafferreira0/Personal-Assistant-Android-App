
/// by Tom Sirdevan for Didimo

Shader "Didimo/fallback/colorLighting"
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
        "LightMode"="ForwardBase" 
      }

      Cull Back

      CGPROGRAM
      #include "didimoCommon.cginc"
      #include "didimoLighting.cginc"
      #pragma vertex didimoVert
      #pragma fragment frag
      
      fixed3 baseColor;
      half roughness;
      half fresnel;
      half fresnelSpread;

      fixed4 _LightColor0;

      fixed4 frag(v2f i) : SV_Target
      {
        /// constants
        const half3 LuminanceVector = {0.299f, 0.587f, 0.114f};

        half3 tN = half3(0, 0, 1);

        half3x3 tS = half3x3(i.wT, i.wB, i.wN);

        half3 tV = normalize(mul(tS, i.wV)); 

        half3 wN = normalize(mul(tN, tS));

        half3 tD = normalize(mul(tS, _WorldSpaceLightPos0.xyz));

        /// direct lighting
        half3 diffuse = half3(0, 0, 0);
        half3 spec = half3(0, 0, 0);
        fixed shadowSum = 0;

        diffuse = evalLambert(tD, tN) * _LightColor0;
        spec = evalPhong(tD, tN, tV, roughness) * _LightColor0;

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
        
        return fixed4(diffuse + spec, 1);
      }
      ENDCG
    }
  }

  Fallback "Standard"
}
