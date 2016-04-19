Shader "Unlit/Texture +300" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }
    Category {
       Lighting Off
       ZWrite On
       Cull Back
       SubShader {
       		Tags {
       			"Queue"="Transparent+300"
       		}
            Pass {
               SetTexture [_MainTex] {
                    Combine texture, texture
                 }
            }
        }
    }
}
