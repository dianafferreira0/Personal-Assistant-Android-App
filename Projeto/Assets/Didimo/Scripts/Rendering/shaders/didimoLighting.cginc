
// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
#pragma exclude_renderers d3d11 gles
#define UNITY_SAMPLE_FULL_SH_PER_PIXEL 1

#include "UnityCG.cginc"
#include "UnityStandardUtils.cginc"

/// exposed uniforms
half directDiffInt;
half indirectDiffInt;

half directSpecInt;
half indirectSpecInt;

half selfShadowDiff;
half selfShadowSpec;
half shadowSpread;

float shadowBias;

/// internal light uniforms
#define MAX_DIR_LIGHTS 4
half4 didimoDirLightColors[MAX_DIR_LIGHTS];
half4 didimoDirLightDirVecs[MAX_DIR_LIGHTS];
int didimoNumDirLights;

#define MAX_SPOT_LIGHTS 4
half4 didimoSpotLightColors[MAX_SPOT_LIGHTS];
half4 didimoSpotLightDirVecs[MAX_SPOT_LIGHTS];
float4 didimoSpotLightPosVecs[MAX_SPOT_LIGHTS];
float4 didimoSpotLightAttens[MAX_SPOT_LIGHTS];
int didimoNumSpotLights;

#define MAX_POINT_LIGHTS 4
half4 didimoPointLightColors[MAX_POINT_LIGHTS];
float4 didimoPointLightPosVecs[MAX_POINT_LIGHTS];
float didimoPointLightAttens[MAX_POINT_LIGHTS];
int didimoNumPointLights;

float4x4 didimoLightSpace0;
sampler2D didimoShadowTex0;
// #define MAX_SHADOW_LIGHTS 2
// sampler2D didimoShadowTexs[MAX_SHADOW_MAPS];
// float4x4 didimoLightSpaces[MAX_SHADOW_MAPS];
// int didimoNumShadowLights;

#define SHADOW_KERNAL_SIZE 4 // 4 8 16
uniform half4 didimoShadowKernal[SHADOW_KERNAL_SIZE] < string UIWidget = "None"; >;

bool didimoCubeMaps;
samplerCUBE didimoIrradiance; 
samplerCUBE didimoRadiance; 

float remapTo01(float v, float fMin, float fMax)
{
  return clamp((v - fMin) / (fMax - fMin), 0, 1);
}

struct Light
{
  half3 tD; /// tangent space direction vector
  half3 color;
  // fixed shadow;
  // fixed3 transmission;
};

half evalDistanceFade(float3 dirVec, float attenValue)
{
  float dirVecDot = dot(dirVec, dirVec);
  float rangeFade = dirVecDot * attenValue;
  rangeFade = saturate(1.0 - rangeFade * rangeFade);
  rangeFade *= rangeFade;
  return rangeFade / max(dirVecDot, 0.00001);
}

half evalSpotFade(float3 lDirVec, float3 nDirVec, float attenValue1, float attenValue2)
{
  float spotFade = dot(lDirVec, nDirVec) * attenValue1 + attenValue2;
  spotFade = saturate(spotFade);
  return spotFade * spotFade;
}


void evalShadowAndTrans(float3 wP, float3 wN, float nBias, float4x4 lightSpace, sampler2D didimoShadowTex, out fixed shadow, out fixed transmission)
{
  shadow = 0;
  transmission = 0;
  
  float4 lP = mul(lightSpace, float4(wP - wN * nBias, 1));

  #if UNITY_REVERSED_Z
    // float4 lP = mul(lightSpace, float4(wP - wN * nBias, 1));
    lP.z = min(lP.z, lP.w * UNITY_NEAR_CLIP_VALUE);
  #else
    // float4 lP = mul(lightSpace, float4(wP + wN * nBias, 1));
    lP.z = max(lP.z, lP.w * UNITY_NEAR_CLIP_VALUE);
  #endif

  float3 lUv = lP.xyz / lP.w;

  if(lUv.x < 0 || lUv.x > 1 || lUv.y < 0 || lUv.y > 1 || lUv.z < 0 || lUv.z > 1) return;

#if UNITY_REVERSED_Z
  float lDepth = lUv.z + (shadowBias / lP.w);
#else
  float lDepth = lUv.z - (shadowBias / lP.w);
#endif
  
  /// transmission
  float lFarPlane = lightSpace[2][3] / (lightSpace[2][2] + 1.0);
  float lD = tex2D(didimoShadowTex, lUv.xy).x;
  float d = max(lD - lUv.z, 0);
  transmission = remapTo01(d, lFarPlane * 0.1, 0);

  /// shadows
  float _shadowSpread = lerp(0.0, 0.002, shadowSpread);
  float shadowSum = 0;

  const uint quarterOfSamples = uint(SHADOW_KERNAL_SIZE / 2);

  [unroll(SHADOW_KERNAL_SIZE)]for(uint i = 0; i < SHADOW_KERNAL_SIZE; i++)
  {
    half4 random = didimoShadowKernal[i];

    float sampleDepth = tex2D(didimoShadowTex, lUv.xy + random.xy * _shadowSpread).x;
  #if UNITY_REVERSED_Z    
    shadowSum += sampleDepth > lDepth ? 1 : 0;
  #else 
    shadowSum += lDepth > sampleDepth ? 1 : 0;
  #endif

    sampleDepth = tex2D(didimoShadowTex, lUv.xy + random.zw * _shadowSpread).x;
  #if UNITY_REVERSED_Z    
    shadowSum += sampleDepth > lDepth ? 1 : 0;
  #else 
    shadowSum += lDepth > sampleDepth ? 1 : 0;
  #endif    

    if(i > quarterOfSamples && shadowSum <= 0) return; /// early return
  }
  shadow = shadowSum / (SHADOW_KERNAL_SIZE * 2);
}


