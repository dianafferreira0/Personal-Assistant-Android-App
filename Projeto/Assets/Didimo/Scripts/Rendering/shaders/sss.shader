Shader "Didimo/sss"
{
  SubShader
  {
    // No culling or depth
    Cull Off
    ZWrite Off 
    ZTest Always

    Pass
    {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag

      #include "UnityCG.cginc"

      struct appdata
      {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
      };

      struct v2f
      {
        float4 vertex : SV_POSITION;
        float2 uv : TEXCOORD0;
      };

      v2f vert (appdata v)
      {
        v2f o;
        o.vertex = float4(v.vertex.xy, 0.0, 1.0);
        o.uv = v.vertex.xy * 0.5 + 0.5;

        if(_ProjectionParams.x < 0.0) o.uv.y = 1.0 - o.uv.y;

        return o;
      }

      sampler2D didimoSkinTex;
      sampler2D didimoDepthTex;

      half didimoSssRadius;
      half didimoSssDepthBias;
      half didimoSssDesat;
      half didimoSssDesatSpread;
      #define SSS_SAMPLES 11 // 11 25 
      float4 didimoSssKernal[SSS_SAMPLES];
      int didimoSssHorizontal = 1;
      int didimoLinear = 1;
      
      half3 sssGetColor(half4 defaultColor, sampler2D colorSamp, half2 uv, float depth)
      {
        const half3 LuminanceVector = {0.299f, 0.587f, 0.114f};
        half origLum = dot(defaultColor.rgb, LuminanceVector) * defaultColor.a;
        if(origLum <= 0) return defaultColor.rgb;

        float4 sssSample = tex2D(colorSamp, uv);
        return lerp(sssSample.rgb, defaultColor, clamp(abs(depth - 1.1) * didimoSssDepthBias, 0, 1));

        // return sssSample.a > 0 ? lerp(sssSample.rgb, defaultColor, clamp(abs(depth - 1.1) * didimoSssDepthBias, 0, 1)) : defaultColor;// * max(0, sssSample.a - 1);
      }

      fixed4 frag (v2f i) : SV_Target
      {
        half4 skinDiff = tex2D(didimoSkinTex, i.uv); 
        if(skinDiff.a <= 0) return skinDiff;

        const half3 LuminanceVector = {0.299f, 0.587f, 0.114f};
        float depth = tex2D(didimoDepthTex, i.uv).r;

        half2 suv = i.uv;

        half origLum = dot(skinDiff.rgb, LuminanceVector);

        /// TODO: unormalized depth would be useful here
      #if UNITY_REVERSED_Z
        const float scale = 64; 
      #else
        const float scale = 64;
      #endif 

        // half4 origSkinDiff = skinDiff;

        half2 offset = half2(0, 0);
        if(didimoSssHorizontal)
          offset.x = (1.0 / _ScreenParams.x) * didimoSssRadius * scale;
        else
          offset.y = (1.0 / _ScreenParams.y) * didimoSssRadius * scale;

        float depthSample = 0;

        skinDiff.rgb *= didimoSssKernal[0].rgb;
        [unroll(SSS_SAMPLES)] for(int i = 1; i < SSS_SAMPLES; i++)
        {
          half2 uvOffset = offset * didimoSssKernal[i].a;

          half3 tmpColor = sssGetColor(skinDiff, didimoSkinTex, suv + uvOffset, depth) + sssGetColor(skinDiff, didimoSkinTex, suv - uvOffset, depth);

          skinDiff.rgb += tmpColor * didimoSssKernal[i].rgb;
        }

        half lum = dot(skinDiff.rgb, LuminanceVector);
        half _desatInt = min(1.0, pow(lum, lerp(5, 0, didimoSssDesatSpread)));
        skinDiff.rgb = lerp(skinDiff.rgb, half3(lum, lum, lum), _desatInt * didimoSssDesat);
        skinDiff.rgb *= max(0, origLum - lum) + 0.7; 
        
        return skinDiff;
      }
      ENDCG
    }
  }
}
