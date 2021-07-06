Shader "Didimo/postProcess"
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

      sampler2D didimoDefaultTex;
      sampler2D didimoDepthTex;
      sampler2D didimoSssTex;
      sampler2D didimoSkinTex;
      int didimoTonemapping;
      half didimoExposure = 1;
      int didimoLinear = 1;

      // vec3 Uncharted2Tonemap(vec3 x)
      // {
      //     float A = 0.15;
      //   float B = 0.50;
      //   float C = 0.10;
      //   float D = 0.20;
      //   float E = 0.02;
      //   float F = 0.30;

      //     return ((x*(A*x+C*B)+D*E)/(x*(A*x+B)+D*F))-E/F;
      // }

      half3 toneMap(half3 color)
      {
        half3 r = color;

        if(didimoTonemapping) r = half3(1, 1, 1) - exp(-r.rgb * (didimoExposure + 1));

        return r;
        // return pow(r, 0.45454545454545); // 0.4545... = 1 / 2.2
        // return pow(r, 2.2) * 2;
      }

      fixed4 frag (v2f i) : SV_Target
      {
        // fixed v = 0.325;
        // // v = pow(v, 0.45454545454545);
        // // v = pow(v, 2.2);
        // // v += v;
        // return fixed4(v, v, v, 1);

        float4 sceneColor = tex2D(didimoDefaultTex, i.uv);
        // return sceneColor;
        // return fixed4(toneMap(sceneColor.rgb), 1);

        fixed4 skinSSS = tex2D(didimoSssTex, i.uv);
        // return skinSSS * skinSSS.a;

        // fixed alpha = skinSSS.a;
        // return fixed4(alpha, alpha, alpha, 1);
  
        sceneColor.rgb += skinSSS.rgb * skinSSS.a;

        return fixed4(toneMap(sceneColor.rgb), 1);
      }
      ENDCG
    }
  }
}
