Shader "Custom/SpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _OutlineColor ("Outline Color", Color) = (1,1,1,0) 
        _OutlineThickness ("Outline Thickness", Range(0, 20)) = 2 
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineThickness;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;

                float tx = _OutlineThickness * _MainTex_TexelSize.x;
                float ty = _OutlineThickness * _MainTex_TexelSize.y;

                float outlineAlpha = 0;

                for(int i = 0; i < 16; i++)
                {
                    float angle = i * 0.392699; 
                    
                    float2 offset = float2(cos(angle) * tx, sin(angle) * ty);
                    
                    float sampleAlpha = step(0.5, tex2D(_MainTex, IN.texcoord + offset).a);
                    
                    outlineAlpha = max(outlineAlpha, sampleAlpha);
                }

                outlineAlpha *= _OutlineColor.a;

                fixed4 finalColor;
                finalColor.rgb = lerp(_OutlineColor.rgb, c.rgb, c.a);

                finalColor.a = max(c.a, outlineAlpha);

                return finalColor;
            }
            ENDCG
        }
    }
}