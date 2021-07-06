
/// by Tom Sirdevan for Didimo

Shader "Didimo/debug"
{
  Properties
  {
    switchState ("Switch", Int) = 0
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
      #pragma vertex didimoVert
      #pragma fragment frag
      
      int switchState;

      float4x4 didimoLightMat0;
      sampler2D didimoShadowTex0;

      fixed4 frag(v2f i) : SV_Target
      {
        if(switchState == 0) return fixed4(i.uv, 0, 1);
        if(switchState == 1) return fixed4(i.wT, 1); 
        if(switchState == 2) return fixed4(i.wB, 1); 
        if(switchState == 3) return fixed4(i.wN, 1); 
        if(switchState == 4) return fixed4(i.wV, 1);
        if(switchState == 5) return fixed4(i.wP, 1);
        if(switchState == 6) /// fresnel
        {
          half f = pow(1 - dot(i.wV, i.wN), 3);
          return fixed4(f, f, f, 1);
        }
        if(switchState == 7) /// shadows
        {
          const float shadowBias = 0.001;

          float4 lP = mul(didimoLightMat0, float4(i.wP, 1));

          #if UNITY_REVERSED_Z
            lP.z = min(lP.z, lP.w * UNITY_NEAR_CLIP_VALUE);
          #else
            lP.z = max(lP.z, lP.w * UNITY_NEAR_CLIP_VALUE);
          #endif

          float3 lUv = lP.xyz / lP.w;

          if(lUv.x < 0 || lUv.x > 1 || lUv.y < 0 || lUv.y > 1 || lUv.z < 0 || lUv.z > 1) return fixed4(0, 0, 0, 1);
          
          float2 uv = lUv.xy;

          float d = lUv.z;

          float md = tex2D(didimoShadowTex0, uv).r;

          half s = md > (d + shadowBias) ? 0 : 1;
          return fixed4(s, s, s, 1);
        }
        // if(switchState == 8) return fixed4(i.oT, 1);
        // if(switchState == 9) return fixed4(i.oB, 1);
        // if(switchState == 10) return fixed4(i.oN, 1);

        return fixed4(1, 0, 1, 1);
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
        // clip(1);
      }
      ENDCG
    }
  }
  Fallback "Standard"
}
