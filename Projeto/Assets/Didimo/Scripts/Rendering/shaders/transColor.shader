
/// by Tom Sirdevan for Didimo

Shader "Didimo/transColor"
{
  Properties
  {
    mainColor ("Color", Color) = (0.083, 0.058, 0.044)
    opacitySampler ("Opacity Map", 2D) = "white" {}
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
      Cull Off

      CGPROGRAM
      #include "didimoCommon.cginc"
      #pragma vertex didimoVert
      #pragma fragment frag
      
      fixed3 mainColor;
      sampler2D opacitySampler;

      DidimoOut frag(v2f i)
      {
        DidimoOut OUT;

        fixed opacity = tex2D(opacitySampler, i.uv).r;

        OUT.main = fixed4(mainColor, opacity);
        OUT.skin = fixed4(0, 0, 0, opacity);
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
