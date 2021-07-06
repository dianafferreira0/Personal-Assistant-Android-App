
/// by Tom Sirdevan for Didimo

Shader "Didimo/hairCap"
{
  Properties
  {
    /// diffuse
    colorSampler ("Color Map", 2D) = "white" {}
    colorTint ("Tint", Color) = (0, 0, 0, 0)
    directDiffInt ("Direct Diffuse", Range(0,2)) = 1
    indirectDiffInt ("Indirect Diffuse", Range(0,2)) = 1

    selfShadowDiff ("Shadow Intensity", Range(0,1)) = 0.5
    shadowSpread ("Shadow Spread", Range(0,1)) = 0.5
    shadowBias ("Shadow Bias", Range(0,0.05)) = 0.007

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

      Blend SrcAlpha OneMinusSrcAlpha

      CGPROGRAM
      #include "didimoCommon.cginc"
      #include "didimoLighting.cginc"
      #pragma vertex didimoVert
      #pragma fragment frag

      sampler2D colorSampler;
      fixed4 colorTint;

			DidimoOut frag(v2f i)
			{
				DidimoOut OUT;

        fixed4 colorMap = tex2D(colorSampler, i.uv);
        fixed opacity = colorMap.a;

        half3 tN = half3(0, 0, 1);

        half3x3 tS = half3x3(i.wT, i.wB, i.wN);

        half3 diffuse = 0;

        /// direct lighting
        fixed shadowSum = 0;
        for(int di = 0; di < didimoNumDirLights; di++)
        {
          Light light = evalDirLight(di, tS);
					diffuse += evalLambert(light.tD, tN) * light.color;
        }
        fixed shadow;
        evalShadowOptimized(i.wP, i.wN, 0, didimoLightSpace0, didimoShadowTex0, shadow);
        shadowSum += shadow;

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

        half3 wN = normalize(mul(tN, tS));
        diffuse += evalIndDiffuse(wN, i.wP);

        diffuse *= lerp(colorMap.rgb, colorTint.rgb, colorTint.a);
        
        diffuse *= lerp(1, 1 - shadowSum, selfShadowDiff);

        OUT.main = fixed4(diffuse, opacity);
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

      CGPROGRAM
      #include "didimoCommon.cginc"
      #pragma vertex didimoShaderCasterVert
      #pragma fragment frag
      
      sampler2D colorSampler;

      void frag(v2f i)
      {
        clip(tex2D(colorSampler, i.uv).a - 0.5);
      }
      ENDCG
    }
  }
  Fallback "Standard"
}