void evalShadow(float3 wP, float3 wN, float nBias, float4x4 lightSpace, sampler2D didimoShadowTex, out half shadow)
{
  shadow = 0;
  
  float4 lP = mul(lightSpace, float4(wP - wN * nBias, 1));

  #if UNITY_REVERSED_Z
    // float4 lP = mul(lightSpace, float4(wP - wN * nBias, 1));
    lP.z = min(lP.z, lP.w * UNITY_NEAR_CLIP_VALUE);
  #else
    // float4 lP = mul(lightSpace, float4(wP + wN * nBias, 1));
    lP.z = max(lP.z, lP.w * UNITY_NEAR_CLIP_VALUE);
  #endif

  float3 lUv = lP.xyz / lP.w;

  if(lUv.x < 0 || lUv.x > 1 || lUv.y < 0 || lUv.y > 1 || lUv.z < 0 || lUv.z > 1) return;

#if UNITY_REVERSED_Z
  float lDepth = lUv.z + (shadowBias / lP.w);
#else
  float lDepth = lUv.z - (shadowBias / lP.w);
#endif
  
  /// shadows
  float _shadowSpread = lerp(0.0, 0.002, shadowSpread);
  float shadowSum = 0;

  const uint quarterOfSamples = uint(SHADOW_KERNAL_SIZE / 2);

  [unroll(SHADOW_KERNAL_SIZE)]for(uint i = 0; i < SHADOW_KERNAL_SIZE; i++)
  {
    half4 random = didimoShadowKernal[i];

    float sampleDepth = tex2D(didimoShadowTex, lUv.xy + random.xy * _shadowSpread).x;
  #if UNITY_REVERSED_Z    
    shadowSum += sampleDepth > lDepth ? 1 : 0;
  #else 
    shadowSum += lDepth > sampleDepth ? 1 : 0;
  #endif

    sampleDepth = tex2D(didimoShadowTex, lUv.xy + random.zw * _shadowSpread).x;
  #if UNITY_REVERSED_Z    
    shadowSum += sampleDepth > lDepth ? 1 : 0;
  #else 
    shadowSum += lDepth > sampleDepth ? 1 : 0;
  #endif    

    if(i > quarterOfSamples && shadowSum <= 0) return; /// early return
  }
  shadow = shadowSum / (SHADOW_KERNAL_SIZE * 2);
}

void evalShadowOptimized(float3 wP, half3 wN, half nBias, float4x4 lightSpace, sampler2D didimoShadowTex, out fixed shadow)
{
  shadow = 0;

  #if UNITY_REVERSED_Z
    float4 lP = mul(lightSpace, float4(wP - wN * nBias, 1));
    lP.z = min(lP.z, lP.w * UNITY_NEAR_CLIP_VALUE);
  #else
    float4 lP = mul(lightSpace, float4(wP + wN * nBias, 1));
    lP.z = max(lP.z, lP.w * UNITY_NEAR_CLIP_VALUE);
  #endif

  float3 lUv = lP.xyz / lP.w;

  if(lUv.x < 0 || lUv.x > 1 || lUv.y < 0 || lUv.y > 1 || lUv.z < 0 || lUv.z > 1) return;

#if UNITY_REVERSED_Z
  float lDepth = lUv.z + (shadowBias / lP.w);
#else
  float lDepth = lUv.z - (shadowBias / lP.w);
#endif
  
    float sampleDepth = tex2D(didimoShadowTex, lUv.xy).x;
  #if UNITY_REVERSED_Z    
    shadow = sampleDepth > lDepth ? 1 : 0;
  #else 
    shadow = lDepth > sampleDepth ? 1 : 0;
  #endif
}


Light evalDirLight(int i, half3x3 tS)
{
  Light light;

  light.tD = normalize(mul(tS, didimoDirLightDirVecs[i]));

  light.color = didimoDirLightColors[i]; 
  
  return light;
}

