Shader "Custom/Terrain"
{
    Properties
    {
        _Color1 ("Color", Color) = (1,1,1,1)
        _Color2 ("Color", Color) = (1,1,1,1)
        _Color3 ("Color", Color) = (1,1,1,1)
        _Color4 ("Color", Color) = (1,1,1,1)
        _threshold1 ("threshold1", Range(-20,10)) = 0.0
        _threshold2 ("threshold2", Range(-10,10)) = 0.0
        _threshold3 ("threshold3", Range(-10,10)) = 0.0
        _threshold4 ("threshold4", Range(-10,20)) = 0.0

        _MainTex1 ("Albedo (RGB)", 2D) = "white" {}
        _MainTex2 ("Albedo (RGB)", 2D) = "white" {}
        _MainTex3 ("Albedo (RGB)", 2D) = "white" {}
        _MainTex4 ("Albedo (RGB)", 2D) = "white" {}

        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex1;
            float2 uv_MainTex2;
            float2 uv_MainTex3;
            float2 uv_MainTex4;
            float3 worldPos;
            float3 worldNormal;
        };

        half _Glossiness;
        half _Metallic;

        fixed3 _Color1;
        fixed3 _Color2;
        fixed3 _Color3;
        fixed3 _Color4;
        sampler2D _MainTex1;
        sampler2D _MainTex2;
        sampler2D _MainTex3;
        sampler2D _MainTex4;
        fixed _threshold1;
        fixed _threshold2;
        fixed _threshold3;
        fixed _threshold4;

      
        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // TODO: color our generated mesh accordingly... we may need another generator script...
            if(IN.worldPos.y < _threshold2){
                // fixed4 c = tex2D (_MainTex1, IN.uv_MainTex1);
                //o.Albedo = c.rgb;
                o.Albedo = _Color1;
            }
            
            if(IN.worldPos.y > _threshold2 && IN.worldPos.y < _threshold3){
                //fixed4 c = tex2D (_MainTex2, IN.uv_MainTex2);
                //o.Albedo = c.rgb;
                o.Albedo = _Color2;
            }
            
            if(IN.worldPos.y > _threshold3 && IN.worldPos.y < _threshold4){
                //fixed4 c = tex2D (_MainTex3, IN.uv_MainTex3);
                //o.Albedo = c.rgb;                
                o.Albedo = _Color3;
            }
            
            if(IN.worldPos.y > _threshold4){
                //fixed4 c = tex2D (_MainTex4, IN.uv_MainTex4);
                //o.Albedo = c.rgb;  
                o.Albedo = _Color4;          
            }
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
