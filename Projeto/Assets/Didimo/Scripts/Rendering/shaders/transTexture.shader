
/// by Tom Sirdevan for Didimo

Shader "Didimo/transTexture"
{
  Properties
  {
    mainMap ("Main Map", 2D) = "white" {}
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
      
      sampler2D mainMap;

      DidimoOut frag(v2f i)
      {
        DidimoOut OUT;
        OUT.main = tex2D(mainMap, i.uv);
        OUT.skin = fixed4(0, 0, 0, 0);
        return OUT;
      }
      ENDCG
    }
  }

  Fallback "Standard"
}
