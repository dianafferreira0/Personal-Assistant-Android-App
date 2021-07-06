
Shader "Didimo/GreenToTransparent"
{
  Properties{
    _MainTex("Base", 2D) = "white" {}
    _Threshold("Threshold", Range(0, 1)) = 0
    _Threshold2("Threshold", Range(0, 1)) = 0
    _Threshold3("Threshold", Range(0, 1)) = 0
    _Stencil("Stencil ID", Float) = 0
    _StencilOp("Stencil Operation", Float) = 0
    _StencilComp("Stencil Comparison", Float) = 8
    _StencilWriteMask("Stencil Write Mask", Float) = 255
    _StencilReadMask("Stencil Read Mask", Float) = 255 
    _ColorMask("Color Mask", Float) = 15 
  }
  SubShader {

    Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

    Pass {
      Tags { "LightMode"="DidimoTrans"}

      Blend SrcAlpha OneMinusSrcAlpha
      Cull Off
      
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #include "UnityCG.cginc"
      struct v2f {
          float4 pos : SV_POSITION;
          float2 uv1 : TEXCOORD0;
      };
      sampler2D _MainTex;
      float4 _MainTex_ST;
      v2f vert(appdata_base v) {
          v2f o;
          o.pos = UnityObjectToClipPos(v.vertex);
          o.uv1 = TRANSFORM_TEX(v.texcoord, _MainTex);
          return o;
      }
      float _Threshold;
      float _Threshold2;
      float _Threshold3;
      fixed4 frag(v2f i) : COLOR {
          fixed4 col1 = tex2D(_MainTex, i.uv1);
          fixed4 val = ceil(saturate(col1.g - col1.r - _Threshold+ _Threshold2)) * ceil(saturate(col1.g - col1.b - _Threshold + _Threshold3));
          return lerp(col1, fixed4(0., 0., 0., 0.), val);
      }
      ENDCG
    }
  }
}
