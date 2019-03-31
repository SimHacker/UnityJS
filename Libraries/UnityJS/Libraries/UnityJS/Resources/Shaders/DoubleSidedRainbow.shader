Shader "DoubleSided" {
 
    Properties {
        _Color ("Diffuse Color", Color) = (1, 1, 1, 1)
        _MainTex ("Diffuse map (RGB)", 2D) = "white" {}
    }
 
    SubShader {    
 
        // Ambient pass
        Pass {
 
            Name "BASE"
            Tags {"LightMode" = "Always"}
            Color [_PPLAmbient]

            SetTexture [_BumpMap] {
                constantColor (0.5, 0.5, 0.5)
                combine constant lerp (texture) previous
            }
 
            SetTexture [_MainTex] {
                constantColor [_Color]
                Combine texture * previous DOUBLE, texture * constant
            }
 
        }
 
        // Vertex lights
        Pass {

            Name "BASE"
            Tags {"LightMode" = "Vertex"}

            Material {
                Diffuse [_Color]
                Emission [_PPLAmbient]
                Shininess [_Shininess]
                Specular [_SpecColor]
            }

            SeparateSpecular On
            Lighting On
            Cull Off

            SetTexture [_BumpMap] {
                constantColor (0.5, 0.5, 0.5)
                combine constant lerp (texture) previous
            }

            SetTexture [_MainTex] {
                Combine texture * previous DOUBLE, texture*primary
            }

        }

    }

    FallBack "Diffuse", 1

}
