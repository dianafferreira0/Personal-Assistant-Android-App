Shader "Didimo/background"
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

      fixed3 didimoBgColor1;
      fixed3 didimoBgColor2;

      fixed4 frag (v2f i) : SV_Target
      {
        half2  scale = half2(_ScreenParams.x / _ScreenParams.y, 1);

        float gradient = length((i.uv - half2(0.5, 0.5)) * scale);
        half3 bgColor = lerp(didimoBgColor1, didimoBgColor2, gradient);
        return fixed4(bgColor, 1);
      }
      ENDCG
    }
  }
}
