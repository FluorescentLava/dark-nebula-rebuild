Shader "tk2d/Blend2TexVertexColor" {
Properties {
 _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
 _GradientTex ("Gradient (RGBA)", 2D) = "white" {}
}
SubShader {
 LOD 100
 Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="True" "RenderType"="Transparent" }
 Pass {
  Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="True" "RenderType"="Transparent" }
  BindChannels {
   Bind "vertex", Vertex
   Bind "color", Color
   Bind "texcoord", TexCoord0
   Bind "texcoord1", TexCoord1
  }
  ZWrite Off
  Cull Off
  Blend SrcAlpha OneMinusSrcAlpha
  SetTexture [_MainTex] { combine texture * primary }
  SetTexture [_GradientTex] { combine texture * previous }
 }
}
}
