Shader "Unlit/BulletsShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #include "utils.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 color: COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _PositionTex;
            sampler2D _ColorTex;
            int _InstanceNum;
            float4 _TextureSize;

            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                float4 position = v.vertex;
                // #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                float id = 0.0;
                // float4 position = float4(0.0, 0.0, 0.0, 0.0);
                // int instanceID = 0;
                // instanceID = unity_InstanceID;
                id = fmod(instanceID, _InstanceNum);
                float uvx = (float)(fmod(id,_TextureSize.x));
                uvx = uvx / (float)(_TextureSize.x-1.0);
                float uvy = (float)(id) / (float)(_TextureSize.x);
                uvy = uvy / (float)(_TextureSize.y-1.0);
                
                float4 uv = float4(uvx, uvy, 0.0, 0.0);

                position = tex2Dlod(_PositionTex, float4(uv));
                // position = float4(id * 0.5, 0.0, 0.0, 1.0);
                position = mul(TranslateMatrix(position.xyz), v.vertex);
                // #endif
                // position.x += 1;
                o.vertex = UnityObjectToClipPos(position);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);

                float4 color = tex2Dlod(_ColorTex, float4(uv));
                o.color = color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                col.a *= i.color.a;
                // col.r = 1.0;
                // col.g = 0.0;
                // col.b = 0.0;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
