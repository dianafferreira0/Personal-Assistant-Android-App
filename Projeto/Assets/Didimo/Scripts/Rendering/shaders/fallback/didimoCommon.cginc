      

/// uniforms
int didimoLinear = 1; /// if ProjectSettings->ColorSpace->[Gamma / Linear]
float zBias = 0;

sampler2D zBiasSampler;

/// tangent (uv) space
half3 getTsNormal(sampler2D normalMap, half2 uv, half height, int flipX, int flipY)
{
  half3 tN = tex2D(normalMap, uv).xyz;
  if(flipX > 0) tN.x = 1 - tN.x;
  if(flipY > 0) tN.y = 1 - tN.y;
  // tN.z = 1 - tN.z;
  tN = (tN * 2.0 - 1.0);
  tN *= height;
  tN.z += 1.0 - height;
  tN = normalize(tN);
  return tN;
}

struct appdata
{
  float4 oP : POSITION;  /// object space position
  float2 uv : TEXCOORD0; /// texture space uv
  float4 oT : TANGENT;   /// object space tangent
  float3 oN : NORMAL;    /// object space normal
};

struct v2f
{
  float4 cP : SV_POSITION; /// clip space vertex
  float2 uv : TEXCOORD0;   /// texture space uv
  float3 wT : TANGENT;     /// world space tangent
  float3 wB : TEXCOORD1;   /// world space binormal
  float3 wN : NORMAL;      /// world space normal
  float3 wV : TEXCOORD2;   /// world space view
  float3 wP : TEXCOORD3;   /// world space position

  /// debug
  float3 oT : TEXCOORD4;   /// object space tangent
  float3 oB : TEXCOORD5;   /// object space binormal
  float3 oN : TEXCOORD6;   /// object space normal
};

v2f didimoVert(appdata i)
{
  v2f o;
  
  o.uv = i.uv;

  // /// debug: flip handedness for comparing with maya viewport shaders
  // float3 oT = normalize(float3(-i.oT.x, i.oT.y, i.oT.z)); 
  // float3 oB = cross(i.oN, i.oT.xyz) * i.oT.w;
  // oB = normalize(float3(-oB.x, oB.y, oB.z));
  // float3 oN = normalize(float3(-i.oN.x, i.oN.y, i.oN.z));

  float3 oT = i.oT;
  float3 oB = cross(i.oN, i.oT.xyz) * i.oT.w;
  float3 oN = i.oN;

  o.oT = oT;
  o.oB = oB;
  o.oN = oN;

  float4x4 wIt = transpose(unity_WorldToObject); 
  o.wT = mul(wIt, oT);
  o.wB = mul(wIt, oB);
  o.wN = mul(wIt, oN);

  o.wP = mul(unity_ObjectToWorld, i.oP);
  o.wV = normalize(_WorldSpaceCameraPos - o.wP);

  float zBiasMap = tex2Dlod(zBiasSampler, float4(i.uv, 0, 0)).x * 2 - 1;

  o.cP = mul(UNITY_MATRIX_VP, float4(o.wP + o.wV * lerp(0, 0.01, zBias + zBiasMap), 1));

  // o.cP = mul(UNITY_MATRIX_VP, float4(o.wP, 1));

  return o;
}

struct sv2f
{
  float4 cP : SV_POSITION; /// clip space vertex
  float2 uv : TEXCOORD0;   /// texture space uv
};

sv2f didimoShaderCasterVert(appdata i)
{
  sv2f o;

  float4 wPos = mul(unity_ObjectToWorld, i.oP);
  o.cP = mul(unity_MatrixVP, wPos);

  // #if UNITY_REVERSED_Z
  //   o.cP.z = min(o.cP.z, o.cP.w * UNITY_NEAR_CLIP_VALUE);
  // #else
  //   o.cP.z = max(o.cP.z, o.cP.w * UNITY_NEAR_CLIP_VALUE);
  // #endif

  o.uv = i.uv;
  return o;
}
