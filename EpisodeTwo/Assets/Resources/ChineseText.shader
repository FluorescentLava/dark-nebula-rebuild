Shader "Dark Nebula/Chinese Text"
{
 Properties { _MainTex("Glyph atlas",2D)="white"{} _TintRamp("Recovered font color",2D)="white"{} _StyleRow("Font style",Float)=0.5 _OriginalGradient("Original gradient",2D)="white"{} _UseGradient("Use original gradient",Float)=0 _GradientIndex("Gradient index",Float)=0 _GradientCount("Gradient count",Float)=1 _MessageStyle("HUD gradient and shadow",Float)=0 _UseLocalHeight("Use glyph height",Float)=0 _GlyphYBounds("Glyph bounds",Vector)=(0,1,0,0) }
 SubShader
 {
  Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
  Pass
  {
   Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
   CGPROGRAM
   #pragma vertex vertShadow
   #pragma fragment fragShadow
   #include "UnityCG.cginc"
   sampler2D _MainTex; float _MessageStyle;
   struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   v2f vertShadow(appdata_full v)
   {
    // Match the bitmap font's lower-left shadow in camera space. Applying
    // this before projection also handles flipped render textures correctly.
    float4 view=mul(UNITY_MATRIX_MV,v.vertex);
    float2 pixelSize=2/(_ScreenParams.xy*abs(float2(UNITY_MATRIX_P._m00,UNITY_MATRIX_P._m11)));
    view.xy+=float2(-0.8,-1.2)*(_ScreenParams.x/540)*pixelSize;
    v2f o;o.vertex=mul(UNITY_MATRIX_P,view);
    o.uv=v.texcoord.xy;o.color=v.color;return o;
   }
   fixed4 fragShadow(v2f i):SV_Target
   {
    clip(_MessageStyle-0.5);
    return fixed4(0,0,0,tex2D(_MainTex,i.uv).a*i.color.a*0.9);
   }
   ENDCG
  }
  Pass
  {
   Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex, _TintRamp, _OriginalGradient; float _StyleRow, _UseGradient, _GradientIndex, _GradientCount, _MessageStyle, _UseLocalHeight; float4 _GlyphYBounds;
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float2 ramp:TEXCOORD1;fixed4 color:COLOR;};
   struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float2 ramp:TEXCOORD1;fixed4 color:COLOR;};
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.ramp=v.ramp;if(_UseLocalHeight>0.5)o.ramp.x=(v.vertex.y-_GlyphYBounds.x)/max(0.001,_GlyphYBounds.y-_GlyphYBounds.x);o.color=v.color;return o;}
   fixed4 frag(v2f i):SV_Target
   {
    if(_MessageStyle>0.5)
    {
     // The bitmap ramp's transparent first/last rows are antialiasing around
     // the Latin glyph, not opacity to apply again to a Chinese glyph.
     fixed3 fill=tex2D(_TintRamp,float2(lerp(0.18,0.93,saturate(i.ramp.x)),_StyleRow)).rgb*i.color.rgb;
     float mask=tex2D(_MainTex,i.uv).a;
     return fixed4(fill,mask*i.color.a);
    }
    fixed4 tint=tex2D(_TintRamp,float2(saturate(i.ramp.x),_StyleRow));
    if(_UseGradient>0.5)tint=tex2D(_OriginalGradient,float2((_GradientIndex+i.ramp.y)/_GradientCount,saturate(i.ramp.x)));
    fixed4 color=i.color*tint;
    color.a*=tex2D(_MainTex,i.uv).a;return color;
   }
   ENDCG
  }
 }
}
