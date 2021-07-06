

/// by Tom Sirdevan for Didimo

Shader "Didimo/simpleHair"
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
        "LightMode"="DidimoTrans"
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

      DidimoOut frag(v2f i)
      {
        DidimoOut OUT;

        half3 tN = half3(0, 0, 1);

        half3x3 tS = half3x3(i.wT, i.wB, i.wN);

        half3 tV = normalize(mul(tS, i.wV));

        half3 wN = normalize(mul(tN, tS));

        /// direct lighting
        half3 diffuse = half3(0, 0, 0);

        for(int di = 0; di < didimoNumDirLights; di++)
        {
          Light light = evalDirLight(di, tS);
          diffuse += evalLambert(light.tD, tN) * light.color;
        }
        for(int si = 0; si < didimoNumSpotLights; si++)
        {
          Light light = evalSpotLight(si, tS, i.wP);
          diffuse += evalLambert(light.tD, tN) * light.color;
        }
        for(int pi = 0; pi < didimoNumPointLights; pi++)
        {
          Light light = evalPointLight(pi, tS, i.wP);
          diffuse += evalLambert(light.tD, tN) * light.color;
        }
        diffuse *= directDiffInt;

        diffuse += evalIndDiffuse(wN, i.wP);

        diffuse *= diffColor;

        OUT.main = fixed4(diffuse, opacity);
        OUT.skin = fixed4(0, 0, 0, opacity);
        return OUT;
      }
      ENDCG
    }
  }
  Fallback "Standard"
}
