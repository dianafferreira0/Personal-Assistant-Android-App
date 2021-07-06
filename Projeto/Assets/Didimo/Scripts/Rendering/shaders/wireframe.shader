
/// by glowtree (Tom Sirdevan) for mydidimo

Shader "Didimo/wireframe"
{
    Properties
    {
      baseColor ("Color", Color) = (0.5,0.5,0.5)
      wireframeColor ("Wireframe Color", Color) = (0,1,0.7,1)
      wireframeThickness ("Wireframe Thickness", Range(0,1)) = 0.2
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
          "LightMode"="DidimoDefault" // ForwardBase Always
        }

        CGPROGRAM

        #pragma vertex vert
        #pragma geometry geo
        #pragma fragment frag
        #include "UnityCG.cginc"

        struct appdata
        {
          float4 oP : POSITION;  /// object space vertex
          float3 oN : NORMAL;
        };

        struct v2g
        {
          float4 cP : SV_POSITION;
          float3 wN : NORMAL;
          float3 wP : TEXCOORD1; /// world space position
          float3 wV : TEXCOORD2; /// world space view
        };

        struct g2f
        {
          float4 cP : SV_POSITION; /// clip space vertex
          float3 wN : NORMAL;
          float3 bC : TEXCOORD1;   /// barycentric coordinates
          float3 wV : TEXCOORD2;  
        };

        fixed3 baseColor;
        fixed3 wireframeColor;
        float wireframeThickness;

        v2g vert(appdata i)
        {
          v2g o;
          o.cP = UnityObjectToClipPos(i.oP);
          o.wP = mul(unity_ObjectToWorld, i.oP);

          o.wV = normalize(_WorldSpaceCameraPos - o.wP);

          float4x4 wIt = transpose(unity_WorldToObject);
          o.wN = mul(wIt, i.oN);
          
          return o;
        }

        [maxvertexcount(3)]
        void geo (triangle v2g i[3], inout TriangleStream<g2f> stream) 
        {
          static float3 baryC[3] = {{1, 0, 0}, {0, 0, 1}, {0, 1, 0}};

          float3 param = float3(0, 0, 0);

          float edgeA = length(i[0].wP - i[1].wP);
          float edgeB = length(i[1].wP - i[2].wP);
          float edgeC = length(i[2].wP - i[0].wP);
          if(edgeA > edgeB && edgeA > edgeC)
            param.y = 1;
          else if (edgeB > edgeC && edgeB > edgeA)
            param.x = 1;
          else
            param.z = 1;

          for(int j = 0; j < 3; j++)
          {
            v2g iv = i[j];
            g2f o;
            o.cP = iv.cP;
            o.bC = baryC[j] + param;// clamp(baryC[j] + param, 0, 1);
            o.wN = i[j].wN;
            o.wV = i[j].wV;
            stream.Append(o);
          }
        }

        fixed4 frag(g2f i) : SV_Target
        {
          float3 baryc = float3(i.bC.x, i.bC.y, i.bC.z);

          float3 delta = fwidth(baryc);
          float3 thickness = delta * lerp(0, 10, wireframeThickness) * (1 - i.cP.z);
          baryc = smoothstep(0, thickness, baryc);
          // baryc = lerp(0, baryc, thickness);

          float minBaryc = min(baryc.x, min(baryc.y, baryc.z));

          fixed3 diffColor = baseColor * max(0, dot(i.wN, i.wV));

          return fixed4(lerp(wireframeColor, diffColor, minBaryc), 1);
          // return fixed4(smoothstep(wireframeColor, baseColor, minBaryc), 1);
        }
        ENDCG
      }
    }
  Fallback "Standard"
}
