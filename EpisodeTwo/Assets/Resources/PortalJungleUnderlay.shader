Shader "DarkNebula/PortalJungleUnderlay" {
 Properties { _MainTex ("Cropped generated scenery", 2D) = "white" {} }
 SubShader {
  Tags { "Queue"="Transparent-20" "RenderType"="Opaque" }
  Pass {
   // The binary mask permits a small forest-edge replacement after native
   // scenery while leaving the original bridge, water and upper forest clear.
   Cull Off ZWrite Off ZTest Always Blend Off
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;
   struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
   struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
   v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
   fixed4 frag(v2f i):SV_Target {
    fixed4 colour = tex2D(_MainTex,i.uv);
    clip(colour.a - 0.5);
    return fixed4(colour.rgb,1);
   }
   ENDCG
  }
 }
}
