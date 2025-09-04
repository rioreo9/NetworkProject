Shader "Unlit/DissolveShader"
{
    Properties
    {
        [Header(Main)]
        _MainTex ("Texture", 2D) = "white" {}
        [HDR]
        _MainColor ("Color", Color) = (1,1,1,1)

        [Header(Dissolve)]
        _DissolveTex ("DissolveTexture", 2D) = "white" {}
        _EdgeWidth ("EdgeWidth", Range(0.0,1.0)) = 0.5
        [HDR]
        _EdgeColor ("EdgeColor", Color) = (0,0,0,0)
        _Threshold ("Threshold", Range(0.0,3.0)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            //#pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                //UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _DissolveTex;
            float4 _DissolveTex_ST;
            fixed4 _MainColor;
            fixed _EdgeWidth;
            fixed4 _EdgeColor;
            fixed _Threshold;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 edgeCol = fixed4(1,1,1,1);
                fixed4 dissolve = tex2D(_DissolveTex, i.uv);
                fixed alpha = dissolve.r * 0.2 + dissolve.g * 0.7 + dissolve.b * 0.1;

                //edgeCol = (alpha < _Threshold) ? discard : _EdgeColor;
                clip(alpha - _Threshold/4);

                fixed4 col = tex2D(_MainTex, i.uv) * _MainColor * edgeCol;
                
                return col;
            }
            ENDCG
        }
    }
}
