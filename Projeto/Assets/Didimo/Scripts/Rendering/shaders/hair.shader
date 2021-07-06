
/// by Tom Sirdevan for Didimo

Shader "Didimo/hair"
{
  Properties
  {
    /// diffuse
    diffColor ("Color", Color) = (0.167, 0.102, 0.062)
    directDiffInt ("Direct Diffuse", Range(0,2)) = 0.348
    indirectDiffInt ("Indirect Diffuse", Range(0,2)) = 0.67

    /// spec
    specColor ("Specular Color", Color) = (0.213, 0.197, 0.160)
    spec1Int ("Specular 1 Intensity", Range(0,2)) = 0.75
    specExp1 ("Specular 1 Spread", Range(0,1)) = 0.2
    spec2Int ("Specular 2 Intensity", Range(0,2)) = 0.5
    specExp2 ("Specular 2 Spread", Range(0,1)) = 0.731
    specShiftSampler ("Specular Shift Map", 2D) = "white" {}
    directSpecInt ("Direct Specular", Range(0,2)) = 1
    indirectSpecInt ("Indirect Specular", Range(0,2)) = 0.21

    /// geometry
    normalMap ("Normal Map", 2D) = "blue" {}
    height ("Height", Range(0,5)) = 1.9
    opacitySampler ("Opacity Map", 2D) = "white" {}
    opacityThreshold ("Opacity Threshold", Range(0,1)) = 0.45

    selfShadowDiff ("Shadow Intensity", Range(0,1)) = 0.417
    shadowSpread ("Shadow Spread", Range(0,1)) = 0.772
    shadowBias ("Shadow Bias", Range(0,0.05)) = 0.007

    zBias ("Z Bias", Range(-1,1)) = 0
  }
  SubShader
  {
    Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
    LOD 100

    Pass /// alpha cutout
    {
      Tags { 
        "LightMode"="DidimoAlpha"
      }

      Cull Off

      CGPROGRAM
      #include "didimoCommon.cginc"
      #include "didimoLighting.cginc"
      #pragma vertex didimoVert
      #pragma fragment frag

      fixed3 diffColor;
      sampler2D opacitySampler;
      half opacityThreshold;
      
      DidimoOut frag(v2f i)
      {
        DidimoOut OUT;

        fixed opacityMap = tex2D(opacitySampler, i.uv).r;

        clip(opacityMap - opacityThreshold);

        half3 diffuse = evalIndDiffuse(i.wN, i.wP);

        diffuse *= diffColor;// * opacityMap;

        OUT.main = fixed4(diffuse, opacityMap);
        OUT.skin = fixed4(0, 0, 0, 0);
        return OUT;
      }
      ENDCG
    }

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

      sampler2D opacitySampler;
      fixed3 diffColor;

      fixed3 specColor;
      fixed spec1Int;
      fixed specExp1;
      fixed spec2Int;
      fixed specExp2;
      sampler2D specShiftSampler;

      sampler2D normalMap;
      half height;

      half evalKajiyaKay(half3 tangent, half3 halfVec, half roughness)
      {
        half  dotTH    = dot(tangent, halfVec);
        half  sinTH    = sqrt(1 - dotTH * dotTH);
        half  dirAtten = smoothstep(-1.0, 0.0, dotTH);
        return max(0, dirAtten * pow(sinTH, roughness));
      }

      void evalBrdf(in Light light, in half3 tN, in half3 tV, in fixed roughness1, in fixed roughness2, in fixed specShift, inout fixed3 diffuse, inout half3 spec1, inout half3 spec2)
      {
        diffuse += evalLambert(light.tD, tN) * light.color;

        const half3 tB = normalize(half3(0, 1, 0));
        half3 halfVec = normalize(light.tD + tV); 

        half primaryShift   = specShift - 0.25;
        half secondaryShift = primaryShift - clamp(dot(light.tD, tB), 0, 1) * 0.25;
        
        half3 T1 = normalize(tB + tN * primaryShift);
        half3 T2 = normalize(tB + tN * secondaryShift);
            
        spec1 += evalKajiyaKay(T1, halfVec, roughness1) * light.color;
        spec2 += evalKajiyaKay(T2, halfVec, roughness2) * light.color;
      }

      DidimoOut frag(v2f i)
      {
        DidimoOut OUT;

        fixed opacityMap = tex2D(opacitySampler, i.uv).r;

        fixed specShiftMap = tex2D(specShiftSampler, i.uv).r;


        half3 tN = getTsNormal(normalMap, i.uv, height, 0, 0);

        half3x3 tS = half3x3(i.wT, i.wB, i.wN);

        half3 tV = normalize(mul(tS, i.wV));

        half _specExp1 = lerp(100, 60, specExp1);
        half _specExp2 = lerp(40, 10, specExp2);

        half3 diffuse = half3(0, 0, 0);
        half3 spec1 = half3(0, 0, 0);
				half3 spec2 = half3(0, 0, 0);

        /// direct lighting
        fixed shadowSum = 0;

        for(int di = 0; di < didimoNumDirLights; di++)
        {
          Light light = evalDirLight(di, tS);
					evalBrdf(light, tN, tV, _specExp1, _specExp2, specShiftMap, diffuse, spec1, spec2);
        }
        half shadow;
        evalShadowOptimized(i.wP, i.wN, 0, didimoLightSpace0, didimoShadowTex0, shadow);
        shadowSum += shadow;

        for(int si = 0; si < didimoNumSpotLights; si++)
        {
          Light light = evalSpotLight(si, tS, i.wP);
					evalBrdf(light, tN, tV, _specExp1, _specExp2, specShiftMap, diffuse, spec1, spec2);
        }

        for(int pi = 0; pi < didimoNumPointLights; pi++)
        {
          Light light = evalPointLight(pi, tS, i.wP);
					evalBrdf(light, tN, tV, _specExp1, _specExp2, specShiftMap, diffuse, spec1, spec2);
        }

        diffuse *= directDiffInt;
        spec1 *= directSpecInt;
        spec2 *= directSpecInt;

        /// indirect 
        half3 wN = normalize(mul(tN, tS));
        diffuse += evalIndDiffuse(wN, i.wP);

        half3 indSpec = evalIndSpec(wN, i.wV, 0.9); /// TODO: also shouldn't wN be biased towards the hair 
        spec1 += indSpec;
        spec2 += indSpec;

        diffuse *= diffColor;
        spec1 *= specColor * spec1Int;
        spec2 *= diffColor * spec2Int; // * specularNoise

        /// limit highlight near root
        fixed rootDampen = min(lerp(0, 2, i.uv.y), 1);
        diffuse *= lerp(1.0, rootDampen, 0.4);
        spec1 *= rootDampen;
        spec2 *= rootDampen;
        
        fixed3 result = diffuse + spec1 + spec2;
   
        fixed shadows = lerp(1, 1 - shadowSum, selfShadowDiff);
        result *= shadows;

        OUT.main = fixed4(result, opacityMap);
        OUT.skin = fixed4(0, 0, 0, opacityMap);
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
      
      sampler2D opacitySampler;
      half opacityThreshold;

      void frag(v2f i)
      {
        clip(tex2D(opacitySampler, i.uv).r - opacityThreshold);

      }
      ENDCG
    }
  }
  Fallback "Standard"
}
