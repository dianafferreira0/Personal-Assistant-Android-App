
/// by Tom Sirdevan for Didimo

Shader "Didimo/glassy"
{
  Properties
  {
    roughness ("Roughness", Range(0,1)) = 0.1
    indirectSpecInt ("Indirect Specular", Range(0,2)) = 1
    opacity ("Opacity", Range(0,1)) = 0.5
    zBias ("Z Bias", Range(-1,1)) = 0
  }
  SubShader
  {
    Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
    LOD 100

    Pass
    {
      Tags { 
        "LightMode"="DidimoTrans"
      }

      ZWrite Off
      Blend SrcAlpha OneMinusSrcAlpha
      // Cull Off

      CGPROGRAM
      #include "didimoCommon.cginc"
      #include "didimoLighting.cginc"
      #pragma vertex didimoVert
      #pragma fragment frag
      
      half roughness;
      half opacity;

      fixed4 frag(v2f i) : SV_Target
      {
        const half3 tN = half3(0, 0, 1); 

        half3x3 tS = half3x3(i.wT, i.wB, i.wN);
        half3 tV = normalize(mul(tS, i.wV)); 

        fixed3 color = evalIndSpec(i.wN, i.wV, roughness) * indirectSpecInt;

        return fixed4(color, opacity);
      }
      ENDCG
    }

  }

  Fallback "Standard"
}
