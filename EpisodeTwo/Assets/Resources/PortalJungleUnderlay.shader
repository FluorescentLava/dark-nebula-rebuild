Shader "DarkNebula/PortalJungleUnderlay" {
 Properties { _MainTex ("Generated original", 2D) = "white" {} }
 SubShader {
  Tags { "Queue"="Transparent-20" "RenderType"="Opaque" }
  Pass {
   // The native bridge shares this plane and draws at 2990 during the shot.
   // Paint its backing first, without depth writes or a coplanar depth test.
   Cull Off ZWrite Off ZTest Always Blend Off
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;
   struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
   struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
   v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
   fixed4 frag(v2f i):SV_Target { return fixed4(tex2D(_MainTex,i.uv).rgb,1); }
   ENDCG
  }
 }
}
