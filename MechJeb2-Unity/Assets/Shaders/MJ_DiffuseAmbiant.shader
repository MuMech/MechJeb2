Shader "MechJeb/DiffuseAmbient"
{
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_MainTex ("Base (RGB)", 2D) = "white" {}
	_SelfAmbiant("_SelfAmbiant", Range(0,1) ) = 0.5
}
SubShader {
	Tags { "RenderType"="Opaque" }
	LOD 200

CGPROGRAM
#pragma surface surf Lambert

sampler2D _MainTex;
fixed4 _Color;
float _SelfAmbiant;

struct Input {
	float2 uv_MainTex; 
};

void surf (Input IN, inout SurfaceOutput o) {
	fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
	o.Albedo = c.rgb;
	o.Alpha = c.a;
	o.Emission = c.rgb * _SelfAmbiant;
}
ENDCG
}

Fallback "VertexLit"
}
