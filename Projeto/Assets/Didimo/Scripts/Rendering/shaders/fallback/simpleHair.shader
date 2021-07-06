

/// by Tom Sirdevan for Didimo

Shader "Didimo/fallback/simpleHair"
{
  Properties
  {
    /// diffuse
    diffColor ("Color", Color) = (0.132, 0.109, 0.091, 1)    
    directDiffInt ("Direct Diffuse", Range(0,2)) = 1
    indirectDiffInt ("Indirect Diffuse", Range(0,2)) = 1

    /// geometry
    opacity ("Opacity", Range(0,1)) = 0.6
    zBias ("Z Bias", Range(-1,1)) = 0
  }
  SubShader
  {
    Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
    LOD 100

    Pass
    {
      Tags { 
        "LightMode"="ForwardBase"
      }

      ZWrite Off
      Blend SrcAlpha OneMinusSrcAlpha
      Cull Off

      CGPROGRAM
      #include "didimoCommon.cginc"
      #include "didimoLighting.cginc"
      #pragma vertex didimoVert
      #pragma fragment frag

      fixed3 diffColor;
      float opacity;

      fixed4 _LightColor0;

      fixed4 frag(v2f i) : SV_Target
      {
        float3 tN = float3(0, 0, 1);

        half3x3 tS = half3x3(i.wT, i.wB, i.wN);

        float3 tV = normalize(mul(tS, i.wV));

        half3 wN = normalize(mul(tN, tS));

        float3 tD = normalize(mul(tS, _WorldSpaceLightPos0.xyz));

        half3 diffuse = half3(0, 0, 0);

        diffuse = evalLambert(tD, tN) * _LightColor0;

        diffuse *= directDiffInt;

        diffuse += evalIndDiffuse(wN, i.wP);

        diffuse *= diffColor;

        return fixed4(diffuse, opacity);
      }
      ENDCG
    }
  }
  Fallback "Standard"
}
