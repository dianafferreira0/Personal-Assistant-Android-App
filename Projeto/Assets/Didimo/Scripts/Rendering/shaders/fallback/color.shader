
/// by Tom Sirdevan for Didimo

Shader "Didimo/fallback/color"
{
  Properties
  {
    baseColor ("Color", Color) = (1,1,1)
    zBias ("Z Bias", Range(-1,1)) = 0
  }
  SubShader
  {
    Tags { 
      "RenderType" = "ForwardBase" 
    }
    LOD 100

    Pass
    {
      Tags { 
        "LightMode"="ForwardBase"
      }

      CGPROGRAM
      #include "didimoCommon.cginc"
      #pragma vertex didimoVert
      #pragma fragment frag
      
      fixed3 baseColor;

      fixed4 frag(v2f i) : SV_Target
      {
        return fixed4(baseColor, 1);
      }
      ENDCG
    }


  }

  Fallback "Standard"
}