Light evalSpotLight(int i, half3x3 tS, float3 wP)
{
  Light light;

  half3 lColor = didimoSpotLightColors[i];
  float4 lPos = didimoSpotLightPosVecs[i];

  float3 dirVec = lPos - wP;
  // half3 nDirVec = normalize(dirVec);

  // light.tD = mul(tS, nDirVec);
  light.tD = normalize(mul(tS, dirVec));
  // light.tD = mul(tS, nDirVec);

  float4 attenuation = didimoSpotLightAttens[i];

  half distFade = evalDistanceFade(dirVec, attenuation.x);

  half3 nDirVec = normalize(dirVec);

  half spotFade = evalSpotFade(didimoSpotLightDirVecs[i], nDirVec, attenuation.z, attenuation.w);
  
  light.color = lColor * distFade * spotFade;
  
  // light.shadow = 0;
  // light.transmission = 0;

  return light;
}

Light evalPointLight(int i, half3x3 tS, float3 wP)
{
  Light light;

  half3 lColor = didimoPointLightColors[i];
  float4 lPos = didimoPointLightPosVecs[i];

  float3 dirVec = lPos - wP;

  light.tD = normalize(mul(tS, dirVec));

  half distFade = evalDistanceFade(dirVec, didimoPointLightAttens[i]);
  light.color = lColor * distFade;

  // light.shadow = 0;
  // light.transmission = 0;

  return light;
}


///
/// indirect
///
half3 evalIndDiffuse(half3 wN, float3 wP)
{
  wN *= half3(-1, 1, 1); /// flip it to match Maya
  
  half3 indirectDiff = 0;
  
  if(didimoCubeMaps)
    indirectDiff = texCUBE(didimoIrradiance, wN).rgb;
  else
    indirectDiff = ShadeSHPerPixel(wN, 1, wP);

    // /// to match Maya (cmft generated cubemap)
    // indirectDiff *= 0.45; 
    // indirectDiff = pow(indirectDiff, 1.4);

  return indirectDiff.rgb * indirectDiffInt; 
}

half3 evalIndSpec(half3 wN, half3 wV, half roughness)
{
  half3 reflDir = normalize(-reflect(wV, wN));
  reflDir *= half3(-1, 1, 1); /// flip it to match Maya
  half dotDown = 1 - max(0, dot(wN, half3(0, -1, 0)));

  half3 indirectSpec = 0;

  if(didimoCubeMaps)
  {
    half _roughness = lerp(0.52, 1, roughness); /// NOTE: because the roughness at lower res doesn't play nice with this lookup
    indirectSpec = texCUBElod(didimoRadiance, half4(reflDir, lerp(0, 9, _roughness))).rgb;
  }
  else
  {
    // /// NOTE: Unity has a different approach to handling radiance for rougher surfaces compared to cmft
    // /// in case you want to attempt to match cmft...
    // // half _roughness = max(0, roughness - 0.2);
    // half _roughness = lerp(0, 0.5, roughness);

    /// or if you don't want to match cmft cubemaps
    half _roughness = roughness;

    half4 radSample = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflDir, lerp(0, 9, _roughness));
    indirectSpec = DecodeHDR(radSample, unity_SpecCube0_HDR);

    // /// to match cmft generated cubemaps
    // indirectSpec *= 0.3;   
    // indirectSpec = pow(indirectSpec, 1.2);
    // indirectSpec *= lerp(1, 2, roughness);
  }

  return indirectSpec * indirectSpecInt * dotDown;
}


/// common brdfs
half evalLambert(half3 tlD, half3 tN)
{
  return max(0, dot(tlD, tN));
}

half evalPhong(half3 tlD, half3 tN, half3 tV, fixed roughness)
{
  half3 reflVec = normalize(reflect(-tlD, tN));
  float dotRV = max(0, dot(reflVec, tV));
  return pow(dotRV, (1 - roughness) * 100);
}

half evalBlinn(half3 tlD, half3 tN, half3 tV, fixed roughness)
{
  half3 halfVec = normalize(tlD + tV);
  float dotNH = max(0, dot(tN, halfVec));
  half specTerm = pow(dotNH, (1 - roughness) * 100);
  return specTerm;
}

half evalAniso(half3 tlD, half3 tN, half3 tV, fixed roughness, fixed angle, fixed minTerm)
{
  fixed sX = angle;
  fixed sY = 1.0 - angle;

  half3 halfVec = normalize(tlD + tV);

  half3 upVec = half3(0, 1, 0);

  half3 B  = normalize(cross(upVec, tN));
  half3 N  = normalize(tN) * sX ;
  half3 T  = cross(N, B) / sY; 

  half dotTH = dot(T, halfVec);
  half dotBH = dot(B, halfVec);
  half dotNH = max(0, dot(N, halfVec));

  half specTerm = dotNH * dotNH / (dotTH * dotTH + dotBH * dotBH);
  
  roughness = max(0.0001, 1 - roughness);

  specTerm = pow(specTerm, roughness);

  specTerm = max((specTerm - minTerm) * dot(tlD, tN), 0);

  return specTerm; 
}


float evalFresnel(float amount, float spread, half3 tN, half3 tV)
{
  // half _frenSpread = lerp(10.0, 1.0, spread);
  // return lerp(1.0, min(1.0, pow(1.0 - abs(dot(tN, tV)), _frenSpread)), amount);
  return lerp(1.0, min(1.0, pow(1.0 - abs(dot(tN, tV)), spread)), amount);
}

