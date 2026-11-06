Shader "Dark Nebula/Forest Portal Cap" {
 Properties {
  _MainTex ("Original forest RGB", 2D) = "white" {}
  _Mask ("Original red-channel mask", 2D) = "white" {}
  _CapRange ("Outer foliage transition", Vector) = (-51008,-50816,0,0)
 }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
  Pass {
   ZTest Always ZWrite On Cull Off Blend SrcAlpha OneMinusSrcAlpha
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex, _Mask;
   float4 _MainTex_ST;
   float4 _CapRange;
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
   struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float worldZ:TEXCOORD1;};
   v2f vert(appdata v) {
    v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.worldZ=mul(unity_ObjectToWorld,v.vertex).z;return o;
   }
   fixed4 frag(v2f i):SV_Target {
    fixed4 c=tex2D(_MainTex,i.uv);
    // The upper foliage rows include an opaque exterior and an abrupt join
    // into the temple artwork. Blend the entire join into the canopy below.
    float exterior=smoothstep(.005,.025,max(c.r,max(c.g,c.b)));
    clip(exterior-.001);
    float blend=1-smoothstep(_CapRange.x,_CapRange.y,i.worldZ);
    c.a=tex2D(_Mask,i.uv).r*exterior*blend;
    return c;
   }
   ENDCG
  }
 }
}
