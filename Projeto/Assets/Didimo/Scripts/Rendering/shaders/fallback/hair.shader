
/// by Tom Sirdevan for Didimo

Shader "Didimo/fallback/hair"
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
    opacityThreshold ("Opacity Threshold", Range(0,1)) = 0.308

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
        "LightMode"="ForwardBase"
      }

      // AlphaToMask On /// alpha-to-coverage pass
      Cull Off

      CGPROGRAM
      #include "didimoCommon.cginc"
      #pragma vertex didimoVert
      #pragma fragment frag

      fixed3 diffColor;
      sampler2D opacitySampler;
      half opacityThreshold;
      
      fixed4 frag (v2f i) : SV_Target
      {
        fixed opacityMap = tex2D(opacitySampler, i.uv).r;

        clip(opacityMap - opacityThreshold);

        return fixed4(diffColor, 1);//opacityMap);
      }
      ENDCG
    }

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

      sampler2D opacitySampler;
      fixed3 diffColor;

      fixed3 specColor;
      float spec1Int;
      float specExp1;
      float spec2Int;
      float specExp2;
      sampler2D specShiftSampler;

      sampler2D normalMap;
      half height;

      fixed4 _LightColor0;

      half evalKajiyaKay(half3 tangent, half3 halfVec, half roughness)
      {
        half  dotTH    = dot(tangent, halfVec);
        half  sinTH    = sqrt(1 - dotTH * dotTH);
        half  dirAtten = smoothstep(-1.0, 0.0, dotTH);
        return max(0, dirAtten * pow(sinTH, roughness));
      }

      void evalBrdf(in half3 lTd, in half3 lColor, in half3 tN, in half3 tV, in fixed roughness1, in fixed roughness2, in fixed specShift, inout fixed3 diffuse, inout half3 spec1, inout half3 spec2)
      {
        diffuse += evalLambert(lTd, tN) * lColor;

        const half3 tB = normalize(half3(0, 1, 0));
        half3 halfVec = normalize(lTd + tV); 

        half primaryShift   = specShift - 0.25;
        half secondaryShift = primaryShift - clamp(dot(lTd, tB), 0, 1) * 0.25;
        
        half3 T1 = normalize(tB + tN * primaryShift);
        half3 T2 = normalize(tB + tN * secondaryShift);
            
        spec1 += evalKajiyaKay(T1, halfVec, roughness1) * lColor;
        spec2 += evalKajiyaKay(T2, halfVec, roughness2) * lColor;
      }

      fixed4 frag(v2f i) : SV_Target
      {
        // return fixed4(1, 0, 0, 1);

        fixed opacityMap = tex2D(opacitySampler, i.uv).r;
        fixed specShiftMap = tex2D(specShiftSampler, i.uv).r;
        // if(didimoLinear) // uncomment if textures are set as single channel
        // {
        //   opacityMap = pow(opacityMap, 2.2);
        // }

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

        float3 tD = normalize(mul(tS, _WorldSpaceLightPos0.xyz));

        evalBrdf(tD, _LightColor0, tN, tV, _specExp1, _specExp2, specShiftMap, diffuse, spec1, spec2);

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
        fixed rootDampen = min(lerp(0, 3, i.uv.y), 1);
        // diffuse *= rootDampen;
        spec1 *= rootDampen;
        // spec2 *= rootDampen;
        
        fixed3 result = diffuse + spec1 + spec2;
        // result *= rootDampen;

        fixed shadows = lerp(1, 1 - shadowSum, selfShadowDiff);
        result *= shadows;

        return fixed4(result, opacityMap);
      }
      ENDCG
    }
  }
  Fallback "Standard"
}
