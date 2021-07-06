
/// by Tom Sirdevan for Didimo

Shader "Didimo/eye"
{
	Properties
	{
		/// diffuse
		colorSampler ("Color Map", 2D) = "white" {}
		directDiffInt ("Direct Diffuse", Range(0,2)) = 0.2
		indirectDiffInt ("Indirect Diffuse", Range(0,2)) = 0.2

		/// spec
		irisSpecInt ("Iris Spec Intensity", Range(0,2)) = 0.2
		irisRough ("Iris Roughness", Range(0,1)) = 0.703

		fresnel ("Cornea Fresnel", Range(0,5)) = 0.5
		corneaSpecInt ("Cornea Direct Specular", Range(0,2)) = 1

		directSpecInt ("Direct Specular", Range(0,2)) = 1
		indirectSpecInt ("Indirect Specular", Range(0,2)) = .124

		/// geo
		normalSampler ("Normal Map", 2D) = "blue" {}
		height ("Height", Range(0,1)) = 0.916
		refrSize ("Refraction Size", Range(0,1)) = 0.389
		refrAmount ("Refraction Amount", Range(0,1)) = 0.258
		uvScale ("UV Scale", Range(0.1, 2)) = 1.04

		/// shadows
		selfShadowDiff ("Shadow Intensity", Range(0,1)) = 0.367
		shadowSpread ("Shadow Spread", Range(0,1)) = 0.744
		shadowBias ("Shadow Bias", Range(0,0.05)) = 0.0079

		zBias ("Z Bias", Range(-1,1)) = 0
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			Tags { 
				"LightMode"="DidimoDefault"
			}

			Cull Back

			CGPROGRAM
			#include "didimoCommon.cginc"
			#include "didimoLighting.cginc"
			#pragma vertex didimoVert
			#pragma fragment frag

			/// diffuse
			sampler2D colorSampler;

			/// spec
			half irisSpecInt;
			half irisRough;
			half corneaSpecInt;
			half fresnel;

			/// geo
			sampler2D normalSampler;
			half height;
			half refrSize;
			half refrAmount;
			half uvScale;

			void evalBrdf(in Light light, in half3 tN, in half3 ctN, in half3 tV, in fixed irisRough, inout fixed3 diffuse, inout half3 irisSpec, inout half3 corneaSpec)
			{
				diffuse += evalLambert(light.tD, tN) * light.color;
				irisSpec += evalBlinn(light.tD, tN, tV, irisRough) * light.color;
				corneaSpec += evalAniso(light.tD, ctN, tV, 0.01, 0.5, 2) * light.color;
			}

			DidimoOut frag(v2f i)
			{
				DidimoOut OUT;
				// return fixed4(1, 0, 0, 1);

				half3x3 tS = half3x3(i.wT, i.wB, i.wN);

				half3 tV = normalize(mul(tS, i.wV));

				/// scale uvs
				half2 uv = i.uv - half2(0.5, 0.5);
				uv *= uvScale;
				uv += half2(0.5, 0.5);

				/// refraction
				half irisMask = 0;
				half refrHeight = 0;
				half radius = lerp(0, 0.3, refrSize);
				half2 center = half2(0.5, 0.5);
				if(pow(uv.x - center.x, 2) + pow(uv.y - center.y, 2) < pow(radius, 2)) /// inside circle test
				{
					refrHeight = 1 - (length(uv - center) / radius);
					irisMask = 1;
				}

				half _refrAmount = lerp(0, 0.5, refrAmount);
				half3 refrVec = -half3(tV.x, tV.y, tV.z);
				half3 offset = refrVec * refrHeight * _refrAmount;
				uv += offset.xy;
				// return fixed4(uv, 0, 1);

				half3 tN = getTsNormal(normalSampler, uv, height * 5, 1, 0);
				// return fixed4(tN, 1);

				half3 wN = normalize(mul(tN, tS)); /// world normal
				// return fixed4(wN, 1);

				/// cornea normals
				const half3 ctN = half3(0, 0, 1);
				half3 cwN = normalize(mul(ctN, tS));

        /// direct lighting
        half3 diffuse = half3(0, 0, 0);
        half3 irisSpec = half3(0, 0, 0);
				half3 corneaSpec = half3(0, 0, 0);
        fixed shadowSum = 0;

        for(int di = 0; di < didimoNumDirLights; di++)
        {
          Light light = evalDirLight(di, tS);
					evalBrdf(light, tN, ctN, tV, irisRough, diffuse, irisSpec, corneaSpec);
        }
				fixed shadow;
        evalShadow(i.wP, i.wN, 0, didimoLightSpace0, didimoShadowTex0, shadow);
        shadowSum += shadow;

        for(int si = 0; si < didimoNumSpotLights; si++)
        {
          Light light = evalSpotLight(si, tS, i.wP);
					evalBrdf(light, tN, ctN, tV, irisRough, diffuse, irisSpec, corneaSpec);
        }

        for(int pi = 0; pi < didimoNumPointLights; pi++)
        {
          Light light = evalPointLight(pi, tS, i.wP);
					evalBrdf(light, tN, ctN, tV, irisRough, diffuse, irisSpec, corneaSpec);
        }

        diffuse *= directDiffInt;
        irisSpec *= directSpecInt;
				corneaSpec *= directSpecInt * corneaSpecInt * 0.01;

				/// indirect lighting
				diffuse += evalIndDiffuse(wN, i.wP);
				corneaSpec += evalIndSpec(cwN, i.wV, 0);

				half3 colorMap = tex2D(colorSampler, uv).xyz;
				diffuse *= colorMap;

				irisSpec *= irisSpecInt * colorMap * irisMask * refrHeight * 5;

				corneaSpec *= evalFresnel(fresnel, 5, ctN, tV);

				half sideDampen = min(1, pow(abs(dot(ctN, tV)), 10) * 10);
				corneaSpec *= sideDampen;

				fixed shadows = lerp(1.0, 1.0 - shadowSum, selfShadowDiff);

				fixed3 finalColor = diffuse + irisSpec + corneaSpec;
				finalColor *= shadows *  0.6; /// for some reason the shadows in Unity aren't recieved as well as in Maya, so this is compensation

				OUT.main = fixed4(finalColor, 1);
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
      
      float4x4 didimoLightMat0;

      void frag(v2f i)
      {
      }
      ENDCG
    }
	}
	Fallback "Standard"
}
