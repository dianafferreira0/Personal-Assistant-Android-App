
/// by Tom Sirdevan for Didimo

Shader "Didimo/test"
{
  Properties
  {
    baseColor ("Color", Color) = (1,1,1,1)
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

      CGPROGRAM
      #include "didimoCommon.cginc"
      #include "didimoLighting.cginc"
      #pragma vertex didimoVert
      #pragma fragment frag
      
      fixed3 baseColor;

      fixed4 frag(v2f i) : SV_Target
      {
        // return fixed4(i.wP, 1);

        half3 tN = half3(0, 0, 1);

        half3x3 tS = half3x3(i.wT, i.wB, i.wN);

        // half3 tangent = normalize(i.wT);
        // half3 binormal = normalize(i.wB);
        // half3 normal = normalize(i.wN);
        // half3x3 tS = half3x3(tangent, binormal, normal);

        half3 diffuse = 0;

        for(int pi = 0; pi < didimoNumPointLights; pi++)
        {
          Light light = evalPointLight(pi, tS, i.wP);
          diffuse += evalLambert(light.tD, tN) * light.color;
          // diffuse += light.color;
        }

        return fixed4(baseColor * diffuse, 1);
      }
      ENDCG
    }

  }

  Fallback "Standard"
}
